#!/bin/bash
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

echo "=== id ==="
id 2>&1
echo "=== pwd ==="
pwd 2>&1
echo "=== uname -a ==="
uname -a 2>&1
echo "=== dotnet --info ==="
dotnet --info 2>&1 || echo "no dotnet"
echo "=== dotnet --list-sdks ==="
dotnet --list-sdks 2>&1 || echo "no sdks"
echo "=== DISPLAY ==="
printf 'DISPLAY=%s\n' "${DISPLAY:-none}"
echo "=== WAYLAND_DISPLAY ==="
printf 'WAYLAND_DISPLAY=%s\n' "${WAYLAND_DISPLAY:-none}"
echo "=== done ==="
