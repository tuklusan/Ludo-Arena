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

namespace LudoNimArena.Core;

/// <summary>Extension point for player controllers (AI or future human).</summary>
public interface IPlayerController
{
    PlayerControllerType ControllerType { get; }
    PlayerColor Color { get; }

    /// <summary>Request a move decision. Returns the selected MoveId.</summary>
    Task<string> RequestMoveAsync(
        GameState state,
        IReadOnlyList<LegalMove> legalMoves,
        int dieResult,
        CancellationToken cancellationToken);
}
