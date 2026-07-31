using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LudoNimArena.Core;
using Microsoft.Extensions.Logging;

namespace LudoNimArena.AI;

/// <summary>
/// Per-player AI session that manages NIM communication with retry, circuit breaker, and fallback.
/// </summary>
public class AiPlayerSession : IDisposable
{
    private readonly NimSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly LocalFallbackAi _fallbackAi;
    private readonly ILogger<AiPlayerSession>? _logger;
    private readonly PlayerColor _color;
    private readonly string _strategyHint;

    private static readonly SemaphoreSlim _requestGate = new(1, 1);
    private DateTimeOffset _lastCallTime = DateTimeOffset.MinValue;
    private int _failureCount;
    private DateTimeOffset _circuitOpenUntil = DateTimeOffset.MinValue;
    private bool _permanentlyDisabled;

    public AiPlayerSession(
        NimSettings settings,
        HttpClient httpClient,
        LocalFallbackAi fallbackAi,
        PlayerColor color,
        string strategyHint,
        ILogger<AiPlayerSession>? logger = null)
    {
        _settings = settings;
        _httpClient = httpClient;
        _fallbackAi = fallbackAi;
        _color = color;
        _strategyHint = strategyHint;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/'));
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.Timeout = TimeSpan.FromSeconds(settings.RequestTimeoutSeconds);
    }

    public PlayerColor Color => _color;
    public string StrategyHint => _strategyHint;
    public bool IsCircuitOpen => DateTimeOffset.UtcNow < _circuitOpenUntil;
    public bool IsPermanentlyDisabled => _permanentlyDisabled;

