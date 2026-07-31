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

/// <summary>Player colors in clockwise order.</summary>
public enum PlayerColor
{
    Red = 0,
    Green = 1,
    Yellow = 2,
    Blue = 3
}

/// <summary>Token states.</summary>
public enum TokenState
{
    InYard,
    OnSharedTrack,
    InHomeLane,
    Finished
}

/// <summary>Game phases for the turn state machine.</summary>
public enum GamePhase
{
    Setup,
    DeterminingFirstPlayer,
    PreparingTurn,
    GeneratingDieResult,
    AnimatingDie,
    RevealingDieResult,
    GeneratingLegalMoves,
    WaitingForAiDecision,
    ValidatingAiDecision,
    AnimatingTokenMove,
    ResolvingMove,
    AnimatingCapture,
    PreparingBonusRoll,
    AdvancingTurn,
    GameOver,
    ShuttingDown
}

/// <summary>Player controller type for extension point.</summary>
public enum PlayerControllerType
{
    AiPlayer = 0,
    HumanPlayer = 1 // Reserved for future revision
}
