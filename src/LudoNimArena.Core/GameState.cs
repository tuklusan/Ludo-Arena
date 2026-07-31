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

namespace LudoNimArena.Core;

/// <summary>Immutable game state with fluent With* methods.</summary>
public sealed record GameState
{
    public Guid GameId { get; init; }
    public ImmutableDictionary<PlayerColor, Player> Players { get; init; }
    public PlayerColor CurrentPlayer { get; init; }
    public GamePhase Phase { get; init; }
    public int TurnNumber { get; init; }
    public int ConsecutiveSixCount { get; init; }
    public bool IsBonusRoll { get; init; }
    public int? LastDieResult { get; init; }
    public Guid CurrentTurnId { get; init; }
    public Guid? CurrentRollId { get; init; }
    public ImmutableList<DomainEvent> EventLog { get; init; }
    public PlayerColor? Winner { get; init; }
    public ImmutableList<LegalMove> CurrentLegalMoves { get; init; }
    public bool IsRollOff { get; init; }
    public ImmutableDictionary<PlayerColor, int> RollOffResults { get; init; }
    public ImmutableList<PlayerColor> RollOffTiedPlayers { get; init; }

    public GameState()
    {
        GameId = Guid.NewGuid();
        Players = ImmutableDictionary<PlayerColor, Player>.Empty;
        Phase = GamePhase.Setup;
        TurnNumber = 0;
        EventLog = ImmutableList<DomainEvent>.Empty;
        CurrentLegalMoves = ImmutableList<LegalMove>.Empty;
        RollOffResults = ImmutableDictionary<PlayerColor, int>.Empty;
        RollOffTiedPlayers = ImmutableList<PlayerColor>.Empty;
        CurrentTurnId = Guid.NewGuid();
    }

    public GameState WithPlayers(ImmutableDictionary<PlayerColor, Player> players) =>
        this with { Players = players };

    public GameState WithPhase(GamePhase phase) =>
        this with { Phase = phase };

    public GameState WithCurrentPlayer(PlayerColor color) =>
        this with { CurrentPlayer = color };

    public GameState WithTurn(int turnNumber) =>
        this with { TurnNumber = turnNumber };

    public GameState WithConsecutiveSixCount(int count) =>
        this with { ConsecutiveSixCount = count };

    public GameState WithBonusRoll(bool isBonus) =>
        this with { IsBonusRoll = isBonus };

    public GameState WithLastDieResult(int? result) =>
        this with { LastDieResult = result };

    public GameState WithCurrentTurnId(Guid turnId) =>
        this with { CurrentTurnId = turnId };

    public GameState WithCurrentRollId(Guid? rollId) =>
        this with { CurrentRollId = rollId };

    public GameState WithEvent(DomainEvent evt) =>
        this with { EventLog = EventLog.Add(evt) };

    public GameState WithWinner(PlayerColor color) =>
        this with { Winner = color, Phase = GamePhase.GameOver };

    public GameState WithLegalMoves(ImmutableList<LegalMove> moves) =>
        this with { CurrentLegalMoves = moves };

    public GameState WithIsRollOff(bool isRollOff) =>
        this with { IsRollOff = isRollOff };

    public GameState WithRollOffResult(PlayerColor color, int result) =>
        this with { RollOffResults = RollOffResults.SetItem(color, result) };

    public GameState WithRollOffTiedPlayers(ImmutableList<PlayerColor> tied) =>
        this with { RollOffTiedPlayers = tied };

    public Player GetPlayer(PlayerColor color) => Players[color];

    public Token GetToken(string tokenId)
    {
        foreach (var (_, player) in Players)
        {
            foreach (var token in player.Tokens)
            {
                if (token.Id == tokenId)
                    return token;
            }
        }
        throw new KeyNotFoundException($"Token {tokenId} not found");
    }

    public IEnumerable<Token> AllTokens => Players.Values.SelectMany(p => p.Tokens);

    public ImmutableArray<Token> TokensAtSharedIndex(int sharedTrackIndex)
    {
        return AllTokens
            .Where(t => t.State == TokenState.OnSharedTrack && t.SharedTrackIndex == sharedTrackIndex)
            .ToImmutableArray();
    }

    public bool HasBlockade(int sharedTrackIndex)
    {
        if (BoardGeometry.SafeIndices.Contains(sharedTrackIndex))
            return false;
        var tokens = TokensAtSharedIndex(sharedTrackIndex);
        return tokens.Length >= 2 && tokens.GroupBy(t => t.Color).Any(g => g.Count() >= 2);
    }

    public PlayerColor? GetBlockadeColor(int sharedTrackIndex)
    {
        if (BoardGeometry.SafeIndices.Contains(sharedTrackIndex))
            return null;
        var tokens = TokensAtSharedIndex(sharedTrackIndex);
        foreach (var group in tokens.GroupBy(t => t.Color))
        {
            if (group.Count() >= 2)
                return group.Key;
        }
        return null;
    }
}
