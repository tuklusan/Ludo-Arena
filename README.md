# SANYALnet Labs Ludo AI Arena

A polished, cross-platform desktop **Ludo** game in which **four autonomous AI players** play a
full match automatically — animated die, tokens gliding cell by cell, captures, blockades, bonus
rolls and a winner screen. Each player consults a language model (via the OpenAI-compatible
**NVIDIA NIM** API) to pick its move from an engine-generated list of legal moves, and the whole
thing falls back to a deterministic local AI the instant the network misbehaves.

Built in **C# on .NET 10 with Avalonia UI**. One codebase, one build command, a native window on
Linux, Windows and macOS.

> Solution/assembly identifiers use `LudoNimArena`; all user-visible header/title text reads
> "SANYALnet Labs Ludo AI Arena".

## Screenshots

| Linux | Windows 10 |
|---|---|
| ![Linux](screenshots/linux.png) | ![Windows 10](screenshots/windows10.png) |

| Windows 11 | macOS Big Sur |
|---|---|
| ![Windows 11](screenshots/windows11.png) | ![macOS](screenshots/macos.png) |

## Features

- Exactly four autonomous AI players (Red, Green, Yellow, Blue), four tokens each, automatic play
  from an animated roll-off to victory.
- A separate NVIDIA NIM decision session per player (isolated persona, history and counters).
- A complete deterministic **local fallback AI** that can finish a whole game with no network.
- "Indian Digital Classic" rule profile: six-to-enter, bonus roll on six/capture/finish,
  three-consecutive-sixes ends the turn, safe squares, blockades, exact roll to finish.
- A 15×15 board drawn with Avalonia vector APIs; smooth interpolated token movement; a die that
  flashes without ever resizing or reflowing the board.
- Per-player "last decision source" indicator (NIM vs. local fallback), a scrollable event log,
  and an always-visible QUIT.
- Cancellation-aware retry, throttling and a circuit breaker around the NIM client (handles
  HTTP 429/529, `Retry-After`, long server waits) — the UI never freezes on the network.

## Requirements

- **.NET 10 SDK** (`net10.0`). Verify with `dotnet --info`.
- A desktop environment (Linux X11/Wayland, Windows, or macOS).
- Optional: an **NVIDIA NIM** API key for live model-driven moves. Without one the game runs on
  the local fallback AI.

## Build and run

```bash
# from the repository root
dotnet restore LudoNimArena.slnx
dotnet build   LudoNimArena.slnx -c Release
dotnet run --project src/LudoNimArena.App -c Release
```

On Windows use `src\LudoNimArena.App`. The setup screen opens; press **START GAME**.

On a minimal Linux desktop with no GPU driver, force software rendering so Avalonia's Skia backend
paints reliably:

```bash
export LIBGL_ALWAYS_SOFTWARE=1 GALLIUM_DRIVER=llvmpipe
dotnet run --project src/LudoNimArena.App -c Release
```

## NVIDIA NIM configuration (environment variables only)

The application reads all NVIDIA settings **from the environment** — no key is ever stored in
source, config, logs or artifacts. Set at least `NVIDIA_API_KEY` for live decisions:

| Variable | Purpose |
|---|---|
| `NVIDIA_API_KEY` | Bearer key for the NIM endpoint (required for live moves). |
| `NVIDIA_MODEL` | Move-picker model, e.g. a small non-reasoning instruct model. |
| `NVIDIA_SECONDARY_MODEL` | Optional hosted failover; empty = skip straight to local fallback. |
| `NVIDIA_BASE_URL` | OpenAI-compatible base URL (keep the `/v1` path segment). |
| `NVIDIA_REQUEST_TIMEOUT_SECONDS` | Per-request timeout. |
| `NVIDIA_MIN_CALL_INTERVAL_SECONDS` | Request spacing to stay under the tier's rate ceiling. |
| `NVIDIA_MAX_RETRY_ELAPSED_SECONDS` | Total retry budget before falling back locally. |
| `NVIDIA_CIRCUIT_BREAKER_SECONDS` | How long the circuit stays open after repeated failures. |

```bash
export NVIDIA_API_KEY='...'                                  # your key — never commit it
export NVIDIA_MODEL='nvidia/nemotron-mini-4b-instruct'       # small, non-reasoning, fast
dotnet run --project src/LudoNimArena.App -c Release
```

If the key is missing or a model is unavailable, the game starts normally, warns, and uses the
local fallback AI. Every move is labelled with its source, so a fully-fallback game is obvious.

## Project structure

```
LudoNimArena.slnx            # solution
Directory.Build.props        # shared MSBuild settings
Directory.Packages.props     # centrally pinned package versions
global.json                  # pinned .NET 10 SDK
src/LudoNimArena.Core        # rules, board geometry, state, legal moves, die abstractions
src/LudoNimArena.AI          # NIM client, per-player sessions, DTOs, local fallback AI
src/LudoNimArena.App         # Avalonia startup, MVVM, board rendering, animation
tests/                       # Core / AI / App test projects
scripts/  *.py  env_check.sh # build and environment-inspection harnesses
docs/REQUIREMENTS_PROMPT.md  # the exact specification the build was driven from
```

## Tests

```bash
dotnet test LudoNimArena.slnx -c Release
```

The Core tests include deterministic four-player fallback simulations that check board invariants
after every move.

## Cross-platform verification

The **identical source** in this repository was built and run, with zero code changes and zero
build errors, on four machines: a Linux desktop, Windows 10, Windows 11, and macOS Big Sur 11
(.NET 10.0.302). Each brought up the same window and played a full game — see `screenshots/`.

## Origin

This game was built by **ChatDev 2.0**, a multi-agent "virtual software company," from the
specification in [`docs/REQUIREMENTS_PROMPT.md`](docs/REQUIREMENTS_PROMPT.md). The full story —
five free models that failed and one ~\$1 paid DeepSeek run that shipped it — is written up in a
three-part blog series on [Supratim Sanyal's Blog](https://supratim-sanyal.blogspot.com/).

## License

Copyright (c) 2026 Supratim Sanyal of SANYALnet Labs. Released for **non-commercial** use under the
terms in [`LICENSE`](LICENSE). Attribution required: *"Based on original work by Supratim Sanyal of
SANYALnet Labs."*
