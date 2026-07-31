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

"""Build helper for Ludo NIM Arena."""
import subprocess, sys, os

workspace = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
os.chdir(workspace)

def run(cmd, **kwargs):
    print(f"$ {' '.join(cmd)}")
    result = subprocess.run(cmd, capture_output=True, text=True, cwd=workspace, **kwargs)
    if result.stdout: print(result.stdout.rstrip())
    if result.stderr: print(result.stderr.rstrip(), file=sys.stderr)
    return result

action = sys.argv[1] if len(sys.argv) > 1 else "all"
sln = "LudoNimArena.slnx"

if action in ("sln", "all"):
    run(["dotnet", "new", "sln", "--name", "LudoNimArena", "--force"])
    for proj in [
        "src/LudoNimArena.Core/LudoNimArena.Core.csproj",
        "src/LudoNimArena.AI/LudoNimArena.AI.csproj",
        "src/LudoNimArena.App/LudoNimArena.App.csproj",
        "tests/LudoNimArena.Core.Tests/LudoNimArena.Core.Tests.csproj",
        "tests/LudoNimArena.AI.Tests/LudoNimArena.AI.Tests.csproj",
        "tests/LudoNimArena.App.Tests/LudoNimArena.App.Tests.csproj",
    ]:
        run(["dotnet", "sln", sln, "add", proj])

if action in ("restore", "all"):
    run(["dotnet", "restore", sln])

if action in ("build", "all"):
    run(["dotnet", "build", sln, "-c", "Release", "--no-restore"])

if action == "test":
    run(["dotnet", "test", sln, "-c", "Release", "--no-build"])

if action == "publish":
    run(["dotnet", "publish", "src/LudoNimArena.App/LudoNimArena.App.csproj",
         "-c", "Release", "-r", "linux-x64", "--self-contained", "false"])
