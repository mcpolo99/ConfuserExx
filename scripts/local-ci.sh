#!/usr/bin/env bash
# =============================================================================
# local-ci.sh — Run the full CI pipeline locally (mirrors GitHub Actions)
#
# Replicates: lint.yml, ci.yml (build + package), test.yml (test + coverage)
#
# Usage:
#   ./scripts/local-ci.sh              # run everything
#   ./scripts/local-ci.sh lint         # lint only
#   ./scripts/local-ci.sh build        # restore + build only
#   ./scripts/local-ci.sh test         # build + test only
#   ./scripts/local-ci.sh package      # build + package only
#   ./scripts/local-ci.sh all          # everything (default)
# =============================================================================

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$REPO_ROOT"

CONFIGURATION="Release"
SLN="Confuser2.sln"
RESULTS_DIR="test-results"
COVERAGE_DIR="coverage"

# ---------------------------------------------------------------------------
# Colors
# ---------------------------------------------------------------------------
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
BOLD='\033[1m'
NC='\033[0m'

step()    { echo -e "\n${CYAN}${BOLD}==> $1${NC}"; }
success() { echo -e "${GREEN}✓ $1${NC}"; }
warn()    { echo -e "${YELLOW}⚠ $1${NC}"; }
fail()    { echo -e "${RED}✗ $1${NC}"; }

# ---------------------------------------------------------------------------
# Find MSBuild via vswhere (CI uses microsoft/setup-msbuild@v2)
# ---------------------------------------------------------------------------
find_msbuild() {
  # vswhere -latest finds the newest VS (2025 > 2022 > 2019).
  # CI uses windows-2025 runners with VS 2025 / MSBuild 18.
  local vswhere="/c/Program Files (x86)/Microsoft Visual Studio/Installer/vswhere.exe"
  if [ -f "$vswhere" ]; then
    # Get installation path first, then construct MSBuild path
    local vs_path
    vs_path=$("$vswhere" -latest -requires Microsoft.Component.MSBuild \
      -property installationPath 2>/dev/null | head -1)
    if [ -n "$vs_path" ]; then
      # Convert Windows path to unix-style for bash
      local unix_path
      unix_path=$(cygpath -u "$vs_path" 2>/dev/null || echo "$vs_path")
      MSBUILD="$unix_path/MSBuild/Current/Bin/MSBuild.exe"
      if [ ! -f "$MSBUILD" ]; then
        MSBUILD=""
      fi
    fi
  fi

  if [ -z "${MSBUILD:-}" ]; then
    warn "MSBuild not found — will use 'dotnet build' (C++/CLI project will be skipped)"
    return 1
  fi

  local ver
  ver=$("$MSBUILD" -version 2>/dev/null | tail -1 || echo "unknown")
  echo "  MSBuild: $MSBUILD (v$ver)"
  return 0
}

# ---------------------------------------------------------------------------
# Phase: Lint (mirrors lint.yml)
# ---------------------------------------------------------------------------
do_lint() {
  step "LINT — whitespace, style, analyzers"

  local any_failed=false

  echo "  Restoring for lint..."
  dotnet restore "$SLN" --verbosity quiet 2>/dev/null

  echo "  Checking whitespace..."
  if dotnet format whitespace "$SLN" --verify-no-changes --verbosity minimal 2>&1 | tail -5; then
    success "Whitespace OK"
  else
    warn "Whitespace issues found"
    any_failed=true
  fi

  echo "  Checking style..."
  if dotnet format style "$SLN" --verify-no-changes --severity warn 2>&1 | tail -5; then
    success "Style OK"
  else
    warn "Style issues found"
    any_failed=true
  fi

  echo "  Checking analyzers..."
  if dotnet format analyzers "$SLN" --verify-no-changes --severity warn 2>&1 | tail -5; then
    success "Analyzers OK"
  else
    warn "Analyzer issues found"
    any_failed=true
  fi

  if [ "$any_failed" = true ]; then
    warn "Lint issues found — run 'dotnet format $SLN' to fix"
    return 1
  else
    success "All lint checks passed"
  fi
}

