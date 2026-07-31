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

"""Environment inspection helper for Ludo NIM Arena build."""
import subprocess
import os
import sys

def run(cmd, **kwargs):
    result = subprocess.run(cmd, capture_output=True, text=True, **kwargs)
    print(f"$ {' '.join(cmd)}")
    if result.stdout:
        print(result.stdout.rstrip())
    if result.stderr:
        print(result.stderr.rstrip(), file=sys.stderr)
    print(f"exit={result.returncode}")
    return result

run(["id"])
run(["pwd"])
run(["uname", "-a"])
run(["dotnet", "--info"])
run(["dotnet", "--list-sdks"])
print(f"DISPLAY={os.environ.get('DISPLAY', '')}")
print(f"WAYLAND_DISPLAY={os.environ.get('WAYLAND_DISPLAY', '')}")
print(f"NVIDIA_API_KEY present: {'NVIDIA_API_KEY' in os.environ}")
