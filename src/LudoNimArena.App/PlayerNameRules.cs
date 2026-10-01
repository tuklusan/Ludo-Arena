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
using System.Globalization;
using System.Text;
using LudoNimArena.Core;

namespace LudoNimArena.App;

/// <summary>
/// Player-name limits shared by the desktop and web UIs. A name is at most <see cref="MaxLength"/>
/// visible characters. When two players end up with the same name, each gets its colour appended
/// (at most six more characters), so a displayed name is never longer than <see cref="MaxTotalLength"/>.
/// </summary>
public static class PlayerNameRules
{
    public const int MaxLength = 10;
    public const int MaxTotalLength = 16;

    /// <summary>Strips control characters, collapses whitespace, trims, and cuts to <see cref="MaxLength"/>.
    /// A blank result becomes <paramref name="fallback"/>.</summary>
    public static string Clean(string? raw, string fallback)
    {
        var sb = new StringBuilder();
        bool pendingSpace = false;
        foreach (var ch in raw ?? "")
        {
            if (char.IsWhiteSpace(ch) || char.IsControl(ch)) { pendingSpace = sb.Length > 0; continue; }
            if (pendingSpace) sb.Append(' ');
            pendingSpace = false;
            sb.Append(ch);
        }
        var cleaned = Cut(sb.ToString(), MaxLength).TrimEnd();
        return cleaned.Length == 0 ? fallback : cleaned;
    }

    /// <summary>Cleans every name and, where names collide (ignoring case), appends the colour to each
    /// colliding one - shortening the name part only as far as needed to stay within
    /// <see cref="MaxTotalLength"/>.</summary>
    public static Dictionary<PlayerColor, string> Resolve(
        params (PlayerColor Color, string? Raw, string Default)[] players)
    {
        var cleaned = players.ToDictionary(p => p.Color, p => Clean(p.Raw, p.Default));
        var result = new Dictionary<PlayerColor, string>(cleaned);

        foreach (var group in cleaned.GroupBy(kv => kv.Value, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
            foreach (var (color, name) in group)
            {
                var suffix = color.ToString();
                var room = Math.Min(MaxLength, MaxTotalLength - 1 - suffix.Length);
                result[color] = $"{Cut(name, room).TrimEnd()} {suffix}";
            }
        return result;
    }

    private static string Cut(string s, int max)
    {
        var sb = new StringBuilder();
        var e = StringInfo.GetTextElementEnumerator(s);
        for (int n = 0; n < max && e.MoveNext(); n++) sb.Append(e.GetTextElement());
        return sb.ToString();
    }
}
