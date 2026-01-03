using Backgammon.Core;
using Backgammon.Server.Extensions;
using Backgammon.Server.Hubs;
using Backgammon.Server.Models;
using Backgammon.Server.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backgammon.Tests.Services;

public class GameCreationServiceTests
{
    private readonly Mock<IGameSessionManager> _mockSessionManager;
    private readonly Mock<IGameStateService> _mockGameStateService;
    private readonly Mock<IGameRepository> _mockGameRepository;
    private readonly Mock<IAiMoveService> _mockAiMoveService;
    private readonly Mock<IGameActionOrchestrator> _mockOrchestrator;
    private readonly Mock<IHubContext<GameHub>> _mockHubContext;
    private readonly Mock<ILogger<GameCreationService>> _mockLogger;
    private readonly GameCreationService _service;

    public GameCreationServiceTests()
    {
        _mockSessionManager = new Mock<IGameSessionManager>();
        _mockGameStateService = new Mock<IGameStateService>();
        _mockGameRepository = new Mock<IGameRepository>();
        _mockAiMoveService = new Mock<IAiMoveService>();
        _mockOrchestrator = new Mock<IGameActionOrchestrator>();
        _mockHubContext = new Mock<IHubContext<GameHub>>();
        _mockLogger = new Mock<ILogger<GameCreationService>>();

        _service = new GameCreationService(
            _mockSessionManager.Object,
            _mockGameStateService.Object,
            _mockGameRepository.Object,
            _mockAiMoveService.Object,
            _mockOrchestrator.Object,
            _mockHubContext.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task JoinGameAsync_NewGame_WaitingForOpponent()
    {
        // Arrange
        var connectionId = "conn-123";
        var playerId = "player-123";
        var displayName = "TestPlayer";
        var gameSession = new GameSession("game-123");
        var mockGroups = new Mock<IGroupManager>();
        var mockClients = new Mock<IHubClients>();
        var mockCallerClients = new Mock<ISingleClientProxy>();

        _mockSessionManager
            .Setup(m => m.JoinOrCreateAsync(playerId, connectionId, null))
            .ReturnsAsync(gameSession);

        _mockHubContext.Setup(h => h.Groups).Returns(mockGroups.Object);
        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.Client(connectionId)).Returns(mockCallerClients.Object);

        // Act
        await _service.JoinGameAsync(connectionId, playerId, displayName, null);

        // Assert
        _mockSessionManager.Verify(m => m.JoinOrCreateAsync(playerId, connectionId, null), Times.Once);
        mockGroups.Verify(g => g.AddToGroupAsync(connectionId, gameSession.Id, default), Times.Once);
        mockCallerClients.Verify(
            c => c.SendCoreAsync(
                "GameUpdate",
                It.IsAny<object[]>(),
                default),
            Times.Once);
        mockCallerClients.Verify(
            c => c.SendCoreAsync(
                "WaitingForOpponent",
                It.Is<object[]>(args => args.Length == 1 && (string)args[0] == gameSession.Id),
                default),
            Times.Once);
    }

    [Fact]
    public async Task JoinGameAsync_GameFull_StartsGame()
    {
        // Arrange
        var connectionId = "conn-123";
        var playerId = "player-123";
        var displayName = "TestPlayer";
        var gameSession = new GameSession("game-123");
        gameSession.AddPlayer("player-456", "conn-456"); // First player
        gameSession.AddPlayer(playerId, connectionId); // Second player - game full

        var mockGroups = new Mock<IGroupManager>();
        _mockSessionManager
            .Setup(m => m.JoinOrCreateAsync(playerId, connectionId, null))
            .ReturnsAsync(gameSession);

        _mockHubContext.Setup(h => h.Groups).Returns(mockGroups.Object);

        // Act
        await _service.JoinGameAsync(connectionId, playerId, displayName, null);

        // Assert
        _mockGameStateService.Verify(s => s.BroadcastGameStartAsync(gameSession), Times.Once);
        // Note: SaveGameAsync may be called from background task or orchestrator
    }

    [Fact]
    public async Task JoinGameAsync_WithDisplayName_SetsPlayerName()
    {
        // Arrange
        var connectionId = "conn-123";
        var playerId = "player-123";
        var displayName = "TestPlayer";
        var gameSession = new GameSession("game-123");
        var mockGroups = new Mock<IGroupManager>();
        var mockClients = new Mock<IHubClients>();
        var mockCallerClients = new Mock<ISingleClientProxy>();

        _mockSessionManager
            .Setup(m => m.JoinOrCreateAsync(playerId, connectionId, null))
            .ReturnsAsync(gameSession);

        _mockHubContext.Setup(h => h.Groups).Returns(mockGroups.Object);
        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.Client(connectionId)).Returns(mockCallerClients.Object);

        // Act
        await _service.JoinGameAsync(connectionId, playerId, displayName, null);

        // Assert
        Assert.Equal(displayName, gameSession.WhitePlayerName);
    }

