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
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace LudoNimArena.Browser;

/// <summary>
/// Responsive game screen for the web build. Reflows by available width, using the
/// conventional breakpoints (CSS pixels, so a phone is roughly 360-430 wide):
///   Compact  below 600   phones: sidebars hidden; die + commentary under the board
///   Medium   below 900   players panel + board, no event log
///   Landscape phone (width 600+, height below 500): only die + commentary beside the board
///   Full     otherwise   the desktop layout: players | board | event log
/// </summary>
public partial class WebView : UserControl
{
    public static readonly IValueConverter BoolToHighlightConverter =
        new FuncValueConverter<bool, IBrush>(active =>
            active ? new SolidColorBrush(Color.FromArgb(80, 100, 180, 255))
                   : Brushes.Transparent);

    public static readonly IValueConverter DieFlashConverter =
        new FuncValueConverter<bool, IBrush>(hi =>
            hi ? new SolidColorBrush(Color.FromRgb(255, 214, 90))
               : new SolidColorBrush(Color.FromRgb(225, 225, 232)));

    private enum SizeClass { Full, Medium, Landscape, Compact }

    private const double CompactBelow = 600;
    private const double MediumBelow = 900;
    private const double ShortBelow = 500;

    private SizeClass? _current;

    public WebView()
    {
        InitializeComponent();
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        Apply(Classify(e.NewSize));
    }

    private static SizeClass Classify(Size size) =>
        size.Width < CompactBelow ? SizeClass.Compact
        : size.Height < ShortBelow ? SizeClass.Landscape
        : size.Width < MediumBelow ? SizeClass.Medium
        : SizeClass.Full;

    private void Apply(SizeClass sc)
    {
        if (_current == sc) return;
        _current = sc;

        var compact = sc == SizeClass.Compact;
        Classes.Set("compact", compact);

        LeftHost.IsVisible = !compact;
        PlayersBlock.IsVisible = sc is SizeClass.Full or SizeClass.Medium;
        RightHost.IsVisible = sc == SizeClass.Full;
        BottomHost.IsVisible = compact;
        CopyrightOverlay.IsVisible = !compact;

        // Die + commentary: in the left panel normally, under the board on phones.
        (DieStatus.Parent as Panel)?.Children.Remove(DieStatus);
        if (compact)
        {
            BottomHost.Children.Insert(0, DieStatus);
            DieStatus.RowDefinitions = new RowDefinitions("Auto");
            DieStatus.ColumnDefinitions = new ColumnDefinitions("112,*");
            Grid.SetRow(DieCard, 0); Grid.SetColumn(DieCard, 0);
            Grid.SetRow(StatusCard, 0); Grid.SetColumn(StatusCard, 1);
            StatusCard.Margin = new Thickness(8, 0, 0, 0);
        }
        else
        {
            LeftStack.Children.Add(DieStatus);
            DieStatus.ColumnDefinitions = new ColumnDefinitions("*");
            DieStatus.RowDefinitions = new RowDefinitions("Auto,Auto");
            Grid.SetRow(DieCard, 0); Grid.SetColumn(DieCard, 0);
            Grid.SetRow(StatusCard, 1); Grid.SetColumn(StatusCard, 0);
            StatusCard.Margin = new Thickness(0, 8, 0, 0);
        }

        switch (sc)
        {
            case SizeClass.Full:
                GameGrid.ColumnDefinitions = new ColumnDefinitions("200,*,210");
                Grid.SetColumn(Board, 1);
                break;
            case SizeClass.Medium:
            case SizeClass.Landscape:
                GameGrid.ColumnDefinitions = new ColumnDefinitions("190,*");
                Grid.SetColumn(Board, 1);
                break;
            default:
                GameGrid.ColumnDefinitions = new ColumnDefinitions("*");
                Grid.SetColumn(Board, 0);
                break;
        }
        GameGrid.RowDefinitions = new RowDefinitions("*,Auto");

        var columns = GameGrid.ColumnDefinitions.Count;
        foreach (var full in new Control[] { SetupBorder, BottomHost, CopyrightOverlay, GameOverBorder })
            Grid.SetColumnSpan(full, columns);
        Grid.SetRowSpan(SetupBorder, 2);
        Grid.SetRowSpan(GameOverBorder, 2);
        Grid.SetRowSpan(CopyrightOverlay, 2);
        Grid.SetColumn(LeftHost, 0);
        Grid.SetColumn(RightHost, 2);
    }
}
