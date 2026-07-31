#!/usr/bin/env python3
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
