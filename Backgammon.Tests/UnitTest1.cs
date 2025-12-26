using Backgammon.Core;

namespace Backgammon.Tests;

public class GameEngineTests
{
    [Fact]
    public void InitialSetup_ShouldPlaceCheckersCorrectly()
    {
        // Arrange
        var game = new GameEngine();
        
        // Act
        game.StartNewGame();
        
        // Assert - White's positions (moves 24->1)
        Assert.Equal(2, game.Board.GetPoint(24).Count);
        Assert.Equal(CheckerColor.White, game.Board.GetPoint(24).Color);
        Assert.Equal(5, game.Board.GetPoint(13).Count);
        Assert.Equal(CheckerColor.White, game.Board.GetPoint(13).Color);
        Assert.Equal(3, game.Board.GetPoint(8).Count);
        Assert.Equal(CheckerColor.White, game.Board.GetPoint(8).Color);
        Assert.Equal(5, game.Board.GetPoint(6).Count);
        Assert.Equal(CheckerColor.White, game.Board.GetPoint(6).Color);
        
        // Assert - Red's positions (moves 1->24)
        Assert.Equal(2, game.Board.GetPoint(1).Count);
        Assert.Equal(CheckerColor.Red, game.Board.GetPoint(1).Color);
        Assert.Equal(5, game.Board.GetPoint(12).Count);
        Assert.Equal(CheckerColor.Red, game.Board.GetPoint(12).Color);
        Assert.Equal(3, game.Board.GetPoint(17).Count);
        Assert.Equal(CheckerColor.Red, game.Board.GetPoint(17).Color);
        Assert.Equal(5, game.Board.GetPoint(19).Count);
        Assert.Equal(CheckerColor.Red, game.Board.GetPoint(19).Color);
    }

    [Fact]
    public void Move_ValidMove_ShouldSucceed()
    {
        // Arrange
        var game = new GameEngine();
        game.StartNewGame();
        
        // Clear and set up simple scenario
        game.Board.GetPoint(1).Checkers.Clear();
        game.Board.GetPoint(1).AddChecker(CheckerColor.White);
        
        while (game.CurrentPlayer.Color != CheckerColor.White)
            game.EndTurn();
        
        game.Dice.SetDice(3, 4);
        game.RemainingMoves.Clear();
        game.RemainingMoves.AddRange(game.Dice.GetMoves());
        
        // Act
        var move = new Move(1, 4, 3);
        var isValid = game.IsValidMove(move);
        
        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void Hitting_BlotHit_ShouldSendToBar()
    {
        // Arrange
        var game = new GameEngine();
        game.StartNewGame();
        
        for (int i = 1; i <= 24; i++)
            game.Board.GetPoint(i).Checkers.Clear();
        
        game.Board.GetPoint(5).AddChecker(CheckerColor.White);
        game.Board.GetPoint(8).AddChecker(CheckerColor.Red);
        
        while (game.CurrentPlayer.Color != CheckerColor.White)
            game.EndTurn();
        
        game.Dice.SetDice(3, 4);
        game.RemainingMoves.Clear();
        game.RemainingMoves.AddRange(game.Dice.GetMoves());
        
        // Act
        var move = new Move(5, 8, 3);
        game.ExecuteMove(move);
        
        // Assert
        Assert.Equal(1, game.RedPlayer.CheckersOnBar);
        Assert.Equal(CheckerColor.White, game.Board.GetPoint(8).Color);
    }

    [Fact]
    public void BearOff_AllCheckersInHome_ShouldBeValid()
    {
        // Arrange
        var game = new GameEngine();
        game.StartNewGame();
        
        for (int i = 1; i <= 24; i++)
            game.Board.GetPoint(i).Checkers.Clear();
        
        game.Board.GetPoint(3).AddChecker(CheckerColor.White);
        game.Board.GetPoint(5).AddChecker(CheckerColor.White);
        
        while (game.CurrentPlayer.Color != CheckerColor.White)
            game.EndTurn();
        
        game.WhitePlayer.CheckersOnBar = 0;
        game.Dice.SetDice(3, 5);
        game.RemainingMoves.Clear();
        game.RemainingMoves.AddRange(game.Dice.GetMoves());
        
        // Act
        var move = new Move(3, 0, 3);
        var isValid = game.IsValidMove(move);
        
        // Assert
        Assert.True(isValid);
        Assert.True(game.Board.AreAllCheckersInHomeBoard(game.WhitePlayer, 0));
    }

    [Fact]
    public void Doubles_ShouldGiveFourMoves()
    {
        // Arrange
        var game = new GameEngine();
        game.Dice.SetDice(4, 4);
        
        // Act
        var moves = game.Dice.GetMoves();
        
        // Assert
        Assert.Equal(4, moves.Count);
        Assert.All(moves, m => Assert.Equal(4, m));
    }

    [Fact]
    public void EnterFromBar_ValidEntry_ShouldSucceed()
    {
        // Arrange
        var game = new GameEngine();
        game.StartNewGame();
        
        while (game.CurrentPlayer.Color != CheckerColor.White)
            game.EndTurn();
        
        game.WhitePlayer.CheckersOnBar = 1;
        game.Dice.SetDice(2, 5);
        game.RemainingMoves.Clear();
        game.RemainingMoves.AddRange(game.Dice.GetMoves());
        
        // Act
        var move = new Move(0, 2, 2);
        var isValid = game.IsValidMove(move);
        
        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void BearOff_AllFifteenCheckers_ShouldWinGame()
    {
        // Arrange
        var game = new GameEngine();
        game.StartNewGame();
        
        while (game.CurrentPlayer.Color != CheckerColor.White)
            game.EndTurn();
        
        game.WhitePlayer.CheckersBornOff = 14;
        
        for (int i = 1; i <= 24; i++)
            game.Board.GetPoint(i).Checkers.Clear();
        
        game.Board.GetPoint(1).AddChecker(CheckerColor.White);
        game.Dice.SetDice(1, 2);
        game.RemainingMoves.Clear();
        game.RemainingMoves.AddRange(game.Dice.GetMoves());
        
        // Act
        var move = new Move(1, 0, 1);
        game.ExecuteMove(move);
        
        // Assert
        Assert.True(game.GameOver);
        Assert.Equal(game.WhitePlayer, game.Winner);
    }

    [Fact]
    public void DoublingCube_CanDouble_ShouldBeValid()
    {
        // Arrange
        var cube = new DoublingCube();
        
        // Act & Assert
        Assert.True(cube.CanDouble(CheckerColor.White));
        Assert.True(cube.CanDouble(CheckerColor.Red));
        Assert.Equal(1, cube.Value);
    }

    [Fact]
    public void DoublingCube_AfterDouble_OnlyOwnerCanRedouble()
    {
        // Arrange
        var cube = new DoublingCube();
        
        // Act
        cube.Double(CheckerColor.White);
        
        // Assert
        Assert.Equal(2, cube.Value);
        Assert.Equal(CheckerColor.White, cube.Owner);
        Assert.True(cube.CanDouble(CheckerColor.White));
        Assert.False(cube.CanDouble(CheckerColor.Red));
    }
}
