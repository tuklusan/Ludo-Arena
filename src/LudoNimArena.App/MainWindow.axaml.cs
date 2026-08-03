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
        SetUpScreenshots();
    }

    // -----------------------------------------------------------------------
    // Optional self-screenshotting, for unattended runs.
    //
    //   LUDO_SCREENSHOT=<prefix>          save <prefix>-001.png, -002.png, … and
    //                                     <prefix>-final.png when a winner is declared
    //   LUDO_SCREENSHOT_INTERVAL=<secs>   how often to grab a frame (default 25)
    //
    // The window renders itself with Avalonia's RenderTargetBitmap rather than
    // relying on an OS screen-capture tool. That matters on a build machine:
    // it needs no attached display, works under a virtual X server, and works
    // from Windows session 0 where the desktop is not reachable at all.
    // -----------------------------------------------------------------------
    private void SetUpScreenshots()
    {
        var prefix = System.Environment.GetEnvironmentVariable("LUDO_SCREENSHOT");
        if (string.IsNullOrWhiteSpace(prefix)) return;

        int seq = 0;
        this.Opened += (_, _) =>
        {
            if (!int.TryParse(System.Environment.GetEnvironmentVariable("LUDO_SCREENSHOT_INTERVAL"),
                    out var secs) || secs <= 0)
                secs = 25;

            var timer = new Avalonia.Threading.DispatcherTimer
            {
                Interval = System.TimeSpan.FromSeconds(secs)
            };
            timer.Tick += (_, _) => CaptureFrame($"{prefix}-{++seq:000}.png");
            timer.Start();

            // Final frame: the winner screen, captured before the app shuts down.
            if (DataContext is MainViewModel vm)
            {
                vm.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(MainViewModel.IsGameOver) && vm.IsGameOver)
                    {
                        timer.Stop();
                        CaptureFrame($"{prefix}-final.png");
                    }
                };
            }
        };
    }

    private void CaptureFrame(string path)
    {
        try
        {
            var w = (int)System.Math.Ceiling(Bounds.Width);
            var h = (int)System.Math.Ceiling(Bounds.Height);
            if (w <= 0 || h <= 0) { w = 1100; h = 700; }

            var size = new PixelSize(w, h);
            using var bitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(size, new Vector(96, 96));
            bitmap.Render(this);

            var dir = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) System.IO.Directory.CreateDirectory(dir);
            bitmap.Save(path);
        }
        catch
        {
            // A screenshot must never be able to disturb the game.
        }
    }
}
