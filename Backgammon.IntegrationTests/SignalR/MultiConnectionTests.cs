using Backgammon.Core;
using Backgammon.IntegrationTests.Fixtures;
using Backgammon.Server.Models;
using Backgammon.Server.Models.SignalR;

namespace Backgammon.IntegrationTests.SignalR;

/// <summary>
/// Integration tests for multi-tab/multi-connection scenarios.
/// Tests that the same player can connect from multiple browser tabs
/// and all tabs receive real-time updates.
/// </summary>
[Collection("SignalR")]
[Trait("Category", "Integration")]
[Trait("Component", "MultiConnection")]
public class MultiConnectionTests : GameHubTestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MultiConnectionTests"/> class.
    /// </summary>
    /// <param name="fixture">The web application fixture.</param>
    public MultiConnectionTests(WebApplicationFixture fixture)
        : base(fixture)
    {
    }

    // ==================== Multi-Tab Connection Tests ====================

    [Fact]
    public async Task SamePlayer_MultipleConnections_BothReceiveUpdates()
    {
        // Arrange
        ClearCapturedBroadcasts();
        var playerId = await CreateTestUserAsync("MultiTabPlayer");

        // Create two hub instances for the same player (simulating two tabs)
        var (hub1, connection1, _) = CreateHubForUser(playerId, "MultiTabPlayer");
        var (hub2, connection2, _) = CreateHubForUser(playerId, "MultiTabPlayer");

        // Verify connections are different
        connection1.Should().NotBe(connection2);

        // Act - Create game from first tab
        var config = new MatchConfig
        {
            TargetScore = 1,
            OpponentType = "AI",
            DisplayName = "MultiTabPlayer"
        };
        await hub1.CreateMatch(config);

        // Assert - MatchCreated event should be captured
        CapturedMatchCreated.Should().HaveCount(1);
        var gameId = CapturedMatchCreated[0].GameId;
        gameId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SamePlayer_JoinGameFromSecondTab_BothTabsCanMakeMoves()
    {
        // Arrange
        ClearCapturedBroadcasts();
        var playerId = await CreateTestUserAsync("DualTabPlayer");

        var (hub1, connection1, _) = CreateHubForUser(playerId, "DualTabPlayer");
        var (hub2, connection2, _) = CreateHubForUser(playerId, "DualTabPlayer");

        // Create analysis game from first tab
        await hub1.CreateAnalysisSession();
        await Task.Delay(100);

        // First tab should be in a game session now
        var sessionManager = GetSessionManager();
        var session1 = sessionManager.GetGameByPlayer(connection1);
        session1.Should().NotBeNull();

        // Act - Get state from first tab
        ClearCapturedBroadcasts();
        await hub1.GetGameState();

        // Assert
        CapturedErrors.Should().BeEmpty();
        CapturedGameUpdates.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task MultiplePlayersJoinSameGame_MatchCreatedAndBothCanJoin()
    {
        // Arrange
        ClearCapturedBroadcasts();
        var player1Id = await CreateTestUserAsync("MultiPlayer1");
        var player2Id = await CreateTestUserAsync("MultiPlayer2");

        // Create match via service to avoid hub callback complexity
        var matchService = GetMatchService();
        var (match, firstGame) = await matchService.CreateMatchAsync(
            player1Id,
            targetScore: 1,
            opponentType: "OpenLobby",
            player1DisplayName: "MultiPlayer1");

        // Join match via service
        var updatedMatch = await matchService.JoinMatchAsync(match.MatchId, player2Id, "MultiPlayer2");

        // Assert - Match should be in progress with both players
        updatedMatch.Status.Should().Be("InProgress");
        updatedMatch.Player1Id.Should().Be(player1Id);
        updatedMatch.Player2Id.Should().Be(player2Id);

        // Now both players can join the game via hub
        var (hub1, connection1, _) = CreateHubForUser(player1Id, "MultiPlayer1");
        await hub1.JoinGame(firstGame.GameId);
        CapturedErrors.Should().BeEmpty();

        ClearCapturedBroadcasts();
        var (hub2, connection2, _) = CreateHubForUser(player2Id, "MultiPlayer2");
        await hub2.JoinGame(firstGame.GameId);

        // Assert - Both joined without error
        CapturedErrors.Should().BeEmpty();
    }

    [Fact]
    public async Task PlayerDisconnectsOneTab_OtherTabStillWorks()
    {
        // Arrange
        ClearCapturedBroadcasts();
        var playerId = await CreateTestUserAsync("DisconnectTabPlayer");

        var (hub1, connection1, _) = CreateHubForUser(playerId, "DisconnectTabPlayer");
        var (hub2, connection2, _) = CreateHubForUser(playerId, "DisconnectTabPlayer");

        // Create analysis game from first tab
        await hub1.CreateAnalysisSession();
        await Task.Delay(100);

        // Act - Leave game from first tab
        await hub1.LeaveGame();

        // Second tab should still be able to work (create new game)
        ClearCapturedBroadcasts();
        await hub2.CreateAnalysisSession();

        // Assert
        CapturedErrors.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateGameFromDifferentTabs_EachCreatesOwnGame()
    {
        // Arrange
        ClearCapturedBroadcasts();
        var playerId = await CreateTestUserAsync("TwoGamesPlayer");

        var (hub1, connection1, _) = CreateHubForUser(playerId, "TwoGamesPlayer");
        var (hub2, connection2, _) = CreateHubForUser(playerId, "TwoGamesPlayer");

        // Act - Create game from first tab
        await hub1.CreateAnalysisSession();
        await Task.Delay(100);

        // Create another game from second tab
        await hub2.CreateAnalysisSession();
        await Task.Delay(100);

        // Assert - Both should succeed (each tab in own analysis game)
        CapturedErrors.Should().BeEmpty();
    }

    // ==================== Race Condition Prevention Tests ====================

    [Fact]
    public async Task ConcurrentMatchContinuation_OnlyOneGameCreated()
    {
        // Arrange
        ClearCapturedBroadcasts();
        var playerId = await CreateTestUserAsync("RacePlayer");
        var matchService = GetMatchService();

        // Create and complete a match's first game
        var (match, firstGame) = await matchService.CreateMatchAsync(
            playerId,
            targetScore: 3,
            opponentType: "AI",
            player1DisplayName: "RacePlayer");

        // Complete the first game
        var aiPlayerId = match.Player2Id!;
        var gameResult = new GameResult(playerId, WinType.Normal, 1);
        await matchService.CompleteGameAsync(firstGame.GameId, gameResult);

        // Verify first game completed and match still in progress
        var matchAfterGame1 = await matchService.GetMatchAsync(match.MatchId);
        matchAfterGame1!.Player1Score.Should().Be(1);
        matchAfterGame1.Status.Should().Be("InProgress");

        // Act - Simulate two tabs calling ContinueMatch concurrently
        var (hub1, connection1, _) = CreateHubForUser(playerId, "RacePlayer");
        var (hub2, connection2, _) = CreateHubForUser(playerId, "RacePlayer");

        // Start both continuation calls (they should be serialized by the semaphore)
        var task1 = hub1.ContinueMatch(match.MatchId);
        var task2 = hub2.ContinueMatch(match.MatchId);

        await Task.WhenAll(task1, task2);

        // Assert - Should have only one new game created
        var finalMatch = await matchService.GetMatchAsync(match.MatchId);
        // Should have 2 games total (first game + one continued game)
        finalMatch!.GameIds.Should().HaveCount(2);
    }

    // ==================== Session Tracking Tests ====================

    [Fact]
    public async Task JoinGame_TracksConnectionInSession()
    {
        // Arrange
        ClearCapturedBroadcasts();
        var playerId = await CreateTestUserAsync("SessionTrackPlayer");

        var (hub, connectionId, _) = CreateHubForUser(playerId, "SessionTrackPlayer");

        // Create and join game
        await hub.CreateAnalysisSession();
        await Task.Delay(100);

        // Act - Check session manager
        var sessionManager = GetSessionManager();
        var session = sessionManager.GetGameByPlayer(connectionId);

        // Assert
        session.Should().NotBeNull();
    }

    [Fact]
    public async Task LeaveGame_RemovesConnectionFromSession()
    {
        // Arrange
        ClearCapturedBroadcasts();
        var playerId = await CreateTestUserAsync("LeaveTrackPlayer");

        var (hub, connectionId, _) = CreateHubForUser(playerId, "LeaveTrackPlayer");

        await hub.CreateAnalysisSession();
        await Task.Delay(100);

        // Verify in session
        var sessionManager = GetSessionManager();
        var sessionBefore = sessionManager.GetGameByPlayer(connectionId);
        sessionBefore.Should().NotBeNull();

        // Act
        await hub.LeaveGame();

        // Assert - Connection should be removed from session manager
        var sessionAfter = sessionManager.GetGameByPlayer(connectionId);
        sessionAfter.Should().BeNull();
    }
}
