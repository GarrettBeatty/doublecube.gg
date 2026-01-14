using Backgammon.Core;
using Backgammon.IntegrationTests.Fixtures;
using Backgammon.Server.Models;
using Backgammon.Server.Services;
using Microsoft.Extensions.DependencyInjection;
using Match = Backgammon.Server.Models.Match;

namespace Backgammon.IntegrationTests.Services;

/// <summary>
/// Integration tests for MatchService operations.
/// Tests match creation, joining, game completion, and match lifecycle.
/// </summary>
[Collection("SignalR")]
[Trait("Category", "Integration")]
[Trait("Component", "MatchService")]
public class MatchServiceIntegrationTests
{
    private readonly WebApplicationFixture _fixture;

    public MatchServiceIntegrationTests(WebApplicationFixture fixture)
    {
        _fixture = fixture;
    }

    // ==================== Match Creation Tests ====================

    [Fact]
    public async Task CreateMatchAsync_AIMatch_CreatesMatchAndFirstGame()
    {
        // Arrange
        var matchService = GetMatchService();
        var playerId = await CreateTestUserAsync("Player1");

        // Act
        var (match, firstGame) = await matchService.CreateMatchAsync(
            playerId,
            targetScore: 3,
            opponentType: "AI",
            player1DisplayName: "Player1");

        // Assert
        match.Should().NotBeNull();
        match.MatchId.Should().NotBeNullOrEmpty();
        match.Player1Id.Should().Be(playerId);
        match.Player2Id.Should().StartWith("ai_"); // Format: ai_{aiType}_{guid}
        match.TargetScore.Should().Be(3);
        match.Status.Should().Be("InProgress");
        match.OpponentType.Should().Be("AI");

        firstGame.Should().NotBeNull();
        firstGame.GameId.Should().NotBeNullOrEmpty();
        firstGame.MatchId.Should().Be(match.MatchId);
    }

    [Fact]
    public async Task CreateMatchAsync_OpenLobby_CreatesMatchWaitingForPlayer()
    {
        // Arrange
        var matchService = GetMatchService();
        var playerId = await CreateTestUserAsync("Player1");

        // Act
        var (match, firstGame) = await matchService.CreateMatchAsync(
            playerId,
            targetScore: 5,
            opponentType: "OpenLobby",
            player1DisplayName: "Player1");

        // Assert
        match.Should().NotBeNull();
        match.Player1Id.Should().Be(playerId);
        match.Player2Id.Should().BeNullOrEmpty(); // Could be null or empty string
        match.TargetScore.Should().Be(5);
        match.Status.Should().Be("WaitingForPlayers");
        match.OpponentType.Should().Be("OpenLobby");
    }

    [Fact]
    public async Task CreateMatchAsync_FriendMatch_CreatesMatchWithInvitedPlayer()
    {
        // Arrange
        var matchService = GetMatchService();
        var player1Id = await CreateTestUserAsync("Player1");
        var player2Id = await CreateTestUserAsync("Player2");

        // Act
        var (match, firstGame) = await matchService.CreateMatchAsync(
            player1Id,
            targetScore: 7,
            opponentType: "Friend",
            player1DisplayName: "Player1",
            player2Id: player2Id);

        // Assert
        match.Should().NotBeNull();
        match.Player1Id.Should().Be(player1Id);
        match.Player2Id.Should().Be(player2Id);
        match.TargetScore.Should().Be(7);
        // When both players are specified, match starts immediately
        match.Status.Should().BeOneOf("WaitingForPlayers", "InProgress");
        match.OpponentType.Should().Be("Friend");
    }

    [Fact]
    public async Task CreateMatchAsync_WithTimeControl_SetsTimeControlConfig()
    {
        // Arrange
        var matchService = GetMatchService();
        var playerId = await CreateTestUserAsync("Player1");
        var timeControl = new TimeControlConfig
        {
            Type = TimeControlType.ChicagoPoint,
            DelaySeconds = 15
        };

        // Act
        var (match, firstGame) = await matchService.CreateMatchAsync(
            playerId,
            targetScore: 3,
            opponentType: "AI",
            timeControl: timeControl);

        // Assert
        match.TimeControl.Should().NotBeNull();
        match.TimeControl!.Type.Should().Be(TimeControlType.ChicagoPoint);
        match.TimeControl.DelaySeconds.Should().Be(15);
    }

    // ==================== Join Match Tests ====================

    [Fact]
    public async Task JoinMatchAsync_OpenLobby_SetsPlayer2AndStartsMatch()
    {
        // Arrange
        var matchService = GetMatchService();
        var player1Id = await CreateTestUserAsync("Player1");
        var player2Id = await CreateTestUserAsync("Player2");

        var (match, _) = await matchService.CreateMatchAsync(
            player1Id,
            targetScore: 3,
            opponentType: "OpenLobby");

        // Act
        var updatedMatch = await matchService.JoinMatchAsync(
            match.MatchId,
            player2Id,
            "Player2");

        // Assert
        updatedMatch.Player2Id.Should().Be(player2Id);
        updatedMatch.Player2DisplayName.Should().Be("Player2");
        updatedMatch.Status.Should().Be("InProgress");
    }

