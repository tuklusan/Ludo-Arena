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

namespace LudoNimArena.Core.Tests;

public class BoardGeometryTests
{
    [Fact]
    public void SharedTrack_Has52Positions()
    {
        BoardGeometry.SharedTrack.Should().HaveCount(52);
    }

    [Fact]
    public void SafeIndices_AreCorrect()
    {
        BoardGeometry.SafeIndices.Should().BeEquivalentTo(new[] { 0, 8, 13, 21, 26, 34, 39, 47 });
    }

    [Theory]
    [InlineData(PlayerColor.Red, 0)]
    [InlineData(PlayerColor.Green, 13)]
    [InlineData(PlayerColor.Yellow, 26)]
    [InlineData(PlayerColor.Blue, 39)]
    public void StartIndices_AreCorrect(PlayerColor color, int expected)
    {
        BoardGeometry.StartIndices[color].Should().Be(expected);
    }

    [Theory]
    [InlineData(PlayerColor.Red, 0)]
    [InlineData(PlayerColor.Green, 13)]
    [InlineData(PlayerColor.Yellow, 26)]
    [InlineData(PlayerColor.Blue, 39)]
    public void ColorOffsets_AreCorrect(PlayerColor color, int expected)
    {
        BoardGeometry.ColorOffsets[color].Should().Be(expected);
    }

    [Theory]
    [InlineData(PlayerColor.Red, 0, 0)]
    [InlineData(PlayerColor.Red, 13, 13)]
    [InlineData(PlayerColor.Red, 51, 51)]
    [InlineData(PlayerColor.Green, 0, 13)]
    [InlineData(PlayerColor.Green, 13, 26)]
    [InlineData(PlayerColor.Yellow, 0, 26)]
    [InlineData(PlayerColor.Blue, 0, 39)]
    [InlineData(PlayerColor.Blue, 12, 51)]
    public void GetSharedTrackIndex_WrapsCorrectly(PlayerColor color, int progress, int expectedIndex)
    {
        BoardGeometry.GetSharedTrackIndex(color, progress).Should().Be(expectedIndex);
    }

    [Fact]
    public void HomeLanes_Have5CellsEach()
    {
        foreach (var color in Enum.GetValues<PlayerColor>())
        {
            BoardGeometry.HomeLanes[color].Should().HaveCount(5);
        }
    }

    [Fact]
    public void Yards_Have4PositionsEach()
    {
        foreach (var color in Enum.GetValues<PlayerColor>())
        {
            BoardGeometry.Yards[color].Should().HaveCount(4);
        }
    }

    [Fact]
    public void StarSquares_Are8_21_34_47()
    {
        BoardGeometry.StarSquares.Should().BeEquivalentTo(new[] { 8, 21, 34, 47 });
    }

    [Fact]
    public void StartSquareCoordinates_AreInSharedTrack()
    {
        foreach (var color in Enum.GetValues<PlayerColor>())
        {
            var startIdx = BoardGeometry.StartIndices[color];
            var coord = BoardGeometry.SharedTrack[startIdx];
            BoardGeometry.SharedTrack.Should().Contain(coord);
        }
    }

    [Fact]
    public void SharedTrack_FirstAndLastCoordinates_AreCorrect()
    {
        BoardGeometry.SharedTrack[0].Should().Be((6, 1));
        BoardGeometry.SharedTrack[51].Should().Be((6, 0));
    }

    [Fact]
    public void HomeLane_Red_IsCorrect()
    {
        BoardGeometry.HomeLanes[PlayerColor.Red][0].Should().Be((7, 1));
        BoardGeometry.HomeLanes[PlayerColor.Red][4].Should().Be((7, 5));
    }

    [Fact]
    public void HomeLane_Green_IsCorrect()
    {
        BoardGeometry.HomeLanes[PlayerColor.Green][0].Should().Be((1, 7));
        BoardGeometry.HomeLanes[PlayerColor.Green][4].Should().Be((5, 7));
    }

    [Fact]
    public void HomeLane_Yellow_IsCorrect()
    {
        BoardGeometry.HomeLanes[PlayerColor.Yellow][0].Should().Be((7, 13));
        BoardGeometry.HomeLanes[PlayerColor.Yellow][4].Should().Be((7, 9));
    }

    [Fact]
    public void HomeLane_Blue_IsCorrect()
    {
        BoardGeometry.HomeLanes[PlayerColor.Blue][0].Should().Be((13, 7));
        BoardGeometry.HomeLanes[PlayerColor.Blue][4].Should().Be((9, 7));
    }
}
