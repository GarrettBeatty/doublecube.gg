using Backgammon.Server.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Backgammon.Tests;

public class PlayerConnectionServiceTests
{
    private readonly Mock<ILogger<PlayerConnectionService>> _loggerMock;
    private readonly PlayerConnectionService _service;

    public PlayerConnectionServiceTests()
    {
        _loggerMock = new Mock<ILogger<PlayerConnectionService>>();
        _service = new PlayerConnectionService(_loggerMock.Object);
    }

    [Fact]
    public void AddConnection_StoresConnectionSuccessfully()
    {
        // Arrange
        var playerId = "player123";
        var connectionId = "connection456";

        // Act
        _service.AddConnection(playerId, connectionId);

        // Assert
        var retrievedConnectionId = _service.GetConnectionId(playerId);
        Assert.Equal(connectionId, retrievedConnectionId);
    }

    [Fact]
    public void AddConnection_OverwritesExistingConnection()
    {
        // Arrange
        var playerId = "player123";
        var firstConnectionId = "connection1";
        var secondConnectionId = "connection2";

        // Act
        _service.AddConnection(playerId, firstConnectionId);
        _service.AddConnection(playerId, secondConnectionId);

        // Assert
        var retrievedConnectionId = _service.GetConnectionId(playerId);
        Assert.Equal(secondConnectionId, retrievedConnectionId);
        Assert.Equal(1, _service.GetConnectionCount()); // Only one connection should exist
    }

    [Fact]
    public void GetConnectionId_ReturnsNullForNonExistentPlayer()
    {
        // Arrange
        var playerId = "nonexistent";

        // Act
        var connectionId = _service.GetConnectionId(playerId);

        // Assert
        Assert.Null(connectionId);
    }

    [Fact]
    public void RemoveConnection_RemovesExistingConnection()
    {
        // Arrange
        var playerId = "player123";
        var connectionId = "connection456";
        _service.AddConnection(playerId, connectionId);

        // Act
        var removed = _service.RemoveConnection(playerId);

        // Assert
        Assert.True(removed);
        Assert.Null(_service.GetConnectionId(playerId));
        Assert.Equal(0, _service.GetConnectionCount());
    }

    [Fact]
    public void RemoveConnection_ReturnsFalseForNonExistentConnection()
    {
        // Arrange
        var playerId = "nonexistent";

        // Act
        var removed = _service.RemoveConnection(playerId);

        // Assert
        Assert.False(removed);
    }

    [Fact]
    public void GetConnectionCount_ReturnsCorrectCount()
    {
        // Arrange & Act
        Assert.Equal(0, _service.GetConnectionCount());

        _service.AddConnection("player1", "conn1");
        Assert.Equal(1, _service.GetConnectionCount());

        _service.AddConnection("player2", "conn2");
        Assert.Equal(2, _service.GetConnectionCount());

        _service.RemoveConnection("player1");
        Assert.Equal(1, _service.GetConnectionCount());

        _service.RemoveConnection("player2");
        Assert.Equal(0, _service.GetConnectionCount());
    }

    [Fact]
    public void AddConnection_WithMultiplePlayers_MaintainsSeparateConnections()
    {
        // Arrange
        var player1Id = "player1";
        var player2Id = "player2";
        var connection1Id = "conn1";
        var connection2Id = "conn2";

        // Act
        _service.AddConnection(player1Id, connection1Id);
        _service.AddConnection(player2Id, connection2Id);

        // Assert
        Assert.Equal(connection1Id, _service.GetConnectionId(player1Id));
        Assert.Equal(connection2Id, _service.GetConnectionId(player2Id));
        Assert.Equal(2, _service.GetConnectionCount());
    }

    [Fact]
    public void PlayerConnectionService_IsThreadSafe_ConcurrentOperations()
    {
        // This test verifies thread-safety by performing concurrent operations
        // Arrange
        var tasks = new List<Task>();
        var playerCount = 100;

        // Act - Add connections concurrently
        for (int i = 0; i < playerCount; i++)
        {
            var playerId = $"player{i}";
            var connectionId = $"conn{i}";
            tasks.Add(Task.Run(() => _service.AddConnection(playerId, connectionId)));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        Assert.Equal(playerCount, _service.GetConnectionCount());

        // Verify all connections are retrievable
        for (int i = 0; i < playerCount; i++)
        {
            var playerId = $"player{i}";
            var expectedConnectionId = $"conn{i}";
            Assert.Equal(expectedConnectionId, _service.GetConnectionId(playerId));
        }

        // Act - Remove connections concurrently
        tasks.Clear();
        for (int i = 0; i < playerCount; i++)
        {
            var playerId = $"player{i}";
            tasks.Add(Task.Run(() => _service.RemoveConnection(playerId)));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        Assert.Equal(0, _service.GetConnectionCount());
    }

    [Fact]
    public void AddConnection_LogsInformation()
    {
        // Arrange
        var playerId = "player123";
        var connectionId = "connection456";

        // Act
        _service.AddConnection(playerId, connectionId);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Tracking player connection")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RemoveConnection_LogsInformationWhenSuccessful()
    {
        // Arrange
        var playerId = "player123";
        var connectionId = "connection456";
        _service.AddConnection(playerId, connectionId);
        _loggerMock.Reset(); // Reset to clear the Add log

        // Act
        _service.RemoveConnection(playerId);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Removed player connection")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void GetConnectionId_LogsDebugWhenFound()
    {
        // Arrange
        var playerId = "player123";
        var connectionId = "connection456";
        _service.AddConnection(playerId, connectionId);

        // Act
        _service.GetConnectionId(playerId);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Found connection")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
