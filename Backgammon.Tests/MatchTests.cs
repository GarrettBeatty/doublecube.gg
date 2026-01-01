using Backgammon.Core;
using Xunit;
using System;

namespace Backgammon.Tests
{
    public class MatchTests
    {
        [Fact]
        public void Match_Constructor_InitializesCorrectly()
        {
            var match = new Match("match123", "player1", "player2", 7);

            Assert.Equal("match123", match.MatchId);
            Assert.Equal("player1", match.Player1Id);
            Assert.Equal("player2", match.Player2Id);
            Assert.Equal(7, match.TargetScore);
            Assert.Equal(0, match.Player1Score);
            Assert.Equal(0, match.Player2Score);
            Assert.Equal(MatchStatus.InProgress, match.Status);
            Assert.False(match.IsCrawfordGame);
            Assert.False(match.HasCrawfordGameBeenPlayed);
        }

        [Fact]
        public void Match_IsMatchComplete_ReturnsTrueWhenPlayerReachesTarget()
        {
            var match = new Match("match123", "player1", "player2", 7);
            match.Player1Score = 7;

            Assert.True(match.IsMatchComplete());
            Assert.Equal("player1", match.GetWinnerId());
        }

        [Fact]
        public void Match_UpdateScores_UpdatesCorrectly()
        {
            var match = new Match("match123", "player1", "player2", 7);

            match.UpdateScores("player1", 2);
            Assert.Equal(2, match.Player1Score);
            Assert.Equal(0, match.Player2Score);

            match.UpdateScores("player2", 3);
            Assert.Equal(2, match.Player1Score);
            Assert.Equal(3, match.Player2Score);
        }

        [Fact]
        public void Match_CrawfordRule_ActivatesWhenPlayerReachesMatchPointMinusOne()
        {
            var match = new Match("match123", "player1", "player2", 7);

            // Player 1 reaches 6 points (match point - 1)
            match.UpdateScores("player1", 6);

            Assert.True(match.IsCrawfordGame);
            Assert.False(match.HasCrawfordGameBeenPlayed);
        }

        [Fact]
        public void Match_CrawfordRule_DeactivatesAfterCrawfordGame()
        {
            var match = new Match("match123", "player1", "player2", 7);
            match.Player1Score = 6;
            match.IsCrawfordGame = true;

            // Simulate Crawford game completion (player 2 wins)
            match.UpdateScores("player2", 1);

            Assert.False(match.IsCrawfordGame);
            Assert.True(match.HasCrawfordGameBeenPlayed);
        }

        [Fact]
        public void Match_CompletionSetsCorrectState()
        {
            var match = new Match("match123", "player1", "player2", 7);

            match.UpdateScores("player1", 7);

            Assert.Equal(MatchStatus.Completed, match.Status);
            Assert.NotNull(match.CompletedAt);
            Assert.Equal("player1", match.GetWinnerId());
        }
    }

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
}