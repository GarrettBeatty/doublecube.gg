using System;
using Backgammon.Core;
using Xunit;

namespace Backgammon.Tests;

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
