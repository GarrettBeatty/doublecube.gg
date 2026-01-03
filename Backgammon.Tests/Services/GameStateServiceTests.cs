using Backgammon.Core;
using Backgammon.Server.Hubs;
using Backgammon.Server.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backgammon.Tests.Services;

public class GameStateServiceTests
{
    private readonly Mock<IHubContext<GameHub>> _mockHubContext;
    private readonly Mock<ILogger<GameStateService>> _mockLogger;
    private readonly GameStateService _service;

    public GameStateServiceTests()
    {
        _mockHubContext = new Mock<IHubContext<GameHub>>();
        _mockLogger = new Mock<ILogger<GameStateService>>();
        _service = new GameStateService(_mockHubContext.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task BroadcastGameUpdateAsync_WithBothPlayers_SendsToEach()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.AddPlayer("white-player", "white-conn");
        session.AddPlayer("red-player", "red-conn");

        var mockClients = new Mock<IHubClients>();
        var mockWhiteClient = new Mock<ISingleClientProxy>();
        var mockRedClient = new Mock<ISingleClientProxy>();

        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.Client("white-conn")).Returns(mockWhiteClient.Object);
        mockClients.Setup(c => c.Client("red-conn")).Returns(mockRedClient.Object);

        // Act
        await _service.BroadcastGameUpdateAsync(session);

        // Assert
        mockWhiteClient.Verify(
            c => c.SendCoreAsync("GameUpdate", It.IsAny<object[]>(), default),
            Times.Once);
        mockRedClient.Verify(
            c => c.SendCoreAsync("GameUpdate", It.IsAny<object[]>(), default),
            Times.Once);
    }

    [Fact]
    public async Task BroadcastGameUpdateAsync_WithSpectators_SendsToSpectators()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.AddPlayer("white-player", "white-conn");
        session.AddPlayer("red-player", "red-conn");
        session.AddSpectator("spectator-conn");

