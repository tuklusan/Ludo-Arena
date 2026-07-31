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

"""Run dotnet commands from workspace root."""
import subprocess, os, sys

workspace = os.path.dirname(os.path.abspath(__file__))
cmd = sys.argv[1:]
full_env = os.environ.copy()
print(f"CWD: {workspace}")
print(f"CMD: {' '.join(cmd)}")
print("---")
r = subprocess.run(cmd, cwd=workspace, capture_output=True, text=True, timeout=180, env=full_env)
if r.stdout:
    print(r.stdout)
if r.stderr:
    print(r.stderr, file=sys.stderr)
print(f"RC: {r.returncode}")
sys.exit(r.returncode)
