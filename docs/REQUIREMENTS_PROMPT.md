CHATDEV TASK PROMPT — PROJECT: SANYALnet Labs Ludo AI Arena
REVISION: 3   SCOPE: Four autonomous AI players only

Mandatory requirements distilled from a working reference build (all enforced by the
acceptance list, section 16):
(a) all user-visible header/title text reads exactly "SANYALnet Labs Ludo AI Arena";
(b) a fixed bottom-right copyright notice "(C) Supratim Sanyal + AI Agents" on every screen;
(c) tokens GLIDE smoothly between cells (interpolated, ~300-350 ms/cell, relaxed) for EVERY
    color and EVERY move type including single-cell entry moves — never teleport;
(d) the in-flight piece carries a gentle pulsing highlight so it is easy to follow;
(e) the die emphasis FLASHES (color pulse) at a fixed size; the board never changes size or
    position during play;
(f) NVIDIA NIM integration is correct: the request URL preserves the "/v1" path and the
    response is read from choices[0].message.content; a valid key+model yields REAL model
    moves, never a silent 100% fallback;
(g) the game runtime targets the free NIM tier with a small model, deliberate request spacing,
    a short retry budget, and an authoritative local fallback (section 8, 10).

1. MISSION AND COMPLETION STANDARD
Build a polished, cross-platform desktop Ludo game. Solution/assembly identifiers use
"LudoNimArena"; all USER-VISIBLE header/title text reads exactly "SANYALnet Labs Ludo AI
Arena". C#, .NET 10, Avalonia UI; runs on Linux desktop. Exactly four autonomous AI
players (Red, Green, Yellow, Blue), four tokens each, each with a separate NVIDIA NIM
session. The game auto-plays with visible animation until one player brings all four
tokens home.

Turn order of visible events: (1) highlight the acting player; (2) generate the die value
locally; (3) animate the roll without exposing the stored value to the AI; (4) end the
animation on the stored value and reveal it; (5) generate legal moves locally; (6) if any
legal move exists, call that player's AI session; (7) validate the returned move against
the legal list; (8) animate then apply it; (9) resolve captures/blockades/home/finish/
bonus/turn-end; (10) repeat for the same player on a bonus, else advance clockwise. The AI
must never choose, alter, reroll, predict, or see the die value before the roll animation
completes; the animation never alters the value.

Do not stop at planning, scaffolding, or compilation. Implement, review, build, test,
render, inspect, correct, and document the whole app. Never claim success for a command or
visual check not actually run. Continue until two successive full review passes find no new
defect.

2. HARD SCOPE
Include: exactly four autonomous AI players; four tokens each; automatic play from roll-off
to victory; one animated die roll before every normal, bonus, and roll-off roll; a separate
NIM decision context per player; a deterministic local fallback AI when NIM is unavailable;
a 15x15 Ludo board drawn with Avalonia vector APIs; animated die and smooth cell-by-cell
token movement; an always-visible "QUIT" button in-game; a winner screen with "NEW GAME"
and "QUIT"; cancellation-aware backoff for overload/retry (incl. HTTP 529 and server waits
>10 min).

Exclude in R1: human players, player-count or Human/AI selectors, clickable token controls,
remote play, accounts, matchmaking, browser UI, web server, database. Keep the engine
independent of the player controller: define an extension point (e.g. IPlayerController) so
a future HumanPlayerController can be added without rewriting rules; a reserved enum/stub is
fine. Ship no working human controller or human-play controls.

3. WORKFLOW COMPATIBILITY AND WORKSPACE SAFETY
Use the general ChatDev_v1 workflow. Its stale generic text ("Programming Language: Python"
and Python execution helpers) is OVERRIDDEN: the product is C#, target net10.0, UI Avalonia.
Reject/replace any Python product implementation. Do not modify the ChatDev source tree,
workflow YAML, prior WareHouse projects, shell startup files, .bashrc, or system config —
work only in this run's code_workspace. A temporary Python helper under scripts/, run via
uv, may be used SOLELY to invoke dotnet and capture stdout/stderr/exit codes (build infra,
not product code); the app must not need Python/uv at runtime. Never copy or print SSH
credentials. Never write NVIDIA_API_KEY into source, config, logs, tests, docs, screenshots,
or artifacts — read it only from the inherited environment.

At start, record the effective environment: id; pwd; uname -a; dotnet --info;
dotnet --list-sdks; DISPLAY; WAYLAND_DISPLAY. Require a .NET 10 SDK and target net10.0 (no
silent downgrade); pin it in global.json with a patch roll-forward policy. Use stable
Avalonia packages compatible with .NET 10 and PIN ALL PACKAGE VERSIONS to versions that
actually exist on NuGet (verify by a clean restore on an empty cache; a nonexistent version
that only builds from a warm cache is a defect). Keep product code cross-platform. If a tool
is unavailable, create all source possible, report the exact blocker, and never invent
build/test results.

