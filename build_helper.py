#!/usr/bin/env python3
"""Build helper for dotnet commands in Ludo NIM Arena."""
import subprocess, os, sys

WORKSPACE = os.path.dirname(os.path.abspath(__file__))
SOLUTION_DIR = os.path.join(WORKSPACE, "LudoNimArena")

def run_cmd(cmd, cwd=None, timeout=120, env=None):
    cwd = cwd or SOLUTION_DIR
    full_env = os.environ.copy()
    if env:
        full_env.update(env)
    print(f"\n>>> {' '.join(cmd)}")
    r = subprocess.run(cmd, cwd=cwd, capture_output=True, text=True, timeout=timeout, env=full_env)
    if r.stdout:
        print(r.stdout)
    if r.stderr:
        print(r.stderr, file=sys.stderr)
    print(f"RC: {r.returncode}")
    return r

def main():
    cmd = sys.argv[1]
    args = sys.argv[2:]
    
    if cmd == "restore":
        run_cmd(["dotnet", "restore"] + args)
    elif cmd == "build":
        run_cmd(["dotnet", "build", "-c", "Release"] + args)
    elif cmd == "test":
        run_cmd(["dotnet", "test", "-c", "Release"] + args)
    elif cmd == "publish":
        run_cmd(["dotnet", "publish", "src/LudoNimArena.App/LudoNimArena.App.csproj",
             "-c", "Release", "-r", "linux-x64", "--self-contained", "false"] + args)
    elif cmd == "run":
        run_cmd(["dotnet", "run", "--project", "src/LudoNimArena.App/LudoNimArena.App.csproj",
             "-c", "Release"] + args, timeout=30)
    elif cmd == "new-sln":
        run_cmd(["dotnet", "new", "sln", "-n", "LudoNimArena", "--force"], cwd=SOLUTION_DIR)
    elif cmd == "new-project":
        template = args[0] if args else "classlib"
        name = args[1] if len(args) > 1 else "Unknown"
        path = args[2] if len(args) > 2 else name
        run_cmd(["dotnet", "new", template, "-n", name, "-o", path, "--force"], cwd=SOLUTION_DIR)
    elif cmd == "sln-add":
        run_cmd(["dotnet", "sln", "add"] + args)
    elif cmd == "add-ref":
        project = args[0]
        references = args[1:]
        for ref in references:
            run_cmd(["dotnet", "add", project, "reference", ref])
    elif cmd == "add-package":
        project = args[0]
        packages = args[1:]
        for pkg in packages:
            run_cmd(["dotnet", "add", project, "package", pkg])
    else:
        run_cmd(["dotnet"] + [cmd] + args)

if __name__ == "__main__":
    main()
