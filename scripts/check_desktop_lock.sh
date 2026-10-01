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
# Desktop lock: the desktop implementation (the Avalonia app, the shared rules engine and AI,
# and their tests) must not change by accident while the web build evolves. This gate fails if
# a push or pull request touches a locked path, unless one of its commit messages contains
# the explicit override token  [desktop-change].
#
#   bash scripts/check_desktop_lock.sh <base-sha> <head-sha>
set -euo pipefail

LOCKED='^(src/LudoNimArena\.App/|src/LudoNimArena\.Core/|src/LudoNimArena\.AI/|tests/|LudoNimArena\.slnx$|global\.json$|Directory\.Packages\.props$)'
OVERRIDE='[desktop-change]'

base="${1:-}"; head="${2:-HEAD}"
if [ -z "$base" ] || [ "$base" = "0000000000000000000000000000000000000000" ]; then
  echo "No base commit to compare against; nothing to check."; exit 0
fi

changed=$(git diff --name-only "$base" "$head" | grep -E "$LOCKED" || true)
if [ -z "$changed" ]; then
  echo "OK: no locked desktop files changed."; exit 0
fi

echo "Locked desktop files changed:"; echo "$changed" | sed 's/^/  /'
if git log --format=%B "$base..$head" | grep -qF "$OVERRIDE"; then
  echo "OK: override token $OVERRIDE present in the commit messages."; exit 0
fi
echo "::error::Desktop lock: the files above are locked. Add $OVERRIDE to a commit message to change them deliberately."
exit 1
