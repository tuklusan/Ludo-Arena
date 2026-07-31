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
        vm.Title.Should().Be("Ludo NIM Arena");
        vm.Subtitle.Should().Be("Four AI Players");
        vm.IsSetupVisible.Should().BeTrue();
        vm.IsGameRunning.Should().BeFalse();
        vm.IsGameOver.Should().BeFalse();
    }

    [AvaloniaFact]
    public void Constructor_LoadsPlayerDefaults()
    {
        var vm = new MainViewModel();
        vm.RedName.Should().Be("Red AI");
        vm.GreenName.Should().Be("Green AI");
        vm.YellowName.Should().Be("Yellow AI");
        vm.BlueName.Should().Be("Blue AI");
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
