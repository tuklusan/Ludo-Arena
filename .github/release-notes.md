<!-- ============================================================================
     Copyright (c) 2026 Supratim Sanyal of SANYALnet Labs.
     Proprietary rights reserved except as expressly licensed herein.

     LUDO ARENA
     This file is governed by the SANYALnet Labs Non-Commercial License in the
     root LICENSE file. Non-Commercial use is permitted; Commercial Use and use
     for AI/ML model training are prohibited unless separately authorized.

     Attribution is required: "Based on original work by Supratim Sanyal of
     SANYALnet Labs." See LICENSE for full terms, warranty disclaimer, termination,
     patent, trademark, and governing-law provisions.
     ============================================================================ -->

## ▶ Play online now

**[Play Ludo AI Arena in your browser →](https://tuklusan.github.io/Ludo-Arena/)** — nothing to install, and on a phone the game starts straight away.

> **Browser vs. desktop:** the online version uses the built-in deterministic strategy bots only. It uses **no AI models and makes no API calls**. The language-model (NVIDIA NIM) players are **only in the desktop app** below.

## ⬇ Download the desktop app (with language-model players)

| Platform | x64 | arm64 |
|---|---|---|
| **Linux** | [`LudoArena-@VERSION@-linux-x64.tar.gz`](https://github.com/tuklusan/Ludo-Arena/releases/download/@TAG@/LudoArena-@VERSION@-linux-x64.tar.gz) | [`LudoArena-@VERSION@-linux-arm64.tar.gz`](https://github.com/tuklusan/Ludo-Arena/releases/download/@TAG@/LudoArena-@VERSION@-linux-arm64.tar.gz) |
| **Windows** | [`LudoArena-@VERSION@-win-x64.zip`](https://github.com/tuklusan/Ludo-Arena/releases/download/@TAG@/LudoArena-@VERSION@-win-x64.zip) | [`LudoArena-@VERSION@-win-arm64.zip`](https://github.com/tuklusan/Ludo-Arena/releases/download/@TAG@/LudoArena-@VERSION@-win-arm64.zip) |
| **macOS** | [`LudoArena-@VERSION@-osx-x64.tar.gz`](https://github.com/tuklusan/Ludo-Arena/releases/download/@TAG@/LudoArena-@VERSION@-osx-x64.tar.gz) | [`LudoArena-@VERSION@-osx-arm64.tar.gz`](https://github.com/tuklusan/Ludo-Arena/releases/download/@TAG@/LudoArena-@VERSION@-osx-arm64.tar.gz) |

- **Needs** the [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0). No installer: extract and run `LudoNimArena.App`; uninstall by deleting the folder.
- **Verify** a download with [`SHA256SUMS.txt`](https://github.com/tuklusan/Ludo-Arena/releases/download/@TAG@/SHA256SUMS.txt): `sha256sum -c SHA256SUMS.txt --ignore-missing`
- **AI players:** set `NVIDIA_API_KEY` (and optionally `NVIDIA_MODEL`) to use live model decisions. Without a key the game plays on the local AI. Details in the [README](https://github.com/tuklusan/Ludo-Arena#nvidia-nim-configuration-environment-variables-only).
- **macOS:** clear Gatekeeper quarantine with `xattr -dr com.apple.quarantine .` in the extracted folder. **Windows 11:** Smart App Control may block unsigned builds (error `0x800711C7`).

---

