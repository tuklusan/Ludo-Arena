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
