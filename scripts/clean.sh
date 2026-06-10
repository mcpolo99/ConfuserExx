#!/usr/bin/env bash
# Clean all build artifacts, test results, and temporary files.
# Usage: ./scripts/clean.sh

set -e

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$REPO_ROOT"

echo "Cleaning build artifacts..."

# Build output
find . -type d \( -name "bin" -o -name "obj" \) -not -path "./.git/*" -exec rm -rf {} + 2>/dev/null || true

# Release output from C++ project
rm -rf Tests/244_ClrProtection/Release Tests/244_ClrProtection/Debug 2>/dev/null || true
rm -rf Release Debug 2>/dev/null || true

# Packages
rm -f ConfuserEx-CLI.zip ConfuserEx-GUI.zip ConfuserEx.zip 2>/dev/null || true
rm -rf combined 2>/dev/null || true

# Test results and coverage
rm -rf coverage test-results TestResults 2>/dev/null || true
find . -name "*.trx" -not -path "./.git/*" -delete 2>/dev/null || true

# NuGet
rm -rf packages 2>/dev/null || true

echo "Done."
