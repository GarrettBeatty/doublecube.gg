using Backgammon.Core;
using Xunit;

namespace Backgammon.Tests.Core;

/// <summary>
/// Tests for GameResult - capturing the result of a completed game.
/// </summary>
public class GameResultTests
{
    [Fact]
    public void Constructor_Default_SetsDefaults()
    {
        // Act
        var result = new GameResult();

        // Assert
        Assert.Equal(string.Empty, result.WinnerId);
        Assert.Null(result.WinnerColor);
        Assert.Equal(0, result.PointsWon);
        Assert.Equal(WinType.Normal, result.WinType);
        Assert.Equal(1, result.CubeValue);
        Assert.Empty(result.MoveHistory);
        Assert.False(result.IsAbandoned);
        Assert.False(result.IsForfeit);
    }

    [Fact]
    public void Constructor_WithParameters_CalculatesPoints()
    {
        // Act
        var result = new GameResult(winnerId: "player1", winType: WinType.Normal, cubeValue: 2);

        // Assert
        Assert.Equal("player1", result.WinnerId);
        Assert.Equal(WinType.Normal, result.WinType);
        Assert.Equal(2, result.CubeValue);
        Assert.Equal(2, result.PointsWon); // Normal (1x) * cube (2) = 2
    }

    [Theory]
    [InlineData(WinType.Normal, 1, 1)]
    [InlineData(WinType.Normal, 2, 2)]
    [InlineData(WinType.Normal, 4, 4)]
    [InlineData(WinType.Gammon, 1, 2)]
    [InlineData(WinType.Gammon, 2, 4)]
    [InlineData(WinType.Gammon, 4, 8)]
    [InlineData(WinType.Backgammon, 1, 3)]
    [InlineData(WinType.Backgammon, 2, 6)]
    [InlineData(WinType.Backgammon, 4, 12)]
    public void PointsWon_CalculatesCorrectly(WinType winType, int cubeValue, int expectedPoints)
    {
        // Act
        var result = new GameResult(winnerId: "player1", winType: winType, cubeValue: cubeValue);

        // Assert
        Assert.Equal(expectedPoints, result.PointsWon);
    }

    [Fact]
    public void SetWinType_RecalculatesPoints()
    {
        // Arrange
        var result = new GameResult(winnerId: "player1", winType: WinType.Normal, cubeValue: 2);
        Assert.Equal(2, result.PointsWon); // Normal * 2 = 2

        // Act
        result.SetWinType(WinType.Gammon);

        // Assert
        Assert.Equal(WinType.Gammon, result.WinType);
        Assert.Equal(4, result.PointsWon); // Gammon (2x) * cube (2) = 4
    }

    [Fact]
    public void SetCubeValue_RecalculatesPoints()
    {
        // Arrange
        var result = new GameResult(winnerId: "player1", winType: WinType.Gammon, cubeValue: 1);
        Assert.Equal(2, result.PointsWon); // Gammon (2x) * 1 = 2

        // Act
        result.SetCubeValue(4);

        // Assert
        Assert.Equal(4, result.CubeValue);
        Assert.Equal(8, result.PointsWon); // Gammon (2x) * cube (4) = 8
    }

    [Fact]
    public void WinnerId_CanBeSet()
    {
        // Arrange
        var result = new GameResult();

        // Act
        result.WinnerId = "winner123";

        // Assert
        Assert.Equal("winner123", result.WinnerId);
    }

    [Fact]
    public void WinnerColor_CanBeSet()
    {
        // Arrange
        var result = new GameResult();

        // Act
        result.WinnerColor = CheckerColor.White;

        // Assert
        Assert.Equal(CheckerColor.White, result.WinnerColor);
    }

    [Fact]
    public void MoveHistory_CanAddMoves()
    {
        // Arrange
        var result = new GameResult();
        var move1 = new Move(24, 20, 4);
        var move2 = new Move(13, 9, 4);

        // Act
        result.MoveHistory.Add(move1);
        result.MoveHistory.Add(move2);

        // Assert
        Assert.Equal(2, result.MoveHistory.Count);
    }

    [Fact]
    public void IsAbandoned_CanBeSet()
    {
        // Arrange
        var result = new GameResult();

        // Act
        result.IsAbandoned = true;

        // Assert
        Assert.True(result.IsAbandoned);
    }

    [Fact]
    public void IsForfeit_CanBeSet()
    {
        // Arrange
        var result = new GameResult();

        // Act
        result.IsForfeit = true;

        // Assert
        Assert.True(result.IsForfeit);
    }

    [Fact]
    public void SetWinType_ToBackgammon_TriplesCubeValue()
    {
        // Arrange
        var result = new GameResult();
        result.SetCubeValue(8);

        // Act
        result.SetWinType(WinType.Backgammon);

        // Assert
        Assert.Equal(24, result.PointsWon); // Backgammon (3x) * cube (8) = 24
    }

    [Fact]
    public void PointsWon_DirectSet_Works()
    {
        // Arrange
        var result = new GameResult();

        // Act
        result.PointsWon = 100;

        // Assert
        Assert.Equal(100, result.PointsWon);
    }

    [Fact]
    public void CubeValue_DirectSet_Works()
    {
        // Arrange
        var result = new GameResult();

        // Act
        result.CubeValue = 32;

        // Assert
        Assert.Equal(32, result.CubeValue);
    }

    [Fact]
    public void WinType_DirectSet_Works()
    {
        // Arrange
        var result = new GameResult();

        // Act
        result.WinType = WinType.Backgammon;

        // Assert
        Assert.Equal(WinType.Backgammon, result.WinType);
    }
}
