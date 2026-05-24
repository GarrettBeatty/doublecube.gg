using Backgammon.AI.Bots;
using Backgammon.Core;
using Backgammon.Plugins.Abstractions;
using Backgammon.Server.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backgammon.Tests.Services;

public class AiMoveServiceTests
{
    private readonly Mock<ILogger<AiMoveService>> _mockLogger;
    private readonly Mock<IBotResolver> _mockBotResolver;
    private readonly AiMoveService _service;

    public AiMoveServiceTests()
    {
        _mockLogger = new Mock<ILogger<AiMoveService>>();
        _mockBotResolver = new Mock<IBotResolver>();

        // Setup default IsBot behavior
        _mockBotResolver.Setup(x => x.IsBot(It.Is<string>(s => s != null && s.StartsWith("ai_")))).Returns(true);
        _mockBotResolver.Setup(x => x.IsBot(It.Is<string?>(s => s == null || !s.StartsWith("ai_")))).Returns(false);

        _service = new AiMoveService(_mockBotResolver.Object, _mockLogger.Object);
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
    public void IsAiPlayer_WithGnubgAiPrefix_ReturnsTrue()
    {
        // Arrange
        var playerId = "ai_gnubg_12345";

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
    }

    [Fact]
    public void GenerateAiPlayerId_WithRandomType_GeneratesRandomId()
    {
        // Act
        var playerId = _service.GenerateAiPlayerId("random");

        // Assert
        Assert.StartsWith("ai_random_", playerId);
    }

    [Fact]
    public void GenerateAiPlayerId_WithGnubgType_GeneratesGnubgId()
    {
        // Act
        var playerId = _service.GenerateAiPlayerId("gnubg");

        // Assert
        Assert.StartsWith("ai_gnubg_", playerId);
    }

    [Fact]
    public void GenerateAiPlayerId_WithDefaultType_GeneratesGreedyId()
    {
        // Act
        var playerId = _service.GenerateAiPlayerId();

        // Assert
        Assert.StartsWith("ai_greedy_", playerId);
    }

    [Fact]
    public void GenerateAiPlayerId_WithInvalidType_GeneratesGreedyId()
    {
        // Act
        var playerId = _service.GenerateAiPlayerId("invalid");

        // Assert
        Assert.StartsWith("ai_greedy_", playerId);
    }

    [Fact]
    public void GenerateAiPlayerId_WithMixedCaseType_GeneratesCorrectId()
    {
        // Act
        var playerId = _service.GenerateAiPlayerId("RANDOM");

        // Assert
        Assert.StartsWith("ai_random_", playerId);
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
        var services = new ServiceCollection();
        services.AddSingleton<GreedyBot>();
        services.AddSingleton<RandomBot>();
        var serviceProvider = services.BuildServiceProvider();
        var botResolver = new BotResolver(serviceProvider);
        var service = new AiMoveService(botResolver, _mockLogger.Object);

        var engine = new GameEngine();
        engine.StartNewGame();

        while (engine.IsOpeningRoll)
        {
            engine.RollOpening(CheckerColor.White);
            engine.RollOpening(CheckerColor.Red);
        }

        engine.RemainingMoves.Clear();
        engine.SetCurrentPlayer(CheckerColor.White);

        var broadcastCallCount = 0;
        Func<Task> broadcastUpdate = () =>
        {
            broadcastCallCount++;
            return Task.CompletedTask;
        };

        await service.ExecuteAiTurnAsync(engine, "game-123", "ai_greedy_123", null, broadcastUpdate);

        Assert.True(broadcastCallCount > 0);
    }

    [Fact]
    public async Task ExecuteAiTurnAsync_WithRandomAi_CompletesWithoutError()
    {
        var services = new ServiceCollection();
        services.AddSingleton<GreedyBot>();
        services.AddSingleton<RandomBot>();
        var serviceProvider = services.BuildServiceProvider();
        var botResolver = new BotResolver(serviceProvider);
        var service = new AiMoveService(botResolver, _mockLogger.Object);

        var engine = new GameEngine();
        engine.StartNewGame();

        while (engine.IsOpeningRoll)
        {
            engine.RollOpening(CheckerColor.White);
            engine.RollOpening(CheckerColor.Red);
        }

        engine.RemainingMoves.Clear();
        engine.SetCurrentPlayer(CheckerColor.White);

        var broadcastCallCount = 0;
        Func<Task> broadcastUpdate = () =>
        {
            broadcastCallCount++;
            return Task.CompletedTask;
        };

        await service.ExecuteAiTurnAsync(engine, "game-123", "ai_random_456", null, broadcastUpdate);

        Assert.True(broadcastCallCount > 0);
    }
}
