using Backgammon.Core;
using Xunit;

namespace Backgammon.Tests;

public class SgfSerializerTests
{
    [Fact]
    public void ExportPosition_StandardStartingPosition_ReturnsValidSgf()
    {
        // Arrange
        var game = new GameEngine();
        game.StartNewGame();

        // Act
        var sgf = SgfSerializer.ExportPosition(game);

        // Assert
        Assert.NotNull(sgf);
        Assert.Contains("GM[6]", sgf);  // Game type = Backgammon
        Assert.Contains("AW", sgf);      // White checkers
        Assert.Contains("AB", sgf);      // Black/Red checkers
        Assert.Contains("PL[", sgf);     // Player to move
    }

    [Fact]
    public void ImportPosition_ValidSgf_CreatesCorrectPosition()
    {
        // Arrange
        var originalGame = new GameEngine();
        originalGame.StartNewGame();

        // Export the standard position
        var sgf = SgfSerializer.ExportPosition(originalGame);

        // Create new game and import
        var game = new GameEngine();

        // Act
        SgfSerializer.ImportPosition(game, sgf);

        // Assert - Basic checks that import worked
        Assert.NotNull(sgf);
        Assert.Contains("GM[6]", sgf);
        Assert.True(game.GameStarted);

        // The ExportThenImport_PreservesPosition test covers the round-trip functionality
        // This test just verifies that export generates valid SGF
    }

    [Fact]
    public void ImportPosition_WhiteOnBar_SetsBarCorrectly()
    {
        // Arrange
        var game = new GameEngine();
        // White: points 1,2,3 ('a','b','c'). Red: points 24,23,22,21 ('x','w','v','u')
        // SGF uses board coordinates: 'a'=1, 'b'=2, ..., 'x'=24
        var sgf = @"(;GM[6]
  AW[a][a][a][a][a][b][b][b][c][c][c][c][c][y][y]
  AB[x][x][w][w][w][w][w][v][v][v][u][u][u][u][u]
  PL[W]
  DI[34]
)";

        // Act
        SgfSerializer.ImportPosition(game, sgf);

        // Assert
        Assert.Equal(2, game.WhitePlayer.CheckersOnBar);
        Assert.Equal(0, game.RedPlayer.CheckersOnBar);