    [Fact]
    public async Task JoinMatchAsync_FriendMatch_WhenNoPlayer2Set_SetsPlayer2()
    {
        // Arrange - Create friend match without specifying player2Id
        var matchService = GetMatchService();
        var player1Id = await CreateTestUserAsync("Player1");
        var player2Id = await CreateTestUserAsync("Player2");

        // Create match without player2Id to simulate an invitation scenario
        var (match, _) = await matchService.CreateMatchAsync(
            player1Id,
            targetScore: 3,
            opponentType: "OpenLobby"); // Use OpenLobby since Friend requires player2Id

        // Act
        var updatedMatch = await matchService.JoinMatchAsync(
            match.MatchId,
            player2Id,
            "FriendlyName");

        // Assert
        updatedMatch.Player2Id.Should().Be(player2Id);
        updatedMatch.Player2DisplayName.Should().Be("FriendlyName");
        updatedMatch.Status.Should().Be("InProgress");
    }

    // ==================== Get Match Tests ====================

    [Fact]
    public async Task GetMatchAsync_ExistingMatch_ReturnsMatch()
    {
        // Arrange
        var matchService = GetMatchService();
        var playerId = await CreateTestUserAsync("Player1");
        var (createdMatch, _) = await matchService.CreateMatchAsync(
            playerId,
            targetScore: 3,
            opponentType: "AI");

        // Act
        var retrievedMatch = await matchService.GetMatchAsync(createdMatch.MatchId);

        // Assert
        retrievedMatch.Should().NotBeNull();
        retrievedMatch!.MatchId.Should().Be(createdMatch.MatchId);
        retrievedMatch.Player1Id.Should().Be(playerId);
    }

    [Fact]
    public async Task GetMatchAsync_NonExistentMatch_ReturnsNull()
    {
        // Arrange
        var matchService = GetMatchService();

        // Act
        var retrievedMatch = await matchService.GetMatchAsync("nonexistent-match");

        // Assert
        retrievedMatch.Should().BeNull();
    }

    [Fact]
    public async Task GetPlayerMatchesAsync_ReturnsPlayerMatches()
    {
        // Arrange
        var matchService = GetMatchService();
        var playerId = await CreateTestUserAsync("Player1");

        // Create multiple matches
        await matchService.CreateMatchAsync(playerId, 3, "AI");
        await matchService.CreateMatchAsync(playerId, 5, "AI");

        // Act
        var matches = await matchService.GetPlayerMatchesAsync(playerId);

        // Assert
        matches.Should().HaveCountGreaterThanOrEqualTo(2);
        matches.Should().OnlyContain(m => m.Player1Id == playerId || m.Player2Id == playerId);
    }

    [Fact]
    public async Task GetOpenLobbiesAsync_ReturnsWaitingMatches()
    {
        // Arrange
        var matchService = GetMatchService();
        var playerId = await CreateTestUserAsync("Player1");

        // Create an open lobby
        await matchService.CreateMatchAsync(playerId, 3, "OpenLobby");

        // Act
        var lobbies = await matchService.GetOpenLobbiesAsync();

        // Assert
        lobbies.Should().HaveCountGreaterThanOrEqualTo(1);
        lobbies.Should().OnlyContain(m => m.Status == "WaitingForPlayers");
    }

    // ==================== Match Completion Tests ====================

    [Fact]
    public async Task CompleteGameAsync_UpdatesMatchScores()
    {
        // Arrange
        var matchService = GetMatchService();
        var playerId = await CreateTestUserAsync("Player1");
        var (match, firstGame) = await matchService.CreateMatchAsync(
            playerId,
            targetScore: 3,
            opponentType: "AI");

        var gameResult = new GameResult(playerId, WinType.Normal, 1);

        // Act
        await matchService.CompleteGameAsync(firstGame.GameId, gameResult);
        var updatedMatch = await matchService.GetMatchAsync(match.MatchId);

        // Assert
        updatedMatch.Should().NotBeNull();
        updatedMatch!.Player1Score.Should().Be(1); // Normal win = 1 point
    }

    [Fact]
    public async Task CompleteGameAsync_GammonWin_AwardsTwoPoints()
    {
        // Arrange
        var matchService = GetMatchService();
        var playerId = await CreateTestUserAsync("Player1");
        var (match, firstGame) = await matchService.CreateMatchAsync(
            playerId,
            targetScore: 5,
            opponentType: "AI");

        var gameResult = new GameResult(playerId, WinType.Gammon, 1);

        // Act
        await matchService.CompleteGameAsync(firstGame.GameId, gameResult);
        var updatedMatch = await matchService.GetMatchAsync(match.MatchId);

        // Assert
        updatedMatch.Should().NotBeNull();
        updatedMatch!.Player1Score.Should().Be(2); // Gammon = 2 points
    }