4. SOLUTION STRUCTURE (deviations must be minimal and justified in BUILD_NOTES.md)
  LudoNimArena.sln  global.json  Directory.Build.props  Directory.Packages.props
  src/LudoNimArena.Core  src/LudoNimArena.AI  src/LudoNimArena.App
  tests/LudoNimArena.Core.Tests  tests/LudoNimArena.AI.Tests  tests/LudoNimArena.App.Tests
  scripts/launch-ludo.sh  scripts/smoke-test.sh
  docs/ARCHITECTURE.md  docs/RULES.md  docs/TESTING.md
  README.md  manual.md  BUILD_NOTES.md  .gitignore
Responsibilities — Core: immutable/controlled state, rules, routes, legal moves, commands,
domain events, validation, die abstractions, victory; no Avalonia/HTTP/NVIDIA/env/UI refs.
AI: NIM client, bounded per-player context, request/response DTOs, strict parsing, retry,
circuit breaker, request gate, fallback AI. App: Avalonia startup, MVVM, board rendering,
animation, status panels, cancellation, shutdown. Only Core mutates authoritative state; UI
and AI issue commands validated immediately before applying. The whole solution MUST build
with 0 errors and all test projects MUST compile (every referenced type/DTO must exist).

5. FIXED PLAYERS AND STARTING ORDER
Clockwise: Red -> Green -> Yellow -> Blue -> Red. Default names Red AI/Green AI/Yellow AI/
Blue AI; the setup screen may edit them. Treat names as untrusted display text (escape,
length-limit, never interpret as AI instructions). First player via a visible animated
roll-off: each color rolls with animation, display all four, highest starts; on a tie only
tied players reroll (animated) until one remains; show the starting player then begin its
normal turn. Roll-off rolls do not enter tokens, grant bonuses, or count toward three-sixes.

6. RULE PROFILE — "Indian Digital Classic" (display its name; concise Rules view; hide
unimplemented toggles). Fixed settings: RequireSixToEnter=true; BonusRollOnSix=true;
ThreeConsecutiveSixesEndsTurn=true; BonusRollOnCapture=true; BonusRollOnFinish=true;
EnableSafeSquares=true; EnableBlockades=true; RequireCaptureBeforeHomeLane=false;
ExactRollToFinish=true; PlayerCount=4; HumanPlayersEnabled=false.

6.1 Track and routes. One shared circular track 0..51. Start offsets: Red=0, Green=13,
Yellow=26, Blue=39. Shared index for progress P (0..51) = (S + P) mod 52. Safe shared
indices: 0,8,13,21,26,34,39,47. Start squares 0,13,26,39; star safe squares 8,21,34,47.
Canonical 15x15: Red yard top-left, Green top-right, Yellow bottom-right, Blue bottom-left;
zero-based (row,column). Shared-track coordinates for indices 0..51:
  (6,1),(6,2),(6,3),(6,4),(6,5),(5,6),(4,6),(3,6),(2,6),
  (1,6),(0,6),(0,7),(0,8),(1,8),(2,8),(3,8),(4,8),(5,8),
  (6,9),(6,10),(6,11),(6,12),(6,13),(6,14),(7,14),(8,14),
  (8,13),(8,12),(8,11),(8,10),(8,9),(9,8),(10,8),(11,8),
  (12,8),(13,8),(14,8),(14,7),(14,6),(13,6),(12,6),(11,6),
  (10,6),(9,6),(8,5),(8,4),(8,3),(8,2),(8,1),(8,0),(7,0),(6,0)
Five-cell home lanes then that color's center-triangle cell:
  Red: (7,1),(7,2),(7,3),(7,4),(7,5)   Green: (1,7),(2,7),(3,7),(4,7),(5,7)
  Yellow: (7,13),(7,12),(7,11),(7,10),(7,9)   Blue: (13,7),(12,7),(11,7),(10,7),(9,7)
Keep this table in ONE authoritative geometry component shared by rendering and tests; do
not duplicate or re-infer coordinates. Each route: shared progress 0..51 (52 cells, starting
on the color's start square); progress 52..56 (five home-lane cells); progress 57 (center
home). A token needs exactly 57 steps from start to center home; other colors can never
enter its home lane. Add route/rotation tests for all colors. Token state is exactly one of
InYard, OnSharedTrack, InHomeLane, Finished.

