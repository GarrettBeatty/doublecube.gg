using System;
using Backgammon.Core;
using Xunit;

namespace Backgammon.Tests;

public class GameResultTests
{
    [Fact]
    public void GameResult_CalculatesPointsCorrectly()
    {
        var result = new GameResult("player1", WinType.Normal, 1);
        Assert.Equal(1, result.PointsWon);

        result = new GameResult("player1", WinType.Gammon, 2);
        Assert.Equal(4, result.PointsWon); // 2 * 2

        result = new GameResult("player1", WinType.Backgammon, 4);
        Assert.Equal(12, result.PointsWon); // 3 * 4
    }

    [Fact]
    public void GameResult_UpdatesPointsWhenCubeValueChanges()
    {
        var result = new GameResult("player1", WinType.Gammon, 1);
        Assert.Equal(2, result.PointsWon); // 2 * 1

        result.SetCubeValue(4);
        Assert.Equal(8, result.PointsWon); // 2 * 4
    }

    [Fact]
    public void GameResult_UpdatesPointsWhenWinTypeChanges()
    {
        var result = new GameResult("player1", WinType.Normal, 2);
        Assert.Equal(2, result.PointsWon); // 1 * 2

        result.SetWinType(WinType.Backgammon);
        Assert.Equal(6, result.PointsWon); // 3 * 2
    }
}
