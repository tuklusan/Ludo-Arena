#!/usr/bin/env python3
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
