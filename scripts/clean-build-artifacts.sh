#!/usr/bin/env bash
# Clean all build artifacts, test results, and temporary files.
# Usage: ./scripts/clean-build-artifacts.sh

set -e

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$REPO_ROOT"

echo "Cleaning build artifacts..."

# Build output — delete bin/ and obj/ from all project directories
for dir in \
  Confuser.CLI Confuser.Core Confuser.DynCipher Confuser.MSBuild.Tasks \
  Confuser.Protections Confuser.Renamer Confuser.Runtime ConfuserEx \
  Tests/*/; do
  rm -rf "$dir/bin" "$dir/obj" 2>/dev/null || true
done

# Release output from C++ project
rm -rf Tests/244_ClrProtection/Release Tests/244_ClrProtection/Debug 2>/dev/null || true
rm -rf Release Debug 2>/dev/null || true

# Obfuscated output from integration tests
find Tests -type d -name "obfuscated*" -exec rm -rf {} + 2>/dev/null || true

# Packages
rm -f ConfuserEx-CLI.zip ConfuserEx-GUI.zip ConfuserEx.zip 2>/dev/null || true
rm -rf combined 2>/dev/null || true

# Test results and coverage
rm -rf coverage test-results TestResults 2>/dev/null || true
find Tests -name "*.trx" -delete 2>/dev/null || true
find Tests -type d -name "TestResults" -exec rm -rf {} + 2>/dev/null || true

# NuGet
rm -rf packages 2>/dev/null || true

echo "Done."
