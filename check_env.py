#!/usr/bin/env python3
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

import subprocess, os, sys

def run(cmd):
    r = subprocess.run(cmd, capture_output=True, text=True, timeout=30)
    print(f"=== {' '.join(cmd)} ===")
    print("STDOUT:", r.stdout)
    print("STDERR:", r.stderr)
    print("RC:", r.returncode)
    print()

run(["id"])
run(["pwd"])
run(["uname", "-a"])
run(["dotnet", "--info"])
run(["dotnet", "--list-sdks"])
print(f"DISPLAY={os.environ.get('DISPLAY', '')}")
print(f"WAYLAND_DISPLAY={os.environ.get('WAYLAND_DISPLAY', '')}")
print(f"PWD={os.getcwd()}")
sys.exit(0)
