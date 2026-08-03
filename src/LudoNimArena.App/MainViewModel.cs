// ============================================================================
// Copyright (c) 2026 Supratim Sanyal of SANYALnet Labs.
// Proprietary rights reserved except as expressly licensed herein.
//
// LUDO ARENA
// This file is governed by the SANYALnet Labs Non-Commercial License in the
// root LICENSE file. Non-Commercial use is permitted; Commercial Use and use
// for AI/ML model training are prohibited unless separately authorized.
//
// Attribution is required: "Based on original work by Supratim Sanyal of
// SANYALnet Labs." See LICENSE for full terms, warranty disclaimer, termination,
// patent, trademark, and governing-law provisions.
// ============================================================================

using System.Collections.Immutable;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LudoNimArena.AI;
using LudoNimArena.Core;

namespace LudoNimArena.App;

public partial class MainViewModel : ObservableObject
{
    private readonly NimSettings _nimSettings;
    private readonly RulesEngine _engine;
    private readonly IDieRoller _dieRoller;

    private GameState? _gameState;
    private ImmutableDictionary<PlayerColor, AiPlayerSession>? _sessions;
    private CancellationTokenSource? _gameCts;

    // ---------------------------------------------------------------------
    // Unattended / CI play support. All opt-in via the environment; with none
    // of these set the game behaves exactly as it does for a human player.
    //
    //   LUDO_AUTOSTART=1        press START GAME automatically (see MainWindow)
    //   LUDO_SPEED=<multiplier> animation speed; 1 = normal, 50 = 50x faster
    //   LUDO_TRANSCRIPT=<path>  append every event-log line to a file
    //   LUDO_EXIT_ON_GAMEOVER=1 close the app once a winner is declared
    // ---------------------------------------------------------------------
    private static readonly double AnimationSpeed = ParseAnimationSpeed();
    private static readonly string? TranscriptPath =
        Environment.GetEnvironmentVariable("LUDO_TRANSCRIPT");
    private static readonly bool ExitOnGameOver =
        Environment.GetEnvironmentVariable("LUDO_EXIT_ON_GAMEOVER") == "1";

    private static double ParseAnimationSpeed()
    {
        var raw = Environment.GetEnvironmentVariable("LUDO_SPEED");
        if (double.TryParse(raw, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var v) && v > 0)
            return v;
        return 1.0;
    }

    /// <summary>Scales a human-paced delay by the configured animation speed.</summary>
    private static Task DelayAsync(int milliseconds, CancellationToken ct)
    {
        if (AnimationSpeed <= 1.0) return Task.Delay(milliseconds, ct);
        int scaled = (int)Math.Round(milliseconds / AnimationSpeed);
        return Task.Delay(Math.Max(0, scaled), ct);
    }

    private static void WriteTranscript(string line)
    {
        if (string.IsNullOrEmpty(TranscriptPath)) return;
        try { File.AppendAllText(TranscriptPath, line + Environment.NewLine); }
        catch { /* a transcript failure must never break the game */ }
    }

    [ObservableProperty] private string _title = "SANYALnet Labs Ludo AI Arena";
    [ObservableProperty] private string _subtitle = "Four AI Players";
    [ObservableProperty] private string _statusMessage = "Ready";
    [ObservableProperty] private string _phaseMessage = "";
    [ObservableProperty] private bool _isGameRunning;
    [ObservableProperty] private bool _isSetupVisible = true;
    [ObservableProperty] private bool _isGameOver;
    [ObservableProperty] private string _winnerMessage = "";

    // Player names (editable in setup)
    [ObservableProperty] private string _redName = "HAL 9000";
    [ObservableProperty] private string _greenName = "Marvin";
    [ObservableProperty] private string _yellowName = "Mal";
    [ObservableProperty] private string _blueName = "Deckard";

    // Die display
    [ObservableProperty] private int _dieValue = 1;
    [ObservableProperty] private bool _isDieRolling;
    [ObservableProperty] private string _dieAnimationClass = "";
    [ObservableProperty] private bool _dieHighlighted;

