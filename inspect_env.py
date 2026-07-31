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

"""Inspect the build environment for Ludo NIM Arena."""
import subprocess, os, sys, shutil

def run_cmd(cmd, timeout=30):
    print(f"\n=== {' '.join(cmd)} ===")
    try:
        r = subprocess.run(cmd, capture_output=True, text=True, timeout=timeout)
        print("STDOUT:", r.stdout)
        print("STDERR:", r.stderr)
        print("RC:", r.returncode)
        return r
    except Exception as e:
        print(f"ERROR: {e}")
        return None

run_cmd(["id"])
run_cmd(["pwd"])
run_cmd(["uname", "-a"])
run_cmd(["dotnet", "--info"], timeout=60)
run_cmd(["dotnet", "--list-sdks"])
print(f"\nDISPLAY={os.environ.get('DISPLAY', '')}")
print(f"WAYLAND_DISPLAY={os.environ.get('WAYLAND_DISPLAY', '')}")
print(f"NVIDIA_API_KEY present: {'NVIDIA_API_KEY' in os.environ}")
print(f"XAUTHORITY={os.environ.get('XAUTHORITY', '')}")
run_cmd(["which", "dotnet"])
run_cmd(["which", "xvfb-run"])
run_cmd(["dotnet", "workload", "list"])
