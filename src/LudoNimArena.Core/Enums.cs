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
