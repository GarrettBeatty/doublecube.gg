using Backgammon.Core;
using Backgammon.Server.Hubs;
using Backgammon.Server.Hubs.Interfaces;
using Backgammon.Server.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backgammon.Tests.Services;

public class ChatServiceTests
{
    private readonly Mock<IGameSessionManager> _mockSessionManager;
    private readonly Mock<IHubContext<GameHub, IGameHubClient>> _mockHubContext;
    private readonly Mock<ILogger<ChatService>> _mockLogger;
    private readonly ChatService _service;

    public ChatServiceTests()
    {
        _mockSessionManager = new Mock<IGameSessionManager>();
        _mockHubContext = new Mock<IHubContext<GameHub, IGameHubClient>>();
        _mockLogger = new Mock<ILogger<ChatService>>();

        _service = new ChatService(
            _mockSessionManager.Object,
            _mockHubContext.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task SendChatMessageAsync_EmptyMessage_DoesNothing()
    {
        // Arrange
        var connectionId = "conn-123";
        var message = string.Empty;

        // Act
        await _service.SendChatMessageAsync(connectionId, message);

        // Assert
        _mockSessionManager.Verify(m => m.GetGameByPlayer(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SendChatMessageAsync_WhitespaceMessage_DoesNothing()
    {
        // Arrange
        var connectionId = "conn-123";
        var message = "   ";

        // Act
        await _service.SendChatMessageAsync(connectionId, message);

        // Assert
        _mockSessionManager.Verify(m => m.GetGameByPlayer(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SendChatMessageAsync_NoSession_SendsError()
    {
        // Arrange
        var connectionId = "conn-123";
        var message = "Hello!";
        var mockClients = new Mock<IHubClients<IGameHubClient>>();
        var mockCallerClient = new Mock<IGameHubClient>();

        _mockSessionManager
            .Setup(m => m.GetGameByPlayer(connectionId))
            .Returns((GameSession?)null);

        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.Client(connectionId)).Returns(mockCallerClient.Object);

        // Act
        await _service.SendChatMessageAsync(connectionId, message);

        // Assert
        mockCallerClient.Verify(
            c => c.Error("Not in a game"),
            Times.Once);
    }

    [Fact]
    public async Task SendChatMessageAsync_PlayerNotInGame_DoesNotBroadcast()
    {
        // Arrange
        var connectionId = "conn-123";
        var message = "Hello!";
        var session = new GameSession("game-123");
        // No players added, so GetPlayerColor will return null

        var mockClients = new Mock<IHubClients<IGameHubClient>>();
        var mockGroupClient = new Mock<IGameHubClient>();

        _mockSessionManager
            .Setup(m => m.GetGameByPlayer(connectionId))
            .Returns(session);

        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.Group(session.Id)).Returns(mockGroupClient.Object);

        // Act
        await _service.SendChatMessageAsync(connectionId, message);

        // Assert
        mockGroupClient.Verify(
            c => c.ReceiveChatMessage(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task SendChatMessageAsync_WhitePlayer_BroadcastsWithWhiteName()
    {
        // Arrange
        var connectionId = "conn-123";
        var message = "Hello from White!";
        var session = new GameSession("game-123");
        session.AddPlayer("white-player", connectionId);
        session.SetPlayerName("white-player", "TestWhite");

        var mockClients = new Mock<IHubClients<IGameHubClient>>();
        var mockGroupClient = new Mock<IGameHubClient>();

        _mockSessionManager
            .Setup(m => m.GetGameByPlayer(connectionId))
            .Returns(session);

        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.Group(session.Id)).Returns(mockGroupClient.Object);

        // Act
        await _service.SendChatMessageAsync(connectionId, message);

        // Assert
        mockGroupClient.Verify(
            c => c.ReceiveChatMessage("TestWhite", message, connectionId),
            Times.Once);
    }

    [Fact]
    public async Task SendChatMessageAsync_RedPlayer_BroadcastsWithRedName()
    {
        // Arrange
        var connectionId = "conn-123";
        var message = "Hello from Red!";
        var session = new GameSession("game-123");
        session.AddPlayer("white-player", "white-conn");
        session.AddPlayer("red-player", connectionId);
        session.SetPlayerName("red-player", "TestRed");

        var mockClients = new Mock<IHubClients<IGameHubClient>>();
        var mockGroupClient = new Mock<IGameHubClient>();

        _mockSessionManager
            .Setup(m => m.GetGameByPlayer(connectionId))
            .Returns(session);

        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.Group(session.Id)).Returns(mockGroupClient.Object);

        // Act
        await _service.SendChatMessageAsync(connectionId, message);

        // Assert
        mockGroupClient.Verify(
            c => c.ReceiveChatMessage("TestRed", message, connectionId),
            Times.Once);
    }

    [Fact]
    public async Task SendChatMessageAsync_NoPlayerName_UsesDefaultName()
    {
        // Arrange
        var connectionId = "conn-123";
        var message = "Hello!";
        var session = new GameSession("game-123");
        session.AddPlayer("white-player", connectionId);
        // No name set explicitly, should auto-generate from playerId: "Player ayer"

        var mockClients = new Mock<IHubClients<IGameHubClient>>();
        var mockGroupClient = new Mock<IGameHubClient>();

        _mockSessionManager
            .Setup(m => m.GetGameByPlayer(connectionId))
            .Returns(session);

        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.Group(session.Id)).Returns(mockGroupClient.Object);

        // Act
        await _service.SendChatMessageAsync(connectionId, message);

        // Assert
        mockGroupClient.Verify(
            c => c.ReceiveChatMessage("Player ayer", It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
    }
}
