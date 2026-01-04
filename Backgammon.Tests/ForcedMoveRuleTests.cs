using Backgammon.Core;

namespace Backgammon.Tests;

/// <summary>
/// Tests for the forced move rule: when only one die can be used,
/// must use the larger die value
/// </summary>
public class ForcedMoveRuleTests
{
    [Fact]
    public void HasValidMoves_WhenNoMovesAvailable_ShouldReturnFalse()
    {
        // Arrange - White has checkers on bar, Red blocks all entry points
        var game = new GameEngine();
        game.StartNewGame();

        // Clear board
        for (int i = 1; i <= 24; i++)
        {
            game.Board.GetPoint(i).Checkers.Clear();
        }

        // White has 3 checkers on bar
        game.WhitePlayer.CheckersOnBar = 3;

        // Red blocks points 20, 21, 22, 23, 24 (all possible entry points for White)
        for (int i = 20; i <= 24; i++)
        {
            game.Board.GetPoint(i).AddChecker(CheckerColor.Red);
            game.Board.GetPoint(i).AddChecker(CheckerColor.Red);
        }

        // White's turn with dice 2, 5
        game.SetCurrentPlayer(CheckerColor.White);
        game.Dice.SetDice(2, 5);
        game.RemainingMoves.Clear();
        game.RemainingMoves.AddRange(game.Dice.GetMoves());

        // Act
        var hasValidMoves = game.HasValidMoves();

        // Assert
        Assert.False(hasValidMoves);
    }

    [Fact]
    public void HasValidMoves_WhenOnlyOneDieCanBeUsed_ShouldReturnTrue()
    {
        // Arrange - White can use 2 but not 5
        var game = new GameEngine();
        game.StartNewGame();

        // Clear board
        for (int i = 1; i <= 24; i++)
        {
            game.Board.GetPoint(i).Checkers.Clear();
        }

        // White has 3 checkers on bar
        game.WhitePlayer.CheckersOnBar = 3;

        // Red blocks point 20 (blocks the 5 roll)
        for (int i = 0; i < 6; i++)
        {
            game.Board.GetPoint(20).AddChecker(CheckerColor.Red);
        }

        // Point 23 is open (allows the 2 roll)
        // Point 23 is empty by default, so White can enter with 2

        // White's turn with dice 2, 5
        game.SetCurrentPlayer(CheckerColor.White);
        game.Dice.SetDice(2, 5);
        game.RemainingMoves.Clear();
        game.RemainingMoves.AddRange(game.Dice.GetMoves());

        // Act - Initially should have valid move (the 2)
        var hasValidMovesBefore = game.HasValidMoves();
        var validMovesBefore = game.GetValidMoves();

        // Execute the 2 move
        var move2 = new Move(0, 23, 2);
        game.ExecuteMove(move2);

        // After using the 2, check if we can use the 5
        var hasValidMovesAfter = game.HasValidMoves();
        var validMovesAfter = game.GetValidMoves();

        // Assert
        Assert.True(hasValidMovesBefore);
        Assert.Single(validMovesBefore); // Only the 2-move should be valid
        Assert.False(hasValidMovesAfter); // The 5 is blocked
        Assert.Empty(validMovesAfter);
    }

    [Fact]
    public void GetValidMoves_WhenOnlyLargerDieCanBeUsed_ShouldReturnOnlyThatMove()
    {
        // Arrange - Setup where only the larger die (5) can be used
        var game = new GameEngine();
        game.StartNewGame();

        // Clear board
        for (int i = 1; i <= 24; i++)
        {
            game.Board.GetPoint(i).Checkers.Clear();
        }

        // White has 1 checker on bar
        game.WhitePlayer.CheckersOnBar = 1;

        // Block point 23 (blocks the 2 roll from bar)
        for (int i = 0; i < 6; i++)
        {
            game.Board.GetPoint(23).AddChecker(CheckerColor.Red);
        }

        // Point 20 is open (allows the 5 roll)

        // White's turn with dice 2, 5
        game.SetCurrentPlayer(CheckerColor.White);
        game.Dice.SetDice(2, 5);
        game.RemainingMoves.Clear();
        game.RemainingMoves.AddRange(game.Dice.GetMoves());

        // Act
        var validMoves = game.GetValidMoves();

        // Assert
        Assert.Single(validMoves);
        Assert.Equal(5, validMoves[0].DieValue); // Only the 5 should be valid
        Assert.Equal(0, validMoves[0].From); // From bar
        Assert.Equal(20, validMoves[0].To); // To point 20
    }

    [Fact]
    public void RemainingMovesCount_WhenMoveExecuted_ShouldDecrementByOne()
    {
        // Arrange
        var game = new GameEngine();
        game.StartNewGame();

        for (int i = 1; i <= 24; i++)
        {
            game.Board.GetPoint(i).Checkers.Clear();
        }

        game.Board.GetPoint(10).AddChecker(CheckerColor.White);

        game.SetCurrentPlayer(CheckerColor.White);
        game.Dice.SetDice(2, 5);
        game.RemainingMoves.Clear();
        game.RemainingMoves.AddRange(game.Dice.GetMoves());

        // Act
        var initialCount = game.RemainingMoves.Count;
        var move = new Move(10, 8, 2);
        game.ExecuteMove(move);
        var afterCount = game.RemainingMoves.Count;

        // Assert
        Assert.Equal(2, initialCount);
        Assert.Equal(1, afterCount);
    }
}
