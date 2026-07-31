namespace LudoNimArena.Core;

/// <summary>Abstraction for generating authoritative die results.</summary>
public interface IDieRoller
{
    /// <summary>Generate a random integer in [minInclusive, maxExclusive).</summary>
    int Roll(int minInclusive, int maxExclusive);
}
