#!/usr/bin/env bash
#
# uniquify.sh — Level 1 source identity for ConfuserExx (issue #69)
#
# Makes a ConfuserExx *build* unrecognizable as ConfuserEx by rewriting the tool's own
# source-level fingerprints, deterministically from a seed. Run once per environment.
#
# What it rewrites (and keeps in sync):
#   * Runtime namespace  Confuser.Runtime  ->  a seed-derived namespace. This is the #1 tool
#     fingerprint. Kept in sync across BOTH sides of the coupling: the runtime source
#     `namespace Confuser.Runtime` declarations, and every obfuscator string literal
#     "Confuser.Runtime.<Type>" used by GetRuntimeType/Find. The assembly file name
#     (Confuser.Runtime.dll) and the "Confuser.Runtime" runtime-service id are deliberately
#     left untouched — the runtime is loaded by file name, and the type is then resolved by
#     its (now seed-derived) full name, so both still line up.
#   * Watermark default attribute name  ConfusedByAttribute  ->  a seed-derived name.
#
# Deliberately NOT rewritten here (needs a semantic/Roslyn rename, not text substitution):
#   runtime TYPE and METHOD names (Constant, Resolve, Get, Value, ...). These are ordinary
#   English words that also appear in unrelated code, so a text swap would corrupt the tree.
#   The runtime DLL rename and anti-debug process-name checks are also left for a follow-up.
#
# Usage:
#   scripts/uniquify.sh --seed "<phrase>" [--dry-run] [--test]
#   scripts/uniquify.sh --restore
#
#   --seed <phrase>  Deterministic: the same phrase always yields the same identifiers.
#   --dry-run        Show what would change; write nothing.
#   --test           After applying, build + run the test suite to prove the rename is sound.
#   --restore        Revert every file this script last changed (via git), back to HEAD.
#
set -euo pipefail

SEED=""
DRY_RUN=0
DO_TEST=0
RESTORE=0

while [ $# -gt 0 ]; do
	case "$1" in
		--seed) SEED="${2:-}"; shift 2 ;;
		--dry-run) DRY_RUN=1; shift ;;
		--test) DO_TEST=1; shift ;;
		--restore) RESTORE=1; shift ;;
		-h|--help) grep '^#' "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
		*) echo "unknown option: $1" >&2; exit 2 ;;
	esac
done

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
MANIFEST="$ROOT/.git/uniquify-manifest"
RUNTIME_DIR="$ROOT/Confuser.Runtime"
OBF_DIRS=("Confuser.Protections" "Confuser.Core" "Confuser.Renamer" "Confuser.DynCipher")
WATERMARK_FILE="$ROOT/Confuser.Core/WatermarkingProtection.cs"

if ! git -C "$ROOT" rev-parse --is-inside-work-tree >/dev/null 2>&1; then
	echo "error: $ROOT is not a git repository (git is required for --restore)." >&2
	exit 1
fi

# --- restore -------------------------------------------------------------------------------
if [ "$RESTORE" -eq 1 ]; then
	if [ ! -f "$MANIFEST" ]; then
		echo "nothing to restore (no manifest at $MANIFEST). Was uniquify applied in this clone?" >&2
		exit 1
	fi
	count=$(wc -l < "$MANIFEST" | tr -d ' ')
	# shellcheck disable=SC2046
	(cd "$ROOT" && xargs git checkout -- < "$MANIFEST")
	rm -f "$MANIFEST"
	echo "restored $count file(s) to HEAD."
	exit 0
fi

[ -n "$SEED" ] || { echo "error: --seed is required (or use --restore)." >&2; exit 2; }

# --- derive identifiers from the seed ------------------------------------------------------
derive() { # derive <salt> -> letter-prefixed hex identifier
	printf '%s' "${SEED}:$1" | sha256sum | cut -c1-10
}
NS="Rt$(derive namespace)"
WM="Wm$(derive watermark)Attribute"

echo "seed        : $SEED"
echo "namespace   : Confuser.Runtime  ->  $NS"
echo "watermark   : ConfusedByAttribute  ->  $WM"

# --- collect the exact files each rule touches --------------------------------------------
list_cs() { # list *.cs under a dir, excluding build output
	find "$1" -type f -name '*.cs' -not -path '*/obj/*' -not -path '*/bin/*'
}

runtime_hits() { list_cs "$RUNTIME_DIR" | while read -r f; do grep -lE 'namespace[[:space:]]+Confuser\.Runtime\b' "$f" || true; done; }
obf_hits() { for d in "${OBF_DIRS[@]}"; do list_cs "$ROOT/$d"; done | while read -r f; do grep -lE '"Confuser\.Runtime\.[A-Z]' "$f" || true; done; }

RUNTIME_FILES=$(runtime_hits | sort -u)
OBF_FILES=$(obf_hits | sort -u)

# --- dry-run: preview and exit -------------------------------------------------------------
if [ "$DRY_RUN" -eq 1 ]; then
	echo
	echo "[dry-run] runtime namespace declarations ->"
	echo "$RUNTIME_FILES" | sed "s#^$ROOT/#  #"
	echo "[dry-run] obfuscator \"Confuser.Runtime.<Type>\" string literals ->"
	echo "$OBF_FILES" | sed "s#^$ROOT/#  #"
	echo "[dry-run] watermark attribute name -> ${WATERMARK_FILE#$ROOT/} ($(grep -c ConfusedByAttribute "$WATERMARK_FILE" || echo 0) occurrence(s))"
	echo
	echo "[dry-run] no files were modified."
	exit 0
fi

# --- apply ---------------------------------------------------------------------------------
: > "$MANIFEST"

apply_runtime() { # namespace Confuser.Runtime -> namespace $NS
	echo "$RUNTIME_FILES" | while read -r f; do
		[ -n "$f" ] || continue
		sed -E -i "s/namespace Confuser\.Runtime\b/namespace ${NS}/g" "$f"
		echo "${f#$ROOT/}" >> "$MANIFEST"
	done
}

apply_obf() { # "Confuser.Runtime.<Type> -> "$NS.<Type>   (leaves .dll and the bare id alone)
	echo "$OBF_FILES" | while read -r f; do
		[ -n "$f" ] || continue
		sed -E -i "s/\"Confuser\.Runtime\.([A-Z])/\"${NS}.\1/g" "$f"
		echo "${f#$ROOT/}" >> "$MANIFEST"
	done
}

apply_watermark() {
	if grep -q ConfusedByAttribute "$WATERMARK_FILE"; then
		sed -E -i "s/ConfusedByAttribute/${WM}/g" "$WATERMARK_FILE"
		echo "${WATERMARK_FILE#$ROOT/}" >> "$MANIFEST"
	fi
}

apply_runtime
apply_obf
apply_watermark

sort -u "$MANIFEST" -o "$MANIFEST"
echo "applied. $(wc -l < "$MANIFEST") file(s) changed (manifest: ${MANIFEST#$ROOT/})."

# --- test ----------------------------------------------------------------------------------
if [ "$DO_TEST" -eq 1 ]; then
	echo
	echo "building + testing (LangVersion=latest works around a known net462 test-harness default)..."
	# A successful obfuscation run is the real proof: if a runtime full name no longer resolves,
	# GetRuntimeType returns null and the protection tests fail.
	dotnet build "$ROOT/Confuser2.sln" -c Release -p:LangVersion=latest
	dotnet test "$ROOT/Confuser2.sln" -c Release -p:LangVersion=latest
fi
