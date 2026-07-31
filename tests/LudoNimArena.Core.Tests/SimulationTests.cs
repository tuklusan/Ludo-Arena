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

using FluentAssertions;
using LudoNimArena.Core;

namespace LudoNimArena.Core.Tests;

public class SimulationTests
{
    [Fact]
    public void RunSingleGame_CompletesWithWinner()
    {
        var engine = new RulesEngine();
        var dieRoller = new SeededDieRoller(42);
        var state = TestStateFactory.CreateFourPlayerGame();

        int maxMoves = 2000;
        int moveCount = 0;
        bool gameOver = false;

        while (!gameOver && moveCount < maxMoves)
        {
            moveCount++;
            var color = state.CurrentPlayer;
            int dieResult = dieRoller.Roll(1, 7);

            // Handle third-six rule
            if (dieResult == 6)
                state = state.WithConsecutiveSixCount(state.ConsecutiveSixCount + 1);
            else
                state = state.WithConsecutiveSixCount(0);

            if (state.ConsecutiveSixCount >= 3)
            {
                state = state.WithConsecutiveSixCount(0);
                state = state.WithCurrentPlayer(engine.GetNextPlayer(color));
                continue;
            }

            var legalMoves = engine.GenerateLegalMoves(state, color, dieResult);

            if (legalMoves.Count > 0)
            {
                // Simple fallback: pick first legal move
                var selected = legalMoves[0];
                var (newState, _) = engine.ApplyMove(state, selected, color);
                state = newState;

                if (state.Winner.HasValue)
                {
                    gameOver = true;
                }
                else if (dieResult == 6)
                {
                    // Bonus roll - same player continues
                    continue;
                }
            }

            state = state.WithCurrentPlayer(engine.GetNextPlayer(color));
        }

        gameOver.Should().BeTrue("game should complete within max moves");
        state.Winner.Should().NotBeNull("a winner should be declared");
        moveCount.Should().BeLessThan(maxMoves, "game should not exceed safety limit");
    }

    [Fact]
    public void RunHundredGames_AllComplete()
    {
        int gameCount = 100;
        int maxMovesPerGame = 2000;
        var winners = new Dictionary<PlayerColor, int>
        {
            [PlayerColor.Red] = 0,
            [PlayerColor.Green] = 0,
            [PlayerColor.Yellow] = 0,
            [PlayerColor.Blue] = 0
        };
        int failures = 0;

        for (int seed = 1; seed <= gameCount; seed++)
        {
            var engine = new RulesEngine();
            var dieRoller = new SeededDieRoller(seed);
            var state = TestStateFactory.CreateFourPlayerGame();

            int moveCount = 0;
            bool gameOver = false;

            while (!gameOver && moveCount < maxMovesPerGame)
            {
                moveCount++;
                var color = state.CurrentPlayer;
                int dieResult = dieRoller.Roll(1, 7);

                if (dieResult == 6)
                    state = state.WithConsecutiveSixCount(state.ConsecutiveSixCount + 1);
                else
                    state = state.WithConsecutiveSixCount(0);

                if (state.ConsecutiveSixCount >= 3)
                {
                    state = state.WithConsecutiveSixCount(0);
                    state = state.WithCurrentPlayer(engine.GetNextPlayer(color));
                    continue;
                }

                var legalMoves = engine.GenerateLegalMoves(state, color, dieResult);

                if (legalMoves.Count > 0)
                {
                    var selected = legalMoves[0];
                    var (newState, _) = engine.ApplyMove(state, selected, color);
                    state = newState;

                    if (state.Winner.HasValue)
                    {
                        gameOver = true;
                        winners[state.Winner.Value]++;
                    }
                    else if (dieResult == 6)
                    {
                        continue; // bonus roll
                    }
                }

                state = state.WithCurrentPlayer(engine.GetNextPlayer(color));
            }

            if (!gameOver)
                failures++;
        }

        failures.Should().Be(0, "all 100 games should complete");
    }

    [Theory]
    [InlineData(0)]   // Red starts
    [InlineData(1)]   // Green starts
    [InlineData(2)]   // Yellow starts
    [InlineData(3)]   // Blue starts
    public void RunGameFromEachStartColor(int startColorIndex)
    {
        var startColor = (PlayerColor)startColorIndex;
        var engine = new RulesEngine();
        var dieRoller = new SeededDieRoller(startColorIndex * 100 + 7);
        var state = TestStateFactory.CreateFourPlayerGame()
            .WithCurrentPlayer(startColor);

        int maxMoves = 2000;
        int moveCount = 0;
        bool gameOver = false;

        while (!gameOver && moveCount < maxMoves)
        {
            moveCount++;
            var color = state.CurrentPlayer;
            int dieResult = dieRoller.Roll(1, 7);

            if (dieResult == 6)
                state = state.WithConsecutiveSixCount(state.ConsecutiveSixCount + 1);
            else
                state = state.WithConsecutiveSixCount(0);

            if (state.ConsecutiveSixCount >= 3)
            {
                state = state.WithConsecutiveSixCount(0);
                state = state.WithCurrentPlayer(engine.GetNextPlayer(color));
                continue;
            }

            var legalMoves = engine.GenerateLegalMoves(state, color, dieResult);
            if (legalMoves.Count > 0)
            {
                var selected = legalMoves[0];
                var (newState, _) = engine.ApplyMove(state, selected, color);
                state = newState;

                if (state.Winner.HasValue)
                {
                    gameOver = true;
                }
                else if (dieResult == 6)
                    continue;
            }
            state = state.WithCurrentPlayer(engine.GetNextPlayer(color));
        }

        gameOver.Should().BeTrue($"game starting with {startColor} should complete");
    }
}
