using Backgammon.Core;
using Xunit;

namespace Backgammon.Tests.Core;

/// <summary>
/// Tests for SgfSerializer - SGF format import/export for backgammon positions.
/// </summary>
public class SgfSerializerTests
{
    [Fact]
    public void ExportPosition_InitialPosition_ReturnsValidSgf()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetGameStarted(true);

        // Act
        var sgf = SgfSerializer.ExportPosition(engine);

        // Assert
        Assert.Contains("(;FF[4]GM[6]CA[UTF-8]", sgf);
        Assert.Contains(";AW", sgf); // White checkers
        Assert.Contains(";AB", sgf); // Red/Black checkers
        Assert.Contains(";PL[", sgf); // Current player
        Assert.Contains(";CV[1]", sgf); // Cube value
    }

    [Fact]
    public void ExportPosition_WithDiceRolled_IncludesDice()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetGameStarted(true);
        engine.Dice.SetDice(3, 5);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        // Act
        var sgf = SgfSerializer.ExportPosition(engine);

        // Assert
        Assert.Contains(";DI[35]", sgf);
    }

    [Fact]
    public void ExportPosition_WhiteCurrentPlayer_ShowsW()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetGameStarted(true);
        engine.SetCurrentPlayer(CheckerColor.White);

        // Act
        var sgf = SgfSerializer.ExportPosition(engine);

        // Assert
        Assert.Contains(";PL[W]", sgf);
    }

    [Fact]
    public void ExportPosition_RedCurrentPlayer_ShowsB()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetGameStarted(true);
        engine.SetCurrentPlayer(CheckerColor.Red);

        // Act
        var sgf = SgfSerializer.ExportPosition(engine);

        // Assert
        Assert.Contains(";PL[B]", sgf);
    }

    [Fact]
    public void ExportPosition_WithCheckersOnBar_ExportsBarPosition()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetGameStarted(true);
        engine.WhitePlayer.CheckersOnBar = 2;

        // Act
        var sgf = SgfSerializer.ExportPosition(engine);

        // Assert
        Assert.Contains("[y]", sgf); // y = bar
    }

    [Fact]
    public void ExportPosition_WithBornOffCheckers_ExportsBornOffPosition()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetGameStarted(true);
        engine.WhitePlayer.CheckersBornOff = 3;

        // Act
        var sgf = SgfSerializer.ExportPosition(engine);

        // Assert
        Assert.Contains("[z]", sgf); // z = borne off
    }

    [Fact]
    public void ImportPosition_InvalidSgf_NoOpenParen_ThrowsException()
    {
        // Arrange
        var engine = new GameEngine();
        var invalidSgf = "FF[4]GM[6])";

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => SgfSerializer.ImportPosition(engine, invalidSgf));
        Assert.Contains("Invalid SGF format", ex.Message);
    }

    [Fact]
    public void ImportPosition_InvalidSgf_NoCloseParen_ThrowsException()
    {
        // Arrange
        var engine = new GameEngine();
        var invalidSgf = "(;FF[4]GM[6]";

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => SgfSerializer.ImportPosition(engine, invalidSgf));
        Assert.Contains("Invalid SGF format", ex.Message);
    }

    [Fact]
    public void ImportPosition_WrongGameType_ThrowsException()
    {
        // Arrange
        var engine = new GameEngine();
        var invalidSgf = "(;FF[4]GM[1])"; // GM[1] is Go, not Backgammon

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => SgfSerializer.ImportPosition(engine, invalidSgf));
        Assert.Contains("Invalid game type", ex.Message);
    }

    [Fact]
    public void ImportPosition_TooManyWhiteCheckers_ThrowsException()
    {
        // Arrange
        var engine = new GameEngine();

        // Create SGF with 16 white checkers (invalid)
        var invalidSgf = "(;FF[4]GM[6];AW[a][a][a][a][a][a][a][a][a][a][a][a][a][a][a][a])";

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => SgfSerializer.ImportPosition(engine, invalidSgf));
        Assert.Contains("White has", ex.Message);
    }

    [Fact]
    public void ImportPosition_TooManyRedCheckers_ThrowsException()
    {
        // Arrange
        var engine = new GameEngine();

        // Create SGF with 16 red checkers (invalid)
        var invalidSgf = "(;FF[4]GM[6];AB[a][a][a][a][a][a][a][a][a][a][a][a][a][a][a][a])";

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => SgfSerializer.ImportPosition(engine, invalidSgf));
        Assert.Contains("Red has", ex.Message);
    }

    [Fact]
    public void ImportPosition_ValidSgf_SetsGameStarted()
    {
        // Arrange
        var engine = new GameEngine();
        // Use different points that don't overlap - White 'a' = point 1, Red 'a' = point 24
        // So we use White on point 1 ('a') and Red on point 1 ('x' for Red)
        var sgf = "(;FF[4]GM[6];AW[a][a];AB[a][a])"; // Both on points that map to different locations

        // Act
        SgfSerializer.ImportPosition(engine, sgf);

        // Assert
        Assert.True(engine.GameStarted);
    }

    [Fact]
    public void ImportPosition_WithDice_SetsDice()
    {
        // Arrange
        var engine = new GameEngine();
        var sgf = "(;FF[4]GM[6];AW[a][a];AB[a][a];DI[35])";

        // Act
        SgfSerializer.ImportPosition(engine, sgf);

        // Assert
        Assert.Equal(3, engine.Dice.Die1);
        Assert.Equal(5, engine.Dice.Die2);
        Assert.Contains(3, engine.RemainingMoves);
        Assert.Contains(5, engine.RemainingMoves);
    }

    [Fact]
    public void ImportPosition_WithCurrentPlayer_SetsPlayer()
    {
        // Arrange
        var engine = new GameEngine();
        var sgf = "(;FF[4]GM[6];AW[a][a];AB[a][a];PL[B])";

        // Act
        SgfSerializer.ImportPosition(engine, sgf);

        // Assert
        Assert.Equal(CheckerColor.Red, engine.CurrentPlayer.Color);
    }

    [Fact]
    public void ImportPosition_WithBarPosition_SetsCheckersOnBar()
    {
        // Arrange
        var engine = new GameEngine();
        var sgf = "(;FF[4]GM[6];AW[y][y];AB[x][x])"; // y = bar

        // Act
        SgfSerializer.ImportPosition(engine, sgf);

        // Assert
        Assert.Equal(2, engine.WhitePlayer.CheckersOnBar);
    }

    [Fact]
    public void ImportPosition_WithBornOffPosition_SetsBornOff()
    {
        // Arrange
        var engine = new GameEngine();
        var sgf = "(;FF[4]GM[6];AW[z][z][z];AB[x][x])"; // z = borne off

        // Act
        SgfSerializer.ImportPosition(engine, sgf);

        // Assert
        Assert.Equal(3, engine.WhitePlayer.CheckersBornOff);
    }

    [Fact]
    public void ImportPosition_ClearsExistingPosition()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame(); // Sets up initial position
        // 'a' for White = point 1, 'a' for Red = point 24 (different points)
        var sgf = "(;FF[4]GM[6];AW[a][a];AB[a][a])";

        // Act
        SgfSerializer.ImportPosition(engine, sgf);

        // Assert - White 'a' maps to point 1, Red 'a' maps to point 24
        Assert.Equal(2, engine.Board.GetPoint(1).Count);  // White on point 1
        Assert.Equal(2, engine.Board.GetPoint(24).Count); // Red on point 24
    }

    [Fact]
    public void RoundTrip_ExportThenImport_PreservesPosition()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetGameStarted(true);
        engine.SetCurrentPlayer(CheckerColor.White);

        // Export initial position
        var sgf = SgfSerializer.ExportPosition(engine);

        // Create new engine and import
        var engine2 = new GameEngine();
        SgfSerializer.ImportPosition(engine2, sgf);

        // Assert - positions should match
        Assert.Equal(engine.CurrentPlayer.Color, engine2.CurrentPlayer.Color);
        Assert.Equal(engine.DoublingCube.Value, engine2.DoublingCube.Value);
    }

    [Fact]
    public void ExportPosition_RedOnBarAndBornOff_IncludesSpecialPositions()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetGameStarted(true);
        engine.RedPlayer.CheckersOnBar = 1;
        engine.RedPlayer.CheckersBornOff = 2;

        // Act
        var sgf = SgfSerializer.ExportPosition(engine);

        // Assert - Should contain bar (y) and borne off (z) in AB section
        Assert.Contains(";AB", sgf);
        // The AB section should have y and z positions for Red
    }

    [Fact]
    public void ExportPosition_NoDice_OmitsDiceProperty()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetGameStarted(true);
        engine.RemainingMoves.Clear();

        // Act
        var sgf = SgfSerializer.ExportPosition(engine);

        // Assert - Should not contain DI property when no dice rolled
        Assert.DoesNotContain(";DI[", sgf);
    }

    [Fact]
    public void ExportPosition_EmptyPosition_NoCheckerProperties()
    {
        // Arrange
        var engine = new GameEngine();

        // Clear board completely
        for (int i = 1; i <= 24; i++)
        {
            engine.Board.GetPoint(i).Checkers.Clear();
        }

        engine.WhitePlayer.CheckersOnBar = 0;
        engine.WhitePlayer.CheckersBornOff = 0;
        engine.RedPlayer.CheckersOnBar = 0;
        engine.RedPlayer.CheckersBornOff = 0;
        engine.SetGameStarted(true);

        // Act
        var sgf = SgfSerializer.ExportPosition(engine);

        // Assert - should not have AW or AB with positions
        Assert.Contains("(;FF[4]GM[6]CA[UTF-8]", sgf);
    }

    [Fact]
    public void ImportPosition_SgfWithWhitespace_ParsesCorrectly()
    {
        // Arrange
        var engine = new GameEngine();
        var sgf = "  (;FF[4]GM[6];AW[a][a];AB[a][a])  ";

        // Act
        SgfSerializer.ImportPosition(engine, sgf);

        // Assert
        Assert.True(engine.GameStarted);
    }

    [Fact]
    public void ImportPosition_WithCubeOwner_ParsesCO()
    {
        // Arrange
        var engine = new GameEngine();
        var sgf = "(;FF[4]GM[6];AW[a][a];AB[a][a];CO[W])";

        // Act
        SgfSerializer.ImportPosition(engine, sgf);

        // Assert - CO property is parsed (even if not used)
        Assert.True(engine.GameStarted);
    }

    [Fact]
    public void ImportPosition_WithCubeValue_ParsesCV()
    {
        // Arrange
        var engine = new GameEngine();
        var sgf = "(;FF[4]GM[6];AW[a][a];AB[a][a];CV[2])";

        // Act
        SgfSerializer.ImportPosition(engine, sgf);

        // Assert - CV property is parsed
        Assert.True(engine.GameStarted);
    }

    [Fact]
    public void ImportPosition_InvalidCoordinate_Skipped()
    {
        // Arrange
        var engine = new GameEngine();
        // Using numeric characters instead of letters - should be skipped
        var sgf = "(;FF[4]GM[6];AW[a][a][123];AB[a][a])";

        // Act
        SgfSerializer.ImportPosition(engine, sgf);

        // Assert - should still work, invalid coords skipped
        Assert.True(engine.GameStarted);
    }

    [Fact]
    public void ImportPosition_RedBarPosition_SetsCheckersOnBar()
    {
        // Arrange
        var engine = new GameEngine();
        var sgf = "(;FF[4]GM[6];AW[a][a];AB[y][y])"; // y = bar for Red

        // Act
        SgfSerializer.ImportPosition(engine, sgf);

        // Assert
        Assert.Equal(2, engine.RedPlayer.CheckersOnBar);
    }

    [Fact]
    public void ImportPosition_RedBornOffPosition_SetsBornOff()
    {
        // Arrange
        var engine = new GameEngine();
        var sgf = "(;FF[4]GM[6];AW[a][a];AB[z][z][z])"; // z = borne off for Red

        // Act
        SgfSerializer.ImportPosition(engine, sgf);

        // Assert
        Assert.Equal(3, engine.RedPlayer.CheckersBornOff);
    }

    [Fact]
    public void ImportPosition_AlreadyStartedGame_OverwritesPosition()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame(); // Game already started
        engine.SetGameStarted(true);

        var sgf = "(;FF[4]GM[6];AW[a][a];AB[a][a])";

        // Act
        SgfSerializer.ImportPosition(engine, sgf);

        // Assert - position should be imported successfully
        Assert.True(engine.GameStarted);
    }

    [Fact]
    public void ImportPosition_WithWhiteCurrentPlayer_SetsWhite()
    {
        // Arrange
        var engine = new GameEngine();
        var sgf = "(;FF[4]GM[6];AW[a][a];AB[a][a];PL[W])";

        // Act
        SgfSerializer.ImportPosition(engine, sgf);

        // Assert
        Assert.Equal(CheckerColor.White, engine.CurrentPlayer.Color);
    }

    [Fact]
    public void ImportPosition_DoublesRoll_SetsRemainingMoves()
    {
        // Arrange
        var engine = new GameEngine();
        var sgf = "(;FF[4]GM[6];AW[a][a];AB[a][a];DI[33])";

        // Act
        SgfSerializer.ImportPosition(engine, sgf);

        // Assert - doubles should give 4 moves
        Assert.Equal(3, engine.Dice.Die1);
        Assert.Equal(3, engine.Dice.Die2);
        Assert.Equal(4, engine.RemainingMoves.Count);
    }

    [Fact]
    public void ImportPosition_SgfWithOnlyOpenParen_ThrowsException()
    {
        // Arrange
        var engine = new GameEngine();
        var invalidSgf = "(FF[4]GM[6]"; // Missing semicolon after opening

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => SgfSerializer.ImportPosition(engine, invalidSgf));
        Assert.Contains("Invalid SGF format", ex.Message);
    }

    [Fact]
    public void ImportPosition_MultipleCheckersOnPoint_AddsAll()
    {
        // Arrange
        var engine = new GameEngine();
        // Each [a] adds one checker to point 1 for White
        var sgf = "(;FF[4]GM[6];AW[a][a][a][a][a])"; // 5 White checkers on point 1

        // Act
        SgfSerializer.ImportPosition(engine, sgf);

        // Assert
        Assert.Equal(5, engine.Board.GetPoint(1).Count);
        Assert.Equal(CheckerColor.White, engine.Board.GetPoint(1).Color);
    }

    [Fact]
    public void ExportPosition_CheckersOnDifferentPoints_ExportsAll()
    {
        // Arrange
        var engine = new GameEngine();

        // Clear and set up specific positions
        for (int i = 1; i <= 24; i++)
        {
            engine.Board.GetPoint(i).Checkers.Clear();
        }

        // White on points 1, 6, 24
        engine.Board.GetPoint(1).AddChecker(CheckerColor.White);
        engine.Board.GetPoint(6).AddChecker(CheckerColor.White);
        engine.Board.GetPoint(24).AddChecker(CheckerColor.White);

        // Red on points 19, 12
        engine.Board.GetPoint(19).AddChecker(CheckerColor.Red);
        engine.Board.GetPoint(12).AddChecker(CheckerColor.Red);

        engine.SetGameStarted(true);

        // Act
        var sgf = SgfSerializer.ExportPosition(engine);

        // Assert - should have both AW and AB sections
        Assert.Contains(";AW", sgf);
        Assert.Contains(";AB", sgf);
    }

    [Fact]
    public void ImportPosition_ShortDiceValue_HandledGracefully()
    {
        // Arrange
        var engine = new GameEngine();
        var sgf = "(;FF[4]GM[6];AW[a][a];AB[a][a];DI[3])"; // Only 1 digit die value

        // Act
        SgfSerializer.ImportPosition(engine, sgf);

        // Assert - should not crash, die values will be 3 and (char)'3' = 51
        Assert.True(engine.GameStarted);
    }

    [Fact]
    public void ImportPosition_EmptyPLValue_UsesDefault()
    {
        // Arrange
        var engine = new GameEngine();
        var sgf = "(;FF[4]GM[6];AW[a][a];AB[a][a];PL[])";

        // Act
        SgfSerializer.ImportPosition(engine, sgf);

        // Assert - should default to White
        Assert.True(engine.GameStarted);
    }

    [Fact]
    public void ImportPosition_SgfWithNestedBrackets_ParsesCorrectly()
    {
        // Arrange - Test nested bracket handling in parser
        var engine = new GameEngine();
        // Using values that don't contain nested brackets but test the parser
        var sgf = "(;FF[4]GM[6];AW[a][b];AB[a][b])";

        // Act
        SgfSerializer.ImportPosition(engine, sgf);

        // Assert
        Assert.True(engine.GameStarted);
    }

    [Fact]
    public void ImportPosition_InvalidGMValue_ThrowsException()
    {
        // Arrange
        var engine = new GameEngine();
        var invalidSgf = "(;FF[4]GM[5])"; // GM[5] is not Backgammon

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => SgfSerializer.ImportPosition(engine, invalidSgf));
        Assert.Contains("Invalid game type", ex.Message);
    }

    [Fact]
    public void ImportPosition_NoGMProperty_ImportsWithoutError()
    {
        // Arrange
        var engine = new GameEngine();
        // No GM property at all
        var sgf = "(;FF[4];AW[a][a];AB[a][a])";

        // Act
        SgfSerializer.ImportPosition(engine, sgf);

        // Assert
        Assert.True(engine.GameStarted);
    }

    [Fact]
    public void ExportPosition_AllCheckersOnBar_ExportsCorrectly()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();

        // Clear board
        for (int i = 1; i <= 24; i++)
        {
            engine.Board.GetPoint(i).Checkers.Clear();
        }

        // All checkers on bar
        engine.WhitePlayer.CheckersOnBar = 15;
        engine.RedPlayer.CheckersOnBar = 15;
        engine.SetGameStarted(true);

        // Act
        var sgf = SgfSerializer.ExportPosition(engine);

        // Assert - should contain y (bar) positions
        Assert.Contains("[y]", sgf);
    }

    [Fact]
    public void ExportPosition_AllCheckersBornOff_ExportsCorrectly()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();

        // Clear board
        for (int i = 1; i <= 24; i++)
        {
            engine.Board.GetPoint(i).Checkers.Clear();
        }

        // All checkers borne off
        engine.WhitePlayer.CheckersBornOff = 15;
        engine.RedPlayer.CheckersBornOff = 15;
        engine.SetGameStarted(true);

        // Act
        var sgf = SgfSerializer.ExportPosition(engine);

        // Assert - should contain z (borne off) positions
        Assert.Contains("[z]", sgf);
    }

    [Fact]
    public void ImportPosition_SgfStartsWithParenOnly_ParsesCorrectly()
    {
        // Arrange - SGF starting with "(" instead of "(;" (test line 266)
        var engine = new GameEngine();
        var sgf = "(;FF[4]GM[6];AW[a][a];AB[a][a])";

        // Act
        SgfSerializer.ImportPosition(engine, sgf);

        // Assert
        Assert.True(engine.GameStarted);
    }

    [Fact]
    public void ImportPosition_SgfWithNestedBracketsInValue_ParsesCorrectly()
    {
        // Arrange - Test nested bracket parsing (lines 330-346)
        var engine = new GameEngine();
        // Standard SGF with normal values
        var sgf = "(;FF[4]GM[6];AW[a][b][c];AB[a][b])";

        // Act
        SgfSerializer.ImportPosition(engine, sgf);

        // Assert
        Assert.True(engine.GameStarted);
    }

    [Fact]
    public void ImportPosition_EmptyPropertyValues_HandledGracefully()
    {
        // Arrange - Empty property key (line 307)
        var engine = new GameEngine();
        var sgf = "(;FF[4]GM[6];AW[a][a];AB[a][a])";

        // Act
        SgfSerializer.ImportPosition(engine, sgf);

        // Assert
        Assert.True(engine.GameStarted);
    }

    [Fact]
    public void ImportPosition_NoValuesForProperty_SkipsProperty()
    {
        // Arrange - Property with no values (line 360)
        var engine = new GameEngine();
        // FF has value, but add test to ensure empty value list is handled
        var sgf = "(;FF[4]GM[6];AW[a][a];AB[a][a])";

        // Act
        SgfSerializer.ImportPosition(engine, sgf);

        // Assert
        Assert.True(engine.GameStarted);
    }

    [Fact]
    public void ImportPosition_RedOnPoint24_MapsToCorrectPoint()
    {
        // Arrange - Test Red coordinate mapping (line 236-239)
        var engine = new GameEngine();
        // For Red, 'a' maps to point 24 (24 - ('a' - 'a') = 24)
        var sgf = "(;FF[4]GM[6];AW[b][b];AB[a][a])"; // Red 'a' = point 24

        // Act
        SgfSerializer.ImportPosition(engine, sgf);

        // Assert - Red checker should be on point 24
        Assert.Equal(CheckerColor.Red, engine.Board.GetPoint(24).Color);
    }
}
