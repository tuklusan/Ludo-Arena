namespace LudoNimArena.Core;

/// <summary>Represents a single token on the board.</summary>
public sealed class Token
{
    public string Id { get; }
    public PlayerColor Color { get; }
    public int Index { get; } // 0..3
    public TokenState State { get; private set; }
    public int Progress { get; private set; } // -1 in yard, 0..57 on route

    public Token(PlayerColor color, int index)
    {
        Color = color;
        Index = index;
        Id = $"{color.ToString().ToLowerInvariant()}-token-{index}";
        State = TokenState.InYard;
        Progress = -1;
    }

    private Token(Token other)
    {
        Id = other.Id;
        Color = other.Color;
        Index = other.Index;
        State = other.State;
        Progress = other.Progress;
    }

    public Token Clone() => new(this);

    /// <summary>Enter the board from yard to progress 0.</summary>
    public Token EnterBoard()
    {
        var t = Clone();
        t.State = TokenState.OnSharedTrack;
        t.Progress = 0;
        return t;
    }

    /// <summary>Move forward by steps. Returns new token or null if overshoot.</summary>
    public Token? MoveForward(int steps, PlayerColor playerColor)
    {
        if (State == TokenState.Finished)
            return null;

        int newProgress = Progress + steps;

        if (newProgress > 57)
            return null; // overshoot

        var t = Clone();
        t.Progress = newProgress;

        if (newProgress == 57)
        {
            t.State = TokenState.Finished;
        }
        else if (newProgress >= 52)
        {
            t.State = TokenState.InHomeLane;
        }
        else
        {
            t.State = TokenState.OnSharedTrack;
        }
        return t;
    }

    /// <summary>Send token back to yard.</summary>
    public Token ReturnToYard()
    {
        var t = Clone();
        t.State = TokenState.InYard;
        t.Progress = -1;
        return t;
    }

    /// <summary>Get the shared track index if on shared track, else -1.</summary>
    public int SharedTrackIndex => State == TokenState.OnSharedTrack
        ? BoardGeometry.GetSharedTrackIndex(Color, Progress)
        : -1;

    /// <summary>Get board position (row, col) or null if in yard.</summary>
    public (int Row, int Col)? GetPosition()
    {
        if (State == TokenState.InYard) return null;
        return BoardGeometry.GetPosition(Color, Progress);
    }

    /// <summary>Check if this token is on a safe square.</summary>
    public bool IsOnSafeSquare => State == TokenState.OnSharedTrack
        && BoardGeometry.SafeIndices.Contains(SharedTrackIndex);

    public override string ToString() => $"{Id} ({State}, progress={Progress})";
}
