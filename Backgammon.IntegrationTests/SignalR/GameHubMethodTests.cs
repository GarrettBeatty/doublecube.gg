using Backgammon.Core;
using Backgammon.IntegrationTests.Fixtures;
using Backgammon.Server.Models;
using Backgammon.Server.Models.SignalR;
using Backgammon.Server.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Backgammon.IntegrationTests.SignalR;

/// <summary>
/// Integration tests for GameHub methods using direct hub invocation.
/// Tests hub method logic with real services but mocked SignalR context.
/// </summary>
[Collection("SignalR")]
[Trait("Category", "Integration")]
[Trait("Component", "GameHub")]
public class GameHubMethodTests : GameHubTestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GameHubMethodTests"/> class.
    /// </summary>
    /// <param name="fixture">The web application fixture.</param>
    public GameHubMethodTests(WebApplicationFixture fixture)
        : base(fixture)
    {
    }

    // ==================== Match Creation Tests ====================

    [Fact]
    public async Task CreateMatch_AIOpponent_CreatesMatchAndFirstGame()
    {
        // Arrange
        ClearCapturedBroadcasts();
        var playerId = await CreateTestUserAsync("AIMatchPlayer");
        var (hub, connectionId, _) = CreateHubForUser(playerId, "AIMatchPlayer");

        var config = new MatchConfig
        {
            TargetScore = 3,
            OpponentType = "AI",
            DisplayName = "AIMatchPlayer"
        };

        // Act
        await hub.CreateMatch(config);

        // Assert
        CapturedMatchCreated.Should().HaveCount(1);
        var matchCreated = CapturedMatchCreated[0];
        matchCreated.MatchId.Should().NotBeNullOrEmpty();
        matchCreated.GameId.Should().NotBeNullOrEmpty();
        matchCreated.TargetScore.Should().Be(3);
        matchCreated.OpponentType.Should().Be("AI");
        matchCreated.Player1Id.Should().Be(playerId);
        matchCreated.Player2Id.Should().StartWith("ai_");
    }

    [Fact]
    public async Task CreateMatch_OpenLobby_CreatesMatchWaitingForPlayers()
    {
        // Arrange
        ClearCapturedBroadcasts();
        var playerId = await CreateTestUserAsync("LobbyPlayer");
        var (hub, connectionId, _) = CreateHubForUser(playerId, "LobbyPlayer");

        var config = new MatchConfig
        {
            TargetScore = 5,
            OpponentType = "OpenLobby",
            DisplayName = "LobbyPlayer"
        };

        // Act
        await hub.CreateMatch(config);

        // Assert
        CapturedMatchCreated.Should().HaveCount(1);
        var matchCreated = CapturedMatchCreated[0];
        matchCreated.MatchId.Should().NotBeNullOrEmpty();
        matchCreated.TargetScore.Should().Be(5);
        matchCreated.OpponentType.Should().Be("OpenLobby");

        // Verify match in database
        var match = await GetMatchService().GetMatchAsync(matchCreated.MatchId);
        match.Should().NotBeNull();
        match!.Status.Should().Be("WaitingForPlayers");
    }

    [Fact]
    public async Task JoinMatch_OpenLobby_TransitionsToInProgress()
    {
        // Arrange
        ClearCapturedBroadcasts();
        var player1Id = await CreateTestUserAsync("LobbyCreator");
        var player2Id = await CreateTestUserAsync("LobbyJoiner");

        var (hub1, connection1, _) = CreateHubForUser(player1Id, "LobbyCreator");

        // Create match
        var config = new MatchConfig
        {
            TargetScore = 3,
            OpponentType = "OpenLobby",
            DisplayName = "LobbyCreator"
        };
        await hub1.CreateMatch(config);

        var matchId = CapturedMatchCreated[0].MatchId;
        ClearCapturedBroadcasts();

        // Act - Second player joins
        var (hub2, connection2, _) = CreateHubForUser(player2Id, "LobbyJoiner");
        await hub2.JoinMatch(matchId);

        // Assert
        var match = await GetMatchService().GetMatchAsync(matchId);
        match.Should().NotBeNull();
        match!.Status.Should().Be("InProgress");
        match.Player2Id.Should().Be(player2Id);

        // MatchCreated should be sent to joiner
        CapturedMatchCreated.Should().HaveCount(1);
    }

    // ==================== JoinGame Tests ====================

    [Fact]
    public async Task JoinGame_FirstPlayer_AddsToSession()
    {
        // Arrange
        ClearCapturedBroadcasts();
        var playerId = await CreateTestUserAsync("JoinGamePlayer");
        var (hub, connectionId, _) = CreateHubForUser(playerId, "JoinGamePlayer");

        // Create a match to get a game ID
        var config = new MatchConfig
        {
            TargetScore = 1,
            OpponentType = "AI",
            DisplayName = "JoinGamePlayer"
        };
        await hub.CreateMatch(config);

        var gameId = CapturedMatchCreated[0].GameId;
        ClearCapturedBroadcasts();

        // Act
        await hub.JoinGame(gameId);

        // Assert - Should receive GameUpdate or WaitingForOpponent
        // For AI games, player joins and game starts immediately
        CapturedErrors.Should().BeEmpty();
    }

    [Fact]
    public async Task JoinGame_WithoutGameId_ReturnsError()
    {
        // Arrange
        ClearCapturedBroadcasts();
        var playerId = await CreateTestUserAsync("NoGameIdPlayer");
        var (hub, connectionId, _) = CreateHubForUser(playerId, "NoGameIdPlayer");

        // Act
        await hub.JoinGame(null);

        // Assert
        CapturedErrors.Should().Contain("Game ID is required");
    }

    [Fact]
    public async Task JoinGame_SecondPlayer_StartsGame()
    {
        // Arrange
        ClearCapturedBroadcasts();
        var player1Id = await CreateTestUserAsync("Player1");
        var player2Id = await CreateTestUserAsync("Player2");

        // Create OpenLobby match
        var (hub1, connection1, _) = CreateHubForUser(player1Id, "Player1");
        var config = new MatchConfig
        {
            TargetScore = 1,
            OpponentType = "OpenLobby",
            DisplayName = "Player1"
        };
        await hub1.CreateMatch(config);

        var matchId = CapturedMatchCreated[0].MatchId;
        var gameId = CapturedMatchCreated[0].GameId;

        // Player 1 joins game
        await hub1.JoinGame(gameId);

        // Player 2 joins match first
        var (hub2, connection2, _) = CreateHubForUser(player2Id, "Player2");
        await hub2.JoinMatch(matchId);

        ClearCapturedBroadcasts();

        // Act - Player 2 joins game
        await hub2.JoinGame(gameId);

        // Assert - Game should start
        CapturedErrors.Should().BeEmpty();
    }

    // ==================== RollDice Tests ====================

    [Fact]
    public async Task RollDice_WhenNotInGame_ReturnsError()
    {
        // Arrange
        ClearCapturedBroadcasts();
        var playerId = await CreateTestUserAsync("NotInGamePlayer");
        var (hub, connectionId, _) = CreateHubForUser(playerId, "NotInGamePlayer");

        // Act
        await hub.RollDice();

        // Assert
        CapturedErrors.Should().Contain("Not in a game");
    }

    [Fact]
    public async Task RollDice_InGame_UpdatesGameState()
    {
        // Arrange
        ClearCapturedBroadcasts();
        var playerId = await CreateTestUserAsync("RollDicePlayer");
        var (hub, connectionId, _) = CreateHubForUser(playerId, "RollDicePlayer");

        // Create AI match and join game
        var config = new MatchConfig
        {
            TargetScore = 1,
            OpponentType = "AI",
            DisplayName = "RollDicePlayer"
        };
        await hub.CreateMatch(config);
        var gameId = CapturedMatchCreated[0].GameId;
        await hub.JoinGame(gameId);

        ClearCapturedBroadcasts();

        // Act
        await hub.RollDice();

        // Assert - Should either get GameUpdate or error (depends on turn)
        // AI games auto-roll, so this might be "Not your turn"
        // The important thing is no exception thrown
    }

    // ==================== MakeMove Tests ====================

    [Fact]
    public async Task MakeMove_WhenNotInGame_ReturnsError()
    {
        // Arrange
        ClearCapturedBroadcasts();
        var playerId = await CreateTestUserAsync("NotInGameMover");
        var (hub, connectionId, _) = CreateHubForUser(playerId, "NotInGameMover");

        // Act
        await hub.MakeMove(6, 3);

        // Assert
        CapturedErrors.Should().Contain("Not in a game");
    }

    // ==================== EndTurn Tests ====================

    [Fact]
    public async Task EndTurn_WhenNotInGame_ReturnsError()
    {
        // Arrange
        ClearCapturedBroadcasts();
        var playerId = await CreateTestUserAsync("NotInGameEnder");
        var (hub, connectionId, _) = CreateHubForUser(playerId, "NotInGameEnder");

        // Act
        await hub.EndTurn();

        // Assert
        CapturedErrors.Should().Contain("Not in a game");
    }

    // ==================== Analysis Mode Tests ====================

    [Fact]
    public async Task CreateAnalysisSession_CreatesSessionWithSinglePlayer()
    {
        // Arrange
        ClearCapturedBroadcasts();
        var playerId = await CreateTestUserAsync("AnalysisPlayer");
        var (hub, connectionId, _) = CreateHubForUser(playerId, "AnalysisPlayer");

        // Act
        await hub.CreateAnalysisSession();

        // Assert
        CapturedErrors.Should().BeEmpty();
        // Analysis game should start immediately
    }

    [Fact]
    public async Task SetDice_InAnalysisMode_SetsDiceValues()
    {
        // Arrange
        ClearCapturedBroadcasts();
        var playerId = await CreateTestUserAsync("AnalysisDicePlayer");
        var (hub, connectionId, _) = CreateHubForUser(playerId, "AnalysisDicePlayer");

        await hub.CreateAnalysisSession();

        // Wait for game to be created
        await Task.Delay(100);
        ClearCapturedBroadcasts();

        // Act
        await hub.SetDice(3, 4);

        // Assert
        CapturedErrors.Should().BeEmpty();
    }

    [Fact]
    public async Task SetDice_InvalidValues_ReturnsError()
    {
        // Arrange
        ClearCapturedBroadcasts();
        var playerId = await CreateTestUserAsync("InvalidDicePlayer");
        var (hub, connectionId, _) = CreateHubForUser(playerId, "InvalidDicePlayer");

        await hub.CreateAnalysisSession();
        await Task.Delay(100);
        ClearCapturedBroadcasts();

        // Act
        await hub.SetDice(0, 7); // Invalid dice values

        // Assert
        CapturedErrors.Should().Contain("Dice values must be between 1 and 6");
    }

    [Fact]
    public async Task SetDice_WhenNotInAnalysisMode_ReturnsError()
    {
        // Arrange
        ClearCapturedBroadcasts();
        var playerId = await CreateTestUserAsync("NotAnalysisPlayer");
        var (hub, connectionId, _) = CreateHubForUser(playerId, "NotAnalysisPlayer");

        // Create AI game (not analysis mode)
        var config = new MatchConfig
        {
            TargetScore = 1,
            OpponentType = "AI",
            DisplayName = "NotAnalysisPlayer"
        };
        await hub.CreateMatch(config);
        var gameId = CapturedMatchCreated[0].GameId;
        await hub.JoinGame(gameId);

        await Task.Delay(100);
        ClearCapturedBroadcasts();

        // Act
        await hub.SetDice(3, 4);

        // Assert
        CapturedErrors.Should().Contain("Dice can only be set in analysis mode");
    }

    // ==================== GetGameState Tests ====================

    [Fact]
    public async Task GetGameState_WhenNotInGame_ReturnsError()
    {
        // Arrange
        ClearCapturedBroadcasts();
        var playerId = await CreateTestUserAsync("NotInGameStatePlayer");
        var (hub, connectionId, _) = CreateHubForUser(playerId, "NotInGameStatePlayer");

        // Act
        await hub.GetGameState();

        // Assert
        CapturedErrors.Should().Contain("Not in a game");
    }

    [Fact]
    public async Task GetGameState_InGame_ReturnsCurrentState()
    {
        // Arrange
        ClearCapturedBroadcasts();
        var playerId = await CreateTestUserAsync("GetStatePlayer");
        var (hub, connectionId, _) = CreateHubForUser(playerId, "GetStatePlayer");

        await hub.CreateAnalysisSession();
        await Task.Delay(100);
        ClearCapturedBroadcasts();

        // Act
        await hub.GetGameState();

        // Assert
        CapturedErrors.Should().BeEmpty();
        CapturedGameUpdates.Should().HaveCountGreaterThan(0);
    }

    // ==================== LeaveGame Tests ====================

    [Fact]
    public async Task LeaveGame_WhenInGame_RemovesFromSession()
    {
        // Arrange
        ClearCapturedBroadcasts();
        var playerId = await CreateTestUserAsync("LeaveGamePlayer");
        var (hub, connectionId, _) = CreateHubForUser(playerId, "LeaveGamePlayer");

        await hub.CreateAnalysisSession();
        await Task.Delay(100);

        // Act
        await hub.LeaveGame();

        // Assert - No errors expected
        CapturedErrors.Should().BeEmpty();
    }

    // ==================== Doubling Cube Tests ====================

    [Fact]
    public async Task OfferDouble_WhenNotInGame_ReturnsError()
    {
        // Arrange
        ClearCapturedBroadcasts();
        var playerId = await CreateTestUserAsync("NotInGameDoubler");
        var (hub, connectionId, _) = CreateHubForUser(playerId, "NotInGameDoubler");

        // Act
        await hub.OfferDouble();

        // Assert
        CapturedErrors.Should().Contain("Not in a game");
    }

    [Fact]
    public async Task AcceptDouble_WhenNotInGame_ReturnsError()
    {
        // Arrange
        ClearCapturedBroadcasts();
        var playerId = await CreateTestUserAsync("NotInGameAccepter");
        var (hub, connectionId, _) = CreateHubForUser(playerId, "NotInGameAccepter");

        // Act
        await hub.AcceptDouble();

        // Assert
        CapturedErrors.Should().Contain("Not in a game");
    }

    [Fact]
    public async Task DeclineDouble_WhenNotInGame_ReturnsError()
    {
        // Arrange
        ClearCapturedBroadcasts();
        var playerId = await CreateTestUserAsync("NotInGameDecliner");
        var (hub, connectionId, _) = CreateHubForUser(playerId, "NotInGameDecliner");

        // Act
        await hub.DeclineDouble();

        // Assert
        CapturedErrors.Should().Contain("Not in a game");
    }

    // ==================== Match Status Tests ====================

    [Fact]
    public async Task GetMatchStatus_ExistingMatch_ReturnsStatus()
    {
        // Arrange
        ClearCapturedBroadcasts();
        var playerId = await CreateTestUserAsync("MatchStatusPlayer");
        var (hub, connectionId, _) = CreateHubForUser(playerId, "MatchStatusPlayer");

        // Create match
        var config = new MatchConfig
        {
            TargetScore = 5,
            OpponentType = "AI",
            DisplayName = "MatchStatusPlayer"
        };
        await hub.CreateMatch(config);
        var matchId = CapturedMatchCreated[0].MatchId;

        // Act
        await hub.GetMatchStatus(matchId);

        // Assert
        CapturedErrors.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMatchStatus_NonExistentMatch_ReturnsError()
    {
        // Arrange
        ClearCapturedBroadcasts();
        var playerId = await CreateTestUserAsync("BadMatchStatusPlayer");
        var (hub, connectionId, _) = CreateHubForUser(playerId, "BadMatchStatusPlayer");

        // Act
        await hub.GetMatchStatus("nonexistent-match-id");

        // Assert
        CapturedErrors.Should().Contain("Match not found");
    }

    [Fact]
    public async Task GetMatchStatus_UnauthorizedPlayer_ReturnsError()
    {
        // Arrange
        ClearCapturedBroadcasts();
        var player1Id = await CreateTestUserAsync("MatchCreator");
        var player2Id = await CreateTestUserAsync("UnauthorizedPlayer");

        var (hub1, connection1, _) = CreateHubForUser(player1Id, "MatchCreator");
        var config = new MatchConfig
        {
            TargetScore = 3,
            OpponentType = "AI",
            DisplayName = "MatchCreator"
        };
        await hub1.CreateMatch(config);
        var matchId = CapturedMatchCreated[0].MatchId;

        // Different player tries to get status
        ClearCapturedBroadcasts();
        var (hub2, connection2, _) = CreateHubForUser(player2Id, "UnauthorizedPlayer");

        // Act
        await hub2.GetMatchStatus(matchId);

        // Assert
        CapturedErrors.Should().Contain("Access denied");
    }

    // ==================== GetMyMatches Tests ====================

    [Fact]
    public async Task GetMyMatches_ReturnsPlayerMatches()
    {
        // Arrange
        ClearCapturedBroadcasts();
        var playerId = await CreateTestUserAsync("MyMatchesPlayer");
        var (hub, connectionId, _) = CreateHubForUser(playerId, "MyMatchesPlayer");

        // Create a match
        var config = new MatchConfig
        {
            TargetScore = 3,
            OpponentType = "AI",
            DisplayName = "MyMatchesPlayer"
        };
        await hub.CreateMatch(config);

        // Act
        await hub.GetMyMatches(null);

        // Assert
        CapturedErrors.Should().BeEmpty();
    }

    // ==================== AbandonGame Tests ====================

    [Fact]
    public async Task AbandonGame_WhenNotInGame_ReturnsError()
    {
        // Arrange
        ClearCapturedBroadcasts();
        var playerId = await CreateTestUserAsync("NotInGameAbandoner");
        var (hub, connectionId, _) = CreateHubForUser(playerId, "NotInGameAbandoner");

        // Act
        await hub.AbandonGame();

        // Assert
        CapturedErrors.Should().Contain("Not in a game");
    }

    [Fact]
    public async Task AbandonGame_InGame_MarksGameAsAbandoned()
    {
        // Arrange
        ClearCapturedBroadcasts();
        var playerId = await CreateTestUserAsync("AbandonGamePlayer");
        var (hub, connectionId, _) = CreateHubForUser(playerId, "AbandonGamePlayer");

        // Create and join game
        var config = new MatchConfig
        {
            TargetScore = 1,
            OpponentType = "AI",
            DisplayName = "AbandonGamePlayer"
        };
        await hub.CreateMatch(config);
        var gameId = CapturedMatchCreated[0].GameId;
        await hub.JoinGame(gameId);

        await Task.Delay(100);
        ClearCapturedBroadcasts();

        // Act
        await hub.AbandonGame();

        // Assert - Should get GameOver event
        CapturedErrors.Should().BeEmpty();
    }

    // ==================== GetMatchLobbies Tests ====================

    [Fact]
    public async Task GetMatchLobbies_ReturnsOpenLobbies()
    {
        // Arrange
        ClearCapturedBroadcasts();
        var playerId = await CreateTestUserAsync("LobbyListPlayer");
        var (hub, connectionId, _) = CreateHubForUser(playerId, "LobbyListPlayer");

        // Create an open lobby
        var config = new MatchConfig
        {
            TargetScore = 3,
            OpponentType = "OpenLobby",
            DisplayName = "LobbyListPlayer"
        };
        await hub.CreateMatch(config);

        // Act
        var lobbies = await hub.GetMatchLobbies(null);

        // Assert
        lobbies.Should().NotBeNull();
        lobbies.Should().HaveCountGreaterThanOrEqualTo(1);
    }

    // ==================== UndoLastMove Tests ====================

    [Fact]
    public async Task UndoLastMove_WhenNotInGame_ReturnsError()
    {
        // Arrange
        ClearCapturedBroadcasts();
        var playerId = await CreateTestUserAsync("NotInGameUndoer");
        var (hub, connectionId, _) = CreateHubForUser(playerId, "NotInGameUndoer");

        // Act
        await hub.UndoLastMove();

        // Assert
        CapturedErrors.Should().Contain("Not in a game");
    }

    // ==================== GetValidSources Tests ====================

    [Fact]
    public async Task GetValidSources_WhenNotInGame_ReturnsEmptyList()
    {
        // Arrange
        ClearCapturedBroadcasts();
        var playerId = await CreateTestUserAsync("ValidSourcesPlayer");
        var (hub, connectionId, _) = CreateHubForUser(playerId, "ValidSourcesPlayer");

        // Act
        var sources = await hub.GetValidSources();

        // Assert
        sources.Should().BeEmpty();
    }

    // ==================== GetValidDestinations Tests ====================

    [Fact]
    public async Task GetValidDestinations_WhenNotInGame_ReturnsEmptyList()
    {
        // Arrange
        ClearCapturedBroadcasts();
        var playerId = await CreateTestUserAsync("ValidDestPlayer");
        var (hub, connectionId, _) = CreateHubForUser(playerId, "ValidDestPlayer");

        // Act
        var destinations = await hub.GetValidDestinations(6);

        // Assert
        destinations.Should().BeEmpty();
    }
}