# ---------------------------------------------------------------------------
# Phase: Restore + Build (mirrors ci.yml)
# ---------------------------------------------------------------------------
do_build() {
  step "BUILD — restore + compile ($CONFIGURATION)"

  local has_msbuild=false
  find_msbuild && has_msbuild=true

  # Use 'dotnet build' for all SDK-style projects (handles .NET SDK resolution).
  # Then use MSBuild.exe for the C++/CLI project if available.
  echo "  Restoring..."
  dotnet restore "$SLN" --verbosity minimal

  echo "  Building (dotnet)..."
  dotnet build "$SLN" -c "$CONFIGURATION" --no-restore 2>&1 \
    | grep -v "244_ClrProtection" || true

  # C++/CLI project requires MSBuild.exe (not dotnet build)
  if [ "$has_msbuild" = true ]; then
    local vcxproj="Tests/244_ClrProtection/244_ClrProtection.vcxproj"
    if [ -f "$vcxproj" ]; then
      echo "  Building C++/CLI test project (msbuild)..."
      "$MSBUILD" "$vcxproj" -p:Configuration="$CONFIGURATION" -verbosity:minimal 2>&1 \
        | tail -3 || warn "C++/CLI project build failed (non-critical)"
    fi
  else
    warn "Skipping C++/CLI project (no MSBuild.exe found)"
  fi

  # Verify key outputs exist
  local cli_dll="Confuser.CLI/bin/$CONFIGURATION/net10.0/Confuser.CLI.dll"
  local gui_dll="ConfuserEx/bin/$CONFIGURATION/net10.0-windows/ConfuserEx.dll"
  local core48="Confuser.Core/bin/$CONFIGURATION/net48/Confuser.Core.dll"
  local corenstd="Confuser.Core/bin/$CONFIGURATION/netstandard2.0/Confuser.Core.dll"

  local all_ok=true
  for f in "$cli_dll" "$gui_dll" "$core48" "$corenstd"; do
    if [ -f "$f" ]; then
      success "$(basename "$f") ($(dirname "$f" | sed "s|.*/bin/$CONFIGURATION/||"))"
    else
      fail "Missing: $f"
      all_ok=false
    fi
  done

  if [ "$all_ok" = false ]; then
    fail "Build produced missing outputs"
    return 1
  fi

  success "Build complete"
}

# ---------------------------------------------------------------------------
# Phase: Test (mirrors test.yml)
# ---------------------------------------------------------------------------
do_test() {
  step "TEST — run all test projects with coverage"

  rm -rf "$RESULTS_DIR" 2>/dev/null || true
  mkdir -p "$RESULTS_DIR"

  local total_passed=0
  local total_failed=0
  local total_skipped=0
  local any_failed=false

  # Find all *.Test.csproj (same as CI: Get-ChildItem -Filter '*.Test.csproj' -Recurse)
  while IFS= read -r proj; do
    local name
    name=$(basename "$proj" .csproj)
    echo -e "\n  ${CYAN}Testing $name...${NC}"

    local output
    output=$(dotnet test "$proj" -c "$CONFIGURATION" --no-build --verbosity minimal \
      --collect:"XPlat Code Coverage" \
      --logger "trx;LogFileName=$name.trx" \
      --results-directory "$RESULTS_DIR/$name" 2>&1) || true

    # Parse summary line: "Passed! - Failed: 0, Passed: 3, Skipped: 0, Total: 3"
    local summary
    summary=$(echo "$output" | grep -E "^(Passed!|Failed!)" | tail -1)

    if [ -n "$summary" ]; then
      local p f s
      p=$(echo "$summary" | grep -oP 'Passed:\s+\K\d+' || echo 0)
      f=$(echo "$summary" | grep -oP 'Failed:\s+\K\d+' || echo 0)
      s=$(echo "$summary" | grep -oP 'Skipped:\s+\K\d+' || echo 0)
      total_passed=$((total_passed + p))
      total_failed=$((total_failed + f))
      total_skipped=$((total_skipped + s))

      if echo "$summary" | grep -q "^Failed!"; then
        fail "$name — $summary"
        any_failed=true
      else
        success "$name — Passed: $p, Failed: $f, Skipped: $s"
      fi
    else
      # No tests discovered
      local no_test
      no_test=$(echo "$output" | grep -c "No test is available" || true)
      if [ "$no_test" -gt 0 ]; then
        warn "$name — no tests discovered (missing Microsoft.NET.Test.Sdk?)"
      else
        warn "$name — no test output"
      fi
    fi
  done < <(find Tests -name "*.Test.csproj" -type f | sort)

  echo ""
  echo "  ─────────────────────────────────────"
  echo -e "  ${BOLD}Total: Passed=$total_passed  Failed=$total_failed  Skipped=$total_skipped${NC}"
  echo "  ─────────────────────────────────────"

  # Generate coverage report if reportgenerator is available
  local reports
  reports=$(find "$RESULTS_DIR" -name "coverage.cobertura.xml" 2>/dev/null | tr '\n' ';')
  if [ -n "$reports" ] && command -v reportgenerator &>/dev/null; then
    step "COVERAGE — generating report"
    mkdir -p "$COVERAGE_DIR/report"
    reportgenerator "-reports:$reports" \
      "-targetdir:$COVERAGE_DIR/report" \
      "-reporttypes:TextSummary" 2>/dev/null
    cat "$COVERAGE_DIR/report/Summary.txt" 2>/dev/null || true
  elif [ -n "$reports" ]; then
    warn "Install reportgenerator for coverage reports: dotnet tool install -g dotnet-reportgenerator-globaltool"
  fi

  if [ "$any_failed" = true ]; then
    fail "Some tests failed"
    return 1
  fi

  success "All tests passed"
}

