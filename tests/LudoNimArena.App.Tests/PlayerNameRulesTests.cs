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
using FluentAssertions;
using LudoNimArena.Core;

namespace LudoNimArena.App.Tests;

public class PlayerNameRulesTests
{
    [Theory]
    [InlineData("Marvin", "Marvin")]
    [InlineData("  Mal   Smith  ", "Mal Smith")]
    [InlineData("ABCDEFGHIJKLMNOP", "ABCDEFGHIJ")]          // cut at 10
    [InlineData("Line1\nLine2", "Line1 Line")]              // control characters become spaces, then cut
    [InlineData("   ", "Default")]
    [InlineData(null, "Default")]
    public void Clean_Limits_Normalizes_AndFallsBack(string? raw, string expected) =>
        PlayerNameRules.Clean(raw, "Default").Should().Be(expected);

    [Fact]
    public void Clean_CutsByVisibleCharacters_NotUtf16Units()
    {
        var emoji = string.Concat(Enumerable.Repeat("\U0001F600", 12));   // 12 emoji = 24 UTF-16 units
        PlayerNameRules.Clean(emoji, "x").Should().Be(string.Concat(Enumerable.Repeat("\U0001F600", 10)));
    }

    [Fact]
    public void Resolve_UniqueNames_StayAsTheyAre()
    {
        var r = PlayerNameRules.Resolve(
            (PlayerColor.Red, "HAL 9000", "a"), (PlayerColor.Green, "Marvin", "b"),
            (PlayerColor.Yellow, "Mal", "c"), (PlayerColor.Blue, "Deckard", "d"));

        r[PlayerColor.Red].Should().Be("HAL 9000");
        r.Values.Should().OnlyContain(v => v.Length <= PlayerNameRules.MaxLength);
    }

    [Fact]
    public void Resolve_DuplicatesGetTheirColour_CaseInsensitively_AndNeverExceed16()
    {
        var r = PlayerNameRules.Resolve(
            (PlayerColor.Red, "Mal", "a"), (PlayerColor.Green, "MAL", "b"),
            (PlayerColor.Yellow, "Marvin", "c"), (PlayerColor.Blue, "Deckard", "d"));

        r[PlayerColor.Red].Should().Be("Mal Red");
        r[PlayerColor.Green].Should().Be("MAL Green");
        r[PlayerColor.Yellow].Should().Be("Marvin");
        r.Values.Should().OnlyContain(v => v.Length <= PlayerNameRules.MaxTotalLength);
    }

    [Fact]
    public void Resolve_LongDuplicateNames_ShortenJustEnoughForTheColour()
    {
        var r = PlayerNameRules.Resolve(
            (PlayerColor.Red, "Abcdefghij", "a"), (PlayerColor.Yellow, "Abcdefghij", "b"),
            (PlayerColor.Green, "Other", "c"), (PlayerColor.Blue, "Another", "d"));

        r[PlayerColor.Red].Should().Be("Abcdefghij Red");          // 14
        r[PlayerColor.Yellow].Should().Be("Abcdefghi Yellow");     // name part trimmed to keep it at 16
        r.Values.Should().OnlyContain(v => v.Length <= 16);
    }

    [Fact]
    public void Resolve_BlankNamesUseDefaults_ThenDeDuplicate()
    {
        var r = PlayerNameRules.Resolve(
            (PlayerColor.Red, "", "Bot"), (PlayerColor.Green, null, "Bot"),
            (PlayerColor.Yellow, "Mal", "c"), (PlayerColor.Blue, "Deckard", "d"));

        r[PlayerColor.Red].Should().Be("Bot Red");
        r[PlayerColor.Green].Should().Be("Bot Green");
    }
}
