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

public class RulesEngineTests
{
    private readonly RulesEngine _engine = new();

    [Fact]
    public void GenerateLegalMoves_FromYard_RequiresSix()
    {
        var state = TestStateFactory.CreateFourPlayerGame();
        var moves = _engine.GenerateLegalMoves(state, PlayerColor.Red, 3);
        moves.Should().BeEmpty("need a six to enter");
    }

    [Fact]
    public void GenerateLegalMoves_FromYard_WithSix_AllowsEntry()
    {
        var state = TestStateFactory.CreateFourPlayerGame();
        var moves = _engine.GenerateLegalMoves(state, PlayerColor.Red, 6);
        moves.Should().NotBeEmpty("six allows entering a token");
        moves.Should().AllSatisfy(m => m.EntersBoard.Should().BeTrue());
        // All four tokens can enter from yard
        moves.Should().HaveCount(4);
    }

    [Fact]
    public void GenerateLegalMoves_OnSharedTrack_MovesByDieValue()
    {
        var state = TestStateFactory.CreateFourPlayerGame()
            .PlaceTokenOnTrack(PlayerColor.Red, 0, 5);

        var moves = _engine.GenerateLegalMoves(state, PlayerColor.Red, 4);
        moves.Should().ContainSingle(m => m.TokenId == "red-token-0" && m.ToProgress == 9);
    }

    [Fact]
    public void GenerateLegalMoves_CannotOvershootFinish()
    {
        var state = TestStateFactory.CreateFourPlayerGame()
            .PlaceTokenInHomeLane(PlayerColor.Red, 0, 55);

        // Need exactly 2 to finish
        var movesWith2 = _engine.GenerateLegalMoves(state, PlayerColor.Red, 2);
        movesWith2.Should().ContainSingle(m => m.Finishes && m.ToProgress == 57);

        // Roll 5 would overshoot
        var movesWith5 = _engine.GenerateLegalMoves(state, PlayerColor.Red, 5);
        movesWith5.Should().NotContain(m => m.TokenId == "red-token-0",
            "cannot overshoot progress 57");
    }

    [Fact]
    public void GenerateLegalMoves_FinishedToken_NeverMoves()
    {
        var state = TestStateFactory.CreateFourPlayerGame()
            .FinishToken(PlayerColor.Red, 0);

        // Roll a six, token 0 should not be in moves
        var moves = _engine.GenerateLegalMoves(state, PlayerColor.Red, 6);
        // Token 0 is finished; tokens 1,2,3 can enter
        moves.Should().NotContain(m => m.TokenId == "red-token-0");
    }

    [Fact]
    public void GenerateLegalMoves_EmptyList_WhenNoMoves()
    {
        var state = TestStateFactory.CreateFourPlayerGame();
        // All tokens in yard, no six
        var moves = _engine.GenerateLegalMoves(state, PlayerColor.Red, 1);
        moves.Should().BeEmpty();
    }

    [Fact]
    public void GenerateLegalMoves_WithCapture()
    {
        var state = TestStateFactory.CreateFourPlayerGame()
            .PlaceTokenOnTrack(PlayerColor.Red, 0, 18)  // Red at shared idx 18
            .PlaceTokenOnTrack(PlayerColor.Green, 0, 9); // Green at shared idx 22 (13+9)

        // Red rolls 4: 18+4=22 = shared idx 22, captures Green
        var moves = _engine.GenerateLegalMoves(state, PlayerColor.Red, 4);
        var captureMove = moves.FirstOrDefault(m => m.TokenId == "red-token-0" && m.Captures.Any());
        captureMove.Should().NotBeNull("Red should capture Green at shared idx 22");
        captureMove!.Captures.Should().Contain("green-token-0");
    }

