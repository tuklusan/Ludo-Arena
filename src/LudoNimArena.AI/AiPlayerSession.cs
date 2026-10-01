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

    /// <summary>
    /// Request a move from NIM or fallback with full retry/circuit-breaker logic. <paramref name="trace"/>
    /// receives one short human-readable line per step (request out, response in, how it resolved, and why
    /// the local AI was used) for the game's event log. It never contains the key, headers or prompt.
    /// </summary>
    public async Task<(string MoveId, string? Reason, bool IsFallback)> RequestMoveAsync(
        GameState state,
        IReadOnlyList<LegalMove> legalMoves,
        int dieResult,
        Guid requestId,
        CancellationToken cancellationToken,
        Action<string>? trace = null)
    {
        // If permanently disabled or no API key, use fallback immediately
        if (_permanentlyDisabled || !_settings.HasApiKey)
        {
            var fallback = _fallbackAi.SelectMove(state, _color, legalMoves);
            _logger?.LogInformation("{Color}: Using fallback (NIM disabled/missing key)", _color);
            trace?.Invoke(_permanentlyDisabled ? "local AI (NIM off for this game)" : "local AI");
            return (fallback.MoveId, "Local fallback AI", true);
        }

        // Check circuit breaker
        if (IsCircuitOpen)
        {
            _logger?.LogInformation("{Color}: Circuit breaker open, using fallback", _color);
            var fallback = _fallbackAi.SelectMove(state, _color, legalMoves);
            var left = (int)Math.Ceiling((_circuitOpenUntil - DateTimeOffset.UtcNow).TotalSeconds);
            trace?.Invoke($"local AI (NIM paused, {left} s left)");
            return (fallback.MoveId, "Local fallback AI (circuit open)", true);
        }

        // A forced move needs no model call.
        if (legalMoves.Count == 1)
        {
            trace?.Invoke("only legal move");
            return (legalMoves[0].MoveId, "Only legal move", false);
        }

        // Compact numbered-menu prompt (see NimPrompt). Identical situations are answered from the cache.
        var userPrompt = NimPrompt.BuildUser(state, _color, _strategyHint, legalMoves, dieResult);
        var cacheKey = userPrompt;
        if (TryGetCached(cacheKey, out var cachedMoveId) && legalMoves.Any(m => m.MoveId == cachedMoveId))
        {
            _logger?.LogDebug("{Color}: decision cache hit", _color);
            trace?.Invoke($"NIM cached: {Describe(legalMoves, cachedMoveId)}");
            return (cachedMoveId, "NIM (cached decision)", false);
        }

        // One request per decision, plus one retry that restates the answer format after an unusable
        // reply. A transient failure (timeout, network error, 429/5xx) never stalls the game: this move
        // falls back to the local AI and the next turn tries NIM again.
        bool transientFailure = false;
        var clock = new System.Diagnostics.Stopwatch();

        for (int attempt = 1; attempt <= 2; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Respect minimum call interval
            await WaitForCallIntervalAsync(cancellationToken);

            try
            {
                // Acquire gate (only one NIM request at a time across all sessions)
                await _requestGate.WaitAsync(cancellationToken);
                try
                {
                    _lastCallTime = DateTimeOffset.UtcNow;

                    trace?.Invoke(attempt == 1
                        ? $">> NIM request ({legalMoves.Count} options)"
                        : ">> NIM retry (format reminder)");
                    clock.Restart();
                    var response = await SendNimRequestAsync(userPrompt, cancellationToken);
                    var seconds = Seconds(clock);

                    // Success - close circuit if it was half-open
                    _failureCount = 0;
                    _circuitOpenUntil = DateTimeOffset.MinValue;

                    var parsed = ParseResponse(response, legalMoves, out var reply);
                    if (parsed != null)
                    {
                        _logger?.LogInformation("{Color}: NIM returned {MoveId}", _color, parsed.Value.MoveId);
                        trace?.Invoke($"<< 200 in {seconds} s: \"{Clip(reply)}\" = {Describe(legalMoves, parsed.Value.MoveId)}");
                        StoreCached(cacheKey, parsed.Value.MoveId);
                        return (parsed.Value.MoveId, SafeReason(parsed.Value.Reason), false);
                    }

                    // Unusable reply - one retry that restates the answer format
                    if (attempt == 1)
                    {
                        trace?.Invoke($"<< 200 in {seconds} s: \"{Clip(reply)}\" is not a valid option; retrying");
                        userPrompt += NimPrompt.RepairSuffix(legalMoves.Count);
                        continue;
                    }

                    // Repair failed, use fallback
                    trace?.Invoke($"<< 200 in {seconds} s: \"{Clip(reply)}\" is not a valid option; local AI for this move (NIM paused {_settings.CircuitBreakerSeconds} s)");
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
                var seconds = Seconds(clock);

                if (IsPermanentFailure(status))
                {
                    // 401/402/403 (key or account) and 404/410 (model missing or retired): stop calling.
                    if (status is HttpStatusCode.Unauthorized or HttpStatusCode.PaymentRequired
                        or HttpStatusCode.Forbidden or HttpStatusCode.NotFound or HttpStatusCode.Gone)
                    {
                        _permanentlyDisabled = true;
                        _logger?.LogWarning("{Color}: NIM permanently disabled after {Status}", _color, status);
                        trace?.Invoke($"<< HTTP {(int)status} in {seconds} s: {DescribeStatus(status)}; NIM off for this game, local AI");
                    }
                    else
                    {
                        trace?.Invoke($"<< HTTP {(int)status} in {seconds} s: {DescribeStatus(status)}; local AI for this move (NIM paused {_settings.CircuitBreakerSeconds} s)");
                    }
                    break;
                }

                // Anything else (429, 5xx, ...): fall back for this move, retry NIM next turn.
                transientFailure = true;
                _logger?.LogInformation("{Color}: NIM {Status}; local fallback for this move", _color, status);
                trace?.Invoke($"<< HTTP {(int)status} in {seconds} s; local AI for this move, NIM retried next turn");
                break;
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Timed out (RequestTimeoutSeconds)
                transientFailure = true;
                trace?.Invoke($"<< no reply in {_settings.RequestTimeoutSeconds} s; local AI for this move, NIM retried next turn");
                break;
            }
            catch (HttpRequestException)
            {
                // Connection error
                transientFailure = true;
                trace?.Invoke("<< network error; local AI for this move, NIM retried next turn");
                break;
            }
        }

        // Unusable replies and permanent request errors open the circuit; transient failures do not.
        if (!_permanentlyDisabled && !transientFailure)
        {
            _failureCount++;
            _circuitOpenUntil = DateTimeOffset.UtcNow.AddSeconds(_settings.CircuitBreakerSeconds);
            _logger?.LogWarning("{Color}: Circuit breaker opened for {Seconds}s after {Failures} failures",
                _color, _settings.CircuitBreakerSeconds, _failureCount);
        }

        var fallbackMove = _fallbackAi.SelectMove(state, _color, legalMoves);
        return (fallbackMove.MoveId, "Local fallback AI", true);
    }

    private static string Seconds(System.Diagnostics.Stopwatch clock) =>
        clock.Elapsed.TotalSeconds.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>"option 2 (token 1)" for the log.</summary>
    private static string Describe(IReadOnlyList<LegalMove> moves, string moveId)
    {
        for (int i = 0; i < moves.Count; i++)
            if (moves[i].MoveId == moveId)
                return $"option {i + 1} (token {moves[i].TokenId[^1]})";
        return moveId;
    }

    /// <summary>Single-line, length-limited copy of the model's reply for the log.</summary>
    private static string Clip(string? reply)
    {
        var s = (reply ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
        return s.Length > 24 ? s[..24] + ".." : s;
    }

    private static string DescribeStatus(HttpStatusCode status) => status switch
    {
        HttpStatusCode.Unauthorized => "key rejected",
        HttpStatusCode.PaymentRequired => "payment required",
        HttpStatusCode.Forbidden => "access denied",
        HttpStatusCode.NotFound => "model not found",
        HttpStatusCode.Gone => "model retired",
        HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity => "request rejected",
        _ => status.ToString()
    };

    private async Task<string> SendNimRequestAsync(string userPrompt, CancellationToken ct)
    {
        // Tuned for nvidia/nemotron-3.5-lightning-30b-a3b: reasoning OFF (chat_template_kwargs
        // enable_thinking=false; with it on, reasoning tokens eat max_tokens and no answer comes back),
        // greedy decoding (temperature 0, fixed seed) for repeatable choices, 32 output tokens for a
        // one-digit answer, and a newline stop so the model cannot ramble.
        var requestBody = new
        {
            model = _settings.Model,
            messages = new[]
            {
                new { role = "system", content = NimPrompt.SystemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = 0.0,
            top_p = 1.0,
            max_tokens = 32,
            seed = 42,
            stop = new[] { "\n" },
            chat_template_kwargs = new { enable_thinking = false },
            stream = false
        };

        var response = await _httpClient.PostAsJsonAsync(
            _settings.ChatCompletionsUrl, requestBody, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    // ---- Decision cache -------------------------------------------------------------------
    // The same position (colour, style, die, tokens, options) always gets the same answer from a
    // temperature-0 model, so remember it instead of paying for another call. Bounded, oldest out.
    private readonly Dictionary<string, string> _cache = new();
    private readonly Queue<string> _cacheOrder = new();
    private readonly object _cacheLock = new();

    private bool TryGetCached(string key, out string moveId)
    {
        lock (_cacheLock) return _cache.TryGetValue(key, out moveId!);
    }

    private void StoreCached(string key, string moveId)
    {
        lock (_cacheLock)
        {
            if (!_cache.TryAdd(key, moveId)) return;
            _cacheOrder.Enqueue(key);
            while (_cacheOrder.Count > Math.Max(1, _settings.DecisionCacheSize))
                _cache.Remove(_cacheOrder.Dequeue());
        }
    }

    /// <summary>Maps the model's reply (an option number) back to the engine's legal move.</summary>
    private (string MoveId, string? Reason)? ParseResponse(string responseBody, IReadOnlyList<LegalMove> legalMoves, out string reply)
    {
        reply = "";
        try
        {
            string content = responseBody;
            using (var doc = JsonDocument.Parse(responseBody))
            {
                content = doc.RootElement.GetProperty("choices")[0].GetProperty("message")
                    .GetProperty("content").GetString() ?? "";
            }

            reply = content;
            var choice = NimPrompt.ParseChoice(content, legalMoves.Count);
            if (choice == null)
            {
                _logger?.LogWarning("NIM reply was not a valid option number: {Reply}", content);
                return null;
            }

            var move = legalMoves[choice.Value - 1];
            return (move.MoveId, $"NIM chose option {choice.Value}: {move.TokenId}");
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
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
        HttpStatusCode.Gone => true,     // model retired (end of life)
        HttpStatusCode.UnprocessableEntity => true,
        _ => false
    };

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
