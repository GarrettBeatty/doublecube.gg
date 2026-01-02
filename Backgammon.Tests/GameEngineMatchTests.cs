using System;
using Backgammon.Core;
using Xunit;

namespace Backgammon.Tests;

public class GameEngineMatchTests
{
    [Fact]
    public void GameEngine_CrawfordRule_PreventsDoubling()
    {
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.IsCrawfordGame = true;

        // Try to offer double - should return false due to Crawford rule
        var canDouble = engine.OfferDouble();
        Assert.False(canDouble);
    }

    [Fact]
    public void GameEngine_CrawfordRule_AllowsDoublingWhenNotCrawford()
    {
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.IsCrawfordGame = false;

        // Should be able to double
        var canDouble = engine.OfferDouble();
        Assert.True(canDouble);
    }

    [Fact]
    public void GameEngine_DetermineWinType_Normal()
    {
        var engine = new GameEngine();
        engine.StartNewGame();

        // Simulate a normal win - red has borne off some checkers
        engine.RedPlayer.CheckersBornOff = 5;
        engine.WhitePlayer.CheckersBornOff = 15; // White wins
        engine.SetGameStarted(true);
        engine.ForfeitGame(engine.WhitePlayer);

        var winType = engine.DetermineWinType();
        Assert.Equal(WinType.Normal, winType);
    }

    [Fact]
    public void GameEngine_DetermineWinType_Gammon()
    {
        var engine = new GameEngine();
        engine.StartNewGame();

        // Clear the board to avoid checkers in home board
        for (int i = 1; i <= 24; i++)
        {
            engine.Board.GetPoint(i).Checkers.Clear();
        }

        // Set up a gammon scenario - red hasn't borne off any checkers
        // Place Red's checkers outside White's home board (e.g., on points 7-12)
        for (int i = 0; i < 15; i++)
        {
            engine.Board.GetPoint(7 + (i % 6)).AddChecker(CheckerColor.Red);
        }

        engine.RedPlayer.CheckersBornOff = 0;
        engine.RedPlayer.CheckersOnBar = 0;
        engine.WhitePlayer.CheckersBornOff = 15; // White wins
        engine.SetGameStarted(true);
        engine.ForfeitGame(engine.WhitePlayer);

        var winType = engine.DetermineWinType();
        Assert.Equal(WinType.Gammon, winType);
    }

    [Fact]
    public void GameEngine_DetermineWinType_Backgammon()
    {
        var engine = new GameEngine();
        engine.StartNewGame();

        // Simulate a backgammon - red has checkers on bar
        engine.RedPlayer.CheckersBornOff = 0;
        engine.RedPlayer.CheckersOnBar = 2;
        engine.WhitePlayer.CheckersBornOff = 15; // White wins
        engine.SetGameStarted(true);
        engine.ForfeitGame(engine.WhitePlayer);

        var winType = engine.DetermineWinType();
        Assert.Equal(WinType.Backgammon, winType);
    }

    [Fact]
    public void GameEngine_CreateGameResult_ReturnsCorrectResult()
    {
        var engine = new GameEngine();
        engine.StartNewGame();

        // Clear the board to avoid checkers in home board
        for (int i = 1; i <= 24; i++)
        {
            engine.Board.GetPoint(i).Checkers.Clear();
        }

        // Set up a gammon scenario - red hasn't borne off any checkers
        // Place Red's checkers outside White's home board (e.g., on points 7-12)
        for (int i = 0; i < 15; i++)
        {
            engine.Board.GetPoint(7 + (i % 6)).AddChecker(CheckerColor.Red);
        }

        // Simulate a gammon with doubling cube at 4
        engine.RedPlayer.CheckersBornOff = 0;
        engine.RedPlayer.CheckersOnBar = 0;
        engine.WhitePlayer.CheckersBornOff = 15;
        engine.DoublingCube.Double(CheckerColor.White);
        engine.DoublingCube.Double(CheckerColor.Red);
        Assert.Equal(4, engine.DoublingCube.Value);

        engine.SetGameStarted(true);
        engine.ForfeitGame(engine.WhitePlayer);

        var result = engine.CreateGameResult();
        Assert.Equal("White", result.WinnerId);
        Assert.Equal(WinType.Gammon, result.WinType);
        Assert.Equal(4, result.CubeValue);
        Assert.Equal(8, result.PointsWon); // Gammon (2) * Cube (4) = 8
    }
}