    // Player status
    [ObservableProperty] private string _redStatus = "Waiting";
    [ObservableProperty] private string _greenStatus = "Waiting";
    [ObservableProperty] private string _yellowStatus = "Waiting";
    [ObservableProperty] private string _blueStatus = "Waiting";

    [ObservableProperty] private bool _isRedActive;
    [ObservableProperty] private bool _isGreenActive;
    [ObservableProperty] private bool _isYellowActive;
    [ObservableProperty] private bool _isBlueActive;

    // Token positions (for rendering)
    [ObservableProperty] private ObservableCollection<TokenDisplayInfo> _tokens = new();

    // In-flight token overlay: BoardControl draws this piece at a fractional board cell,
    // with a flashing ring, so it glides smoothly between cells (never teleports).
    private string? _animTokenId;
    public bool MovingActive { get; private set; }
    public PlayerColor MovingColor { get; private set; }
    public int MovingIndex { get; private set; }
    public double MovingRow { get; private set; }
    public double MovingCol { get; private set; }
    public bool MovingFlash { get; private set; }

    // Event log
    [ObservableProperty] private ObservableCollection<string> _eventLog = new();

    // NIM status
    [ObservableProperty] private string _nimStatus = "";
    [ObservableProperty] private bool _nimApiKeyPresent;

    public MainViewModel()
    {
        _engine = new RulesEngine();
        _dieRoller = new CryptoDieRoller();
        _nimSettings = LoadNimSettings();
        _nimApiKeyPresent = _nimSettings.HasApiKey;
    }

    private static NimSettings LoadNimSettings()
    {
        return new NimSettings
        {
            ApiKey = Environment.GetEnvironmentVariable("NVIDIA_API_KEY") ?? "",
            Model = Environment.GetEnvironmentVariable("NVIDIA_MODEL") ?? "nvidia/llama-3.3-nemotron-super-49b-v1.5",
            BaseUrl = Environment.GetEnvironmentVariable("NVIDIA_BASE_URL") ?? "https://integrate.api.nvidia.com/v1",
            RequestTimeoutSeconds = int.TryParse(Environment.GetEnvironmentVariable("NVIDIA_REQUEST_TIMEOUT_SECONDS"), out var t) ? t : 90,
            MaxRetryDelaySeconds = int.TryParse(Environment.GetEnvironmentVariable("NVIDIA_MAX_RETRY_DELAY_SECONDS"), out var mrd) ? mrd : 1800,
            MaxRetryElapsedSeconds = int.TryParse(Environment.GetEnvironmentVariable("NVIDIA_MAX_RETRY_ELAPSED_SECONDS"), out var mre) ? mre : 3600,
            MinCallIntervalSeconds = int.TryParse(Environment.GetEnvironmentVariable("NVIDIA_MIN_CALL_INTERVAL_SECONDS"), out var mci) ? mci : 5,
            CircuitBreakerSeconds = int.TryParse(Environment.GetEnvironmentVariable("NVIDIA_CIRCUIT_BREAKER_SECONDS"), out var cb) ? cb : 300,
            FailurePolicy = Environment.GetEnvironmentVariable("NVIDIA_FAILURE_POLICY") ?? "wait-then-fallback"
        };
    }

