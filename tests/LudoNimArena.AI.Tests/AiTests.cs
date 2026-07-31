using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using LudoNimArena.AI;
using LudoNimArena.Core;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace LudoNimArena.AI.Tests;

public class LocalFallbackAiTests
{
    private readonly LocalFallbackAi _ai = new(NullLogger<LocalFallbackAi>.Instance);

    [Fact]
    public void SelectMove_WithSingleOption_ReturnsIt()
    {
        var state = TestStateFactory.CreateFourPlayerGame();
        var move = new LegalMove("red-token-0", -1, 0, entersBoard: true);

        var result = _ai.SelectMove(state, PlayerColor.Red, new[] { move });
        result.Should().Be(move);
    }

    [Fact]
    public void SelectMove_WithMultipleOptions_PrefersVictory()
    {
        var state = TestStateFactory.CreateFourPlayerGame()
            .FinishToken(PlayerColor.Red, 0)
            .FinishToken(PlayerColor.Red, 1)
            .FinishToken(PlayerColor.Red, 2)
            .PlaceTokenInHomeLane(PlayerColor.Red, 3, 55);

        var finishMove = new LegalMove("red-token-3", 55, 57, entersBoard: false, finishes: true);
        var enterMove = new LegalMove("red-token-0", -1, 0, entersBoard: true); // won't be generated since finished

        // Only finish move is legal in this scenario
        var result = _ai.SelectMove(state, PlayerColor.Red, new[] { finishMove });
        result.Finishes.Should().BeTrue();
    }

    [Fact]
    public void SelectMove_IsDeterministic()
    {
        var state = TestStateFactory.CreateFourPlayerGame();
        var moves = new[]
        {
            new LegalMove("red-token-0", -1, 0, entersBoard: true),
            new LegalMove("red-token-1", -1, 0, entersBoard: true),
        };

        var result1 = _ai.SelectMove(state, PlayerColor.Red, moves);
        var result2 = _ai.SelectMove(state, PlayerColor.Red, moves);

        result1.MoveId.Should().Be(result2.MoveId, "fallback AI must be deterministic");
    }

    [Fact]
    public void SelectMove_ThrowsOnEmptyList()
    {
        var state = TestStateFactory.CreateFourPlayerGame();
        var act = () => _ai.SelectMove(state, PlayerColor.Red, Array.Empty<LegalMove>());
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void SelectMove_PrefersCaptureOverSafeMove()
    {
        // Red at progress 18, Green lone at progress 22 (Red relative)
        // If Red rolls 4, it can capture Green
        var state = TestStateFactory.CreateFourPlayerGame()
            .PlaceTokenOnTrack(PlayerColor.Red, 0, 18)
            .PlaceTokenOnTrack(PlayerColor.Green, 0, 9); // shared idx 22

        var captureMove = new LegalMove("red-token-0", 18, 22, entersBoard: false,
            captures: new[] { "green-token-0" });
        var otherMove = new LegalMove("red-token-1", -1, 0, entersBoard: true);

        var result = _ai.SelectMove(state, PlayerColor.Red, new LegalMove[] { captureMove, otherMove });
        result.MoveId.Should().Be(captureMove.MoveId, "should prefer capture");
    }
}

public class NimSettingsTests
{
    [Fact]
    public void Defaults_AreCorrect()
    {
        var settings = new NimSettings();
        settings.Model.Should().Be("nvidia/llama-3.3-nemotron-super-49b-v1.5");
        settings.BaseUrl.Should().Be("https://integrate.api.nvidia.com/v1");
        settings.RequestTimeoutSeconds.Should().Be(90);
        settings.MaxRetryDelaySeconds.Should().Be(1800);
        settings.MaxRetryElapsedSeconds.Should().Be(3600);
        settings.MinCallIntervalSeconds.Should().Be(5);
        settings.CircuitBreakerSeconds.Should().Be(300);
        settings.FailurePolicy.Should().Be("wait-then-fallback");
    }

    [Fact]
    public void HasApiKey_FalseWhenEmpty()
    {
        var settings = new NimSettings { ApiKey = "" };
        settings.HasApiKey.Should().BeFalse();
    }

    [Fact]
    public void HasApiKey_TrueWhenSet()
    {
        var settings = new NimSettings { ApiKey = "test-key" };
        settings.HasApiKey.Should().BeTrue();
    }

