using LudoNimArena.Core;
using Microsoft.Extensions.Logging;

namespace LudoNimArena.AI;

/// <summary>
/// Deterministic local fallback AI that selects moves using a scoring policy.
/// Never uses randomness to break ties.
/// </summary>
public class LocalFallbackAi
{
    private readonly ILogger<LocalFallbackAi>? _logger;

    public LocalFallbackAi(ILogger<LocalFallbackAi>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>Select the best move from legal moves using deterministic scoring.</summary>
    public LegalMove SelectMove(GameState state, PlayerColor color, IReadOnlyList<LegalMove> legalMoves)
    {
        if (legalMoves.Count == 0)
            throw new InvalidOperationException("No legal moves to select from");

        if (legalMoves.Count == 1)
        {
            _logger?.LogDebug("Local fallback: only one move {MoveId}", legalMoves[0].MoveId);
            return legalMoves[0];
        }

        var scored = legalMoves.Select(m => (Move: m, Score: ScoreMove(state, color, m))).ToList();
        // Sort by score descending, then by token ID, then by MoveId for deterministic tie-breaking
        var best = scored
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Move.TokenId)
            .ThenBy(x => x.Move.MoveId)
            .First();

        _logger?.LogDebug("Local fallback selected {MoveId} with score {Score}",
            best.Move.MoveId, best.Score);
        return best.Move;
    }

    private int ScoreMove(GameState state, PlayerColor color, LegalMove move)
    {
        int score = 0;

        // Immediate victory
        var player = state.GetPlayer(color);
        int tokensFinished = player.TokensFinished;
        if (move.Finishes && tokensFinished == 3)
            return 10000; // winning move

        // Finishing a token
        if (move.Finishes)
            score += 500;

        // Capturing an opponent
        if (move.Captures.Any())
            score += 300;

        // Landing on safe square
        if (move.LandsSafe)
            score += 80;

        // Entering a yard token (getting more tokens in play)
        if (move.EntersBoard)
            score += 100;

        // Forming a blockade
        if (move.FormsBlockade)
            score += 150;

        // Forward progress (higher is better)
        score += move.ToProgress * 2;

        // Prefer tokens closer to home
        var token = player.GetTokenById(move.TokenId);
        if (token.State == TokenState.OnSharedTrack)
        {
            // Bonus for being close to home lane
            if (move.ToProgress >= 46)
                score += 50;
            if (move.ToProgress >= 52)
                score += 100;
        }

        // Escape capture risk: if current position is not safe and move makes it safe
        if (token.State == TokenState.OnSharedTrack && !token.IsOnSafeSquare && move.LandsSafe)
            score += 120;

        // Avoid leaving token exposed on non-safe square
        if (move.ToProgress <= 51 && !move.LandsSafe)
            score -= 30;

        // Prefer maintaining multiple active tokens
        int activeAfter = player.Tokens.Count(t =>
            t.State != TokenState.InYard && t.State != TokenState.Finished && t.Id != move.TokenId);
        if (move.EntersBoard)
            activeAfter++;
        if (activeAfter >= 2)
            score += 40;

        return score;
    }
}
