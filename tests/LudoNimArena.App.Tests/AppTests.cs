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

using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using FluentAssertions;
using LudoNimArena.App;
using LudoNimArena.Core;
using Xunit;

namespace LudoNimArena.App.Tests;

public class MainViewModelTests
{
    [AvaloniaFact]
    public void Constructor_SetsDefaultValues()
    {
        var vm = new MainViewModel();
        // The specification requires this exact user-visible branding.
        vm.Title.Should().Be("SANYALnet Labs Ludo AI Arena");
        vm.Subtitle.Should().Be("Four AI Players");
        vm.IsSetupVisible.Should().BeTrue();
        vm.IsGameRunning.Should().BeFalse();
        vm.IsGameOver.Should().BeFalse();
    }

    [AvaloniaFact]
    public void Constructor_LoadsPlayerDefaults()
    {
        // Shipped default roster (editable on the setup screen every game).
        var vm = new MainViewModel();
        vm.RedName.Should().Be("HAL 9000");
        vm.GreenName.Should().Be("Marvin");
        vm.YellowName.Should().Be("Mal");
        vm.BlueName.Should().Be("Deckard");
    }

    [AvaloniaFact]
    public void DieValue_HasDefault()
    {
        var vm = new MainViewModel();
        vm.DieValue.Should().Be(1); // default
        vm.IsDieRolling.Should().BeFalse();
    }
}

public class BoardGeometryValidationTests
{
    [Fact]
    public void SharedTrack_Has52UniquePositions()
    {
        var distinct = BoardGeometry.SharedTrack.Distinct().Count();
        distinct.Should().Be(52);
    }

    [Fact]
    public void AllCoordinates_Within15x15Bounds()
    {
        foreach (var (row, col) in BoardGeometry.SharedTrack)
        {
            row.Should().BeInRange(0, 14);
            col.Should().BeInRange(0, 14);
        }

        foreach (var color in Enum.GetValues<PlayerColor>())
        {
            foreach (var (row, col) in BoardGeometry.HomeLanes[color])
            {
                row.Should().BeInRange(0, 14);
                col.Should().BeInRange(0, 14);
            }

            var (cr, cc) = BoardGeometry.CenterHomes[color];
            cr.Should().BeInRange(0, 14);
            cc.Should().BeInRange(0, 14);

            foreach (var (yr, yc) in BoardGeometry.Yards[color])
            {
                yr.Should().BeInRange(0, 14);
                yc.Should().BeInRange(0, 14);
            }
        }
    }

    [Fact]
    public void SafeSquares_AreCorrectSubset()
    {
        // Starts: 0 (Red), 13 (Green), 26 (Yellow), 39 (Blue)
        // Stars: 8, 21, 34, 47
        BoardGeometry.SafeIndices.Should().Contain(new[] { 0, 8, 13, 21, 26, 34, 39, 47 });
        BoardGeometry.StarSquares.Should().BeEquivalentTo(new[] { 8, 21, 34, 47 });
    }

    [Fact]
    public void ColorOffsets_MapCorrectly()
    {
        BoardGeometry.ColorOffsets[PlayerColor.Red].Should().Be(0);
        BoardGeometry.ColorOffsets[PlayerColor.Green].Should().Be(13);
        BoardGeometry.ColorOffsets[PlayerColor.Yellow].Should().Be(26);
        BoardGeometry.ColorOffsets[PlayerColor.Blue].Should().Be(39);
    }

    [Fact]
    public void HomeLanes_DontOverlapSharedTrack()
    {
        foreach (var color in Enum.GetValues<PlayerColor>())
        {
            var homeLaneCoords = BoardGeometry.HomeLanes[color].ToHashSet();
            var sharedTrackCoords = BoardGeometry.SharedTrack.ToHashSet();
            homeLaneCoords.Intersect(sharedTrackCoords).Should().BeEmpty(
                $"{color} home lane should not overlap shared track");
        }
    }
}
