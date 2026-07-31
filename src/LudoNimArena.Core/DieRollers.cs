using System.Collections.Immutable;
using System.Security.Cryptography;

namespace LudoNimArena.Core;

/// <summary>Production die roller using cryptographic RNG.</summary>
public class CryptoDieRoller : IDieRoller
{
    public int Roll(int minInclusive, int maxExclusive)
    {
        return RandomNumberGenerator.GetInt32(minInclusive, maxExclusive);
    }
}

/// <summary>Deterministic die roller for testing.</summary>
public class SeededDieRoller : IDieRoller
{
    private readonly Random _random;

    public SeededDieRoller(int seed)
    {
        _random = new Random(seed);
    }

    public int Roll(int minInclusive, int maxExclusive)
    {
        return _random.Next(minInclusive, maxExclusive);
    }
}