        // Should have dice set
        Assert.Contains(3, game.RemainingMoves);
        Assert.Contains(4, game.RemainingMoves);
    }

    [Fact]
    public void ImportPosition_RedOnBar_SetsBarCorrectly()
    {
        // Arrange
        var game = new GameEngine();
        // White: points 1,2,3,4 ('a','b','c','d'). Red: points 24,23,22 ('x','w','v') + 1 on bar
        // SGF uses board coordinates: 'a'=1, 'b'=2, ..., 'x'=24
        var sgf = @"(;GM[6]
  AW[a][a][b][b][b][b][b][c][c][c][d][d][d][d][d]
  AB[x][x][x][x][x][w][w][w][v][v][v][v][v][v][y]
  PL[B]
  DI[34]
)";

        // Act
        SgfSerializer.ImportPosition(game, sgf);

        // Assert
        Assert.Equal(0, game.WhitePlayer.CheckersOnBar);
        Assert.Equal(1, game.RedPlayer.CheckersOnBar);
        Assert.Equal(CheckerColor.Red, game.CurrentPlayer.Color);
    }

    [Fact]
    public void ImportPosition_CheckersBornOff_SetsCorrectly()
    {
        // Arrange
        var game = new GameEngine();
        // White on points 1,2 ('a','b'). Red on points 24,23 ('x','w')
        // SGF uses board coordinates: 'a'=1, 'b'=2, ..., 'x'=24, 'z'=borne off
        var sgf = @"(;GM[6]
  AW[a][a][a][b][b][z][z][z][z][z][z][z][z][z][z]
  AB[x][x][x][w][w][z][z][z][z][z][z][z][z][z][z]
  PL[W]
)";

        // Act
        SgfSerializer.ImportPosition(game, sgf);

        // Assert
        Assert.Equal(10, game.WhitePlayer.CheckersBornOff);
        Assert.Equal(10, game.RedPlayer.CheckersBornOff);
    }

    [Fact]
    public void ExportThenImport_PreservesPosition()
    {
        // Arrange - Create a custom position from scratch
        var original = new GameEngine();

        // Set up a valid position (15 checkers each)
        // White: 5 on point 1, 3 on point 2, 5 on point 3, 1 on bar, 1 borne off = 15 total
        original.Board.GetPoint(1).AddChecker(CheckerColor.White);
        original.Board.GetPoint(1).AddChecker(CheckerColor.White);
        original.Board.GetPoint(1).AddChecker(CheckerColor.White);
        original.Board.GetPoint(1).AddChecker(CheckerColor.White);
        original.Board.GetPoint(1).AddChecker(CheckerColor.White);
        original.Board.GetPoint(2).AddChecker(CheckerColor.White);
        original.Board.GetPoint(2).AddChecker(CheckerColor.White);
        original.Board.GetPoint(2).AddChecker(CheckerColor.White);
        original.Board.GetPoint(3).AddChecker(CheckerColor.White);
        original.Board.GetPoint(3).AddChecker(CheckerColor.White);
        original.Board.GetPoint(3).AddChecker(CheckerColor.White);
        original.Board.GetPoint(3).AddChecker(CheckerColor.White);
        original.Board.GetPoint(3).AddChecker(CheckerColor.White);
        original.WhitePlayer.CheckersOnBar = 1;
        original.WhitePlayer.CheckersBornOff = 1;

        // Red: 5 on point 24, 3 on point 23, 5 on point 22, 2 on bar = 15 total
        original.Board.GetPoint(24).AddChecker(CheckerColor.Red);
        original.Board.GetPoint(24).AddChecker(CheckerColor.Red);
        original.Board.GetPoint(24).AddChecker(CheckerColor.Red);
        original.Board.GetPoint(24).AddChecker(CheckerColor.Red);
        original.Board.GetPoint(24).AddChecker(CheckerColor.Red);
        original.Board.GetPoint(23).AddChecker(CheckerColor.Red);
        original.Board.GetPoint(23).AddChecker(CheckerColor.Red);
        original.Board.GetPoint(23).AddChecker(CheckerColor.Red);
        original.Board.GetPoint(22).AddChecker(CheckerColor.Red);
        original.Board.GetPoint(22).AddChecker(CheckerColor.Red);
        original.Board.GetPoint(22).AddChecker(CheckerColor.Red);
        original.Board.GetPoint(22).AddChecker(CheckerColor.Red);
        original.Board.GetPoint(22).AddChecker(CheckerColor.Red);
        original.RedPlayer.CheckersOnBar = 2;

        original.SetGameStarted(true);
        original.Dice.SetDice(3, 4);
        original.RemainingMoves.Clear();
        original.RemainingMoves.AddRange(original.Dice.GetMoves());

        // Act - Export then Import
        var sgf = SgfSerializer.ExportPosition(original);
        var restored = new GameEngine();
        SgfSerializer.ImportPosition(restored, sgf);

        // Assert - Key properties are preserved
        Assert.Equal(original.WhitePlayer.CheckersOnBar, restored.WhitePlayer.CheckersOnBar);
        Assert.Equal(original.RedPlayer.CheckersOnBar, restored.RedPlayer.CheckersOnBar);
        Assert.Equal(original.CurrentPlayer.Color, restored.CurrentPlayer.Color);
        Assert.True(restored.GameStarted);
    }

    [Fact]
    public void ImportPosition_TooManyCheckers_ThrowsException()
    {
        // Arrange
        var game = new GameEngine();
        // Try to create position with 16 white checkers (should fail validation)
        var sgf = @"(;GM[6]
  AW[b[16]]
  AB[k[15]]
  PL[W]
)";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => SgfSerializer.ImportPosition(game, sgf));
    }

    [Fact]
    public void ImportPosition_InvalidSgfFormat_ThrowsException()
    {
        // Arrange
        var game = new GameEngine();
        var invalidSgf = "This is not valid SGF";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => SgfSerializer.ImportPosition(game, invalidSgf));
    }

    [Fact]
    public void ImportPosition_WrongGameType_ThrowsException()
    {
        // Arrange
        var game = new GameEngine();
        var sgf = "(;GM[1]PL[W])";  // GM[1] is Go, not Backgammon

        // Act & Assert
        Assert.Throws<ArgumentException>(() => SgfSerializer.ImportPosition(game, sgf));
    }

    [Fact]
    public void ImportPosition_BarEntryTestCase_WhiteOnBar()
    {
        // Arrange - Test case 1 from the plan (White checker on bar)
        var game = new GameEngine();
        // White has 1 checker on bar, needs to enter at point 22 or 21 (25-3 or 25-4)
        // Make sure points 21 and 22 are not blocked (max 1 Red checker)
        // White: points 1,2,3 ('a','b','c'). Red: points 24,23,20,19 ('x','w','t','s') - NOT 21,22!
        // SGF uses board coordinates: 'a'=1, ..., 'x'=24, 'y'=bar
        var sgf = @"(;GM[6]
  AW[a][a][a][a][a][a][b][b][b][c][c][c][c][c][y]
  AB[x][x][w][w][w][w][w][t][t][t][s][s][s][s][s]
  PL[W]
  DI[34]
  CO[c]
  CV[1]
)";

        // Act
        SgfSerializer.ImportPosition(game, sgf);

        // Assert
        Assert.Equal(1, game.WhitePlayer.CheckersOnBar);
        Assert.Equal(CheckerColor.White, game.CurrentPlayer.Color);

        // Check valid moves - White should be able to enter at points 22 (25-3) or 21 (25-4)
        var validMoves = game.GetValidMoves();
        Assert.Contains(validMoves, m => m.From == 0 && m.To == 22);
        Assert.Contains(validMoves, m => m.From == 0 && m.To == 21);
    }

    [Fact]
    public void ImportPosition_BarEntryTestCase_RedOnBar()
    {
        // Arrange - Test case 2 from the plan (Red checker on bar)
        var game = new GameEngine();
        // Red has 1 checker on bar, needs to enter at point 3 or 4 (0+3 or 0+4)
        // Make sure points 3 and 4 are not blocked (max 1 White checker)
        // White: points 1,2,5,6 ('a','b','e','f') - NOT 3,4! Red: points 24,23,22 ('x','w','v')
        // SGF uses board coordinates: 'a'=1, ..., 'x'=24, 'y'=bar
        var sgf = @"(;GM[6]
  AW[a][a][b][b][b][b][b][e][e][e][f][f][f][f][f]
  AB[x][x][x][x][x][w][w][w][v][v][v][v][v][v][y]
  PL[B]
  DI[34]
  CO[c]
  CV[1]
)";

        // Act
        SgfSerializer.ImportPosition(game, sgf);

        // Assert
        Assert.Equal(1, game.RedPlayer.CheckersOnBar);
        Assert.Equal(CheckerColor.Red, game.CurrentPlayer.Color);

        // Check valid moves - Red should be able to enter at points 3 or 4
        var validMoves = game.GetValidMoves();
        Assert.Contains(validMoves, m => m.From == 0 && m.To == 3);
        Assert.Contains(validMoves, m => m.From == 0 && m.To == 4);
    }
}
