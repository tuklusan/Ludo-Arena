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
using System.Text;
using System.Text.Json;
using FluentAssertions;
using LudoNimArena.Core;

namespace LudoNimArena.AI.Tests;

/// <summary>The event-log trace: request out, response in, how it resolved, and why local AI was used.</summary>
public class NimTraceTests
{
    private sealed class ScriptedHandler : HttpMessageHandler
    {
        public Queue<Func<HttpResponseMessage>> Script = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var next = Script.Count > 0 ? Script.Dequeue() : () => Reply("1");
            return Task.FromResult(next());
        }

        public static HttpResponseMessage Reply(string content) => new(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(new
            {
                choices = new[] { new { message = new { role = "assistant", content } } }
            }), Encoding.UTF8, "application/json")
        };
    }

    private static readonly LegalMove[] TwoMoves =
    {
        new("red-token-0", 18, 22, entersBoard: false, captures: new[] { "green-token-0" }),
        new("red-token-1", -1, 0, entersBoard: true),
    };

    private static (AiPlayerSession Session, ScriptedHandler Handler, List<string> Lines) Make(string apiKey = "k")
    {
        var handler = new ScriptedHandler();
        var settings = new NimSettings { ApiKey = apiKey, MinCallIntervalSeconds = 0 };
        var session = new AiPlayerSession(settings, new HttpClient(handler), new LocalFallbackAi(),
            PlayerColor.Red, "Assertive");
        return (session, handler, new List<string>());
    }

    private static Task<(string MoveId, string? Reason, bool IsFallback)> Ask(
        AiPlayerSession s, List<string> lines, int die = 4) =>
        s.RequestMoveAsync(NimTestState.Game(), TwoMoves, die, Guid.NewGuid(), CancellationToken.None, lines.Add);

    [Fact]
    public async Task Success_LogsRequestThenResponseWithTheChoice()
    {
        var (session, handler, lines) = Make();
        handler.Script.Enqueue(() => ScriptedHandler.Reply("1"));

        await Ask(session, lines);

        lines.Should().HaveCount(2);
        lines[0].Should().StartWith(">> NIM request (2 options)");
        lines[1].Should().StartWith("<< 200 in ").And.Contain("\"1\" = option 1 (token 0)");
    }

    [Fact]
    public async Task UnusableReply_LogsTheRetry_ThenSaysWhyLocalAiWasUsed()
    {
        var (session, handler, lines) = Make();
        handler.Script.Enqueue(() => ScriptedHandler.Reply("no idea"));
        handler.Script.Enqueue(() => ScriptedHandler.Reply("still no idea"));

        var result = await Ask(session, lines);

        result.IsFallback.Should().BeTrue();
        lines.Should().HaveCount(4);
        lines[1].Should().Contain("is not a valid option; retrying");
        lines[2].Should().StartWith(">> NIM retry");
        lines[3].Should().Contain("is not a valid option; local AI for this move");
    }

    [Fact]
    public async Task RetiredModel_SaysSo_AndLaterMovesJustSayNimIsOff()
    {
        var (session, handler, lines) = Make();
        handler.Script.Enqueue(() => new HttpResponseMessage(HttpStatusCode.Gone));

        await Ask(session, lines);
        lines.Last().Should().Contain("HTTP 410").And.Contain("model retired").And.Contain("NIM off for this game");

        lines.Clear();
        await Ask(session, lines, die: 5);
        lines.Should().Equal("local AI (NIM off for this game)");
    }

    [Fact]
    public async Task ServerError_SaysLocalAiForThisMove_NimRetriedNextTurn()
    {
        var (session, handler, lines) = Make();
        handler.Script.Enqueue(() => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        await Ask(session, lines);

        lines.Last().Should().Contain("HTTP 503").And.Contain("NIM retried next turn");
    }

    [Fact]
    public async Task Timeout_LogsNoReply()
    {
        var (session, handler, lines) = Make();
        handler.Script.Enqueue(() => throw new TaskCanceledException("timeout"));

        await Ask(session, lines);

        lines.Last().Should().Contain("no reply in 60 s").And.Contain("local AI for this move");
    }

    [Fact]
    public async Task NoKey_IsAPlainLocalAiLine()
    {
        var (session, _, lines) = Make(apiKey: "");

        await Ask(session, lines);

        lines.Should().Equal("local AI");
    }

    [Fact]
    public async Task ForcedMoveAndCacheHit_AreSingleLines()
    {
        var (session, handler, lines) = Make();
        handler.Script.Enqueue(() => ScriptedHandler.Reply("2"));

        await session.RequestMoveAsync(NimTestState.Game(), new[] { TwoMoves[0] }, 4, Guid.NewGuid(),
            CancellationToken.None, lines.Add);
        lines.Should().Equal("only legal move");

        lines.Clear();
        await Ask(session, lines);          // real call
        lines.Clear();
        await Ask(session, lines);          // same position -> cache
        lines.Should().ContainSingle().Which.Should().StartWith("NIM cached: option 2 (token 1)");
    }

    [Fact]
    public async Task NeverLogsTheKeyOrPrompt()
    {
        var (session, handler, lines) = Make(apiKey: "super-secret-key");
        handler.Script.Enqueue(() => ScriptedHandler.Reply("1"));

        await Ask(session, lines);

        string.Join("\n", lines).Should().NotContain("super-secret-key").And.NotContain("Style:");
    }
}
