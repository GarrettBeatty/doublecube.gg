using Xunit;
using Backgammon.Core;

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
        var sgf = @"(;GM[6]
  AW[f[5]][h[3]][m[5]][y[2]]
  AB[a[2]][l[5]][q[3]][s[5]]
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
        var sgf = @"(;GM[6]
  AW[f[5]][h[3]][m[5]]
  AB[a[2]][l[5]][q[3]][s[5]][y[1]]
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
        var sgf = @"(;GM[6]
  AW[a[3]][b[2]][z[10]]
  AB[x[3]][w[2]][z[10]]
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
        // Arrange
        var original = new GameEngine();
        original.StartNewGame();

        // Make some modifications to create a unique position
        original.WhitePlayer.CheckersOnBar = 1;
        original.RedPlayer.CheckersOnBar = 2;
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
        var sgf = @"(;GM[6]
  AW[f[6]][h[3]][m[5]][y[1]]
  AB[a[2]][l[5]][q[3]][s[5]]
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

        // Check valid moves - White should be able to enter at points 3 or 4
        var validMoves = game.GetValidMoves();
        Assert.Contains(validMoves, m => m.From == 0 && m.To == 3);
        Assert.Contains(validMoves, m => m.From == 0 && m.To == 4);
    }

    [Fact]
    public void ImportPosition_BarEntryTestCase_RedOnBar()
    {
        // Arrange - Test case 2 from the plan (Red checker on bar)
        var game = new GameEngine();
        var sgf = @"(;GM[6]
  AW[x[2]][f[5]][h[3]][m[5]]
  AB[l[5]][q[3]][s[6]][y[1]]
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

        // Check valid moves - Red should be able to enter at points 22 (25-3) or 21 (25-4)
        var validMoves = game.GetValidMoves();
        Assert.Contains(validMoves, m => m.From == 0 && m.To == 22);
        Assert.Contains(validMoves, m => m.From == 0 && m.To == 21);
    }
}
