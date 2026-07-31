using System.Collections.Immutable;

namespace LudoNimArena.Core;

/// <summary>
/// Authoritative Ludo board geometry: 15×15 grid, shared track, home lanes, and center triangles.
/// This is the single source of truth for coordinates.
/// </summary>
public static class BoardGeometry
{
    public const int Rows = 15;
    public const int Cols = 15;

    /// <summary>Shared track coordinates indexed 0..51.</summary>
    public static readonly ImmutableArray<(int Row, int Col)> SharedTrack = ImmutableArray.Create(
        (6,1),(6,2),(6,3),(6,4),(6,5),(5,6),(4,6),(3,6),(2,6),
        (1,6),(0,6),(0,7),(0,8),(1,8),(2,8),(3,8),(4,8),(5,8),
        (6,9),(6,10),(6,11),(6,12),(6,13),(6,14),(7,14),(8,14),
        (8,13),(8,12),(8,11),(8,10),(8,9),(9,8),(10,8),(11,8),
        (12,8),(13,8),(14,8),(14,7),(14,6),(13,6),(12,6),(11,6),
        (10,6),(9,6),(8,5),(8,4),(8,3),(8,2),(8,1),(8,0),(7,0),(6,0)
    );

    /// <summary>Safe shared track indices.</summary>
    public static readonly ImmutableHashSet<int> SafeIndices = ImmutableHashSet.Create(
        0, 8, 13, 21, 26, 34, 39, 47
    );

    /// <summary>Start square indices for each color (also safe squares).</summary>
    public static readonly ImmutableDictionary<PlayerColor, int> StartIndices = ImmutableDictionary<PlayerColor, int>.Empty
        .Add(PlayerColor.Red, 0)
        .Add(PlayerColor.Green, 13)
        .Add(PlayerColor.Yellow, 26)
        .Add(PlayerColor.Blue, 39);

    /// <summary>Track offsets: the shared track index corresponding to progress 0 for each color.</summary>
    public static readonly ImmutableDictionary<PlayerColor, int> ColorOffsets = ImmutableDictionary<PlayerColor, int>.Empty
        .Add(PlayerColor.Red, 0)
        .Add(PlayerColor.Green, 13)
        .Add(PlayerColor.Yellow, 26)
        .Add(PlayerColor.Blue, 39);

    /// <summary>Home lane coordinates for each color (5 cells + center).</summary>
    public static readonly ImmutableDictionary<PlayerColor, ImmutableArray<(int Row, int Col)>> HomeLanes =
        ImmutableDictionary<PlayerColor, ImmutableArray<(int Row, int Col)>>.Empty
            .Add(PlayerColor.Red, ImmutableArray.Create((7,1),(7,2),(7,3),(7,4),(7,5)))
            .Add(PlayerColor.Green, ImmutableArray.Create((1,7),(2,7),(3,7),(4,7),(5,7)))
            .Add(PlayerColor.Yellow, ImmutableArray.Create((7,13),(7,12),(7,11),(7,10),(7,9)))
            .Add(PlayerColor.Blue, ImmutableArray.Create((13,7),(12,7),(11,7),(10,7),(9,7)));

    /// <summary>Center home positions for each color.</summary>
    public static readonly ImmutableDictionary<PlayerColor, (int Row, int Col)> CenterHomes =
        ImmutableDictionary<PlayerColor, (int Row, int Col)>.Empty
            .Add(PlayerColor.Red, (7, 2))
            .Add(PlayerColor.Green, (2, 7))
            .Add(PlayerColor.Yellow, (7, 12))
            .Add(PlayerColor.Blue, (12, 7));

    /// <summary>Yard positions for each color (4 tokens).</summary>
    public static readonly ImmutableDictionary<PlayerColor, ImmutableArray<(int Row, int Col)>> Yards =
        ImmutableDictionary<PlayerColor, ImmutableArray<(int Row, int Col)>>.Empty
            .Add(PlayerColor.Red, ImmutableArray.Create((2,2),(2,4),(4,2),(4,4)))
            .Add(PlayerColor.Green, ImmutableArray.Create((2,10),(2,12),(4,10),(4,12)))
            .Add(PlayerColor.Yellow, ImmutableArray.Create((10,10),(10,12),(12,10),(12,12)))
            .Add(PlayerColor.Blue, ImmutableArray.Create((10,2),(10,4),(12,2),(12,4)));

    /// <summary>Star safe square indices.</summary>
    public static readonly ImmutableHashSet<int> StarSquares = ImmutableHashSet.Create(8, 21, 34, 47);

    /// <summary>
    /// Get shared track index for a color at route progress P (0..51).
    /// </summary>
    public static int GetSharedTrackIndex(PlayerColor color, int progress)
    {
        int offset = ColorOffsets[color];
        return (offset + progress) % 52;
    }

    /// <summary>
    /// Get the (row, col) for a color at a given route progress.
    /// Progress 0..51: shared track; 52..56: home lane; 57: center home.
    /// </summary>
    public static (int Row, int Col) GetPosition(PlayerColor color, int progress)
    {
        if (progress is >= 0 and <= 51)
        {
            int idx = GetSharedTrackIndex(color, progress);
            return SharedTrack[idx];
        }
        if (progress is >= 52 and <= 56)
        {
            return HomeLanes[color][progress - 52];
        }
        // progress 57: center home
        return CenterHomes[color];
    }

    /// <summary>Get the next clockwise color.</summary>
    public static PlayerColor NextColor(PlayerColor color) => color switch
    {
        PlayerColor.Red => PlayerColor.Green,
        PlayerColor.Green => PlayerColor.Yellow,
        PlayerColor.Yellow => PlayerColor.Blue,
        PlayerColor.Blue => PlayerColor.Red,
        _ => throw new ArgumentOutOfRangeException(nameof(color))
    };
}