    /// <summary>Request a move from NIM or fallback with full retry/circuit-breaker logic.</summary>
    public async Task<(string MoveId, string? Reason, bool IsFallback)> RequestMoveAsync(
        GameState state,
        IReadOnlyList<LegalMove> legalMoves,
        int dieResult,
        Guid requestId,
        CancellationToken cancellationToken)
    {
        // If permanently disabled or no API key, use fallback immediately
        if (_permanentlyDisabled || !_settings.HasApiKey)
        {
            var fallback = _fallbackAi.SelectMove(state, _color, legalMoves);
            _logger?.LogInformation("{Color}: Using fallback (NIM disabled/missing key)", _color);
            return (fallback.MoveId, "Local fallback AI", true);
        }

        // Check circuit breaker
        if (IsCircuitOpen)
        {
            _logger?.LogInformation("{Color}: Circuit breaker open, using fallback", _color);
            var fallback = _fallbackAi.SelectMove(state, _color, legalMoves);
            return (fallback.MoveId, "Local fallback AI (circuit open)", true);
        }

        // Build DTO
        var dto = BuildGameStateDto(state, legalMoves, dieResult, requestId);

        // Try NIM request with retries
        var totalStarted = DateTimeOffset.UtcNow;
        var totalBudget = TimeSpan.FromSeconds(_settings.MaxRetryElapsedSeconds);
        int attempt = 0;
        TimeSpan localBackoff = TimeSpan.FromSeconds(15);

        while (DateTimeOffset.UtcNow - totalStarted < totalBudget)
        {
            cancellationToken.ThrowIfCancellationRequested();
            attempt++;

            // Respect minimum call interval
            await WaitForCallIntervalAsync(cancellationToken);

            try
            {
                // Acquire gate (only one NIM request at a time across all sessions)
                await _requestGate.WaitAsync(cancellationToken);
                try
                {
                    _lastCallTime = DateTimeOffset.UtcNow;

                    var response = await SendNimRequestAsync(dto, cancellationToken);

                    // Success - close circuit if it was half-open
                    _failureCount = 0;
                    _circuitOpenUntil = DateTimeOffset.MinValue;

                    var parsed = ParseResponse(response, legalMoves);
                    if (parsed != null)
                    {
                        _logger?.LogInformation("{Color}: NIM returned {MoveId}", _color, parsed.Value.MoveId);
                        return (parsed.Value.MoveId, SafeReason(parsed.Value.Reason), false);
                    }

                    // Parse failed - try repair request
                    if (attempt == 1)
                    {
                        dto = BuildRepairDto(dto, legalMoves);
                        continue;
                    }

                    // Repair failed, use fallback
                    break;
                }
                finally
                {
                    _requestGate.Release();
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (HttpRequestException ex) when (ex.StatusCode != null)
            {
                var status = (HttpStatusCode)ex.StatusCode;

                if (IsPermanentFailure(status))
                {
                    if (status is HttpStatusCode.Unauthorized or HttpStatusCode.PaymentRequired or HttpStatusCode.Forbidden)
                    {
                        _permanentlyDisabled = true;
                        _logger?.LogWarning("{Color}: NIM permanently disabled after {Status}", _color, status);
                    }
                    break;
                }

                // Transient - retry
                var retryAfter = GetRetryAfter(ex);
                localBackoff = await WaitForRetryAsync(retryAfter, localBackoff, attempt, totalStarted, totalBudget, cancellationToken);
                _logger?.LogDebug("{Color}: Retry attempt {Attempt} after {Status}", _color, attempt, status);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Timeout - treat as transient
                localBackoff = await WaitForRetryAsync(null, localBackoff, attempt, totalStarted, totalBudget, cancellationToken);
            }
            catch (HttpRequestException)
            {
                // Connection error - transient
                localBackoff = await WaitForRetryAsync(null, localBackoff, attempt, totalStarted, totalBudget, cancellationToken);
            }
        }

        // Budget exhausted or permanent failure - use fallback and open circuit
        if (!_permanentlyDisabled)
        {
            _failureCount++;
            _circuitOpenUntil = DateTimeOffset.UtcNow.AddSeconds(_settings.CircuitBreakerSeconds);
            _logger?.LogWarning("{Color}: Circuit breaker opened for {Seconds}s after {Failures} failures",
                _color, _settings.CircuitBreakerSeconds, _failureCount);
        }

        var fallbackMove = _fallbackAi.SelectMove(state, _color, legalMoves);
        return (fallbackMove.MoveId, "Local fallback AI", true);
    }

    private async Task<string> SendNimRequestAsync(NimGameStateDto dto, CancellationToken ct)
    {
        var requestBody = new
        {
            model = _settings.Model,
            messages = new[]
            {
                new { role = "system", content = BuildSystemPrompt() },
                new { role = "user", content = JsonSerializer.Serialize(dto) }
            },
            temperature = 0.1,
            max_tokens = 128,
            stream = false
        };

        var response = await _httpClient.PostAsJsonAsync(
            _settings.ChatCompletionsUrl, requestBody, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    private string BuildSystemPrompt()
    {
        return $"""
            You are a Ludo AI player controlling the {_color} tokens.
            Strategy: {_strategyHint}

            You MUST respond with exactly one JSON object containing:
            - "moveId": The exact moveId from one of the provided legal moves.
            - "reason": A brief explanation of your choice (max 160 chars).

            Do NOT include any other text, code, or explanation outside the JSON.
            Respond ONLY with valid JSON.
            """;
    }

    private NimGameStateDto BuildGameStateDto(GameState state, IReadOnlyList<LegalMove> moves, int dieResult, Guid requestId)
    {
        var dto = new NimGameStateDto
        {
            GameId = state.GameId.ToString(),
            TurnId = state.CurrentTurnId.ToString(),
            RollId = (state.CurrentRollId ?? Guid.NewGuid()).ToString(),
            RequestId = requestId.ToString(),
            PlayerColor = _color.ToString(),
            StrategyHint = _strategyHint,
            DieResult = dieResult,
            ConsecutiveSixCount = state.ConsecutiveSixCount,
            IsBonusRoll = state.IsBonusRoll
        };

        // Token positions
        foreach (var token in state.AllTokens)
        {
            dto.TokenPositions[token.Id] = token.State switch
            {
                TokenState.InYard => "yard",
                TokenState.Finished => "home",
                _ => $"progress:{token.Progress}"
            };
        }

        // Safe squares
        dto.SafeSquares = BoardGeometry.SafeIndices.ToList();

        // Blockades
        for (int i = 0; i < 52; i++)
        {
            var blockColor = state.GetBlockadeColor(i);
            if (blockColor.HasValue)
            {
                dto.Blockades.Add(new BlockadeInfo { SharedIndex = i, Color = blockColor.Value.ToString() });
            }
        }

        // Recent events
        dto.RecentEvents = state.EventLog.TakeLast(10).Select(e => e.ToString()).ToList();

        // Legal moves
        dto.LegalMoves = moves.Select(m => new NimMoveDto
        {
            MoveId = m.MoveId,
            TokenId = m.TokenId,
            From = m.EntersBoard ? "yard" : $"track:{m.FromProgress}",
            To = m.Finishes ? "home" : $"track:{m.ToProgress}",
            EntersBoard = m.EntersBoard,
            Captures = m.Captures.ToList(),
            LandsSafe = m.LandsSafe,
            Finishes = m.Finishes,
            FormsBlockade = m.FormsBlockade
        }).ToList();

        return dto;
    }

    private NimGameStateDto BuildRepairDto(NimGameStateDto original, IReadOnlyList<LegalMove> moves)
    {
        // For repair, add validation error message
        original.RecentEvents.Insert(0,
            $"ERROR: Previous response was invalid. You MUST return exactly one JSON object with moveId from the allowed list. Allowed moveIds: {string.Join(", ", moves.Select(m => m.MoveId))}");
        return original;
    }

    private (string MoveId, string? Reason)? ParseResponse(string responseBody, IReadOnlyList<LegalMove> legalMoves)
    {
        try
        {
            // Strip markdown fences
            string json = responseBody.Trim();
            try { using var _env = System.Text.Json.JsonDocument.Parse(responseBody);
                  json = (_env.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? json).Trim(); }
            catch { }
            if (json.StartsWith("```"))
            {
                var lines = json.Split('\n');
                json = string.Join("\n", lines.Skip(1).TakeWhile(l => !l.Trim().StartsWith("```")));
            }

            var dto = JsonSerializer.Deserialize<NimResponseDto>(json);
            if (dto == null || string.IsNullOrWhiteSpace(dto.MoveId))
                return null;

            // Validate moveId
            if (!legalMoves.Any(m => m.MoveId == dto.MoveId))
            {
                _logger?.LogWarning("NIM returned unknown moveId: {MoveId}", dto.MoveId);
                return null;
            }

            return (dto.MoveId, dto.Reason);
        }
        catch (JsonException ex)
        {
            _logger?.LogWarning("Failed to parse NIM response: {Error}", ex.Message);
            return null;
        }
    }

    private static string SafeReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason)) return "";
        // Escape, normalize, and cap
        var safe = reason.Replace("\0", "").Replace("\r", "").Replace("\n", " ").Trim();
        return safe.Length > 160 ? safe[..160] : safe;
    }

    private bool IsPermanentFailure(HttpStatusCode status) => status switch
    {
        HttpStatusCode.BadRequest => true,
        HttpStatusCode.Unauthorized => true,
        HttpStatusCode.PaymentRequired => true,
        HttpStatusCode.Forbidden => true,
        HttpStatusCode.NotFound => true, // model not found
        HttpStatusCode.UnprocessableEntity => true,
        _ => false
    };

    private TimeSpan? GetRetryAfter(HttpRequestException ex)
    {
        // Check for Retry-After header in response
        // Since HttpRequestException doesn't expose headers easily, we estimate
        if (ex.StatusCode == HttpStatusCode.TooManyRequests ||
            ex.StatusCode == (HttpStatusCode)529)
        {
            // Default: use local backoff
            return null;
        }
        return null;
    }

    private async Task<TimeSpan> WaitForRetryAsync(TimeSpan? serverDelay, TimeSpan localBackoff, int attempt,
        DateTimeOffset totalStarted, TimeSpan totalBudget, CancellationToken ct)
    {
        TimeSpan effectiveDelay;

        if (serverDelay.HasValue)
        {
            effectiveDelay = serverDelay.Value > localBackoff ? serverDelay.Value : localBackoff;
        }
        else
        {
            effectiveDelay = localBackoff;
        }

        // Add jitter (0-20%)
        var jitter = TimeSpan.FromMilliseconds(
            Random.Shared.NextDouble() * effectiveDelay.TotalMilliseconds * 0.2);
        effectiveDelay += jitter;

        // Cap at max retry delay
        var maxDelay = TimeSpan.FromSeconds(_settings.MaxRetryDelaySeconds);
        if (effectiveDelay > maxDelay && serverDelay == null)
            effectiveDelay = maxDelay;

        // Check remaining budget
        var remaining = totalBudget - (DateTimeOffset.UtcNow - totalStarted);
        if (effectiveDelay > remaining)
        {
            // If server-directed, respect it but don't retry after budget
            if (serverDelay.HasValue)
                throw new TimeoutException("Server-directed delay exceeds retry budget");
            effectiveDelay = remaining;
        }

        if (effectiveDelay > TimeSpan.Zero)
        {
            _logger?.LogInformation("{Color}: Waiting {Delay} before retry (attempt {Attempt})",
                _color, effectiveDelay, attempt);
            await Task.Delay(effectiveDelay, ct);
        }

        // Progress local backoff
        return localBackoff.TotalSeconds switch
        {
            <= 15 => TimeSpan.FromSeconds(30),
            <= 30 => TimeSpan.FromSeconds(60),
            <= 60 => TimeSpan.FromSeconds(120),
            <= 120 => TimeSpan.FromSeconds(240),
            <= 240 => TimeSpan.FromSeconds(480),
            _ => TimeSpan.FromSeconds(900)
        };
    }

    private async Task WaitForCallIntervalAsync(CancellationToken ct)
    {
        var elapsed = DateTimeOffset.UtcNow - _lastCallTime;
        var minInterval = TimeSpan.FromSeconds(_settings.MinCallIntervalSeconds);
        if (elapsed < minInterval)
        {
            var wait = minInterval - elapsed;
            await Task.Delay(wait, ct);
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
