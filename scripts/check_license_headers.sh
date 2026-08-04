#!/usr/bin/env bash
# ============================================================================
# Copyright (c) 2026 Supratim Sanyal of SANYALnet Labs.
# Proprietary rights reserved except as expressly licensed herein.
#
# LUDO ARENA
# This file is governed by the SANYALnet Labs Non-Commercial License in the
# root LICENSE file. Non-Commercial use is permitted; Commercial Use and use
# for AI/ML model training are prohibited unless separately authorized.
#
# Attribution is required: "Based on original work by Supratim Sanyal of
# SANYALnet Labs." See LICENSE for full terms, warranty disclaimer, termination,
# patent, trademark, and governing-law provisions.
# ============================================================================
#
# Gate: fails if any project source file is missing the mandatory license header.
# Run locally with `bash scripts/check_license_headers.sh`; also enforced in CI by
# .github/workflows/license-header.yml. Source file types checked are listed below;
# files with no comment syntax (e.g. *.json, *.lock) are intentionally excluded.
set -euo pipefail

MARKER='Copyright (c) 2026 Supratim Sanyal of SANYALnet Labs.'
cd "$(git rev-parse --show-toplevel)"

missing=0
checked=0
while IFS= read -r f; do
  case "$f" in
    *.cs|*.axaml|*.py|*.sh|*.toml|*.yml|*.yaml|*.csproj|*.props|*.slnx|*.config) ;;
    *) continue ;;
  esac
  checked=$((checked+1))
  if ! grep -qF "$MARKER" "$f"; then
    echo "::error file=$f::missing mandatory SANYALnet Labs license header"
    echo "MISSING HEADER: $f"
    missing=$((missing+1))
  fi
done < <(git ls-files)

echo "Checked $checked source file(s)."
if [ "$missing" -gt 0 ]; then
  echo "FAIL: $missing source file(s) missing the mandatory license header."
  exit 1
fi
echo "OK: every project source file carries the mandatory license header."