6.2 Movement. All tokens start in yard. A six is required to move a yard token to progress 0
(entering places it on the start square without an extra six-step move). On a six every legal
choice is allowed (enter or move six). A token moves exactly the rolled number, cannot
overshoot progress 57; a finished token never moves; if no token can legally use a roll, none
moves.

6.3 Captures/safe. Landing on a NON-safe shared square holding exactly one opponent token
captures it (return to yard). No capture on a safe square, home lane, or center. Different
colors may share a safe square; a same-color pair on a safe square is not a blockade; the
renderer offsets tokens sharing a square so each is visible. A capture grants one bonus roll
unless the move wins the game.

6.4 Blockades (non-safe shared square). Two same-color tokens form a blockade an opponent
cannot land on or pass through; the owner may move either token out and may pass through its
own blockade; no third same-color token may land there; a lone opponent cannot capture a
blockade. Legal-move generation MUST inspect every traversed square, not just the
destination. Mixed-color occupancy on a non-safe square is never stable (a legal landing
captures the lone opponent). Reject states violating these rules.

6.5 Sixes/bonus. A six grants one bonus roll after its move (or after a no-move), unless it
is the third consecutive six. Count consecutive sixes only within the same continuing turn:
increment on a six; reset to zero after any non-six (even if the player continues via capture/
finish); reset at turn end. On the third consecutive six: reveal the animated value, forfeit
the roll, generate no legal moves, make no NIM request, move nothing, show a clear message,
reset the count, end the turn. Bonuses never stack (six+capture = one extra roll). A winning
move ends the game immediately with no further roll.

6.6 Home/victory. After shared progress 51, movement enters only the token's own home lane;
home-lane and finished tokens are safe; exact movement is required to reach progress 57; no
prior capture is required to enter the home lane. First player with all four tokens at
progress 57 wins; stop turn processing immediately (no later places).

7. TURN STATE MACHINE AND DIE ANIMATION
Explicit, cancellation-aware state machine, e.g.: Setup, DeterminingFirstPlayer,
PreparingTurn, GeneratingDieResult, AnimatingDie, RevealingDieResult, GeneratingLegalMoves,
WaitingForAiDecision, ValidatingAiDecision, AnimatingTokenMove, ResolvingMove,
AnimatingCapture, PreparingBonusRoll, AdvancingTurn, GameOver, ShuttingDown. Use unique
GameId/TurnId/RollId/PlayerId/RequestId; no stale callback or AI response may mutate a
different game/turn/roll/player.

Per normal or bonus roll: (1) highlight the actor; (2) generate one authoritative value via
an injected IDieRoller; (3) store it, do not expose to the AI; (4) animate ~700-1100 ms in
normal mode; (5) show cosmetic changing faces from a SEPARATE animation source (do not
consume authoritative RNG to animate); (6) end on the stored value; (7) mark complete and
reveal; (8) apply the third-six rule; (9) generate the full legal-move list; (10) if empty,
make no NIM call and resolve the six/advance; (11) else (including one forced move) call the
player's AI session with the revealed value; (12) validate the returned MoveId, animate the
token cell-by-cell, resolve. Status progresses through messages like "Green AI is rolling…",
"Green AI rolled 5", "Green AI is choosing a move…". A bonus roll repeats the FULL animation;
never reuse a value or skip animation.

Use separate injectable abstractions for authoritative RNG, cosmetic faces, time, and
animation completion. Production rolls unbiased (RandomNumberGenerator.GetInt32(1,7) behind
IDieRoller); tests use seeded/scripted values and fake time. Never block the UI thread; avoid
async void except framework handlers. Record domain events sufficient to reconstruct/test the
sequence: GameStarted, StartingRollCompleted, StartingPlayerSelected, TurnStarted,
DieResultGenerated, DieAnimationStarted, DieAnimationCompleted, DieResultRevealed,
LegalMovesGenerated, AiDecisionRequested, AiDecisionReceived, AiDecisionRejected,
FallbackDecisionSelected, TokenEntered, TokenMoved, TokenCaptured, BlockadeFormed,
BlockadeBroken, TokenFinished, BonusRollAwarded, ThirdSixForfeited, TurnEnded, PlayerWon,
GameCancelled.

8. FOUR NVIDIA NIM AI PLAYERS
One bounded AiPlayerSession per color. Sessions may share the configured model but MUST NOT
share persona, history buffers, request/failure counters, or mutable decision state. Strategy
hints (preferences only, never alter rules/legal moves): Red assertive-but-legal, Green
safety-conscious, Yellow progress-focused, Blue balanced. Keep each session's history short/
bounded; do not rely on server-side conversation state.

