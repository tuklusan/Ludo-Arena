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

public class NimPromptTests
{
    [Theory]
    [InlineData("2", 3, 2)]
    [InlineData("Option 3", 3, 3)]
    [InlineData("1)", 2, 1)]
    [InlineData(" 2\n", 4, 2)]
    public void ParseChoice_AcceptsNumbers(string reply, int count, int expected) =>
        NimPrompt.ParseChoice(reply, count).Should().Be(expected);

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("I would move the red token")]
    [InlineData("0")]
    [InlineData("9")]
    public void ParseChoice_RejectsNonsense(string? reply) =>
        NimPrompt.ParseChoice(reply, 3).Should().BeNull();

    [Fact]
    public void BuildUser_ListsEveryOption_AndStaysSmall()
    {
        var state = NimTestState.Game();
        var moves = new[]
        {
            new LegalMove("red-token-0", 18, 22, entersBoard: false, captures: new[] { "green-token-0" }),
            new LegalMove("red-token-1", -1, 0, entersBoard: true, landsSafe: true),
        };

        var text = NimPrompt.BuildUser(state, PlayerColor.Red, "Assertive", moves, 4);

        text.Should().Contain("Die: 4").And.Contain("1) ").And.Contain("2) ")
            .And.Contain("CAPTURES green 0").And.Contain("enters the board")
            .And.Contain("Answer with one number 1-2.");
        text.Length.Should().BeLessThan(900, "a 4K-context model should get a short prompt");
    }
}

public class NimSessionTests
{
    private sealed class CountingHandler : HttpMessageHandler
    {
        public int Calls;
        public string? LastBody;
        public string Reply = "2";
        public HttpStatusCode Status = HttpStatusCode.OK;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls++;
            LastBody = request.Content == null ? null : await request.Content.ReadAsStringAsync(ct);
            if (Status != HttpStatusCode.OK) return new HttpResponseMessage(Status);
            var json = JsonSerializer.Serialize(new
            {
                choices = new[] { new { message = new { role = "assistant", content = Reply } } }
            });
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }
    }

    private static (AiPlayerSession Session, CountingHandler Handler) Make()
    {
        var handler = new CountingHandler();
        var settings = new NimSettings { ApiKey = "test-key", MinCallIntervalSeconds = 0 };
        var session = new AiPlayerSession(settings, new HttpClient(handler), new LocalFallbackAi(),
            PlayerColor.Red, "Assertive");
        return (session, handler);
    }

    private static readonly LegalMove[] TwoMoves =
    {
        new("red-token-0", 18, 22, entersBoard: false, captures: new[] { "green-token-0" }),
        new("red-token-1", -1, 0, entersBoard: true),
    };

    [Fact]
    public async Task SingleLegalMove_MakesNoApiCall()
    {
        var (session, handler) = Make();
        var one = new[] { TwoMoves[0] };

        var (moveId, _, isFallback) = await session.RequestMoveAsync(
            NimTestState.Game(), one, 4, Guid.NewGuid(), CancellationToken.None);

        moveId.Should().Be(one[0].MoveId);
        isFallback.Should().BeFalse();
        handler.Calls.Should().Be(0);
    }

    [Fact]
    public async Task Request_UsesTunedParameters_AndMapsOptionNumberToMove()
    {
        var (session, handler) = Make();
        handler.Reply = "2";

        var (moveId, _, isFallback) = await session.RequestMoveAsync(
            NimTestState.Game(), TwoMoves, 4, Guid.NewGuid(), CancellationToken.None);

        moveId.Should().Be(TwoMoves[1].MoveId);
        isFallback.Should().BeFalse();
        using var doc = JsonDocument.Parse(handler.LastBody!);
        var root = doc.RootElement;
        root.GetProperty("model").GetString().Should().Be("nvidia/nemotron-3.5-lightning-30b-a3b");
        root.GetProperty("temperature").GetDouble().Should().Be(0.0);
        root.GetProperty("max_tokens").GetInt32().Should().Be(32);
        root.GetProperty("chat_template_kwargs").GetProperty("enable_thinking").GetBoolean().Should().BeFalse();
        root.GetProperty("messages")[0].GetProperty("content").GetString().Should().Be(NimPrompt.SystemPrompt);
    }

    [Fact]
    public async Task RepeatedPosition_IsServedFromCache()
    {
        var (session, handler) = Make();
        var state = NimTestState.Game();

        var first = await session.RequestMoveAsync(state, TwoMoves, 4, Guid.NewGuid(), CancellationToken.None);
        var second = await session.RequestMoveAsync(state, TwoMoves, 4, Guid.NewGuid(), CancellationToken.None);

        second.MoveId.Should().Be(first.MoveId);
        handler.Calls.Should().Be(1, "the second identical position must not hit the API");
    }

    [Theory]
    [InlineData(HttpStatusCode.Gone)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task RetiredOrMissingModel_FallsBackAtOnce_AndStopsCalling(HttpStatusCode status)
    {
        var (session, handler) = Make();
        handler.Status = status;

        var first = await session.RequestMoveAsync(
            NimTestState.Game(), TwoMoves, 4, Guid.NewGuid(), CancellationToken.None);
        var second = await session.RequestMoveAsync(
            NimTestState.Game(), TwoMoves, 5, Guid.NewGuid(), CancellationToken.None);

        first.IsFallback.Should().BeTrue();
        second.IsFallback.Should().BeTrue();
        session.IsPermanentlyDisabled.Should().BeTrue();
        handler.Calls.Should().Be(1, "after a 404/410 the session must not call the API again");
    }

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    public async Task TransientError_FallsBackForThisMove_ButTriesAgainNextTurn(HttpStatusCode status)
    {
        var (session, handler) = Make();
        handler.Status = status;

        var first = await session.RequestMoveAsync(
            NimTestState.Game(), TwoMoves, 4, Guid.NewGuid(), CancellationToken.None);
        handler.Status = HttpStatusCode.OK;
        var second = await session.RequestMoveAsync(
            NimTestState.Game(), TwoMoves, 5, Guid.NewGuid(), CancellationToken.None);

        first.IsFallback.Should().BeTrue();
        second.IsFallback.Should().BeFalse("the next turn must reach NIM again");
        session.IsPermanentlyDisabled.Should().BeFalse();
        session.IsCircuitOpen.Should().BeFalse();
        handler.Calls.Should().Be(2);
    }

    [Fact]
    public async Task GarbageReply_RetriesOnce_ThenFallsBack()
    {
        var (session, handler) = Make();
        handler.Reply = "no idea";

        var (_, _, isFallback) = await session.RequestMoveAsync(
            NimTestState.Game(), TwoMoves, 4, Guid.NewGuid(), CancellationToken.None);

        isFallback.Should().BeTrue();
        handler.Calls.Should().Be(2);
    }
}

internal static class NimTestState
{
    public static GameState Game()
    {
        var players = System.Collections.Immutable.ImmutableDictionary<PlayerColor, Player>.Empty
            .Add(PlayerColor.Red, new Player(PlayerColor.Red, "Red AI", "Assertive"))
            .Add(PlayerColor.Green, new Player(PlayerColor.Green, "Green AI", "Safety-conscious"))
            .Add(PlayerColor.Yellow, new Player(PlayerColor.Yellow, "Yellow AI", "Progress-focused"))
            .Add(PlayerColor.Blue, new Player(PlayerColor.Blue, "Blue AI", "Balanced"));
        return new GameState().WithPlayers(players).WithPhase(GamePhase.PreparingTurn);
    }
}
