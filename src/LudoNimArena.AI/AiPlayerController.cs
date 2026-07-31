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