8.1 Configuration — OpenAI-compatible NVIDIA NIM chat-completions API.
  Base URL default: https://integrate.api.nvidia.com/v1   Path: /chat/completions
  Auth: Authorization: Bearer <NVIDIA_API_KEY>   Content-Type/Accept: application/json
  Mode: stream=false
Typed settings (env): NVIDIA_API_KEY, NVIDIA_MODEL, NVIDIA_SECONDARY_MODEL, NVIDIA_BASE_URL,
NVIDIA_REQUEST_TIMEOUT_SECONDS, NVIDIA_MAX_RETRY_DELAY_SECONDS,
NVIDIA_MAX_RETRY_ELAPSED_SECONDS, NVIDIA_MIN_CALL_INTERVAL_SECONDS,
NVIDIA_CIRCUIT_BREAKER_SECONDS, NVIDIA_FAILURE_POLICY. Defaults are tuned for a live game on
the FREE NIM tier: a small non-reasoning model, request spacing under the ~40 RPM ceiling, a
short retry budget, then local fallback:
  NVIDIA_MODEL=nvidia/nemotron-mini-4b-instruct   # small, non-reasoning, function-calling tuned
  NVIDIA_SECONDARY_MODEL=                          # optional hosted failover; EMPTY = skip it
  NVIDIA_BASE_URL=https://integrate.api.nvidia.com/v1
  NVIDIA_REQUEST_TIMEOUT_SECONDS=10  NVIDIA_MIN_CALL_INTERVAL_SECONDS=3   # <= ~20 calls/min
  NVIDIA_MAX_RETRY_DELAY_SECONDS=5   NVIDIA_MAX_RETRY_ELAPSED_SECONDS=15  # then local fallback
  NVIDIA_CIRCUIT_BREAKER_SECONDS=30  NVIDIA_FAILURE_POLICY=wait-then-fallback
SECONDARY / FAILOVER: support an optional NVIDIA_SECONDARY_MODEL. On a primary-model failure
(timeout, 429, 529, or malformed result) within the retry budget, if NVIDIA_SECONDARY_MODEL is
NON-EMPTY, try it once at the same endpoint; if it is EMPTY, SKIP hosted failover and go
straight to the local deterministic AI. Keep the secondary code path present but inert when the
name is empty. Do NOT reuse ChatDev's build-time retry values (dozens of attempts, 20-minute
delays, multi-hour windows) at game time — during a match use at most 2-3 attempts and a ~15 s
total budget, then fall back locally. The local deterministic AI is authoritative whenever NIM
is unavailable; the game must never stall on the network. All values are env-overridable.
NVIDIA_API_KEY is required for live decisions; NVIDIA_MODEL overrides the default. Keep the
default in non-secret typed config (changeable without code edits). If the key is missing or
the model is rejected/unavailable, start normally, warn clearly, and use fallback. Never log/
display the key, bearer header, full environment, or secrets; redact them in exceptions.

8.2 URL AND RESPONSE PARSING — CORRECTNESS (previous builds shipped two silent, show-stopping
defects here; both are forbidden and MUST have regression tests):
  (a) URL: the effective request URL MUST be the configured base URL WITH its full path
      preserved, plus "/chat/completions" — i.e. https://integrate.api.nvidia.com/v1/chat/
      completions. Do NOT drop the "/v1" segment. With System.Net.Http this happens when
      HttpClient.BaseAddress lacks a trailing slash and the request path starts with "/"
      (a leading-slash path is resolved from the host root, discarding "/v1"). Build the URL
      safely: post to an absolute URL string composed as baseUrl.TrimEnd('/') +
      "/chat/completions" (or set BaseAddress ending in "/v1/" and use a relative path with NO
      leading slash). A request that 404s because "/v1" was dropped is a defect.
  (b) RESPONSE: the HTTP body is an OpenAI-style envelope
      {"choices":[{"message":{"content":"<the move JSON as a string>"}}]}. Parse
      choices[0].message.content FIRST, then parse the strict move JSON from that content
      string. Do NOT deserialize the raw envelope into the move DTO (it has no top-level
      moveId, so the move would always be empty and the game would silently fall back).
  (c) NO-SILENT-FALLBACK: when a valid key and model are present the app MUST actually use
      model decisions. Log, per turn, whether the move came from NIM or fallback, and expose a
      visible per-player "last decision source" indicator. Acceptance requires demonstrating
      real NIM-driven moves (not 100% fallback) in a live check.