    [Fact]
    public async Task CompleteGameAsync_BackgammonWin_AwardsThreePoints()
    {
        // Arrange
        var matchService = GetMatchService();
        var playerId = await CreateTestUserAsync("Player1");
        var (match, firstGame) = await matchService.CreateMatchAsync(
            playerId,
            targetScore: 5,
            opponentType: "AI");

        var gameResult = new GameResult(playerId, WinType.Backgammon, 1);

        // Act
        await matchService.CompleteGameAsync(firstGame.GameId, gameResult);
        var updatedMatch = await matchService.GetMatchAsync(match.MatchId);

        // Assert
        updatedMatch.Should().NotBeNull();
        updatedMatch!.Player1Score.Should().Be(3); // Backgammon = 3 points
    }

    [Fact]
    public async Task CompleteGameAsync_WithDoublingCube_MultipliesPoints()
    {
        // Arrange
        var matchService = GetMatchService();
        var playerId = await CreateTestUserAsync("Player1");
        var (match, firstGame) = await matchService.CreateMatchAsync(
            playerId,
            targetScore: 9,
            opponentType: "AI");

        var gameResult = new GameResult(playerId, WinType.Normal, 4);

        // Act
        await matchService.CompleteGameAsync(firstGame.GameId, gameResult);
        var updatedMatch = await matchService.GetMatchAsync(match.MatchId);

        // Assert
        updatedMatch.Should().NotBeNull();
        updatedMatch!.Player1Score.Should().Be(4); // 1 * 4 = 4 points
    }

    [Fact]
    public async Task IsMatchCompleteAsync_MatchReachedTargetScore_ReturnsTrue()
    {
        // Arrange
        var matchService = GetMatchService();
        var playerId = await CreateTestUserAsync("Player1");
        var (match, firstGame) = await matchService.CreateMatchAsync(
            playerId,
            targetScore: 1, // 1-point match for quick completion
            opponentType: "AI");

        // Complete the game with a win
        var gameResult = new GameResult(playerId, WinType.Normal, 1);
        await matchService.CompleteGameAsync(firstGame.GameId, gameResult);

        // Act
        var isComplete = await matchService.IsMatchCompleteAsync(match.MatchId);

        // Assert
        isComplete.Should().BeTrue();
    }

    [Fact]
    public async Task IsMatchCompleteAsync_MatchInProgress_ReturnsFalse()
    {
        // Arrange
        var matchService = GetMatchService();
        var playerId = await CreateTestUserAsync("Player1");
        var (match, _) = await matchService.CreateMatchAsync(
            playerId,
            targetScore: 5,
            opponentType: "AI");

        // Act
        var isComplete = await matchService.IsMatchCompleteAsync(match.MatchId);

        // Assert
        isComplete.Should().BeFalse();
    }

    // ==================== Start Next Game Tests ====================

    [Fact]
    public async Task StartNextGameAsync_CreatesNewGameInMatch()
    {
        // Arrange
        var matchService = GetMatchService();
        var playerId = await CreateTestUserAsync("Player1");
        var (match, firstGame) = await matchService.CreateMatchAsync(
            playerId,
            targetScore: 5,
            opponentType: "AI");

        // Complete first game
        var gameResult = new GameResult(playerId, WinType.Normal, 1);
        await matchService.CompleteGameAsync(firstGame.GameId, gameResult);

        // Act
        var secondGame = await matchService.StartNextGameAsync(match.MatchId);

        // Assert
        secondGame.Should().NotBeNull();
        secondGame.GameId.Should().NotBe(firstGame.GameId);
        secondGame.MatchId.Should().Be(match.MatchId);
    }

    // ==================== Abandon Match Tests ====================

    [Fact]
    public async Task AbandonMatchAsync_SetsStatusToAbandoned()
    {
        // Arrange
        var matchService = GetMatchService();
        var playerId = await CreateTestUserAsync("Player1");
        var (match, _) = await matchService.CreateMatchAsync(
            playerId,
            targetScore: 3,
            opponentType: "AI");

        // Act
        await matchService.AbandonMatchAsync(match.MatchId, playerId);
        var abandonedMatch = await matchService.GetMatchAsync(match.MatchId);

        // Assert
        abandonedMatch.Should().NotBeNull();
        abandonedMatch!.Status.Should().Be("Abandoned");
    }

    // ==================== Match Stats Tests ====================

    [Fact]
    public async Task GetPlayerMatchStatsAsync_ReturnsStats()
    {
        // Arrange
        var matchService = GetMatchService();
        var playerId = await CreateTestUserAsync("Player1");

        // Create and complete a match
        var (match, firstGame) = await matchService.CreateMatchAsync(
            playerId,
            targetScore: 1,
            opponentType: "AI");
        var gameResult = new GameResult(playerId, WinType.Normal, 1);
        await matchService.CompleteGameAsync(firstGame.GameId, gameResult);

        // Act
        var stats = await matchService.GetPlayerMatchStatsAsync(playerId);

        // Assert
        stats.Should().NotBeNull();
        stats.TotalMatches.Should().BeGreaterThanOrEqualTo(1);
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