    [RelayCommand]
    private async Task StartGame()
    {
        IsSetupVisible = false;
        IsGameRunning = true;
        IsGameOver = false;
        EventLog.Clear();
        Tokens.Clear();
        SetAllActive(false);

        _gameCts = new CancellationTokenSource();

        // Create sessions
        var httpClient = new HttpClient();
        var fallbackAi = new LocalFallbackAi();

        _sessions = ImmutableDictionary<PlayerColor, AiPlayerSession>.Empty
            .Add(PlayerColor.Red, new AiPlayerSession(_nimSettings, httpClient, fallbackAi, PlayerColor.Red, "assertive but legal"))
            .Add(PlayerColor.Green, new AiPlayerSession(_nimSettings, httpClient, fallbackAi, PlayerColor.Green, "safety-conscious"))
            .Add(PlayerColor.Yellow, new AiPlayerSession(_nimSettings, httpClient, fallbackAi, PlayerColor.Yellow, "progress-focused"))
            .Add(PlayerColor.Blue, new AiPlayerSession(_nimSettings, httpClient, fallbackAi, PlayerColor.Blue, "balanced"));

        try
        {
            await RunGameLoopAsync(_gameCts.Token);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Game cancelled";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsGameRunning = false;
            CleanupSessions();
        }
    }