8.3 Request timing/content. One NIM request per AI roll that has >=1 legal move (even a single
forced move). No request when: game over, no legal move, third-six forfeit, cancellation
requested, or stale request identity. Send compact JSON: GameId/TurnId/RollId/RequestId/
PlayerId/color; strategy hint; revealed die value and consecutive-six count; concise token
positions for all players; safe-square and blockade info; recent public events; the complete
engine-generated legal-move list; a strict response contract. Never send secrets, drawing
coordinates, full logs, hidden chain-of-thought, or another player's private history.
Example legal move:
  {"moveId":"red-token-2:18->24","tokenId":"red-token-2","from":"track:18","to":"track:24",
   "entersBoard":false,"captures":["green-token-1"],"landsSafe":false,"finishes":false,
   "formsBlockade":false}
Required response (exactly one compact object):
  {"moveId":"red-token-2:18->24","reason":"Captures an exposed opponent while advancing."}
Use stream=false, temperature 0, top_p 1, and a small output limit (max_tokens ~64) in greedy /
non-reasoning mode — the recommended setting for these small models. The reply is a tiny JSON
object, not an essay. (If a reasoning-capable model is configured instead, raise max_tokens
enough that it finishes reasoning AND still emits the JSON, e.g. 512-1024, or it returns empty
content and forces fallback.) Keep the request compact (< ~1500 tokens): color, die, consecutive-
six count, token positions, safe squares, blockades, the legal-move list, and a short strategy
hint — nothing else. Do NOT expose callable tools/functions to the model; require plain JSON
content parsed with System.Text.Json (strip harmless Markdown fences). Require the exact moveId
(+ optional short reason); reject prose-only output, multiple objects, unknown fields, unknown
MoveIds, altered state, commands, or code. One primary request and at most one repair request
per eligible roll
(repair carries only the validation error, allowed MoveIds, and exact shape); if repair fails,
use fallback. Treat reason as untrusted display text (escape, normalize control chars, cap 160
chars). Before applying a response verify GameId/TurnId/RollId/PlayerId/RequestId; discard
stale responses without mutating state.

9. LOCAL FALLBACK AI
A complete deterministic local AI that can finish a game without NIM, choosing only from
engine-generated legal moves. Documented scoring/priority considering: immediate victory;
finishing a token; capturing; escaping capture risk; landing safe; entering a yard token;
forming/preserving a useful blockade; breaking a harmful one; forward progress/distance to
home; nearby opponents/avoidable exposure; keeping >1 useful active token. Deterministic
tie-break (token ID then MoveId; no randomness). Label every fallback decision "Local fallback
AI"; never present it as an NVIDIA decision.

10. RETRY, THROTTLING, CIRCUIT BREAKER
One cancellation-aware async request gate so only one live NIM request/retry runs at a time
(prevent a four-player stampede); apply the minimum interval; release the gate on every
success/exception/timeout/cancel. Retry transient failures: HTTP 408,425,429,500,502,503,504,
529; connection reset; temporary DNS/network failure; non-cancellation timeout. Do not
normally retry 400/401/402/403/404(bad config)/422; treat 401/402/403 and model-not-found as
config/access failures — disable live NIM for the process (until config reload), warn safely,
use fallback. Honor Retry-After (delta-seconds or HTTP-date; support >10 min; never retry
earlier than requested; a past HTTP-date = no server delay). Local schedule when no valid
Retry-After: ~15,30,60,120,240,480,720,900 s, plus nonnegative jitter up to ~20%, capped at
NVIDIA_MAX_RETRY_DELAY_SECONDS; when Retry-After is valid use the greater of server delay and
capped local delay (do not cap a valid server delay). Honor a delay only within the remaining
NVIDIA_MAX_RETRY_ELAPSED_SECONDS budget (elapsed includes HTTP attempts + local + server
waits); if it exceeds the budget, open the circuit and use fallback rather than violating
Retry-After. Use TimeProvider/monotonic time so waits/countdowns are testable. During waits
keep the UI responsive showing player name/color, a safe category (e.g. "Service busy"),
attempt number, time to next retry, total elapsed, that the game is active, and the QUIT
button. QUIT must promptly cancel an in-flight request or a 10-30 min wait (fake-time tests
verify cancellation without waiting out the delay). After the budget is exhausted, pick a
fallback move and continue; open the circuit for NVIDIA_CIRCUIT_BREAKER_SECONDS; then allow
one half-open probe on a later eligible turn; close on success or reopen on transient failure.