    [Fact]
    public void ChatCompletionsUrl_IsCorrect()
    {
        var settings = new NimSettings();
        settings.ChatCompletionsUrl.Should().Be("https://integrate.api.nvidia.com/v1/chat/completions");
    }

    [Fact]
    public void CustomBaseUrl_AppendsCorrectly()
    {
        var settings = new NimSettings { BaseUrl = "https://custom.api.com/v1/" };
        settings.ChatCompletionsUrl.Should().Be("https://custom.api.com/v1/chat/completions");
    }
}

public class NimDtosTests
{
    [Fact]
    public void NimGameStateDto_SerializesCorrectly()
    {
        var dto = new NimGameStateDto
        {
            GameId = "game-1",
            TurnId = "turn-1",
            RollId = "roll-1",
            RequestId = "req-1",
            PlayerColor = "Red",
            StrategyHint = "Assertive",
            DieResult = 5,
            ConsecutiveSixCount = 1,
            IsBonusRoll = false,
            TokenPositions = new Dictionary<string, string> { ["red-token-0"] = "track:10" },
            SafeSquares = new List<int> { 0, 8, 13, 21, 26, 34, 39, 47 },
            LegalMoves = new List<NimLegalMoveDto>
            {
                new() { MoveId = "red-token-0:track:10->track:15", TokenId = "red-token-0" }
            }
        };

        var json = JsonSerializer.Serialize(dto);
        json.Should().Contain("\"gameId\"");
        json.Should().Contain("\"game-1\"");
        json.Should().Contain("\"dieResult\":5");
    }
}

// Reuse TestStateFactory from Core.Tests
file static class TestStateFactory
{
    public static GameState CreateFourPlayerGame()
    {
        var players = System.Collections.Immutable.ImmutableDictionary<PlayerColor, Player>.Empty
            .Add(PlayerColor.Red, new Player(PlayerColor.Red, "Red AI", "Assertive but legal"))
            .Add(PlayerColor.Green, new Player(PlayerColor.Green, "Green AI", "Safety-conscious"))
            .Add(PlayerColor.Yellow, new Player(PlayerColor.Yellow, "Yellow AI", "Progress-focused"))
            .Add(PlayerColor.Blue, new Player(PlayerColor.Blue, "Blue AI", "Balanced"));

        return new GameState().WithPlayers(players)
            .WithPhase(GamePhase.PreparingTurn);
    }

    public static GameState PlaceTokenOnTrack(this GameState state, PlayerColor color,
        int tokenIndex, int routeProgress)
    {
        var player = state.GetPlayer(color);
        var token = player.Tokens[tokenIndex];
        var updated = token.EnterBoard();
        if (routeProgress > 0)
            updated = updated.MoveForward(routeProgress, color)!;
        var tokensBuilder = player.Tokens.ToBuilder();
        tokensBuilder[tokenIndex] = updated;
        var updatedPlayer = player.WithTokens(tokensBuilder.ToImmutable());
        var playersBuilder = state.Players.ToBuilder();
        playersBuilder[color] = updatedPlayer;
        return state.WithPlayers(playersBuilder.ToImmutable());
    }

    public static GameState PlaceTokenInHomeLane(this GameState state, PlayerColor color,
        int tokenIndex, int homeLaneCell)
    {
        var player = state.GetPlayer(color);
        var token = player.Tokens[tokenIndex];
        var updated = token.EnterBoard();
        updated = updated.MoveForward(homeLaneCell, color)!;
        var tokensBuilder = player.Tokens.ToBuilder();
        tokensBuilder[tokenIndex] = updated;
        var updatedPlayer = player.WithTokens(tokensBuilder.ToImmutable());
        var playersBuilder = state.Players.ToBuilder();
        playersBuilder[color] = updatedPlayer;
        return state.WithPlayers(playersBuilder.ToImmutable());
    }

    public static GameState FinishToken(this GameState state, PlayerColor color, int tokenIndex)
    {
        var player = state.GetPlayer(color);
        var token = player.Tokens[tokenIndex];
        var updated = token.EnterBoard();
        updated = updated.MoveForward(57, color)!;
        var tokensBuilder = player.Tokens.ToBuilder();
        tokensBuilder[tokenIndex] = updated;
        var updatedPlayer = player.WithTokens(tokensBuilder.ToImmutable());
        var playersBuilder = state.Players.ToBuilder();
        playersBuilder[color] = updatedPlayer;
        return state.WithPlayers(playersBuilder.ToImmutable());
    }
}
