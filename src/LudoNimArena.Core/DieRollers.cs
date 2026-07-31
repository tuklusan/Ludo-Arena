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