11. AVALONIA UI — polished, responsive, and fully automatic once START GAME is pressed.
11.0 BRANDING (mandatory, both screens): the top-left header AND the window title read
exactly "SANYALnet Labs Ludo AI Arena". A small, unobtrusive copyright notice is fixed at the
BOTTOM-RIGHT of both the setup and game screens, reading exactly "(C) Supratim Sanyal + AI
Agents" (the © glyph is acceptable). Neither the header nor the copyright may overlap or shift
the board or status panels at any window size.
11.1 Setup screen: header/title as above; "Four AI players" subtitle; editable Red/Green/
Yellow/Blue names; the four strategy hints; rules-profile name + Rules button; NVIDIA key-
present status (without the key); NVIDIA model+endpoint status; a warning when fallback will
be used; START GAME and QUIT; bottom-right copyright. No human/player-count controls.
11.2 Game screen: a square central Ludo board; four player status cards; an animated die area;
current phase, active player, revealed value, consecutive-six count, bonus-roll status; NIM
request/retry/fallback/circuit status; a concise scrollable event log; an always-visible QUIT;
bottom-right copyright. Each card shows name, color, strategy, active indicator, tokens in
yard/on track/in home lane/finished, current AI state, and whether the last move came from NIM
or fallback (do not rely on color alone).
11.3 Board rendering: Avalonia vector drawing (no bitmap board); correct 15x15 geometry — four
yards, 52 shared cells, colored start cells, star safe cells, four five-cell home lanes, center
triangles — consuming the authoritative coordinate table. Separate logical positions from pixel
coordinates. LAYOUT STABILITY (mandatory): the board keeps a fixed, square size and position
throughout a game; NO other element (die highlight, status text, event log growth, retry
countdown) may resize or reflow the board. Reserve fixed space for the die/status area so its
content changes never change the board's measured size. Support high DPI; cache brushes/pens/
text/paths where practical; no clipping/overlap at compact or large sizes. Tokens have
outlines, subtle shadows, and a number/shape id; offset multiple tokens on one safe cell;
distinguish active/moving/finished/captured without relying on color alone.
11.4 Animation.
  Die: correct pip layouts 1-6; a polished roll/tumble/bounce/easing; rapid cosmetic
  intermediate faces; normal duration ~700-1100 ms; final face exactly equals the stored
  value; no UI-thread blocking; a reduced-motion mode that still visibly reveals a roll.
  DIE HIGHLIGHT — FLASH, NEVER RESIZE (mandatory): to emphasize the revealed number, FLASH the
  die display (e.g. a brief 2-3x opacity/background/foreground pulse) WITHOUT changing the die
  control's size, width, height, measured bounds, or layout slot. Never enlarge the die to
  show the result — that reflows the board and makes it jitter. If any scale effect is used it
  must be a render-only RenderTransform (ScaleTransform) that does not trigger layout, inside a
  fixed-size container; the preferred solution is a non-resizing flash.
  Token movement — SMOOTH GLIDE, NEVER TELEPORT (mandatory): on every move the piece visibly
  travels from its origin to its destination by INTERPOLATING between consecutive cell centres
  (several sub-steps per cell), so it glides rather than jumps. This applies to EVERY color and
  EVERY move type, INCLUDING single-cell entry moves (yard->start) and home-lane/finishing
  moves — none may teleport. Pace is relaxed: about 300-350 ms per cell. While a piece is in
  flight it must carry a gentle PULSING highlight (e.g. a ring/glow at a few Hz, not a rapid
  flash) so the viewer can follow which piece is moving; captures animate the captured token
  back to its yard. Provide brief tasteful effects for entry, capture, safe landing, blockade,
  home-lane entry, finish, and victory; a configurable animation-speed multiplier; and a small
  inter-turn pause.
  IMPLEMENTATION NOTE (custom-drawn board): if the board is a custom Control, it MUST be
  invalidated/redrawn on EVERY animation frame (e.g. subscribe to the token collection's change
  notifications, or bump a repaint trigger, then call InvalidateVisual) — do not rely on
  incidental repaints from other bindings, or the glide will not render.
11.5 Quit/game-over: in-game QUIT confirms, then cancels animations/HTTP/retry waits/
countdowns/queued requests/state machine before closing; dispose resources; no unobserved task
exceptions. On victory, stop normal processing and show the winner's name/color, final stats, a
restrained celebration, NEW GAME, and QUIT. NEW GAME returns to setup, retains safe name/config
values, creates fresh game/AI-session identifiers, and resets all state. No NEW GAME control
during an active game.
11.6 Accessibility: keyboard navigation, visible focus, Enter/Space activation, accessible
control names, text in addition to color, readable contrast, scalable text, reduced motion, no
rapid flashing, clear disabled states, and safe user-facing errors (no raw stack traces).