    [Fact]
    public void GenerateLegalMoves_SafeSquare_PreventsCapture()
    {
        // Safe square at shared index 8
        // Green enters at progress 0 -> shared 13, needs to reach shared 8
        // Green progress: (8 - 13 + 52) % 52 = 47
        var state = TestStateFactory.CreateFourPlayerGame()
            .PlaceTokenOnTrack(PlayerColor.Green, 0, 47) // shared idx (13+47)%52=8
            .PlaceTokenOnTrack(PlayerColor.Red, 0, 4);   // shared idx 4

        // Red rolls 4: 4+4=8, lands on shared idx 8 (safe)
        var moves = _engine.GenerateLegalMoves(state, PlayerColor.Red, 4);
        var moveTo8 = moves.FirstOrDefault(m => m.TokenId == "red-token-0" && m.ToProgress == 8);
        moveTo8.Should().NotBeNull();
        moveTo8!.Captures.Should().BeEmpty("safe square prevents capture");
        moveTo8.LandsSafe.Should().BeTrue();
    }

    [Fact]
    public void GenerateLegalMoves_Blockade_BlocksOpponent()
    {
        // Green blockade at shared index 20
        // Green: progress (20-13+52)%52 = 7
        var state = TestStateFactory.CreateFourPlayerGame()
            .PlaceTokenOnTrack(PlayerColor.Green, 0, 7)  // shared idx 20
            .PlaceTokenOnTrack(PlayerColor.Green, 1, 7)  // shared idx 20 (blockade)
            .PlaceTokenOnTrack(PlayerColor.Red, 0, 18);  // shared idx 18

        // Red rolls 4: progress 18->22 (shared idx 22)
        // Path: shared idx 18->19->20->21->22
        // Blocked at shared idx 20
        var moves = _engine.GenerateLegalMoves(state, PlayerColor.Red, 4);
        moves.Should().NotContain(m => m.TokenId == "red-token-0",
            "blockade at shared index 20 should block Red");
    }

    [Fact]
    public void GenerateLegalMoves_OwnBlockade_DoesNotBlockOwner()
    {
        // Red blockade at shared index 10
        var state = TestStateFactory.CreateFourPlayerGame()
            .PlaceTokenOnTrack(PlayerColor.Red, 1, 10)   // shared idx 10
            .PlaceTokenOnTrack(PlayerColor.Red, 2, 10)   // shared idx 10 (own blockade)
            .PlaceTokenOnTrack(PlayerColor.Red, 0, 6);   // shared idx 6

        // Red token 0 rolls 6: progress 6->12, passes through shared idx 10
        var moves = _engine.GenerateLegalMoves(state, PlayerColor.Red, 6);
        moves.Should().Contain(m => m.TokenId == "red-token-0" && m.ToProgress == 12,
            "own blockade should not block");
    }

    [Fact]
    public void GenerateLegalMoves_CannotLandOnOwnBlockadeWithThird()
    {
        var state = TestStateFactory.CreateFourPlayerGame()
            .PlaceTokenOnTrack(PlayerColor.Red, 1, 10)   // shared idx 10
            .PlaceTokenOnTrack(PlayerColor.Red, 2, 10)   // shared idx 10 (blockade)
            .PlaceTokenOnTrack(PlayerColor.Red, 0, 8);   // shared idx 8

        // Red token 0 rolls 2: progress 8->10, lands on own blockade
        var moves = _engine.GenerateLegalMoves(state, PlayerColor.Red, 2);
        moves.Should().NotContain(m => m.TokenId == "red-token-0" && m.ToProgress == 10,
            "third token cannot land on own blockade");
    }

    [Fact]
    public void ApplyMove_EnterBoard_SetsProgressZero()
    {
        var state = TestStateFactory.CreateFourPlayerGame();
        var player = state.GetPlayer(PlayerColor.Red);
        player.Tokens[0].State.Should().Be(TokenState.InYard);

        var move = new LegalMove("red-token-0", -1, 0, entersBoard: true);
        var (newState, events) = _engine.ApplyMove(state, move, PlayerColor.Red);

        var updatedPlayer = newState.GetPlayer(PlayerColor.Red);
        updatedPlayer.Tokens[0].State.Should().Be(TokenState.OnSharedTrack);
        updatedPlayer.Tokens[0].Progress.Should().Be(0);
        events.Should().Contain(e => e is TokenEntered);
    }

