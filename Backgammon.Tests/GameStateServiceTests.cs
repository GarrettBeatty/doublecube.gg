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
    private readonly GameStateService _service;

    public GameStateServiceTests()
    {
        _loggerMock = new Mock<ILogger<GameStateService>>();
        _service = new GameStateService(_loggerMock.Object);
    }

    [Fact]
    public async Task BroadcastGameUpdateAsync_SendsToAllPlayers()
    {
        // Arrange
        var session = CreateTestGameSession();
        var clientsMock = new Mock<IHubCallerClients>();
        var clientProxyMock = new Mock<ISingleClientProxy>();

        clientsMock.Setup(c => c.Client(It.IsAny<string>())).Returns(clientProxyMock.Object);

        // Act
        await _service.BroadcastGameUpdateAsync(session, clientsMock.Object);

        // Assert
        clientProxyMock.Verify(
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
        var clientsMock = new Mock<IHubCallerClients>();
        var clientProxyMock = new Mock<ISingleClientProxy>();

        clientsMock.Setup(c => c.Client(It.IsAny<string>())).Returns(clientProxyMock.Object);

        // Act
        await _service.BroadcastGameStartAsync(session, clientsMock.Object);

        // Assert
        clientProxyMock.Verify(
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

        var clientsMock = new Mock<IHubCallerClients>();
        var groupProxyMock = new Mock<IClientProxy>();

        clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(groupProxyMock.Object);

        // Act
        await _service.BroadcastGameOverAsync(session, clientsMock.Object);

        // Assert
        groupProxyMock.Verify(
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
        var clientsMock = new Mock<IHubCallerClients>();
        var clientProxyMock = new Mock<ISingleClientProxy>();

        clientsMock.Setup(c => c.Client(connectionId)).Returns(clientProxyMock.Object);

        // Act
        await _service.SendGameStateToConnectionAsync(session, connectionId, clientsMock.Object);

        // Assert
        clientProxyMock.Verify(
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
