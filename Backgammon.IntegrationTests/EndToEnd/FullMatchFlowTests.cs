using Backgammon.Core;
using Backgammon.IntegrationTests.Fixtures;
using Backgammon.Server.Models;
using Backgammon.Server.Services;
using Microsoft.Extensions.DependencyInjection;
using Match = Backgammon.Server.Models.Match;

namespace Backgammon.IntegrationTests.EndToEnd;

/// <summary>
/// End-to-end integration tests for full match flows.
/// Tests complete match lifecycle from creation through completion.
/// </summary>
[Collection("SignalR")]
[Trait("Category", "Integration")]
[Trait("Component", "EndToEnd")]
public class FullMatchFlowTests
{
    private readonly WebApplicationFixture _fixture;

    public FullMatchFlowTests(WebApplicationFixture fixture)
    {
        _fixture = fixture;
    }

    // ==================== Complete Match Flow Tests ====================

    [Fact]
    public async Task ThreePointMatch_AIOpponent_CompletesSuccessfully()
    {
        // Arrange
        var matchService = GetMatchService();
        var playerId = await CreateTestUserAsync("Player1");

        // Act - Create 3-point match vs AI
        var (match, game1) = await matchService.CreateMatchAsync(
            playerId,
            targetScore: 3,
            opponentType: "AI");

        // Assert initial state
        match.Status.Should().Be("InProgress");
        match.TargetScore.Should().Be(3);
        match.Player1Score.Should().Be(0);
        match.Player2Score.Should().Be(0);

        // Win game 1 (normal win = 1 point)
        await matchService.CompleteGameAsync(
            game1.GameId,
            new GameResult(playerId, WinType.Normal, 1));

        var matchAfterGame1 = await matchService.GetMatchAsync(match.MatchId);
        matchAfterGame1!.Player1Score.Should().Be(1);
        matchAfterGame1.Status.Should().Be("InProgress");

        // Start and win game 2 (gammon = 2 points, reach 3)
        var game2 = await matchService.StartNextGameAsync(match.MatchId);
        await matchService.CompleteGameAsync(
            game2.GameId,
            new GameResult(playerId, WinType.Gammon, 1));

        var finalMatch = await matchService.GetMatchAsync(match.MatchId);
        finalMatch!.Player1Score.Should().Be(3);
        finalMatch.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task FivePointMatch_MultipleGames_TracksScoreCorrectly()
    {
        // Arrange
        var matchService = GetMatchService();
        var playerId = await CreateTestUserAsync("Player1");

        var (match, game1) = await matchService.CreateMatchAsync(
            playerId,
            targetScore: 5,
            opponentType: "AI");

        var aiPlayerId = match.Player2Id!;

        // Game 1: Player wins normal (1-0)
        await matchService.CompleteGameAsync(
            game1.GameId,
            new GameResult(playerId, WinType.Normal, 1));

        // Game 2: AI wins gammon (1-2)
        var game2 = await matchService.StartNextGameAsync(match.MatchId);
        await matchService.CompleteGameAsync(
            game2.GameId,
            new GameResult(aiPlayerId, WinType.Gammon, 1));

        var matchAfterGame2 = await matchService.GetMatchAsync(match.MatchId);
        matchAfterGame2!.Player1Score.Should().Be(1);
        matchAfterGame2.Player2Score.Should().Be(2);

        // Game 3: Player wins backgammon (4-2)
        var game3 = await matchService.StartNextGameAsync(match.MatchId);
        await matchService.CompleteGameAsync(
            game3.GameId,
            new GameResult(playerId, WinType.Backgammon, 1));

        var matchAfterGame3 = await matchService.GetMatchAsync(match.MatchId);
        matchAfterGame3!.Player1Score.Should().Be(4);
        matchAfterGame3.Player2Score.Should().Be(2);

        // Game 4: Player wins normal (5-2, match complete)
        var game4 = await matchService.StartNextGameAsync(match.MatchId);
        await matchService.CompleteGameAsync(
            game4.GameId,
            new GameResult(playerId, WinType.Normal, 1));

        var finalMatch = await matchService.GetMatchAsync(match.MatchId);
        finalMatch!.Player1Score.Should().Be(5);
        finalMatch.Player2Score.Should().Be(2);
        finalMatch.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task Match_WithDoublingCube_MultipliesPointsCorrectly()
    {
        // Arrange
        var matchService = GetMatchService();
        var playerId = await CreateTestUserAsync("Player1");

        var (match, game1) = await matchService.CreateMatchAsync(
            playerId,
            targetScore: 9,
            opponentType: "AI");

        // Game 1: Player wins with cube at 4 (normal = 4 points)
        await matchService.CompleteGameAsync(
            game1.GameId,
            new GameResult(playerId, WinType.Normal, 4));

        var matchAfterGame1 = await matchService.GetMatchAsync(match.MatchId);
        matchAfterGame1!.Player1Score.Should().Be(4);

        // Game 2: Player wins gammon with cube at 2 (gammon = 4 points, total 8)
        var game2 = await matchService.StartNextGameAsync(match.MatchId);
        await matchService.CompleteGameAsync(
            game2.GameId,
            new GameResult(playerId, WinType.Gammon, 2));

        var matchAfterGame2 = await matchService.GetMatchAsync(match.MatchId);
        matchAfterGame2!.Player1Score.Should().Be(8);

        // Game 3: Player wins (1 point, total 9, match complete)
        var game3 = await matchService.StartNextGameAsync(match.MatchId);
        await matchService.CompleteGameAsync(
            game3.GameId,
            new GameResult(playerId, WinType.Normal, 1));

        var finalMatch = await matchService.GetMatchAsync(match.MatchId);
        finalMatch!.Player1Score.Should().Be(9);
        finalMatch.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task OpenLobby_JoinAndComplete_WorksEndToEnd()
    {
        // Arrange
        var matchService = GetMatchService();
        var player1Id = await CreateTestUserAsync("Player1");
        var player2Id = await CreateTestUserAsync("Player2");

        // Create open lobby
        var (match, _) = await matchService.CreateMatchAsync(
            player1Id,
            targetScore: 1,
            opponentType: "OpenLobby");

        match.Status.Should().Be("WaitingForPlayers");

        // Player 2 joins
        var updatedMatch = await matchService.JoinMatchAsync(
            match.MatchId,
            player2Id,
            "Player2");

        updatedMatch.Status.Should().Be("InProgress");
        updatedMatch.Player2Id.Should().Be(player2Id);

        // Get the current game
        var currentMatch = await matchService.GetMatchAsync(match.MatchId);
        var gameId = currentMatch!.CurrentGameId;

        // Complete the game
        await matchService.CompleteGameAsync(
            gameId!,
            new GameResult(player1Id, WinType.Normal, 1));

        var finalMatch = await matchService.GetMatchAsync(match.MatchId);
        finalMatch!.Status.Should().Be("Completed");
        finalMatch.Player1Score.Should().Be(1);
    }

    // ==================== Match Abandonment Tests ====================

    [Fact]
    public async Task Match_AbandonMidGame_SetsStatusToAbandoned()
    {
        // Arrange
        var matchService = GetMatchService();
        var playerId = await CreateTestUserAsync("Player1");

        var (match, game1) = await matchService.CreateMatchAsync(
            playerId,
            targetScore: 5,
            opponentType: "AI");

        // Win one game
        await matchService.CompleteGameAsync(
            game1.GameId,
            new GameResult(playerId, WinType.Normal, 1));

        var matchAfterGame1 = await matchService.GetMatchAsync(match.MatchId);
        matchAfterGame1!.Player1Score.Should().Be(1);

        // Abandon mid-match
        await matchService.AbandonMatchAsync(match.MatchId, playerId);

        var abandonedMatch = await matchService.GetMatchAsync(match.MatchId);
        abandonedMatch!.Status.Should().Be("Abandoned");
        // Score should be preserved
        abandonedMatch.Player1Score.Should().Be(1);
    }

    // ==================== Match Stats Verification ====================

    [Fact]
    public async Task Match_AfterCompletion_UpdatesPlayerStats()
    {
        // Arrange
        var matchService = GetMatchService();
        var playerId = await CreateTestUserAsync("StatsPlayer");

        // Get initial stats
        var initialStats = await matchService.GetPlayerMatchStatsAsync(playerId);

        // Create and complete a 1-point match
        var (match, game1) = await matchService.CreateMatchAsync(
            playerId,
            targetScore: 1,
            opponentType: "AI");

        await matchService.CompleteGameAsync(
            game1.GameId,
            new GameResult(playerId, WinType.Normal, 1));

        // Get updated stats
        var finalStats = await matchService.GetPlayerMatchStatsAsync(playerId);

        // Stats should reflect the completed match
        finalStats.TotalMatches.Should().BeGreaterThan(initialStats.TotalMatches);
    }

    // ==================== Helper Methods ====================

    private IMatchService GetMatchService() => _fixture.Services.GetRequiredService<IMatchService>();

    private IUserRepository GetUserRepository() => _fixture.Services.GetRequiredService<IUserRepository>();

    private async Task<string> CreateTestUserAsync(string displayName = "TestPlayer")
    {
        var userId = $"test_{Guid.NewGuid():N}";
        var user = new User
        {
            UserId = userId,
            Username = displayName,
            UsernameNormalized = displayName.ToLowerInvariant(),
            DisplayName = displayName,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow,
            Stats = new UserStats(),
            IsActive = true
        };
        await GetUserRepository().CreateUserAsync(user);
        return userId;
    }
}