    [Fact]
    public void ApplyMove_Capture_ReturnsTokenToYard()
    {
        var state = TestStateFactory.CreateFourPlayerGame()
            .PlaceTokenOnTrack(PlayerColor.Red, 0, 18)
            .PlaceTokenOnTrack(PlayerColor.Green, 0, 9); // shared idx 22

        var move = new LegalMove("red-token-0", 18, 22, entersBoard: false,
            captures: new[] { "green-token-0" });
        var (newState, events) = _engine.ApplyMove(state, move, PlayerColor.Red);

        var greenToken = newState.GetToken("green-token-0");
        greenToken.State.Should().Be(TokenState.InYard);
        greenToken.Progress.Should().Be(-1);
        events.Should().Contain(e => e is TokenCaptured);
    }

    [Fact]
    public void ApplyMove_Finish_SetsFinishedState()
    {
        var state = TestStateFactory.CreateFourPlayerGame()
            .PlaceTokenInHomeLane(PlayerColor.Red, 0, 55);

        var move = new LegalMove("red-token-0", 55, 57, entersBoard: false, finishes: true);
        var (newState, events) = _engine.ApplyMove(state, move, PlayerColor.Red);

        var token = newState.GetToken("red-token-0");
        token.State.Should().Be(TokenState.Finished);
        token.Progress.Should().Be(57);
        events.Should().Contain(e => e is TokenFinished);
    }

    [Fact]
    public void ApplyMove_Victory_DeclaresWinner()
    {
        var state = TestStateFactory.CreateFourPlayerGame()
            .FinishToken(PlayerColor.Red, 0)
            .FinishToken(PlayerColor.Red, 1)
            .FinishToken(PlayerColor.Red, 2)
            .PlaceTokenInHomeLane(PlayerColor.Red, 3, 55);

        var move = new LegalMove("red-token-3", 55, 57, entersBoard: false, finishes: true);
        var (newState, events) = _engine.ApplyMove(state, move, PlayerColor.Red);

        newState.Winner.Should().Be(PlayerColor.Red);
        newState.Phase.Should().Be(GamePhase.GameOver);
        events.Should().Contain(e => e is PlayerWon);
    }

    [Fact]
    public void GetNextPlayer_ClockwiseOrder()
    {
        _engine.GetNextPlayer(PlayerColor.Red).Should().Be(PlayerColor.Green);
        _engine.GetNextPlayer(PlayerColor.Green).Should().Be(PlayerColor.Yellow);
        _engine.GetNextPlayer(PlayerColor.Yellow).Should().Be(PlayerColor.Blue);
        _engine.GetNextPlayer(PlayerColor.Blue).Should().Be(PlayerColor.Red);
    }

    [Fact]
    public void TurnOrder_IsFixedClockwise()
    {
        RulesEngine.TurnOrder.Should().Equal(
            PlayerColor.Red, PlayerColor.Green, PlayerColor.Yellow, PlayerColor.Blue);
    }

    [Fact]
    public void GenerateLegalMoves_HomeLaneEntry()
    {
        // Place Red at progress 50 (shared idx 50)
        var state = TestStateFactory.CreateFourPlayerGame()
            .PlaceTokenOnTrack(PlayerColor.Red, 0, 50);

        // Roll 4: progress 50->54 (home lane cell 2)
        var moves = _engine.GenerateLegalMoves(state, PlayerColor.Red, 4);
        moves.Should().ContainSingle(m => m.TokenId == "red-token-0" && m.ToProgress == 54);
    }

    [Fact]
    public void GenerateLegalMoves_MultipleTokenChoices()
    {
        // Red: token 0 at progress 10, token 1 at progress 30
        var state = TestStateFactory.CreateFourPlayerGame()
            .PlaceTokenOnTrack(PlayerColor.Red, 0, 10)
            .PlaceTokenOnTrack(PlayerColor.Red, 1, 30);

        var moves = _engine.GenerateLegalMoves(state, PlayerColor.Red, 5);
        // Should have moves for both tokens
        moves.Should().Contain(m => m.TokenId == "red-token-0" && m.ToProgress == 15);
        moves.Should().Contain(m => m.TokenId == "red-token-1" && m.ToProgress == 35);
    }
}
