#!/usr/bin/env python3
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