12. CONCURRENCY, SECURITY, LOGGING
CancellationToken through the game loop, animation, NIM client, retry, gate, countdown, and
shutdown; never block the UI thread; prevent overlapping rolls/turns/animations/decisions/move-
application. Only one authoritative command resolves a roll; an illegal or stale move fails with
no mutation; the game-over transition is atomic. Structured logging for startup/shutdown, game/
turn ids, state transitions, die values, animation completion, legal-move count, safe AI request
metadata, HTTP status, retry delay, circuit state, fallback, moves, captures, finishes, winner,
cancellation, exceptions. Never log the key, Authorization header, full environment, raw prompts
by default, or unsanitized model output; redact secrets; never execute model-provided code.

13. TESTS — deterministic, never call live NIM, never real-time sleep for long delays; use fake
HTTP handlers, scripted IDieRoller, seeded RNG, fake TimeProvider, fake animation completion.
All test projects MUST compile and pass.
13.1 Core rules: fixed four-player order; animated roll-off + tied rerolls; every offset/route
rotation/shared coordinate/home-lane coordinate/center transition; 52 shared + 5 home + progress
57; safe indices 0,8,13,21,26,34,39,47; six-to-enter and entry at progress 0; entry-vs-move
choice; exact move and overshoot rejection; capture+return; safe immunity and mixed-color safe
occupancy; blockade create/passage/split/own-passage/opponent-block/third-token-rejection; six/
capture/finish bonus and non-stacking; consecutive-six reset after a non-six bonus continuation;
third-six forfeit; no-legal-move on six and non-six; exact finish, finished-token immobility,
immediate victory, no turn after victory; full new-game reset.
13.2 Sequence/invariant: value generated before animation; AI cannot receive it before
completion+reveal; legal moves/NIM only after reveal; every bonus gets a new value + full
animation; no-move and third-six make no NIM request; token animation begins only after
validation; cancellation during animation/HTTP/retry/movement stops safely; stale ids rejected;
one valid location per token; no token in another color's home lane; finished tokens never move;
no illegal stacks/overshoots; illegal commands do not mutate; only the active player acts; at
most one winner.
13.3 Simulation/autoplay: >=1,000 deterministic four-player fallback games over multiple seeds,
invariants checked after each move, each ending with one winner within a documented generous
safety limit (a limit hit is a failure to investigate, not a draw); record count, seed range,
max/avg rolls, winner distribution, failures. Plus an accelerated app-level autoplay (fake
animation, scripted/fallback AI) driving setup->roll-off->turns->bonus->movement->victory->game-
over->NEW GAME with no gameplay input.
13.4 NIM/resilience: correct base URL, PATH-PRESERVING full request URL (assert the effective
URL is .../v1/chat/completions, i.e. "/v1" is not dropped), bearer auth, model, JSON request;
ENVELOPE PARSING — given a realistic {"choices":[{"message":{"content":"{...moveId...}"}}]}
body, the parser extracts choices[0].message.content and returns the correct move (a regression
test for both prior defects); secret redaction; four isolated sessions; request only after
reveal incl. one forced move; valid/fenced/malformed JSON, unknown MoveId, one repair, fallback
after repair; safe reason rendering + length cap; transient retry for 408/425/429/500/502/503/
504/529; no ordinary retry for 400/401/402/403/bad-404/422; Retry-After delta and HTTP-date; a
simulated 12-min Retry-After; backoff progression, nonnegative jitter, per-delay cap, total-time
cap, and Retry-After exceeding the budget; single-flight, min interval, gate release, circuit
open/half-open/close/reopen; cancellation during HTTP and a fake 12-min wait; stale-response
rejection after shutdown or new game.
13.5 Avalonia headless/rendered-frame: use the supported headless integration; enable Skia for
pixel tests. Cover setup, game start, roll-off, active player, mid-roll, final die face, AI
waiting, retry countdown, board layout, token offsets, capture, home lane, game over, NEW GAME,
QUIT, keyboard nav, reduced motion, compact and large windows. Additionally assert: the header
text "SANYALnet Labs Ludo AI Arena" is present; the bottom-right copyright is present; the
board's size/position is UNCHANGED before/during/after a die highlight (layout-stability test);
and a token move visits intermediate cells (smooth-travel test, e.g. via animation keyframes or
sampled positions). Save representative PNGs and inspect pixels (board squareness, alignment,
center geometry, pips, text, no token clipping, safe stars, cards, active indicator, retry
display, QUIT). If no agent can actually view images, say so; do not label metadata-only checks
as visual inspection.
13.6 Optional live NIM smoke test: normal tests never call NVIDIA; permit one opt-in synthetic
decision only when ALLOW_LIVE_NIM_TEST=1 and valid inherited config exist — strict timeout, tiny
legal-move list, no full game, redacted logs.

