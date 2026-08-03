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

/// <summary>Core Ludo rules engine. Generates legal moves and applies valid moves.</summary>
public class RulesEngine
{
    /// <summary>Generate all legal moves for the given player and die result.</summary>
    public ImmutableList<LegalMove> GenerateLegalMoves(GameState state, PlayerColor color, int dieResult)
    {
        var player = state.GetPlayer(color);
        var moves = ImmutableList.CreateBuilder<LegalMove>();
        bool isSix = dieResult == 6;

        // Check each token
        foreach (var token in player.Tokens)
        {
            if (token.State == TokenState.Finished)
                continue;

            if (token.State == TokenState.InYard)
            {
                // Need a six to enter
                if (!isSix)
                    continue;

                // Check if start square is blocked by own blockade
                int startIdx = BoardGeometry.StartIndices[color];
                if (state.HasBlockade(startIdx))
                    continue;

                // Check if an opponent token is there (would be a capture, which is fine)
                // But we need to check: if opponent has blockade at start, can't enter
                var tokensAtStart = state.TokensAtSharedIndex(startIdx);
                if (tokensAtStart.Any(t => t.Color != color && state.GetPlayer(t.Color).Tokens
                    .Count(t2 => t2.State == TokenState.OnSharedTrack && t2.SharedTrackIndex == startIdx) >= 2))
                    continue;

                moves.Add(new LegalMove(
                    token.Id,
                    fromProgress: -1,
                    toProgress: 0,
                    entersBoard: true,
                    captures: GetCapturesAt(state, color, 0, startIdx),
                    landsSafe: BoardGeometry.SafeIndices.Contains(startIdx),
                    finishes: false,
                    formsBlockade: WouldFormBlockade(state, color, 0, startIdx)
                ));
            }
            else if (token.State is TokenState.OnSharedTrack or TokenState.InHomeLane)
            {
                int newProgress = token.Progress + dieResult;

                if (newProgress > 57)
                    continue; // overshoot

                // For shared track movement, check path for blockades
                if (token.State == TokenState.OnSharedTrack && newProgress <= 51)
                {
                    // Check every intermediate square on the shared track
                    bool blocked = false;
                    for (int p = token.Progress + 1; p <= newProgress; p++)
                    {
                        int sharedIdx = BoardGeometry.GetSharedTrackIndex(color, p);
                        var blockColor = state.GetBlockadeColor(sharedIdx);
                        if (blockColor.HasValue && blockColor.Value != color)
                        {
                            blocked = true;
                            break;
                        }
                    }
                    if (blocked)
                        continue;
                }

                // Check if home lane entry is valid
                if (token.State == TokenState.OnSharedTrack && newProgress >= 52)
                {
                    // Check path through remaining shared track
                    bool blocked = false;
                    int remainingShared = 51 - token.Progress;
                    for (int p = token.Progress + 1; p <= 51; p++)
                    {
                        int sharedIdx = BoardGeometry.GetSharedTrackIndex(color, p);
                        var blockColor = state.GetBlockadeColor(sharedIdx);
                        if (blockColor.HasValue && blockColor.Value != color)
                        {
                            blocked = true;
                            break;
                        }
                    }
                    if (blocked)
                        continue;
                }

                // Check destination blockade (only on shared track)
                if (newProgress <= 51)
                {
                    int destIdx = BoardGeometry.GetSharedTrackIndex(color, newProgress);
                    var blockColor = state.GetBlockadeColor(destIdx);
                    if (blockColor.HasValue && blockColor.Value != color)
                        continue;

                    // Cannot land on a square with two same-color tokens (even own)
                    // Actually own tokens: can't land if two own tokens already there
                    var tokensAtDest = state.TokensAtSharedIndex(destIdx);
                    var ownTokens = tokensAtDest.Where(t => t.Color == color).ToList();
                    if (ownTokens.Count >= 2)
                        continue;
                    // But one own token is fine (for forming blockade), as long as it's not the same token
                    if (ownTokens.Count == 1 && ownTokens[0].Id == token.Id)
                        continue; // can't land on yourself
                }

                bool finishes = newProgress == 57;
                string[]? captures = null;
                bool landsSafe = false;
                bool formsBlockade = false;

                if (newProgress <= 51)
                {
                    int destIdx = BoardGeometry.GetSharedTrackIndex(color, newProgress);
                    captures = GetCapturesAt(state, color, newProgress, destIdx);
                    landsSafe = BoardGeometry.SafeIndices.Contains(destIdx);
                    formsBlockade = WouldFormBlockade(state, color, newProgress, destIdx);
                }

                moves.Add(new LegalMove(
                    token.Id,
                    token.Progress,
                    newProgress,
                    entersBoard: false,
                    captures: captures,
                    landsSafe: landsSafe,
                    finishes: finishes,
                    formsBlockade: formsBlockade
                ));
            }
        }

        return moves.ToImmutable();
    }

