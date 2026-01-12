using Backgammon.Core;
using Backgammon.Server.Services;

namespace Backgammon.Tests;

/// <summary>
/// Tests for combined dice moves functionality.
/// </summary>
public class CombinedMovesTests
{
    [Fact]
    public void CombinedMoves_TwoDice_ShouldReturnCombinedDestination()
    {
        // Arrange - Set up a simple scenario with unblocked path
        var session = new GameSession("test-game");
        session.Engine.StartNewGame();

        // Clear points 17, 18, 23 to ensure path is unblocked
        session.Engine.Board.GetPoint(17).Checkers.Clear();
        session.Engine.Board.GetPoint(18).Checkers.Clear();
        session.Engine.Board.GetPoint(23).Checkers.Clear();

        // Set current player to White
        session.Engine.SetCurrentPlayer(CheckerColor.White);

        // Set dice to 6-1
        session.Engine.Dice.SetDice(6, 1);
        session.Engine.RemainingMoves.Clear();
        session.Engine.RemainingMoves.AddRange(session.Engine.Dice.GetMoves());

        // Act - Get game state which includes combined moves
        var state = session.GetState("test-connection");

        // Assert - Should have combined move from 24 to 17 (6+1)
        var combinedMoves = state.ValidMoves.Where(m => m.IsCombinedMove).ToList();
        Assert.NotEmpty(combinedMoves);

        var move24To17 = combinedMoves.FirstOrDefault(m => m.From == 24 && m.To == 17);
        Assert.NotNull(move24To17);
        Assert.True(move24To17.IsCombinedMove);
        Assert.Equal(7, move24To17.DieValue); // 6 + 1
        Assert.NotNull(move24To17.DiceUsed);
        Assert.Equal(2, move24To17.DiceUsed.Length);
        Assert.NotNull(move24To17.IntermediatePoints);
        Assert.Single(move24To17.IntermediatePoints); // One intermediate point (18 or 23)
    }

    [Fact]
    public void CombinedMoves_BlockedIntermediate_ShouldNotReturnCombinedMove()
    {
        // Arrange - White at point 24, dice 6-1, but point 18 blocked by 2 Red checkers
        var session = new GameSession("test-game");
        session.Engine.StartNewGame();

        // Set current player to White
        session.Engine.SetCurrentPlayer(CheckerColor.White);

        // Block point 18 with 2 Red checkers
        session.Engine.Board.GetPoint(18).Checkers.Clear();
        session.Engine.Board.GetPoint(18).AddChecker(CheckerColor.Red);
        session.Engine.Board.GetPoint(18).AddChecker(CheckerColor.Red);

        // Also block point 23 (the other intermediate for 24 via 1 then 6)
        session.Engine.Board.GetPoint(23).Checkers.Clear();
        session.Engine.Board.GetPoint(23).AddChecker(CheckerColor.Red);
        session.Engine.Board.GetPoint(23).AddChecker(CheckerColor.Red);

        // Set dice to 6-1
        session.Engine.Dice.SetDice(6, 1);
        session.Engine.RemainingMoves.Clear();
        session.Engine.RemainingMoves.AddRange(session.Engine.Dice.GetMoves());

        // Act
        var state = session.GetState("test-connection");

        // Assert - Should NOT have combined move to 17 since both intermediates are blocked
        var combinedMoves = state.ValidMoves.Where(m => m.IsCombinedMove).ToList();
        var move24To17 = combinedMoves.FirstOrDefault(m => m.From == 24 && m.To == 17);
        Assert.Null(move24To17);
    }

    [Fact]
    public void CombinedMoves_Doubles_ShouldReturnMultipleCombinations()
    {
        // Arrange - White at point 24 with doubles 3-3-3-3
        var session = new GameSession("test-game");
        session.Engine.StartNewGame();

        // Set current player to White
        session.Engine.SetCurrentPlayer(CheckerColor.White);

        // Clear blocking pieces on path (12-23)
        for (int i = 12; i <= 23; i++)
        {
            session.Engine.Board.GetPoint(i).Checkers.Clear();
        }

        // Set dice to 3-3 (doubles)
        session.Engine.Dice.SetDice(3, 3);
        session.Engine.RemainingMoves.Clear();
        session.Engine.RemainingMoves.AddRange(session.Engine.Dice.GetMoves());

        // Act
        var state = session.GetState("test-connection");

        // Assert - Should have combined moves for 6 (2x3), 9 (3x3), and 12 (4x3)
        var combinedMovesFrom24 = state.ValidMoves
            .Where(m => m.IsCombinedMove && m.From == 24)
            .ToList();

        // Should have 2-dice combination (24 to 18, distance 6)
        var twoDiceMove = combinedMovesFrom24.FirstOrDefault(m => m.To == 18);
        Assert.NotNull(twoDiceMove);
        Assert.Equal(2, twoDiceMove.DiceUsed?.Length);

        // Should have 3-dice combination (24 to 15, distance 9)
        var threeDiceMove = combinedMovesFrom24.FirstOrDefault(m => m.To == 15);
        Assert.NotNull(threeDiceMove);
        Assert.Equal(3, threeDiceMove.DiceUsed?.Length);

        // Should have 4-dice combination (24 to 12, distance 12)
        var fourDiceMove = combinedMovesFrom24.FirstOrDefault(m => m.To == 12);
        Assert.NotNull(fourDiceMove);
        Assert.Equal(4, fourDiceMove.DiceUsed?.Length);
    }

