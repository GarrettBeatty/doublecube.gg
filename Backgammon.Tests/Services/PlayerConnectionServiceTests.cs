using Backgammon.Server.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backgammon.Tests.Services;

public class PlayerConnectionServiceTests
{
    private readonly Mock<ILogger<PlayerConnectionService>> _mockLogger;
    private readonly PlayerConnectionService _service;

    public PlayerConnectionServiceTests()
    {
        _mockLogger = new Mock<ILogger<PlayerConnectionService>>();
        _service = new PlayerConnectionService(_mockLogger.Object);
    }

    [Fact]
    public void AddConnection_AddsPlayerConnection()
    {
        // Arrange
        var playerId = "player-123";
        var connectionId = "conn-456";

        // Act
        _service.AddConnection(playerId, connectionId);

        // Assert
        var retrieved = _service.GetConnectionId(playerId);
        Assert.Equal(connectionId, retrieved);
        Assert.Equal(1, _service.GetConnectionCount());
    }

    [Fact]
    public void AddConnection_UpdatesExistingConnection()
    {
        // Arrange
        var playerId = "player-123";
        var oldConnectionId = "conn-old";
        var newConnectionId = "conn-new";

        // Act
        _service.AddConnection(playerId, oldConnectionId);
        _service.AddConnection(playerId, newConnectionId);

        // Assert
        var retrieved = _service.GetConnectionId(playerId);
        Assert.Equal(newConnectionId, retrieved);
        Assert.Equal(1, _service.GetConnectionCount()); // Still just 1 connection
    }

    [Fact]
    public void AddConnection_MultiplePlayersTracked()
    {
        // Arrange
        var player1 = "player-1";
        var player2 = "player-2";
        var conn1 = "conn-1";
        var conn2 = "conn-2";

        // Act
        _service.AddConnection(player1, conn1);
        _service.AddConnection(player2, conn2);

        // Assert
        Assert.Equal(conn1, _service.GetConnectionId(player1));
        Assert.Equal(conn2, _service.GetConnectionId(player2));
        Assert.Equal(2, _service.GetConnectionCount());
    }

    [Fact]
    public void RemoveConnection_ExistingPlayer_ReturnsTrue()
    {
        // Arrange
        var playerId = "player-123";
        var connectionId = "conn-456";
        _service.AddConnection(playerId, connectionId);

        // Act
        var result = _service.RemoveConnection(playerId);

        // Assert
        Assert.True(result);
        Assert.Null(_service.GetConnectionId(playerId));
        Assert.Equal(0, _service.GetConnectionCount());
    }

    [Fact]
    public void RemoveConnection_NonExistentPlayer_ReturnsFalse()
    {
        // Arrange
        var playerId = "non-existent";

        // Act
        var result = _service.RemoveConnection(playerId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void RemoveConnection_DoesNotAffectOtherConnections()
    {
        // Arrange
        var player1 = "player-1";
        var player2 = "player-2";
        var conn1 = "conn-1";
        var conn2 = "conn-2";
        _service.AddConnection(player1, conn1);
        _service.AddConnection(player2, conn2);

        // Act
        _service.RemoveConnection(player1);

        // Assert
        Assert.Null(_service.GetConnectionId(player1));
        Assert.Equal(conn2, _service.GetConnectionId(player2));
        Assert.Equal(1, _service.GetConnectionCount());
    }

    [Fact]
    public void GetConnectionId_ExistingPlayer_ReturnsConnectionId()
    {
        // Arrange
        var playerId = "player-123";
        var connectionId = "conn-456";
        _service.AddConnection(playerId, connectionId);

        // Act
        var result = _service.GetConnectionId(playerId);

        // Assert
        Assert.Equal(connectionId, result);
    }

    [Fact]
    public void GetConnectionId_NonExistentPlayer_ReturnsNull()
    {
        // Arrange
        var playerId = "non-existent";

        // Act
        var result = _service.GetConnectionId(playerId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetConnectionCount_InitiallyZero()
    {
        // Act
        var count = _service.GetConnectionCount();

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public void GetConnectionCount_ReflectsAdditionsAndRemovals()
    {
        // Arrange & Act
        Assert.Equal(0, _service.GetConnectionCount());

        _service.AddConnection("player-1", "conn-1");
        Assert.Equal(1, _service.GetConnectionCount());

        _service.AddConnection("player-2", "conn-2");
        Assert.Equal(2, _service.GetConnectionCount());

        _service.RemoveConnection("player-1");
        Assert.Equal(1, _service.GetConnectionCount());

        _service.RemoveConnection("player-2");
        Assert.Equal(0, _service.GetConnectionCount());
    }
}
