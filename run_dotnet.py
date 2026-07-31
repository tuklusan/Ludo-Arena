#!/usr/bin/env python3
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