    [Fact]
    public void CombinedMoves_BarEntry_ShouldNotHaveCombinedMoves()
    {
        // Arrange - White checker on bar
        var session = new GameSession("test-game");
        session.Engine.StartNewGame();

        // Set current player to White
        session.Engine.SetCurrentPlayer(CheckerColor.White);

        // Put a White checker on the bar
        session.Engine.Board.GetPoint(24).RemoveChecker();
        session.Engine.WhitePlayer.CheckersOnBar = 1;

        // Set dice to 6-1
        session.Engine.Dice.SetDice(6, 1);
        session.Engine.RemainingMoves.Clear();
        session.Engine.RemainingMoves.AddRange(session.Engine.Dice.GetMoves());

        // Act
        var state = session.GetState("test-connection");

        // Assert - Should NOT have any combined moves (must enter from bar first)
        var combinedMoves = state.ValidMoves.Where(m => m.IsCombinedMove).ToList();
        Assert.Empty(combinedMoves);
    }

    [Fact]
    public void CombinedMoves_SingleDieDestinationsExcluded()
    {
        // Arrange - Verify that destinations reachable with single die are not in combined moves
        var session = new GameSession("test-game");
        session.Engine.StartNewGame();

        // Set White as current player
        while (session.Engine.CurrentPlayer.Color != CheckerColor.White)
        {
            session.Engine.EndTurn();
        }

        // Set dice to 6-1
        session.Engine.Dice.SetDice(6, 1);
        session.Engine.RemainingMoves.Clear();
        session.Engine.RemainingMoves.AddRange(session.Engine.Dice.GetMoves());

        // Act
        var state = session.GetState("test-connection");

        // Get single-die destinations from point 24
        var singleMoveDests = state.ValidMoves
            .Where(m => !m.IsCombinedMove && m.From == 24)
            .Select(m => m.To)
            .ToHashSet();

        // Get combined move destinations from point 24
        var combinedMoveDests = state.ValidMoves
            .Where(m => m.IsCombinedMove && m.From == 24)
            .Select(m => m.To)
            .ToHashSet();

        // Assert - No overlap between single and combined destinations
        var overlap = singleMoveDests.Intersect(combinedMoveDests);
        Assert.Empty(overlap);
    }

    [Fact]
    public void CombinedMoves_IntermediatePointsCorrect()
    {
        // Arrange
        var session = new GameSession("test-game");
        session.Engine.StartNewGame();

        // Clear points 17, 18, 23 to ensure path is unblocked
        session.Engine.Board.GetPoint(17).Checkers.Clear();
        session.Engine.Board.GetPoint(18).Checkers.Clear();
        session.Engine.Board.GetPoint(23).Checkers.Clear();

        // Set current player to White
        session.Engine.SetCurrentPlayer(CheckerColor.White);

        // Set dice to 6-1
        session.Engine.Dice.SetDice(6, 1);
        session.Engine.RemainingMoves.Clear();
        session.Engine.RemainingMoves.AddRange(session.Engine.Dice.GetMoves());

        // Act
        var state = session.GetState("test-connection");

        // Assert - Combined move from 24 to 17 should have intermediate at 18 or 23
        var move24To17 = state.ValidMoves.FirstOrDefault(m =>
            m.IsCombinedMove && m.From == 24 && m.To == 17);

        Assert.NotNull(move24To17);
        Assert.NotNull(move24To17.IntermediatePoints);
        Assert.Single(move24To17.IntermediatePoints);

        var intermediate = move24To17.IntermediatePoints[0];
        Assert.True(
            intermediate == 18 || intermediate == 23,
            $"Intermediate should be 18 (via 6 first) or 23 (via 1 first), got {intermediate}");
    }

    [Fact]
    public void CombinedMoves_BearingOff_ShouldWork()
    {
        // Arrange - All White checkers in home board, dice that can bear off combined
        var session = new GameSession("test-game");
        session.Engine.StartNewGame();

        // Clear board and set up bearing off scenario
        for (int i = 1; i <= 24; i++)
        {
            session.Engine.Board.GetPoint(i).Checkers.Clear();
        }

        // Put White checkers in home board (points 1-6)
        session.Engine.Board.GetPoint(6).AddChecker(CheckerColor.White);
        session.Engine.Board.GetPoint(5).AddChecker(CheckerColor.White);

        // Set White as current player
        while (session.Engine.CurrentPlayer.Color != CheckerColor.White)
        {
            session.Engine.EndTurn();
        }

        // Set dice to 3-2 (can bear off from point 5 with 3+2=5)
        session.Engine.Dice.SetDice(3, 2);
        session.Engine.RemainingMoves.Clear();
        session.Engine.RemainingMoves.AddRange(session.Engine.Dice.GetMoves());

        // Act
        var state = session.GetState("test-connection");

        // Assert - Combined bear-off from point 5 to 0 (via intermediate point 2 or 3)
        // Path: 5 -> 2 (using 3) -> 0 (using 2), or 5 -> 3 (using 2) -> 0 (using 3)
        var combinedBearOff = state.ValidMoves.FirstOrDefault(m =>
            m.IsCombinedMove && m.From == 5 && m.To == 25);

        Assert.NotNull(combinedBearOff);
        Assert.True(combinedBearOff.IsCombinedMove);
        Assert.Equal(5, combinedBearOff.DieValue); // 3 + 2 = 5
        Assert.NotNull(combinedBearOff.DiceUsed);
        Assert.Equal(2, combinedBearOff.DiceUsed.Length);
    }

