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
using System.Text;
using System.Text.RegularExpressions;
using LudoNimArena.Core;

namespace LudoNimArena.AI;

/// <summary>
/// Prompt design for nvidia/nemotron-3.5-lightning-30b-a3b run with reasoning off.
/// Models are far more reliable picking a number from a short menu than emitting JSON, so
/// the model sees a compact numbered list of the engine's legal moves and answers with one digit.
/// The system prompt is constant (the server can reuse its prefix) and the answer is capped at a
/// few tokens, which keeps each decision to a few hundred input tokens and a handful of output.
/// </summary>
public static class NimPrompt
{
    public const string SystemPrompt =
        "You play Ludo. Choose the single best move from the numbered options. " +
        "Normally prefer, in order: finish a token, capture an opponent, land on a safe square, " +
        "enter a token (needs a six), form a blockade, then advance the token that is furthest along. " +
        "Follow the player's style when it conflicts. " +
        "Reply with ONLY the option number, nothing else.";

    /// <summary>The per-decision user message: style, die, a one-line board summary, numbered options.</summary>
    public static string BuildUser(
        GameState state, PlayerColor color, string strategyHint,
        IReadOnlyList<LegalMove> moves, int dieResult)
    {
        var sb = new StringBuilder(384);
        sb.Append("You are ").Append(color).Append(". Style: ").Append(strategyHint).Append('\n');
        sb.Append("Die: ").Append(dieResult);
        if (state.IsBonusRoll) sb.Append(" (bonus roll)");
        if (state.ConsecutiveSixCount > 0) sb.Append(" (sixes in a row: ").Append(state.ConsecutiveSixCount).Append(')');
        sb.Append('\n');

        // Board: per colour, each token as Y(ard) / H(ome) / progress along its own route (0-57).
        sb.Append("Tokens (Y=yard, H=home, number=steps travelled):\n");
        foreach (var group in state.AllTokens.GroupBy(t => t.Color).OrderBy(g => g.Key != color).ThenBy(g => g.Key))
        {
            sb.Append(group.Key == color ? "You" : group.Key.ToString()).Append(": ");
            sb.Append(string.Join(' ', group.OrderBy(t => t.Index).Select(Describe)));
            sb.Append('\n');
        }

        sb.Append("Options:\n");
        for (int i = 0; i < moves.Count; i++)
            sb.Append(i + 1).Append(") ").Append(DescribeMove(moves[i])).Append('\n');
        sb.Append("Answer with one number 1-").Append(moves.Count).Append('.');
        return sb.ToString();
    }

    /// <summary>Appended on the single retry after an unusable reply.</summary>
    public static string RepairSuffix(int optionCount) =>
        $"\nYour last reply was not a valid choice. Reply with only one digit from 1 to {optionCount}.";

    /// <summary>
    /// Pulls the chosen option (1-based) out of the model's reply. Accepts "3", "Option 3", "3)" and
    /// similar; rejects out-of-range numbers. Returns null when no valid choice is found.
    /// </summary>
    public static int? ParseChoice(string? content, int optionCount)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;
        var m = Regex.Match(content, @"\d+");
        if (!m.Success || !int.TryParse(m.Value, out var n)) return null;
        return n >= 1 && n <= optionCount ? n : null;
    }

    private static string Describe(Token t) => t.State switch
    {
        TokenState.InYard => "Y",
        TokenState.Finished => "H",
        _ => t.Progress.ToString()
    };

    private static string DescribeMove(LegalMove m)
    {
        var sb = new StringBuilder();
        sb.Append("token ").Append(m.TokenId[^1]).Append(' ')
          .Append(m.EntersBoard ? "enters the board" : $"{m.FromProgress} to {(m.Finishes ? "home" : m.ToProgress.ToString())}");
        if (m.Captures.Length > 0) sb.Append(" - CAPTURES ").Append(string.Join(", ", m.Captures.Select(ShortId)));
        if (m.Finishes) sb.Append(" - FINISHES");
        if (m.LandsSafe) sb.Append(" - safe square");
        if (m.FormsBlockade) sb.Append(" - blockade");
        return sb.ToString();
    }

    private static string ShortId(string id) => id.Replace("-token-", " ");
}