    [Fact]
    public async Task JoinGameAsync_GameFull_AIGoesFirst_TriggersAiTurn()
    {
        // Arrange
        var connectionId = "conn-123";
        var playerId = "player-123";
        var aiPlayerId = "ai-456";
        var gameSession = new GameSession("game-123");

        // Add AI as White (goes first)
        gameSession.AddPlayer(aiPlayerId, string.Empty);
        gameSession.AddPlayer(playerId, connectionId);

        var mockGroups = new Mock<IGroupManager>();
        _mockSessionManager
            .Setup(m => m.JoinOrCreateAsync(playerId, connectionId, null))
            .ReturnsAsync(gameSession);

        // Setup AI check to return true for AI player and false for human
        _mockAiMoveService.Setup(s => s.IsAiPlayer(aiPlayerId)).Returns(true);
        _mockAiMoveService.Setup(s => s.IsAiPlayer(playerId)).Returns(false);

        _mockHubContext.Setup(h => h.Groups).Returns(mockGroups.Object);

        // Act
        await _service.JoinGameAsync(connectionId, playerId, null, null);

        // Assert
        // Should check if the current player (White) is AI
        _mockAiMoveService.Verify(s => s.IsAiPlayer(It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task CreateAnalysisGameAsync_CreatesGameWithSamePlayerBothSides()
    {
        // Arrange
        var connectionId = "conn-123";
        var userId = "user-123";
        var gameSession = new GameSession("game-123");
        var mockGroups = new Mock<IGroupManager>();
        var mockClients = new Mock<IHubClients>();
        var mockCallerClients = new Mock<ISingleClientProxy>();

        _mockSessionManager
            .Setup(m => m.JoinOrCreateAsync(userId, connectionId, null))
            .ReturnsAsync(gameSession);

        _mockHubContext.Setup(h => h.Groups).Returns(mockGroups.Object);
        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.Client(connectionId)).Returns(mockCallerClients.Object);

        // Act
        await _service.CreateAnalysisGameAsync(connectionId, userId);

        // Assert
        Assert.Equal(userId, gameSession.WhitePlayerId);
        Assert.Equal(userId, gameSession.RedPlayerId);
        Assert.True(gameSession.IsAnalysisMode);
        mockCallerClients.Verify(
            c => c.SendCoreAsync(
                "GameStart",
                It.IsAny<object[]>(),
                default),
            Times.Once);
    }

    [Fact]
    public async Task CreateAiGameAsync_CreatesGameWithAiOpponent()
    {
        // Arrange
        var connectionId = "conn-123";
        var playerId = "player-123";
        var displayName = "TestPlayer";
        var aiPlayerId = "ai-456";
        var gameSession = new GameSession("game-123");
        var mockGroups = new Mock<IGroupManager>();
        var mockClients = new Mock<IHubClients>();
        var mockCallerClients = new Mock<ISingleClientProxy>();

        _mockSessionManager
            .Setup(m => m.CreateGame())
            .Returns(gameSession);
        _mockAiMoveService
            .Setup(s => s.GenerateAiPlayerId())
            .Returns(aiPlayerId);

        _mockHubContext.Setup(h => h.Groups).Returns(mockGroups.Object);
        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.Client(connectionId)).Returns(mockCallerClients.Object);

        // Act
        await _service.CreateAiGameAsync(connectionId, playerId, displayName);

        // Assert
        _mockSessionManager.Verify(m => m.CreateGame(), Times.Once);
        _mockSessionManager.Verify(m => m.RegisterPlayerConnection(connectionId, gameSession.Id), Times.Once);
        _mockAiMoveService.Verify(s => s.GenerateAiPlayerId(), Times.Once);
        Assert.Equal(playerId, gameSession.WhitePlayerId);
        Assert.Equal(aiPlayerId, gameSession.RedPlayerId);
        Assert.Equal(displayName, gameSession.WhitePlayerName);
        Assert.Equal("Computer", gameSession.RedPlayerName);
        mockCallerClients.Verify(
            c => c.SendCoreAsync(
                "GameStart",
                It.IsAny<object[]>(),
                default),
            Times.Once);
    }

    [Fact]
    public async Task CreateAiGameAsync_WithoutDisplayName_DoesNotSetName()
    {
        // Arrange
        var connectionId = "conn-123";
        var playerId = "player-123";
        var aiPlayerId = "ai-456";
        var gameSession = new GameSession("game-123");
        var mockGroups = new Mock<IGroupManager>();
        var mockClients = new Mock<IHubClients>();
        var mockCallerClients = new Mock<ISingleClientProxy>();

        _mockSessionManager.Setup(m => m.CreateGame()).Returns(gameSession);
        _mockAiMoveService.Setup(s => s.GenerateAiPlayerId()).Returns(aiPlayerId);

        _mockHubContext.Setup(h => h.Groups).Returns(mockGroups.Object);
        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.Client(connectionId)).Returns(mockCallerClients.Object);

        // Act
        await _service.CreateAiGameAsync(connectionId, playerId, null);

        // Assert
        Assert.Equal("Player -123", gameSession.WhitePlayerName); // Auto-generated from "player-123"
        Assert.Equal("Computer", gameSession.RedPlayerName);
    }
}
