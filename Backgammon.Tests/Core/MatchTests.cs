using Backgammon.Core;
using Xunit;

namespace Backgammon.Tests.Core;

/// <summary>
/// Tests for Match - managing multi-game match play.
/// </summary>
public class MatchTests
{
    [Fact]
    public void Constructor_Default_SetsWaitingStatus()
    {
        // Act
        var match = new Match();

        // Assert
        Assert.Equal(MatchStatus.WaitingForPlayers, match.Status);
        Assert.NotEqual(default(DateTime), match.CreatedAt);
    }

    [Fact]
    public void Constructor_WithParameters_SetsInProgressStatus()
    {
        // Act
        var match = new Match("match-123", "player1", "player2", 5);

        // Assert
        Assert.Equal(MatchStatus.InProgress, match.Status);
        Assert.Equal("match-123", match.MatchId);
        Assert.Equal("player1", match.Player1Id);
        Assert.Equal("player2", match.Player2Id);
        Assert.Equal(5, match.TargetScore);
    }

    [Fact]
    public void IsMatchComplete_BothScoresZero_ReturnsFalse()
    {
        // Arrange
        var match = new Match("match-123", "player1", "player2", 5);

        // Assert
        Assert.False(match.IsMatchComplete());
    }

    [Fact]
    public void IsMatchComplete_Player1ReachesTarget_ReturnsTrue()
    {
        // Arrange
        var match = new Match("match-123", "player1", "player2", 5);
        match.Player1Score = 5;

        // Assert
        Assert.True(match.IsMatchComplete());
    }

    [Fact]
    public void IsMatchComplete_Player2ReachesTarget_ReturnsTrue()
    {
        // Arrange
        var match = new Match("match-123", "player1", "player2", 5);
        match.Player2Score = 5;

        // Assert
        Assert.True(match.IsMatchComplete());
    }

    [Fact]
    public void GetWinnerId_Player1Wins_ReturnsPlayer1Id()
    {
        // Arrange
        var match = new Match("match-123", "player1", "player2", 5);
        match.Player1Score = 5;

        // Act
        var winnerId = match.GetWinnerId();

        // Assert
        Assert.Equal("player1", winnerId);
    }

    [Fact]
    public void GetWinnerId_Player2Wins_ReturnsPlayer2Id()
    {
        // Arrange
        var match = new Match("match-123", "player1", "player2", 5);
        match.Player2Score = 5;

        // Act
        var winnerId = match.GetWinnerId();

        // Assert
        Assert.Equal("player2", winnerId);
    }

    [Fact]
    public void GetWinnerId_NoWinnerYet_ReturnsNull()
    {
        // Arrange
        var match = new Match("match-123", "player1", "player2", 5);
        match.Player1Score = 3;
        match.Player2Score = 2;

        // Act
        var winnerId = match.GetWinnerId();

        // Assert
        Assert.Null(winnerId);
    }

    [Fact]
    public void AddGame_AddsToGamesAndSetsCurrentGame()
    {
        // Arrange
        var match = new Match("match-123", "player1", "player2", 5);
        var game = new Game("game-1");

        // Act
        match.AddGame(game);

        // Assert
        Assert.Single(match.Games);
        Assert.Same(game, match.CurrentGame);
        Assert.Equal(1, match.TotalGamesPlayed);
    }

    [Fact]
    public void UpdateScores_Player1Wins_UpdatesPlayer1Score()
    {
        // Arrange
        var match = new Match("match-123", "player1", "player2", 5);

        // Act
        match.UpdateScores("player1", 2);

        // Assert
        Assert.Equal(2, match.Player1Score);
        Assert.Equal(0, match.Player2Score);
    }

    [Fact]
    public void UpdateScores_Player2Wins_UpdatesPlayer2Score()
    {
        // Arrange
        var match = new Match("match-123", "player1", "player2", 5);

        // Act
        match.UpdateScores("player2", 2);

        // Assert
        Assert.Equal(0, match.Player1Score);
        Assert.Equal(2, match.Player2Score);
    }

    [Fact]
    public void UpdateScores_MatchCompletes_SetsCompletedStatus()
    {
        // Arrange
        var match = new Match("match-123", "player1", "player2", 5);
        match.Player1Score = 4;

        // Act
        match.UpdateScores("player1", 1);

        // Assert
        Assert.Equal(MatchStatus.Completed, match.Status);
        Assert.NotNull(match.CompletedAt);
    }

    [Fact]
    public void UpdateScores_CrawfordRule_ActivatesAtTargetMinus1()
    {
        // Arrange
        var match = new Match("match-123", "player1", "player2", 5);

        // Act - Player1 reaches 4 points (target - 1)
        match.UpdateScores("player1", 4);

        // Assert - Crawford game should be activated
        Assert.True(match.IsCrawfordGame);
        Assert.False(match.HasCrawfordGameBeenPlayed);
    }

    [Fact]
    public void UpdateScores_AfterCrawford_DeactivatesCrawford()
    {
        // Arrange
        var match = new Match("match-123", "player1", "player2", 5);
        match.Player1Score = 4;
        match.IsCrawfordGame = true;

        // Act - Play another game (Crawford game was just played)
        match.UpdateScores("player2", 1);

        // Assert
        Assert.False(match.IsCrawfordGame);
        Assert.True(match.HasCrawfordGameBeenPlayed);
    }

    [Fact]
    public void CanContinueToNextGame_InProgress_ReturnsTrue()
    {
        // Arrange
        var match = new Match("match-123", "player1", "player2", 5);

        // Assert
        Assert.True(match.CanContinueToNextGame());
    }

    [Fact]
    public void CanContinueToNextGame_MatchComplete_ReturnsFalse()
    {
        // Arrange
        var match = new Match("match-123", "player1", "player2", 5);
        match.Player1Score = 5;

        // Assert
        Assert.False(match.CanContinueToNextGame());
    }

    [Fact]
    public void CanContinueToNextGame_WaitingStatus_ReturnsFalse()
    {
        // Arrange
        var match = new Match();

        // Assert
        Assert.False(match.CanContinueToNextGame());
    }

    [Fact]
    public void CanPlayerReconnect_ValidPlayer_ReturnsTrue()
    {
        // Arrange
        var match = new Match("match-123", "player1", "player2", 5);

        // Assert
        Assert.True(match.CanPlayerReconnect("player1"));
        Assert.True(match.CanPlayerReconnect("player2"));
    }

    [Fact]
    public void CanPlayerReconnect_InvalidPlayer_ReturnsFalse()
    {
        // Arrange
        var match = new Match("match-123", "player1", "player2", 5);

        // Assert
        Assert.False(match.CanPlayerReconnect("player3"));
    }

    [Fact]
    public void CanPlayerReconnect_MatchComplete_ReturnsFalse()
    {
        // Arrange
        var match = new Match("match-123", "player1", "player2", 5);
        match.Status = MatchStatus.Completed;

        // Assert
        Assert.False(match.CanPlayerReconnect("player1"));
    }

    [Fact]
    public void TimeControl_DefaultsToNoTimeControl()
    {
        // Arrange
        var match = new Match();

        // Assert
        Assert.NotNull(match.TimeControl);
        Assert.Equal(TimeControlType.None, match.TimeControl.Type);
    }

    [Fact]
    public void TotalGamesPlayed_MultipleGames_ReturnsCount()
    {
        // Arrange
        var match = new Match("match-123", "player1", "player2", 5);
        match.AddGame(new Game("game-1"));
        match.AddGame(new Game("game-2"));
        match.AddGame(new Game("game-3"));

        // Assert
        Assert.Equal(3, match.TotalGamesPlayed);
    }
}