    private async Task RunGameLoopAsync(CancellationToken ct)
    {
        // Initialize game
        _gameState = new GameState();

        var gameId = _gameState.GameId;

        var players = ImmutableDictionary<PlayerColor, Player>.Empty
            .Add(PlayerColor.Red, new Player(PlayerColor.Red, RedName, "assertive but legal"))
            .Add(PlayerColor.Green, new Player(PlayerColor.Green, GreenName, "safety-conscious"))
            .Add(PlayerColor.Yellow, new Player(PlayerColor.Yellow, YellowName, "progress-focused"))
            .Add(PlayerColor.Blue, new Player(PlayerColor.Blue, BlueName, "balanced"));

        _gameState = _gameState.WithPlayers(players)
            .WithEvent(new GameStarted(gameId));

        AddLog("Game started!");

        // Roll-off to determine first player
        var firstPlayer = await DetermineFirstPlayerAsync(ct);
        _gameState = _gameState.WithCurrentPlayer(firstPlayer)
            .WithPhase(GamePhase.PreparingTurn);

        AddLog($"{_gameState.GetPlayer(firstPlayer).DisplayName} goes first!");

        // Main game loop
        while (!ct.IsCancellationRequested && _gameState.Winner == null)
        {
            await ProcessTurnAsync(ct);
            if (_gameState.Winner != null) break;
            _gameState = _gameState.WithCurrentPlayer(_engine.GetNextPlayer(_gameState.CurrentPlayer));
        }

        // Game over
        if (_gameState.Winner.HasValue)
        {
            var winner = _gameState.GetPlayer(_gameState.Winner.Value);
            IsGameOver = true;
            WinnerMessage = $"{winner.DisplayName} ({_gameState.Winner}) wins!";
            StatusMessage = WinnerMessage;

            // Machine-checkable completion record for unattended/CI runs.
            WriteTranscript($"WINNER: {winner.DisplayName} ({_gameState.Winner})");
            WriteTranscript($"TURNS: {_gameState.TurnNumber}");
            WriteTranscript("GAME COMPLETE");
        }

        UpdateTokenDisplay();

        if (IsGameOver && ExitOnGameOver)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (Avalonia.Application.Current?.ApplicationLifetime
                    is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                    desktop.Shutdown(0);
            });
        }
    }

    private async Task<PlayerColor> DetermineFirstPlayerAsync(CancellationToken ct)
    {
        _gameState = _gameState!.WithIsRollOff(true);
        var results = new Dictionary<PlayerColor, int>();

        foreach (var color in RulesEngine.TurnOrder)
        {
            await HighlightPlayerAsync(color);
            StatusMessage = $"{_gameState.GetPlayer(color).DisplayName} rolls for start...";

            int roll = _dieRoller.Roll(1, 7);
            await AnimateDieAsync(roll, ct);
            results[color] = roll;
            _gameState = _gameState.WithRollOffResult(color, roll)
                .WithEvent(new StartingRollCompleted(_gameState.GameId, color, roll));
            AddLog($"{_gameState.GetPlayer(color).DisplayName} rolled {roll}");
        }

        // Find highest
        int maxRoll = results.Values.Max();
        var tiedPlayers = results.Where(r => r.Value == maxRoll).Select(r => r.Key).ToList();

        // Tiebreak if needed
        while (tiedPlayers.Count > 1)
        {
            AddLog($"Tie at {maxRoll}! Rerolling: {string.Join(", ", tiedPlayers)}");
            var newResults = new Dictionary<PlayerColor, int>();

            foreach (var color in tiedPlayers)
            {
                await HighlightPlayerAsync(color);
                StatusMessage = $"{_gameState.GetPlayer(color).DisplayName} tiebreak roll...";

                int roll = _dieRoller.Roll(1, 7);
                await AnimateDieAsync(roll, ct);
                newResults[color] = roll;
                _gameState = _gameState.WithRollOffResult(color, roll);
                AddLog($"{_gameState.GetPlayer(color).DisplayName} rolled {roll} (tiebreak)");
            }

            maxRoll = newResults.Values.Max();
            tiedPlayers = newResults.Where(r => r.Value == maxRoll).Select(r => r.Key).ToList();
        }

        _gameState = _gameState.WithIsRollOff(false)
            .WithEvent(new StartingPlayerSelected(_gameState.GameId, tiedPlayers[0]));

        return tiedPlayers[0];
    }

    private async Task ProcessTurnAsync(CancellationToken ct)
    {
        var color = _gameState!.CurrentPlayer;
        var turnId = Guid.NewGuid();
        _gameState = _gameState.WithCurrentTurnId(turnId)
            .WithTurn(_gameState.TurnNumber + 1)
            .WithConsecutiveSixCount(0)
            .WithBonusRoll(false);

        await HighlightPlayerAsync(color);
        UpdateTokenDisplay();

        _gameState = _gameState.WithEvent(new TurnStarted(_gameState.GameId, turnId, color, _gameState.TurnNumber));
        StatusMessage = $"{_gameState.GetPlayer(color).DisplayName}'s turn";
        AddLog($"Turn {_gameState.TurnNumber}: {_gameState.GetPlayer(color).DisplayName}");

        bool continueTurn = true;
        while (continueTurn && !ct.IsCancellationRequested && _gameState.Winner == null)
        {
            // Generate die result
            PhaseMessage = "Rolling...";
            int dieResult = _dieRoller.Roll(1, 7);
            var rollId = Guid.NewGuid();
            _gameState = _gameState.WithCurrentRollId(rollId)
                .WithEvent(new DieResultGenerated(_gameState.GameId, turnId, rollId, color, dieResult, _gameState.IsBonusRoll));

            // Animate die
            StatusMessage = $"{_gameState.GetPlayer(color).DisplayName} is rolling...";
            await AnimateDieAsync(dieResult, ct);

            _gameState = _gameState.WithLastDieResult(dieResult)
                .WithEvent(new DieAnimationCompleted(_gameState.GameId, rollId))
                .WithEvent(new DieResultRevealed(_gameState.GameId, turnId, rollId, color, dieResult));

            StatusMessage = $"{_gameState.GetPlayer(color).DisplayName} rolled {dieResult}";
            AddLog($"Rolled {dieResult}");

            // Check third six rule
            if (dieResult == 6)
            {
                _gameState = _gameState.WithConsecutiveSixCount(_gameState.ConsecutiveSixCount + 1);
                if (_gameState.ConsecutiveSixCount >= 3)
                {
                    AddLog("Third consecutive six! Turn forfeited.");
                    _gameState = _gameState.WithEvent(new ThirdSixForfeited(_gameState.GameId, turnId, color))
                        .WithEvent(new TurnEnded(_gameState.GameId, turnId, color, _gameState.TurnNumber));
                    StatusMessage = $"{_gameState.GetPlayer(color).DisplayName} forfeits turn (three sixes)";
                    return;
                }
            }
            else
            {
                _gameState = _gameState.WithConsecutiveSixCount(0);
            }

            // Generate legal moves
            PhaseMessage = "Generating moves...";
            var legalMoves = _engine.GenerateLegalMoves(_gameState, color, dieResult);
            _gameState = _gameState.WithLegalMoves(legalMoves)
                .WithEvent(new LegalMovesGenerated(_gameState.GameId, turnId, legalMoves.Count));

            if (legalMoves.Count == 0)
            {
                AddLog($"No legal moves for {dieResult}");
                if (dieResult == 6 && _gameState.ConsecutiveSixCount < 3)
                {
                    _gameState = _gameState.WithBonusRoll(true)
                        .WithEvent(new BonusRollAwarded(_gameState.GameId, turnId, color, "Six with no move"));
                    continue; // bonus roll for six
                }
                _gameState = _gameState.WithEvent(new TurnEnded(_gameState.GameId, turnId, color, _gameState.TurnNumber));
                return;
            }

            // Get AI decision
            PhaseMessage = "AI thinking...";
            StatusMessage = $"{_gameState.GetPlayer(color).DisplayName} is choosing...";

            var session = _sessions![color];
            var requestId = Guid.NewGuid();
            _gameState = _gameState.WithEvent(new AiDecisionRequested(_gameState.GameId, turnId, rollId, color, requestId));

            string selectedMoveId;
            try
            {
                var (moveId, reason, isFallback) = await session.RequestMoveAsync(
                    _gameState, legalMoves, dieResult, requestId, ct);

                selectedMoveId = moveId;

                if (isFallback)
                {
                    _gameState = _gameState.WithEvent(
                        new FallbackDecisionSelected(_gameState.GameId, turnId, color, moveId));
                    AddLog($"{_gameState.GetPlayer(color).DisplayName}: {reason}");
                }
                else
                {
                    _gameState = _gameState.WithEvent(
                        new AiDecisionReceived(_gameState.GameId, turnId, rollId, color, moveId, reason));
                    AddLog($"{_gameState.GetPlayer(color).DisplayName}: {reason}");
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                AddLog($"AI error: {ex.Message}, using fallback");
                var fallback = new LocalFallbackAi().SelectMove(_gameState, color, legalMoves);
                selectedMoveId = fallback.MoveId;
                _gameState = _gameState.WithEvent(
                    new FallbackDecisionSelected(_gameState.GameId, turnId, color, fallback.MoveId));
            }

            // Validate and apply move
            var selectedMove = legalMoves.FirstOrDefault(m => m.MoveId == selectedMoveId);
            if (selectedMove == null)
            {
                AddLog($"Invalid move {selectedMoveId}, using fallback");
                var fallback = new LocalFallbackAi().SelectMove(_gameState, color, legalMoves);
                selectedMove = fallback;
            }

            PhaseMessage = "Animating...";
            StatusMessage = $"{_gameState.GetPlayer(color).DisplayName} moving {selectedMove.TokenId}...";
            await AnimateTokenMoveAsync(selectedMove, color, ct);

            var (newState, events) = _engine.ApplyMove(_gameState, selectedMove, color);
            _gameState = newState;

            foreach (var evt in events)
            {
                AddLog(FormatEvent(evt));
            }

            UpdateTokenDisplay();

            if (_gameState.Winner.HasValue)
                return;

            // Determine bonus roll
            bool hasCapture = selectedMove.Captures.Any();
            bool isSix = dieResult == 6;
            bool finishes = selectedMove.Finishes;

            if ((isSix || hasCapture || finishes) && _gameState.ConsecutiveSixCount < 3)
            {
                string reason = isSix ? "rolled six" : hasCapture ? "captured" : "finished token";
                _gameState = _gameState.WithBonusRoll(true)
                    .WithEvent(new BonusRollAwarded(_gameState.GameId, turnId, color, reason));
                AddLog($"Bonus roll: {reason}");
                continueTurn = true;
            }
            else
            {
                continueTurn = false;
                _gameState = _gameState.WithEvent(new TurnEnded(_gameState.GameId, turnId, color, _gameState.TurnNumber));
            }
        }
    }

    private async Task AnimateDieAsync(int finalValue, CancellationToken ct)
    {
        IsDieRolling = true;
        DieAnimationClass = "rolling";

        // Rapid cosmetic faces
        var random = new Random();
        int frameCount = random.Next(12, 20);
        int frameDelay = 700 / frameCount; // ~700ms total

        for (int i = 0; i < frameCount; i++)
        {
            if (ct.IsCancellationRequested) break;
            DieValue = random.Next(1, 7);
            await DelayAsync(frameDelay, ct);
        }

        // End on final value
        DieValue = finalValue;
        IsDieRolling = false;
        DieAnimationClass = "final";

        // Emphasize the revealed number by FLASHING the die box a few times.
        // The die display keeps a fixed size, so the board never resizes/jitters.
        for (int f = 0; f < 3 && !ct.IsCancellationRequested; f++)
        {
            DieHighlighted = true;
            await DelayAsync(120, ct);
            DieHighlighted = false;
            await DelayAsync(90, ct);
        }
    }

    private async Task AnimateTokenMoveAsync(LegalMove move, PlayerColor color, CancellationToken ct)
    {
        var token = _gameState!.AllTokens.FirstOrDefault(t => t.Id == move.TokenId);
        int index = token?.Index ?? 0;

        // Waypoints (fractional cell centres) from the token's origin to its destination.
        var points = new List<(double R, double C)>();
        if (move.EntersBoard)
        {
            var yard = BoardGeometry.Yards[color][index];
            points.Add((yard.Row, yard.Col));
            var start = BoardGeometry.GetPosition(color, 0);
            points.Add((start.Row, start.Col));
        }
        else
        {
            for (int p = move.FromProgress; p <= move.ToProgress; p++)
            {
                var cell = BoardGeometry.GetPosition(color, p);
                points.Add((cell.Row, cell.Col));
            }
        }

        // Glide the piece smoothly between every pair of cells (interpolated sub-steps), drawn
        // as a flashing overlay so it is easy to follow. Works for EVERY color and move type,
        // including single-cell entry moves. Never teleports.
        _animTokenId = move.TokenId;
        MovingColor = color;
        MovingIndex = index;
        MovingActive = true;
        RenderAnimatedTokens();
        try
        {
            const int subSteps = 6;   // per-cell interpolation frames
            int frame = 0;
            for (int seg = 0; seg < points.Count - 1; seg++)
            {
                var (r0, c0) = points[seg];
                var (r1, c1) = points[seg + 1];
                for (int s = 1; s <= subSteps; s++)
                {
                    if (ct.IsCancellationRequested) return;
                    double f = (double)s / subSteps;
                    MovingRow = r0 + (r1 - r0) * f;
                    MovingCol = c0 + (c1 - c0) * f;
                    MovingFlash = (frame++ % 6) < 3;   // gentle pulse of the moving piece
                    RenderAnimatedTokens();            // triggers a board repaint each frame
                    await DelayAsync(55, ct);          // ~330 ms per cell: slow and relaxed
                }
            }
            MovingFlash = false;
            if (move.Captures.Any())
                await DelayAsync(220, ct);
        }
        finally
        {
            MovingActive = false;
            _animTokenId = null;
        }
    }

    // Rebuild the token display EXCLUDING the in-flight token (BoardControl draws that one as an
    // overlay at its fractional position). Clearing/adding here triggers a board repaint.
    private void RenderAnimatedTokens()
    {
        if (_gameState == null) return;
        Tokens.Clear();
        foreach (var t in _gameState.AllTokens)
        {
            if (_animTokenId != null && t.Id == _animTokenId) continue;
            var pos = t.GetPosition();
            Tokens.Add(new TokenDisplayInfo(
                t.Id, t.Color, t.State, t.Progress, pos?.Row ?? -1, pos?.Col ?? -1, t.Index));
        }
    }

    private async Task HighlightPlayerAsync(PlayerColor color)
    {
        SetAllActive(false);
        switch (color)
        {
            case PlayerColor.Red: IsRedActive = true; break;
            case PlayerColor.Green: IsGreenActive = true; break;
            case PlayerColor.Yellow: IsYellowActive = true; break;
            case PlayerColor.Blue: IsBlueActive = true; break;
        }
        UpdatePlayerStatus();
        await DelayAsync(500, CancellationToken.None); // brief highlight
    }

    private void SetAllActive(bool active)
    {
        IsRedActive = active;
        IsGreenActive = active;
        IsYellowActive = active;
        IsBlueActive = active;
    }

    private void UpdatePlayerStatus()
    {
        if (_gameState == null) return;
        RedStatus = FormatPlayerStatus(PlayerColor.Red);
        GreenStatus = FormatPlayerStatus(PlayerColor.Green);
        YellowStatus = FormatPlayerStatus(PlayerColor.Yellow);
        BlueStatus = FormatPlayerStatus(PlayerColor.Blue);
    }

    private string FormatPlayerStatus(PlayerColor color)
    {
        if (_gameState == null || !_gameState.Players.ContainsKey(color)) return "";
        var p = _gameState.GetPlayer(color);
        return $"Yard:{p.TokensInYard} Track:{p.TokensOnTrack} Home:{p.TokensInHomeLane} Done:{p.TokensFinished}";
    }

    private void UpdateTokenDisplay()
    {
        if (_gameState == null) return;
        Tokens.Clear();
        foreach (var token in _gameState.AllTokens)
        {
            var pos = token.GetPosition();
            Tokens.Add(new TokenDisplayInfo(
                token.Id, token.Color, token.State, token.Progress,
                pos?.Row ?? -1, pos?.Col ?? -1, token.Index));
        }
        UpdatePlayerStatus();
    }

    private void AddLog(string message)
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("HH:mm:ss");
        EventLog.Insert(0, $"[{timestamp}] {message}");
        while (EventLog.Count > 100)
            EventLog.RemoveAt(EventLog.Count - 1);

        // The on-screen log is capped at 100 lines; the transcript keeps everything.
        WriteTranscript($"[{timestamp}] {message}");
    }

    private static string FormatEvent(DomainEvent evt) => evt switch
    {
        TokenEntered e => $"{e.Color}: Token {e.TokenId} entered board",
        TokenMoved e => $"{e.Color}: {e.TokenId} moved {e.FromProgress}→{e.ToProgress}",
        TokenCaptured e => $"{e.Color}'s {e.CapturedTokenId} captured by {e.CapturingTokenId}!",
        TokenFinished e => $"{e.Color}: {e.TokenId} reached home!",
        BlockadeFormed e => $"{e.Color}: Blockade at {e.SharedTrackIndex}!",
        BonusRollAwarded e => $"Bonus roll for {e.Color}: {e.Reason}",
        ThirdSixForfeited e => $"{e.Color}: Three sixes - turn forfeited!",
        PlayerWon e => $"*** {e.DisplayName} ({e.Color}) WINS in {e.TotalTurns} turns! ***",
        _ => evt.ToString()
    };

    [RelayCommand]
    private void Quit()
    {
        _gameCts?.Cancel();
        CleanupSessions();
        Environment.Exit(0);
    }

    [RelayCommand]
    private void NewGame()
    {
        CleanupSessions();
        IsGameOver = false;
        IsSetupVisible = true;
        IsGameRunning = false;
        StatusMessage = "Ready";
        EventLog.Clear();
        Tokens.Clear();
    }

    private void CleanupSessions()
    {
        if (_sessions != null)
        {
            foreach (var (_, session) in _sessions)
                session.Dispose();
            _sessions = null;
        }
    }
}

public record TokenDisplayInfo(
    string Id, PlayerColor Color, TokenState State, int Progress,
    int Row, int Col, int Index);
