using Backgammon.Core;
using Backgammon.Server.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backgammon.Tests.Services;

public class AiMoveServiceTests
{
    private readonly Mock<ILogger<AiMoveService>> _mockLogger;
    private readonly AiMoveService _service;

    public AiMoveServiceTests()
    {
        _mockLogger = new Mock<ILogger<AiMoveService>>();
        _service = new AiMoveService(_mockLogger.Object);
    }

    [Fact]
    public void IsAiPlayer_WithNullPlayerId_ReturnsFalse()
    {
        // Act
        var result = _service.IsAiPlayer(null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsAiPlayer_WithGreedyAiPrefix_ReturnsTrue()
    {
        // Arrange
        var playerId = "ai_greedy_12345";

        // Act
        var result = _service.IsAiPlayer(playerId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsAiPlayer_WithRandomAiPrefix_ReturnsTrue()
    {
        // Arrange
        var playerId = "ai_random_67890";

        // Act
        var result = _service.IsAiPlayer(playerId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsAiPlayer_WithHumanPlayerId_ReturnsFalse()
    {
        // Arrange
        var playerId = "player-123";

        // Act
        var result = _service.IsAiPlayer(playerId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsAiPlayer_WithEmptyString_ReturnsFalse()
    {
        // Arrange
        var playerId = string.Empty;

        // Act
        var result = _service.IsAiPlayer(playerId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GenerateAiPlayerId_WithGreedyType_GeneratesGreedyId()
    {
        // Act
        var playerId = _service.GenerateAiPlayerId("greedy");

        // Assert
        Assert.StartsWith("ai_greedy_", playerId);
        Assert.True(_service.IsAiPlayer(playerId));
    }

    [Fact]
    public void GenerateAiPlayerId_WithRandomType_GeneratesRandomId()
    {
        // Act
        var playerId = _service.GenerateAiPlayerId("random");

        // Assert
        Assert.StartsWith("ai_random_", playerId);
        Assert.True(_service.IsAiPlayer(playerId));
    }

    [Fact]
    public void GenerateAiPlayerId_WithDefaultType_GeneratesGreedyId()
    {
        // Act
        var playerId = _service.GenerateAiPlayerId();

        // Assert
        Assert.StartsWith("ai_greedy_", playerId);
        Assert.True(_service.IsAiPlayer(playerId));
    }

    [Fact]
    public void GenerateAiPlayerId_WithInvalidType_GeneratesGreedyId()
    {
        // Act
        var playerId = _service.GenerateAiPlayerId("invalid");

        // Assert
        Assert.StartsWith("ai_greedy_", playerId);
        Assert.True(_service.IsAiPlayer(playerId));
    }

    [Fact]
    public void GenerateAiPlayerId_WithMixedCaseType_GeneratesCorrectId()
    {
        // Act
        var playerId = _service.GenerateAiPlayerId("RANDOM");

        // Assert
        Assert.StartsWith("ai_random_", playerId);
        Assert.True(_service.IsAiPlayer(playerId));
    }

    [Fact]
    public void GenerateAiPlayerId_GeneratesUniqueIds()
    {
        // Act
        var id1 = _service.GenerateAiPlayerId("greedy");
        var id2 = _service.GenerateAiPlayerId("greedy");

        // Assert
        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public async Task ExecuteAiTurnAsync_CompletesWithoutError()
    {
        // Arrange
        var session = new GameSession("game-123");
        // Add players - White first, then Red
        session.AddPlayer("ai_greedy_123", string.Empty);
        session.AddPlayer("human-player", "conn-123");
        session.Engine.StartNewGame();

        // Complete opening roll so AI can take a normal turn
        session.Engine.RollOpening(CheckerColor.White);
        session.Engine.RollOpening(CheckerColor.Red);

        // Clear remaining moves first
        session.Engine.RemainingMoves.Clear();

        // Ensure it's the AI's turn - if not, swap current player
        var aiColor = session.WhitePlayerId == "ai_greedy_123" ? CheckerColor.White : CheckerColor.Red;
        if (session.Engine.CurrentPlayer?.Color != aiColor)
        {
            // Swap to make it AI's turn
            session.Engine.EndTurn();
        }

        var broadcastCallCount = 0;
        Func<Task> broadcastUpdate = () =>
        {
            broadcastCallCount++;
            return Task.CompletedTask;
        };

        // Act - use the AI player ID we set up
        await _service.ExecuteAiTurnAsync(session, "ai_greedy_123", broadcastUpdate);

        // Assert - verify broadcast was called at least once (for dice roll)
        Assert.True(broadcastCallCount > 0);
    }

    [Fact]
    public async Task ExecuteAiTurnAsync_WithRandomAi_CompletesWithoutError()
    {
        // Arrange
        var session = new GameSession("game-123");
        // Add players - White first, then Red
        session.AddPlayer("ai_random_456", string.Empty);
        session.AddPlayer("human-player", "conn-123");
        session.Engine.StartNewGame();

        // Complete opening roll so AI can take a normal turn
        session.Engine.RollOpening(CheckerColor.White);
        session.Engine.RollOpening(CheckerColor.Red);

        // Clear remaining moves first
        session.Engine.RemainingMoves.Clear();

        // Ensure it's the AI's turn - if not, swap current player
        var aiColor = session.WhitePlayerId == "ai_random_456" ? CheckerColor.White : CheckerColor.Red;
        if (session.Engine.CurrentPlayer?.Color != aiColor)
        {
            // Swap to make it AI's turn
            session.Engine.EndTurn();
        }

        var broadcastCallCount = 0;
        Func<Task> broadcastUpdate = () =>
        {
            broadcastCallCount++;
            return Task.CompletedTask;
        };

        // Act - use the AI player ID we set up
        await _service.ExecuteAiTurnAsync(session, "ai_random_456", broadcastUpdate);

        // Assert
        Assert.True(broadcastCallCount > 0);
    }
}
