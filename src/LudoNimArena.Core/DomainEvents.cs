namespace LudoNimArena.Core;

/// <summary>Domain events for reconstructing and testing game sequences.</summary>
public abstract record DomainEvent
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

public record GameStarted(Guid GameId) : DomainEvent;
public record StartingRollCompleted(Guid GameId, PlayerColor Color, int Result) : DomainEvent;
public record StartingPlayerSelected(Guid GameId, PlayerColor Color) : DomainEvent;
public record TurnStarted(Guid GameId, Guid TurnId, PlayerColor Color, int TurnNumber) : DomainEvent;
public record DieResultGenerated(Guid GameId, Guid TurnId, Guid RollId, PlayerColor Color, int Result, bool IsBonusRoll) : DomainEvent;
public record DieAnimationStarted(Guid GameId, Guid RollId) : DomainEvent;
public record DieAnimationCompleted(Guid GameId, Guid RollId) : DomainEvent;
public record DieResultRevealed(Guid GameId, Guid TurnId, Guid RollId, PlayerColor Color, int Result) : DomainEvent;
public record LegalMovesGenerated(Guid GameId, Guid TurnId, int MoveCount) : DomainEvent;
public record AiDecisionRequested(Guid GameId, Guid TurnId, Guid RollId, PlayerColor Color, Guid RequestId) : DomainEvent;
public record AiDecisionReceived(Guid GameId, Guid TurnId, Guid RollId, PlayerColor Color, string MoveId, string? Reason) : DomainEvent;
public record AiDecisionRejected(Guid GameId, Guid TurnId, Guid RollId, PlayerColor Color, string Reason) : DomainEvent;
public record FallbackDecisionSelected(Guid GameId, Guid TurnId, PlayerColor Color, string MoveId) : DomainEvent;
public record TokenEntered(Guid GameId, Guid TurnId, PlayerColor Color, string TokenId) : DomainEvent;
public record TokenMoved(Guid GameId, Guid TurnId, PlayerColor Color, string TokenId, int FromProgress, int ToProgress) : DomainEvent;
public record TokenCaptured(Guid GameId, Guid TurnId, PlayerColor Color, string CapturedTokenId, string CapturingTokenId) : DomainEvent;
public record BlockadeFormed(Guid GameId, Guid TurnId, int SharedTrackIndex, PlayerColor Color) : DomainEvent;
public record BlockadeBroken(Guid GameId, Guid TurnId, int SharedTrackIndex, PlayerColor Color) : DomainEvent;
public record TokenFinished(Guid GameId, Guid TurnId, PlayerColor Color, string TokenId) : DomainEvent;
public record BonusRollAwarded(Guid GameId, Guid TurnId, PlayerColor Color, string Reason) : DomainEvent;
public record ThirdSixForfeited(Guid GameId, Guid TurnId, PlayerColor Color) : DomainEvent;
public record TurnEnded(Guid GameId, Guid TurnId, PlayerColor Color, int TurnNumber) : DomainEvent;
public record PlayerWon(Guid GameId, PlayerColor Color, string DisplayName, int TotalTurns) : DomainEvent;
public record GameCancelled(Guid GameId) : DomainEvent;
