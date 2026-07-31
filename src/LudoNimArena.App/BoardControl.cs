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

using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using LudoNimArena.Core;

namespace LudoNimArena.App;

/// <summary>Custom control that renders the Ludo board using vector graphics.</summary>
public class BoardControl : Control
{
    public static readonly StyledProperty<MainViewModel?> ViewModelProperty =
        AvaloniaProperty.Register<BoardControl, MainViewModel?>(nameof(ViewModel));

    public MainViewModel? ViewModel
    {
        get => GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    static BoardControl()
    {
        // Repaint when the ViewModel reference changes.
        AffectsRender<BoardControl>(ViewModelProperty);
    }

    // Repaint the board whenever the token collection changes, including every frame of a
    // token-move animation (Tokens is rebuilt per step). Without this the board would only
    // repaint incidentally and animation would not be visible.
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ViewModelProperty)
        {
            if (change.GetOldValue<MainViewModel?>() is { } oldVm)
                oldVm.Tokens.CollectionChanged -= OnTokensChanged;
            if (change.GetNewValue<MainViewModel?>() is { } newVm)
                newVm.Tokens.CollectionChanged += OnTokensChanged;
            InvalidateVisual();
        }
    }

    private void OnTokensChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (Dispatcher.UIThread.CheckAccess())
            InvalidateVisual();
        else
            Dispatcher.UIThread.Post(InvalidateVisual);
    }

    // Cached brushes
    private static readonly SolidColorBrush RedBrush = new(Color.FromRgb(220, 50, 50));
    private static readonly SolidColorBrush GreenBrush = new(Color.FromRgb(50, 180, 50));
    private static readonly SolidColorBrush YellowBrush = new(Color.FromRgb(220, 200, 30));
    private static readonly SolidColorBrush BlueBrush = new(Color.FromRgb(50, 80, 220));
    private static readonly SolidColorBrush WhiteBrush = new(Colors.White);
    private static readonly SolidColorBrush LightGrayBrush = new(Color.FromRgb(240, 240, 240));
    private static readonly SolidColorBrush DarkGrayBrush = new(Color.FromRgb(180, 180, 180));
    private static readonly SolidColorBrush BoardBgBrush = new(Color.FromRgb(250, 245, 235));
    private static readonly SolidColorBrush TextBrush = new(Colors.Black);
    private static readonly SolidColorBrush StarBrush = new(Color.FromRgb(255, 200, 50));
    private static readonly IPen BlackPen = new Pen(Brushes.Black, 1);
    private static readonly IPen ThickPen = new Pen(Brushes.Black, 2);

    private static readonly Dictionary<PlayerColor, SolidColorBrush> ColorBrushes = new()
    {
        [PlayerColor.Red] = RedBrush,
        [PlayerColor.Green] = GreenBrush,
        [PlayerColor.Yellow] = YellowBrush,
        [PlayerColor.Blue] = BlueBrush
    };

    public override void Render(DrawingContext context)
    {
        var bounds = Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0) return;

        // Make it square
        double size = Math.Min(bounds.Width, bounds.Height);
        double cellSize = size / 15;
        double offsetX = (bounds.Width - size) / 2;
        double offsetY = (bounds.Height - size) / 2;

        // Background
        context.DrawRectangle(BoardBgBrush, null, new Rect(offsetX, offsetY, size, size));

        // Draw grid cells
        for (int r = 0; r < 15; r++)
        {
            for (int c = 0; c < 15; c++)
            {
                var rect = new Rect(offsetX + c * cellSize, offsetY + r * cellSize, cellSize, cellSize);

                // Determine cell color
                var brush = GetCellBrush(r, c);
                if (brush != null)
                {
                    context.DrawRectangle(brush, BlackPen, rect);
                }
                else
                {
                    context.DrawRectangle(null, BlackPen, rect);
                }

                // Draw safe stars
                if (IsStarCell(r, c))
                {
                    DrawStar(context, rect);
                }
            }
        }

        // Draw home lanes as colored paths
        DrawHomeLanes(context, offsetX, offsetY, cellSize);

        // Draw center triangles
        DrawCenterTriangles(context, offsetX + 7 * cellSize, offsetY + 7 * cellSize, cellSize);

        // Draw tokens
        DrawTokens(context, offsetX, offsetY, cellSize);
    }

    private static SolidColorBrush? GetCellBrush(int r, int c)
    {
        // Yard areas
        if (r <= 5 && c <= 5) return new SolidColorBrush(Color.FromRgb(255, 230, 230)); // Red yard
        if (r <= 5 && c >= 9) return new SolidColorBrush(Color.FromRgb(230, 255, 230)); // Green yard
        if (r >= 9 && c >= 9) return new SolidColorBrush(Color.FromRgb(255, 255, 230)); // Yellow yard
        if (r >= 9 && c <= 5) return new SolidColorBrush(Color.FromRgb(230, 230, 255)); // Blue yard

        // Home lane columns
        if (c == 7 && r >= 1 && r <= 5) return new SolidColorBrush(Color.FromRgb(255, 240, 240)); // Red home
        if (r == 7 && c >= 9 && c <= 13) return new SolidColorBrush(Color.FromRgb(255, 255, 240)); // Yellow home
        if (c == 7 && r >= 9 && r <= 13) return new SolidColorBrush(Color.FromRgb(240, 240, 255)); // Blue home
        if (r >= 1 && r <= 5 && c == 7) return new SolidColorBrush(Color.FromRgb(240, 255, 240)); // Green home

        return null;
    }

    private static bool IsStarCell(int r, int c)
    {
        return BoardGeometry.SafeIndices.Any(i =>
        {
            var pos = BoardGeometry.SharedTrack[i];
            return pos.Row == r && pos.Col == c;
        }) && BoardGeometry.StarSquares.Any(i =>
        {
            var pos = BoardGeometry.SharedTrack[i];
            return pos.Row == r && pos.Col == c;
        });
    }

    private static void DrawStar(DrawingContext context, Rect rect)
    {
        double cx = rect.Center.X;
        double cy = rect.Center.Y;
        double outerR = rect.Width * 0.35;
        double innerR = outerR * 0.4;

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            for (int i = 0; i < 5; i++)
            {
                double angle = -Math.PI / 2 + i * 2 * Math.PI / 5;
                double outerX = cx + outerR * Math.Cos(angle);
                double outerY = cy + outerR * Math.Sin(angle);

                double innerAngle = angle + Math.PI / 5;
                double innerX = cx + innerR * Math.Cos(innerAngle);
                double innerY = cy + innerR * Math.Sin(innerAngle);

                if (i == 0)
                    ctx.BeginFigure(new Point(outerX, outerY), true);
                else
                    ctx.LineTo(new Point(outerX, outerY));
                ctx.LineTo(new Point(innerX, innerY));
            }
            ctx.EndFigure(true);
        }
        context.DrawGeometry(StarBrush, null, geometry);
    }

    private static void DrawHomeLanes(DrawingContext context, double ox, double oy, double cs)
    {
        foreach (var (color, lane) in BoardGeometry.HomeLanes)
        {
            var brush = ColorBrushes[color];
            foreach (var (r, c) in lane)
            {
                var rect = new Rect(ox + c * cs, oy + r * cs, cs, cs);
                context.DrawRectangle(new SolidColorBrush(Color.FromArgb(80,
                    brush.Color.R, brush.Color.G, brush.Color.B)), null, rect);
            }
        }
    }

    private static void DrawCenterTriangles(DrawingContext context, double cx, double cy, double cs)
    {
        var triangles = new (PlayerColor Color, Point[] Points)[]
        {
            (PlayerColor.Red, new[] { new Point(cx, cy), new Point(cx - cs*1.5, cy - cs*1.5), new Point(cx + cs*1.5, cy - cs*1.5) }),
            (PlayerColor.Green, new[] { new Point(cx, cy), new Point(cx + cs*1.5, cy - cs*1.5), new Point(cx + cs*1.5, cy + cs*1.5) }),
            (PlayerColor.Yellow, new[] { new Point(cx, cy), new Point(cx + cs*1.5, cy + cs*1.5), new Point(cx - cs*1.5, cy + cs*1.5) }),
            (PlayerColor.Blue, new[] { new Point(cx, cy), new Point(cx - cs*1.5, cy + cs*1.5), new Point(cx - cs*1.5, cy - cs*1.5) }),
        };

        foreach (var (color, pts) in triangles)
        {
            var brush = ColorBrushes[color];
            var fillBrush = new SolidColorBrush(Color.FromArgb(60,
                brush.Color.R, brush.Color.G, brush.Color.B));
            var geom = new StreamGeometry();
            using (var ctx = geom.Open())
            {
                ctx.BeginFigure(pts[0], true);
                ctx.LineTo(pts[1]);
                ctx.LineTo(pts[2]);
                ctx.EndFigure(true);
            }
            context.DrawGeometry(fillBrush, BlackPen, geom);
        }
    }

    private void DrawTokens(DrawingContext context, double ox, double oy, double cs)
    {
        if (ViewModel?.Tokens == null) return;

        // Group tokens by position to handle offsets
        var groups = ViewModel.Tokens
            .Where(t => t.Row >= 0 && t.Col >= 0)
            .GroupBy(t => (t.Row, t.Col))
            .ToList();

        foreach (var group in groups)
        {
            var tokens = group.ToList();
            double cellCx = ox + group.Key.Col * cs + cs / 2;
            double cellCy = oy + group.Key.Row * cs + cs / 2;
            double tokenR = cs * 0.3;

            // Offset multiple tokens
            if (tokens.Count == 1)
            {
                DrawToken(context, tokens[0], cellCx, cellCy, tokenR);
            }
            else
            {
                double spread = tokenR * 0.8;
                for (int i = 0; i < tokens.Count; i++)
                {
                    double angle = i * 2 * Math.PI / tokens.Count;
                    double tx = cellCx + spread * Math.Cos(angle);
                    double ty = cellCy + spread * Math.Sin(angle);
                    DrawToken(context, tokens[i], tx, ty, tokenR * 0.85);
                }
            }
        }

        // Draw yard tokens
        foreach (var color in new[] { PlayerColor.Red, PlayerColor.Green, PlayerColor.Yellow, PlayerColor.Blue })
        {
            var yardTokens = ViewModel.Tokens.Where(t => t.Color == color && t.State == TokenState.InYard).ToList();
            var yardPositions = BoardGeometry.Yards[color];
            var brush = ColorBrushes[color];

            for (int i = 0; i < yardTokens.Count && i < yardPositions.Length; i++)
            {
                var (yr, yc) = yardPositions[i];
                double yx = ox + yc * cs + cs / 2;
                double yy = oy + yr * cs + cs / 2;
                double yr2 = cs * 0.25;

                context.DrawEllipse(brush, BlackPen, new Point(yx, yy), yr2, yr2);
                DrawTokenNumber(context, yx, yy, yr2, yardTokens[i].Index.ToString());
            }
        }

        // In-flight moving token: drawn on top at its fractional position with a pulsing ring
        // so the piece you are watching glides smoothly and is easy to follow.
        if (ViewModel.MovingActive)
        {
            double mcx = ox + ViewModel.MovingCol * cs + cs / 2;
            double mcy = oy + ViewModel.MovingRow * cs + cs / 2;
            double mr = cs * 0.32;
            var mbrush = ColorBrushes[ViewModel.MovingColor];

            context.DrawEllipse(new SolidColorBrush(Color.FromArgb(70, 0, 0, 0)), null,
                new Point(mcx + 1.5, mcy + 1.5), mr, mr);
            if (ViewModel.MovingFlash)
                context.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromRgb(255, 235, 120)), 3.5),
                    new Point(mcx, mcy), mr + 3, mr + 3);
            context.DrawEllipse(mbrush, ThickPen, new Point(mcx, mcy), mr, mr);
            DrawTokenNumber(context, mcx, mcy, mr, ViewModel.MovingIndex.ToString());
        }
    }

    private static void DrawToken(DrawingContext context, TokenDisplayInfo token,
        double cx, double cy, double r)
    {
        var brush = ColorBrushes[token.Color];

        // Shadow
        context.DrawEllipse(new SolidColorBrush(Color.FromArgb(60, 0, 0, 0)), null,
            new Point(cx + 1.5, cy + 1.5), r, r);

        // Token body
        var pen = token.State == TokenState.Finished
            ? new Pen(Brushes.Gold, 2)
            : BlackPen;
        context.DrawEllipse(brush, pen, new Point(cx, cy), r, r);

        // Highlight for active
        if (token.State == TokenState.OnSharedTrack)
        {
            context.DrawEllipse(null, new Pen(Brushes.White, 1),
                new Point(cx - r * 0.3, cy - r * 0.3), r * 0.2, r * 0.2);
        }

        // Number
        DrawTokenNumber(context, cx, cy, r, token.Index.ToString());
    }

    private static void DrawTokenNumber(DrawingContext context, double cx, double cy, double r, string text)
    {
        var ft = new FormattedText(text, System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight, Typeface.Default, r * 1.2, Brushes.White);
        context.DrawText(ft, new Point(cx - ft.Width / 2, cy - ft.Height / 2));
    }
}