# ---------------------------------------------------------------------------
# Phase: Package (mirrors ci.yml packaging steps)
# ---------------------------------------------------------------------------
do_package() {
  step "PACKAGE — create release archives"

  local cli_dir="Confuser.CLI/bin/$CONFIGURATION/net10.0"
  local gui_dir="ConfuserEx/bin/$CONFIGURATION/net10.0-windows"

  # CLI zip
  if [ -d "$cli_dir" ]; then
    rm -f ConfuserEx-CLI.zip 2>/dev/null || true
    (cd "$cli_dir" && find . -not -name '*.pdb' -not -name '*.xml' -not -path './runtimes/*/native/*' \
      -type f | sort | zip -q "$REPO_ROOT/ConfuserEx-CLI.zip" -@)
    local size
    size=$(du -h ConfuserEx-CLI.zip | cut -f1)
    success "ConfuserEx-CLI.zip ($size)"
  else
    fail "CLI output not found at $cli_dir — run build first"
  fi

  # GUI zip
  if [ -d "$gui_dir" ]; then
    rm -f ConfuserEx-GUI.zip 2>/dev/null || true
    (cd "$gui_dir" && find . -not -name '*.pdb' -not -name '*.xml' \
      -type f | sort | zip -q "$REPO_ROOT/ConfuserEx-GUI.zip" -@)
    size=$(du -h ConfuserEx-GUI.zip | cut -f1)
    success "ConfuserEx-GUI.zip ($size)"
  else
    fail "GUI output not found at $gui_dir — run build first"
  fi

  # Combined zip
  rm -rf combined 2>/dev/null || true
  mkdir -p combined
  cp "$cli_dir"/* combined/ 2>/dev/null || true
  cp "$gui_dir"/* combined/ 2>/dev/null || true
  rm -f combined/*.pdb combined/*.xml 2>/dev/null || true
  if [ "$(ls -A combined 2>/dev/null)" ]; then
    rm -f ConfuserEx.zip 2>/dev/null || true
    (cd combined && find . -type f | sort | zip -q "$REPO_ROOT/ConfuserEx.zip" -@)
    size=$(du -h ConfuserEx.zip | cut -f1)
    success "ConfuserEx.zip ($size)"
  fi
  rm -rf combined

  # NuGet package
  local nupkg
  nupkg=$(find Confuser.MSBuild.Tasks/bin/$CONFIGURATION -name "*.nupkg" 2>/dev/null | head -1)
  if [ -n "$nupkg" ]; then
    success "$(basename "$nupkg")"
  else
    warn "No .nupkg found (MSBuild-only build may be required)"
  fi

  success "Packaging complete"
}

# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------
MODE="${1:-all}"

echo -e "${BOLD}╔══════════════════════════════════════╗${NC}"
echo -e "${BOLD}║   ConfuserEx Local CI Pipeline       ║${NC}"
echo -e "${BOLD}╚══════════════════════════════════════╝${NC}"
echo "  Mode:    $MODE"
echo "  Config:  $CONFIGURATION"
echo "  Root:    $REPO_ROOT"

ERRORS=0

case "$MODE" in
  lint)
    do_lint || ERRORS=$((ERRORS + 1))
    ;;
  build)
    do_build || ERRORS=$((ERRORS + 1))
    ;;
  test)
    do_build || ERRORS=$((ERRORS + 1))
    do_test  || ERRORS=$((ERRORS + 1))
    ;;
  package)
    do_build   || ERRORS=$((ERRORS + 1))
    do_package || ERRORS=$((ERRORS + 1))
    ;;
  all)
    do_lint    || ERRORS=$((ERRORS + 1))
    do_build   || ERRORS=$((ERRORS + 1))
    do_test    || ERRORS=$((ERRORS + 1))
    do_package || ERRORS=$((ERRORS + 1))
    ;;
  *)
    echo "Usage: $0 [lint|build|test|package|all]"
    exit 1
    ;;
esac

echo ""
if [ "$ERRORS" -gt 0 ]; then
  fail "Pipeline finished with $ERRORS failed phase(s)"
  exit 1
else
  success "Pipeline complete — all phases passed"
fi
