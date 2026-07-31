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

using LudoNimArena.Core;
using Microsoft.Extensions.Logging;

namespace LudoNimArena.AI;

/// <summary>AI player controller implementing IPlayerController.</summary>
public class AiPlayerController : IPlayerController
{
    private readonly AiPlayerSession _session;
    private readonly ILogger<AiPlayerController>? _logger;

    public PlayerControllerType ControllerType => PlayerControllerType.AiPlayer;
    public PlayerColor Color => _session.Color;

    public AiPlayerController(AiPlayerSession session, ILogger<AiPlayerController>? logger = null)
    {
        _session = session;
        _logger = logger;
    }

    public async Task<string> RequestMoveAsync(
        GameState state,
        IReadOnlyList<LegalMove> legalMoves,
        int dieResult,
        CancellationToken cancellationToken)
    {
        var requestId = Guid.NewGuid();
        _logger?.LogInformation("{Color}: Requesting AI move (request {RequestId})", Color, requestId);

        var (moveId, reason, isFallback) = await _session.RequestMoveAsync(
            state, legalMoves, dieResult, requestId, cancellationToken);

        _logger?.LogInformation("{Color}: Move={MoveId}, Fallback={IsFallback}, Reason={Reason}",
            Color, moveId, isFallback, reason);

        return moveId;
    }
}