14. BUILD, RUN, DELIVERY VERIFICATION
Create executable scripts/launch-ludo.sh (strict shell options; locate the solution relative to
itself; verify dotnet and .NET 10; preserve inherited env; never print secrets; launch the app;
return its exit status; handle Ctrl+C). Run and record real output + exit code of:
  dotnet restore LudoNimArena.sln
  dotnet build LudoNimArena.sln -c Release --no-restore
  dotnet test LudoNimArena.sln -c Release --no-build
  dotnet publish src/LudoNimArena.App/LudoNimArena.App.csproj -c Release -r linux-x64 --self-contained false
GUI smoke test: timeout 30s ./scripts/launch-ludo.sh (or timeout 30s xvfb-run -a ./scripts/
launch-ludo.sh if no display). A deliberate timeout counts only if logs prove the app started
without a startup exception and stayed alive to the timeout. Never call an unknown/failed
timeout a success.

15. DOCUMENTATION (must match delivered code, commands, defaults, counts)
README.md: scope, four-AI limitation, features, prerequisites, build, run, env vars, NIM+
fallback behavior, test commands, structure, screenshots/rendered artifacts, security, known
limitations; state human players are a future revision. RULES.md: exact track, offsets, safe
indices, routes, entry, movement, captures, safe occupancy, blockades, three sixes, non-stacking
bonuses, home entry, exact finish, victory. ARCHITECTURE.md: project boundaries, state machine,
roll-before-AI sequence, RNG/time abstractions, command/event flow, per-player sessions, request
identity, retry/circuit flow, cancellation, rendering, the future human-controller extension
point, AND the NIM URL/response-parsing contract from section 8.2. TESTING.md: unit tests,
simulations, fake time/HTTP, headless UI, rendered pixels, optional live NIM test, reproducing
failures. manual.md: names, starting, automatic turns, rules, NIM variables, fallback, long
retry countdowns, QUIT, game over, NEW GAME, logs, troubleshooting. BUILD_NOTES.md: inspected
environment, SDK + pinned package versions, commands executed, actual results, test totals,
simulation statistics, smoke-test result, rendered-frame checks, defects found and corrected,
honest limitations. Never fabricate counts or successful checks.

16. FINAL REVIEW AND ACCEPTANCE
Separate product, rules, sequence, concurrency, resilience, security, visual, and documentation
reviews; correct findings, rerun affected tests, repeat; completion requires two successive full
passes with no new defect. Complete only when all applicable items are true:
- Exactly four autonomous AI players; no human-play controls. C#/net10.0; no Python product.
- The whole solution builds with 0 errors; ALL test projects compile; Release build and tests
  pass (or exact unresolvable environment blockers are reported without false success).
- Headers on both screens read "SANYALnet Labs Ludo AI Arena"; the bottom-right copyright
  "(C) Supratim Sanyal + AI Agents" appears on both screens.
- The board is correctly rendered, responsive, and NEVER changes size/position during play
  (die highlight flashes, does not resize; nothing reflows the board).
- Every roll-off/normal/bonus roll is visibly animated; the AI receives the value only after
  animation completion + reveal.
- Tokens ALWAYS glide smoothly (interpolated, relaxed ~300-350 ms/cell) for every color and
  every move type incl. single-cell entry moves — never teleporting; the in-flight piece is
  visibly highlighted while it travels (verified).
- Legal moves are generated/validated only by the engine; each color has an isolated session
  and every eligible roll gets its own NIM request.
- The NIM request URL preserves "/v1" (no 404) and the response parser reads
  choices[0].message.content; with a valid key+model the game demonstrably makes REAL NIM-driven
  moves (not 100% fallback), with a visible per-player decision-source indicator.
- Game runtime uses a small free-tier model (default nvidia/nemotron-mini-4b-instruct) at
  temperature 0 / max_tokens ~64, spaces requests >= 3 s apart (<= ~20 RPM), retries at most
  2-3 times within ~45 s, then uses the authoritative local fallback; it never freezes on the
  network.
- NIM failure, HTTP 529, Retry-After >10 min, cancellation, and circuit breaking work without
  freezing the UI; local fallback can finish a full game.
- >=1,000 deterministic simulations pass all invariants.
- QUIT works during normal play, die animation, token animation, HTTP, and long retry waits;
  victory stops play and offers NEW GAME and QUIT.
- Rendered-frame checks were performed and accurately reported; docs match code/commands/
  defaults/test counts; no credential/secret appears anywhere.
End with a concise report: (1) project location; (2) implemented features; (3) confirmation of
four AI players and no human mode; (4) build/publish results; (5) test totals + simulation
stats; (6) GUI and rendered-frame verification (incl. real-NIM-move demonstration, header,
copyright, board-stability, smooth-travel); (7) exact launch command; (8) required NVIDIA env
var names (never values); (9) honest remaining limitations.
