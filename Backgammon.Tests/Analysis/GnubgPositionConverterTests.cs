using Backgammon.Analysis.Gnubg;
using Backgammon.Core;
using Xunit;
using Xunit.Abstractions;

namespace Backgammon.Tests.Analysis;

public class GnubgPositionConverterTests
{
    private readonly ITestOutputHelper _output;

    public GnubgPositionConverterTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ToPositionId_StartingPosition_ReturnsExpectedId()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();

        // Act
        var positionId = GnubgPositionConverter.ToPositionId(engine);

        // Assert
        _output.WriteLine($"Position ID: {positionId}");
        // gnubg's starting position ID
        Assert.Equal("4HPwATDgc/ABMA", positionId);
    }

    [Fact]
    public void ToPositionId_WhiteOnBar_IncludesBarChecker()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();

        // Put one white checker on bar (remove from point 6 which has 5 White checkers)
        engine.Board.GetPoint(6).RemoveChecker();
        engine.WhitePlayer.CheckersOnBar++;

        // Act
        var positionId = GnubgPositionConverter.ToPositionId(engine);

        // Assert
        _output.WriteLine($"White on bar Position ID: {positionId}");
        // Position ID should be different from starting position
        Assert.NotEqual("4HPwATDgc/ABMA", positionId);
    }

    [Fact]
    public void ToPositionId_RedOnBar_IncludesBarChecker()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();

        // Put one red checker on bar (remove from point 19 which has Red checkers)
        engine.Board.GetPoint(19).RemoveChecker();
        engine.RedPlayer.CheckersOnBar++;

        // Act
        var positionId = GnubgPositionConverter.ToPositionId(engine);

        // Assert
        _output.WriteLine($"Red on bar Position ID: {positionId}");
        // Position ID should be different from starting position
        Assert.NotEqual("4HPwATDgc/ABMA", positionId);
    }
}