    [Fact]
    public void CombinedMoves_BearingOff_ShouldNotAllowWhenHigherPointsOccupied()
    {
        // Arrange - Bug scenario: dice 6,2 with checkers on points 1-5
        // Combined move 3→1→off should NOT be valid because:
        // - After 3→1, highest point is still 5
        // - Bearing off from 1 with die 6 (6 > 1) requires 1 to be highest point
        // - Since 5 is occupied, 1 is not the highest point
        var session = new GameSession("test-game");
        session.Engine.StartNewGame();

        // Clear board and set up scenario
        for (int i = 1; i <= 24; i++)
        {
            session.Engine.Board.GetPoint(i).Checkers.Clear();
        }

        // Put White checkers on points 1-5 (similar to screenshot scenario)
        session.Engine.Board.GetPoint(1).AddChecker(CheckerColor.White);
        session.Engine.Board.GetPoint(1).AddChecker(CheckerColor.White);
        session.Engine.Board.GetPoint(2).AddChecker(CheckerColor.White);
        session.Engine.Board.GetPoint(2).AddChecker(CheckerColor.White);
        session.Engine.Board.GetPoint(3).AddChecker(CheckerColor.White);
        session.Engine.Board.GetPoint(4).AddChecker(CheckerColor.White);
        session.Engine.Board.GetPoint(5).AddChecker(CheckerColor.White);

        // Set White as current player
        session.Engine.SetCurrentPlayer(CheckerColor.White);

        // Set dice to 6-2 (the scenario in the bug report)
        session.Engine.Dice.SetDice(6, 2);
        session.Engine.RemainingMoves.Clear();
        session.Engine.RemainingMoves.AddRange(session.Engine.Dice.GetMoves());

        // Act
        var state = session.GetState("test-connection");

        // Assert - Should NOT have combined bear-off from point 3 to 0
        // Because after 3→1, highest point is still 5, so can't bear off from 1 with die 6
        var invalidCombinedBearOff = state.ValidMoves.FirstOrDefault(m =>
            m.IsCombinedMove && m.From == 3 && m.To == 25);

        Assert.Null(invalidCombinedBearOff);

        // However, valid single moves should still exist:
        // - 5→off with die 6 (can bear off from 5 when 5 is highest and die > 5)
        var validBearOff = state.ValidMoves.FirstOrDefault(m =>
            !m.IsCombinedMove && m.From == 5 && m.To == 25);
        Assert.NotNull(validBearOff);
    }

    [Fact]
    public void CombinedMoves_BearingOff_ShouldAllowWhenHighestPointMovedAway()
    {
        // Arrange - If the original source IS the highest point and has only 1 checker,
        // then after moving, the next point becomes the effective highest
        var session = new GameSession("test-game");
        session.Engine.StartNewGame();

        // Clear board and set up scenario
        for (int i = 1; i <= 24; i++)
        {
            session.Engine.Board.GetPoint(i).Checkers.Clear();
        }

        // Put only 2 White checkers: one on point 4 and one on point 2
        session.Engine.Board.GetPoint(4).AddChecker(CheckerColor.White);
        session.Engine.Board.GetPoint(2).AddChecker(CheckerColor.White);

        // Set White as current player
        session.Engine.SetCurrentPlayer(CheckerColor.White);

        // Set dice to 3-2 (neither can bear off from 4 directly since 3<4 and 2<4)
        session.Engine.Dice.SetDice(3, 2);
        session.Engine.RemainingMoves.Clear();
        session.Engine.RemainingMoves.AddRange(session.Engine.Dice.GetMoves());

        // Act
        var state = session.GetState("test-connection");

        // Assert - Combined move 4→2→off SHOULD be valid because:
        // - Move 4→2 with die 2
        // - After this, original source (4) is empty, so effective highest is now 2
        // - Bear off from 2 with die 3 is valid because 3 > 2 and 2 is now the highest point
        // Note: Bear-off destination (0) is NOT reachable with single die from 4
        var validCombinedBearOff = state.ValidMoves.FirstOrDefault(m =>
            m.IsCombinedMove && m.From == 4 && m.To == 25);

        Assert.NotNull(validCombinedBearOff);
    }
}