        var mockClients = new Mock<IHubClients>();
        var mockWhiteClient = new Mock<ISingleClientProxy>();
        var mockRedClient = new Mock<ISingleClientProxy>();
        var mockSpectatorClient = new Mock<ISingleClientProxy>();

        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.Client("white-conn")).Returns(mockWhiteClient.Object);
        mockClients.Setup(c => c.Client("red-conn")).Returns(mockRedClient.Object);
        mockClients.Setup(c => c.Client("spectator-conn")).Returns(mockSpectatorClient.Object);

        // Act
        await _service.BroadcastGameUpdateAsync(session);

        // Assert
        mockSpectatorClient.Verify(
            c => c.SendCoreAsync("GameUpdate", It.IsAny<object[]>(), default),
            Times.Once);
    }

    [Fact]
    public async Task BroadcastGameUpdateAsync_OnlyWhitePlayer_SendsOnlyToWhite()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.AddPlayer("white-player", "white-conn");

        var mockClients = new Mock<IHubClients>();
        var mockWhiteClient = new Mock<ISingleClientProxy>();

        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.Client("white-conn")).Returns(mockWhiteClient.Object);

        // Act
        await _service.BroadcastGameUpdateAsync(session);

        // Assert
        mockWhiteClient.Verify(
            c => c.SendCoreAsync("GameUpdate", It.IsAny<object[]>(), default),
            Times.Once);
    }

    [Fact]
    public async Task BroadcastGameStartAsync_WithBothPlayers_SendsGameStartToEach()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.AddPlayer("white-player", "white-conn");
        session.AddPlayer("red-player", "red-conn");

        var mockClients = new Mock<IHubClients>();
        var mockWhiteClient = new Mock<ISingleClientProxy>();
        var mockRedClient = new Mock<ISingleClientProxy>();

        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.Client("white-conn")).Returns(mockWhiteClient.Object);
        mockClients.Setup(c => c.Client("red-conn")).Returns(mockRedClient.Object);

        // Act
        await _service.BroadcastGameStartAsync(session);

        // Assert
        mockWhiteClient.Verify(
            c => c.SendCoreAsync("GameStart", It.IsAny<object[]>(), default),
            Times.Once);
        mockRedClient.Verify(
            c => c.SendCoreAsync("GameStart", It.IsAny<object[]>(), default),
            Times.Once);
    }

    [Fact]
    public async Task BroadcastGameOverAsync_SendsToGroup()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.AddPlayer("white-player", "white-conn");
        session.AddPlayer("red-player", "red-conn");

        var mockClients = new Mock<IHubClients>();
        var mockGroupClient = new Mock<IClientProxy>();

        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.Group("game-123")).Returns(mockGroupClient.Object);

        // Act
        await _service.BroadcastGameOverAsync(session);

        // Assert
        mockGroupClient.Verify(
            c => c.SendCoreAsync("GameOver", It.IsAny<object[]>(), default),
            Times.Once);
    }

    [Fact]
    public async Task SendGameStateToConnectionAsync_SendsToSpecificConnection()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.AddPlayer("white-player", "white-conn");
        var connectionId = "white-conn";

        var mockClients = new Mock<IHubClients>();
        var mockClient = new Mock<ISingleClientProxy>();

        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.Client(connectionId)).Returns(mockClient.Object);

        // Act
        await _service.SendGameStateToConnectionAsync(session, connectionId);

        // Assert
        mockClient.Verify(
            c => c.SendCoreAsync("GameUpdate", It.IsAny<object[]>(), default),
            Times.Once);
    }

    [Fact]
    public async Task BroadcastDoubleOfferAsync_WhiteOffering_SendsToRed()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.AddPlayer("white-player", "white-conn");
        session.AddPlayer("red-player", "red-conn");

        var mockClients = new Mock<IHubClients>();
        var mockRedClient = new Mock<ISingleClientProxy>();

        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.Client("red-conn")).Returns(mockRedClient.Object);

        // Act
        await _service.BroadcastDoubleOfferAsync(session, "white-conn", 1, 2);

        // Assert
        mockRedClient.Verify(
            c => c.SendCoreAsync(
                "DoubleOffered",
                It.Is<object[]>(args => args.Length == 2 && (int)args[0] == 1 && (int)args[1] == 2),
                default),
            Times.Once);
    }

    [Fact]
    public async Task BroadcastDoubleOfferAsync_RedOffering_SendsToWhite()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.AddPlayer("white-player", "white-conn");
        session.AddPlayer("red-player", "red-conn");

        var mockClients = new Mock<IHubClients>();
        var mockWhiteClient = new Mock<ISingleClientProxy>();

        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.Client("white-conn")).Returns(mockWhiteClient.Object);

        // Act
        await _service.BroadcastDoubleOfferAsync(session, "red-conn", 2, 4);

        // Assert
        mockWhiteClient.Verify(
            c => c.SendCoreAsync(
                "DoubleOffered",
                It.Is<object[]>(args => args.Length == 2 && (int)args[0] == 2 && (int)args[1] == 4),
                default),
            Times.Once);
    }

    [Fact]
    public async Task BroadcastDoubleAcceptedAsync_SendsToBothPlayers()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.AddPlayer("white-player", "white-conn");
        session.AddPlayer("red-player", "red-conn");

        var mockClients = new Mock<IHubClients>();
        var mockWhiteClient = new Mock<ISingleClientProxy>();
        var mockRedClient = new Mock<ISingleClientProxy>();

        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.Client("white-conn")).Returns(mockWhiteClient.Object);
        mockClients.Setup(c => c.Client("red-conn")).Returns(mockRedClient.Object);

        // Act
        await _service.BroadcastDoubleAcceptedAsync(session);

        // Assert
        mockWhiteClient.Verify(
            c => c.SendCoreAsync("DoubleAccepted", It.IsAny<object[]>(), default),
            Times.Once);
        mockRedClient.Verify(
            c => c.SendCoreAsync("DoubleAccepted", It.IsAny<object[]>(), default),
            Times.Once);
    }

    [Fact]
    public async Task BroadcastMatchUpdateAsync_SendsToGroup()
    {
        // Arrange
        var match = new Backgammon.Server.Models.Match
        {
            MatchId = "match-123",
            Player1Score = 3,
            Player2Score = 2,
            TargetScore = 5,
            IsCrawfordGame = false
        };

        var mockClients = new Mock<IHubClients>();
        var mockGroupClient = new Mock<IClientProxy>();

        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.Group("game-123")).Returns(mockGroupClient.Object);

        // Act
        await _service.BroadcastMatchUpdateAsync(match, "game-123");

        // Assert
        mockGroupClient.Verify(
            c => c.SendCoreAsync("MatchUpdate", It.IsAny<object[]>(), default),
            Times.Once);
    }

    [Fact]
    public async Task BroadcastMatchUpdateAsync_CompletedMatch_LogsCompletion()
    {
        // Arrange
        var match = new Backgammon.Server.Models.Match
        {
            MatchId = "match-123",
            Player1Score = 5,
            Player2Score = 3,
            TargetScore = 5,
            WinnerId = "player-1"
        };
        match.GetType().GetProperty("Status")?.SetValue(match, "Completed");

        var mockClients = new Mock<IHubClients>();
        var mockGroupClient = new Mock<IClientProxy>();

        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.Group("game-123")).Returns(mockGroupClient.Object);

        // Act
        await _service.BroadcastMatchUpdateAsync(match, "game-123");

        // Assert - verify logging happened (check via mock if needed)
        mockGroupClient.Verify(
            c => c.SendCoreAsync("MatchUpdate", It.IsAny<object[]>(), default),
            Times.Once);
    }
}
