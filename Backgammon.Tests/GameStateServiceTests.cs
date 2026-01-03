using Backgammon.Server.Hubs;
using Backgammon.Server.Models;
using Backgammon.Server.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Backgammon.Tests;

public class GameStateServiceTests
{
    private readonly Mock<ILogger<GameStateService>> _loggerMock;
    private readonly Mock<IHubContext<GameHub>> _hubContextMock;
    private readonly Mock<IHubClients> _clientsMock;
    private readonly Mock<ISingleClientProxy> _clientProxyMock;
    private readonly Mock<IClientProxy> _groupProxyMock;
    private readonly GameStateService _service;

    public GameStateServiceTests()
    {
        _loggerMock = new Mock<ILogger<GameStateService>>();
        _hubContextMock = new Mock<IHubContext<GameHub>>();
        _clientsMock = new Mock<IHubClients>();
        _clientProxyMock = new Mock<ISingleClientProxy>();
        _groupProxyMock = new Mock<IClientProxy>();

        // Setup hub context to return clients mock
        _hubContextMock.Setup(h => h.Clients).Returns(_clientsMock.Object);
        _clientsMock.Setup(c => c.Client(It.IsAny<string>())).Returns(_clientProxyMock.Object);
        _clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_groupProxyMock.Object);

        _service = new GameStateService(_hubContextMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task BroadcastGameUpdateAsync_SendsToAllPlayers()
    {
        // Arrange
        var session = CreateTestGameSession();

        // Act
        await _service.BroadcastGameUpdateAsync(session);

        // Assert
        _clientProxyMock.Verify(
            c => c.SendCoreAsync(
                "GameUpdate",
                It.IsAny<object[]>(),
                default),
            Times.AtLeast(2)); // At least 2 calls (white + red players)
    }

    [Fact]
    public async Task BroadcastGameStartAsync_SendsToAllPlayers()
    {
        // Arrange
        var session = CreateTestGameSession();

        // Act
        await _service.BroadcastGameStartAsync(session);

        // Assert
        _clientProxyMock.Verify(
            c => c.SendCoreAsync(
                "GameStart",
                It.IsAny<object[]>(),
                default),
            Times.Exactly(2)); // White + red players
    }

    [Fact]
    public async Task BroadcastGameOverAsync_SendsToGroup()
    {
        // Arrange
        var session = CreateTestGameSession();
        session.Engine.StartNewGame();
        // Force a game over by forfeiting
        session.Engine.ForfeitGame(session.Engine.WhitePlayer);

        // Act
        await _service.BroadcastGameOverAsync(session);

        // Assert
        _groupProxyMock.Verify(
            c => c.SendCoreAsync(
                "GameOver",
                It.IsAny<object[]>(),
                default),
            Times.Once);
    }

    [Fact]
    public async Task SendGameStateToConnectionAsync_SendsToSpecificConnection()
    {
        // Arrange
        var session = CreateTestGameSession();
        var connectionId = "test-connection";

        // Act
        await _service.SendGameStateToConnectionAsync(session, connectionId);

        // Assert
        _clientProxyMock.Verify(
            c => c.SendCoreAsync(
                "GameUpdate",
                It.IsAny<object[]>(),
                default),
            Times.Once);
    }

    private GameSession CreateTestGameSession()
    {
        var session = new GameSession("test-game-id");
        session.AddPlayer("white-player", "white-connection");
        session.AddPlayer("red-player", "red-connection");
        session.Engine.StartNewGame();
        return session;
    }
}