    private string[]? GetCapturesAt(GameState state, PlayerColor movingColor, int progress, int sharedIdx)
    {
        if (BoardGeometry.SafeIndices.Contains(sharedIdx))
            return null;

        if (progress > 51)
            return null;

        var tokensAtDest = state.TokensAtSharedIndex(sharedIdx);
        // Can only capture a lone opponent token (not a token of the same color, not a blockade)
        var opponentTokens = tokensAtDest.Where(t => t.Color != movingColor).ToList();
        if (opponentTokens.Count == 1)
        {
            // Check it's not part of a blockade
            var sameColorCount = tokensAtDest.Count(t => t.Color == opponentTokens[0].Color);
            if (sameColorCount == 1)
            {
                return new[] { opponentTokens[0].Id };
            }
        }
        return null;
    }

    private bool WouldFormBlockade(GameState state, PlayerColor color, int progress, int sharedIdx)
    {
        if (BoardGeometry.SafeIndices.Contains(sharedIdx))
            return false;
        if (progress > 51)
            return false;

        var tokensAtDest = state.TokensAtSharedIndex(sharedIdx);
        // Already has one own token at destination -> landing would form blockade
        return tokensAtDest.Any(t => t.Color == color);
    }

    /// <summary>Apply a legal move and return the new game state with events.</summary>
    public (GameState NewState, ImmutableList<DomainEvent> Events) ApplyMove(
        GameState state, LegalMove move, PlayerColor color)
    {
        var events = ImmutableList.CreateBuilder<DomainEvent>();
        var player = state.GetPlayer(color);
        var token = player.GetTokenById(move.TokenId);

        // Update the moved token
        Token updatedToken;
        if (move.EntersBoard)
        {
            updatedToken = token.EnterBoard();
            events.Add(new TokenEntered(state.GameId, state.CurrentTurnId, color, token.Id));
        }
        else
        {
            updatedToken = token.MoveForward(move.ToProgress - move.FromProgress, color)!;
            events.Add(new TokenMoved(state.GameId, state.CurrentTurnId, color, token.Id,
                move.FromProgress, move.ToProgress));
        }

        // Build new tokens array
        var tokensBuilder = player.Tokens.ToBuilder();
        tokensBuilder[token.Index] = updatedToken;

        // Handle captures
        foreach (var capturedId in move.Captures)
        {
            var capturedToken = state.GetToken(capturedId);
            var capturedPlayer = state.GetPlayer(capturedToken.Color);
            var capturedTokensBuilder = capturedPlayer.Tokens.ToBuilder();
            capturedTokensBuilder[capturedToken.Index] = capturedToken.ReturnToYard();

            var newPlayers = state.Players.ToBuilder();
            newPlayers[capturedToken.Color] = capturedPlayer.WithTokens(capturedTokensBuilder.ToImmutable());
            state = state.WithPlayers(newPlayers.ToImmutable());

            events.Add(new TokenCaptured(state.GameId, state.CurrentTurnId,
                capturedToken.Color, capturedId, move.TokenId));
        }

        // Update the moving player
        var playersBuilder = state.Players.ToBuilder();
        playersBuilder[color] = player.WithTokens(tokensBuilder.ToImmutable());

        // Handle blockade events
        if (move.FormsBlockade && move.ToProgress <= 51)
        {
            int sharedIdx = BoardGeometry.GetSharedTrackIndex(color, move.ToProgress);
            events.Add(new BlockadeFormed(state.GameId, state.CurrentTurnId, sharedIdx, color));
        }

        if (move.Finishes)
        {
            events.Add(new TokenFinished(state.GameId, state.CurrentTurnId, color, token.Id));
        }

        var newState = state.WithPlayers(playersBuilder.ToImmutable());

        // Add all events
        foreach (var evt in events)
            newState = newState.WithEvent(evt);

        // Check victory. The PlayerWon event must go into BOTH the returned event
        // collection and the new state — callers observe the returned events, so
        // recording it only in the state made every win look eventless.
        if (newState.GetPlayer(color).HasWon)
        {
            var playerWon = new PlayerWon(newState.GameId, color,
                newState.GetPlayer(color).DisplayName, newState.TurnNumber);
            events.Add(playerWon);
            newState = newState.WithEvent(playerWon);
            newState = newState.WithWinner(color);
        }

        return (newState, events.ToImmutable());
    }

    /// <summary>Get the next player in clockwise order.</summary>
    public PlayerColor GetNextPlayer(PlayerColor current) => BoardGeometry.NextColor(current);

    /// <summary>The clockwise order.</summary>
    public static readonly ImmutableArray<PlayerColor> TurnOrder = ImmutableArray.Create(
        PlayerColor.Red, PlayerColor.Green, PlayerColor.Yellow, PlayerColor.Blue
    );
}
