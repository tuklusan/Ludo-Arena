#!/bin/bash
echo "=== id ==="
id 2>&1
echo "=== pwd ==="
pwd 2>&1
echo "=== uname -a ==="
uname -a 2>&1
echo "=== dotnet --info ==="
dotnet --info 2>&1 || echo "no dotnet"
echo "=== dotnet --list-sdks ==="
dotnet --list-sdks 2>&1 || echo "no sdks"
echo "=== DISPLAY ==="
printf 'DISPLAY=%s\n' "${DISPLAY:-none}"
echo "=== WAYLAND_DISPLAY ==="
printf 'WAYLAND_DISPLAY=%s\n' "${WAYLAND_DISPLAY:-none}"
echo "=== done ==="
