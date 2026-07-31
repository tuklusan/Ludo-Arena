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
using LudoNimArena.Core;

namespace LudoNimArena.Core.Tests;

/// <summary>Test helpers for creating GameState instances with specific token positions.</summary>
public static class TestStateFactory
{
    public static GameState CreateFourPlayerGame()
    {
        var players = ImmutableDictionary<PlayerColor, Player>.Empty
            .Add(PlayerColor.Red, new Player(PlayerColor.Red, "Red AI", "Assertive but legal"))
            .Add(PlayerColor.Green, new Player(PlayerColor.Green, "Green AI", "Safety-conscious"))
            .Add(PlayerColor.Yellow, new Player(PlayerColor.Yellow, "Yellow AI", "Progress-focused"))
            .Add(PlayerColor.Blue, new Player(PlayerColor.Blue, "Blue AI", "Balanced"));

        return new GameState().WithPlayers(players)
            .WithPhase(GamePhase.PreparingTurn);
    }

    /// <summary>
    /// Place a specific token on the shared track at the given route progress (0..51).
    /// The token is entered from yard and moved forward to the target progress.
    /// </summary>
    public static GameState PlaceTokenOnTrack(this GameState state, PlayerColor color,
        int tokenIndex, int routeProgress)
    {
        if (routeProgress is < 0 or > 51)
            throw new ArgumentOutOfRangeException(nameof(routeProgress),
                "Route progress must be 0..51 for OnSharedTrack");

        var player = state.GetPlayer(color);
        var token = player.Tokens[tokenIndex];

        // Enter board (progress 0), then move forward
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

    /// <summary>
    /// Place a token in the home lane at progress 52..56.
    /// </summary>
    public static GameState PlaceTokenInHomeLane(this GameState state, PlayerColor color,
        int tokenIndex, int homeLaneCell)
    {
        if (homeLaneCell is < 52 or > 56)
            throw new ArgumentOutOfRangeException(nameof(homeLaneCell));

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

    /// <summary>Finish a token (progress 57).</summary>
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
