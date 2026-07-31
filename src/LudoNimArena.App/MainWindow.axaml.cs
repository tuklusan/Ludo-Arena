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

namespace LudoNimArena.App;

public partial class MainWindow : Window
{
    public static readonly IValueConverter BoolToHighlightConverter =
        new FuncValueConverter<bool, IBrush>(active =>
            active ? new SolidColorBrush(Color.FromArgb(80, 100, 180, 255))
                   : Brushes.Transparent);

    // Die box background: a bright flash when highlighted, a fixed neutral fill otherwise.
    // Only the color changes — never the size — so the board never reflows.
    public static readonly IValueConverter DieFlashConverter =
        new FuncValueConverter<bool, IBrush>(hi =>
            hi ? new SolidColorBrush(Color.FromRgb(255, 214, 90))
               : new SolidColorBrush(Color.FromRgb(225, 225, 232)));

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
        if (System.Environment.GetEnvironmentVariable("LUDO_AUTOSTART") == "1")
        {
            this.Opened += async (s, e) =>
            {
                await System.Threading.Tasks.Task.Delay(1500);
                if (DataContext is MainViewModel vm && vm.StartGameCommand.CanExecute(null))
                    vm.StartGameCommand.Execute(null);
            };
        }
    }
}
