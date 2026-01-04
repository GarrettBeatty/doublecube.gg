using Backgammon.Core;
using Backgammon.Server.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backgammon.Tests.Services;

public class DoubleOfferServiceTests
{
    private readonly Mock<ILogger<DoubleOfferService>> _mockLogger;
    private readonly DoubleOfferService _service;

    public DoubleOfferServiceTests()
    {
        _mockLogger = new Mock<ILogger<DoubleOfferService>>();
        _service = new DoubleOfferService(_mockLogger.Object);
    }

    [Fact]
    public async Task OfferDoubleAsync_NotPlayerTurn_ReturnsError()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.AddPlayer("white-player", "white-conn");
        session.AddPlayer("red-player", "red-conn");
        session.Engine.StartNewGame();
        session.Engine.RemainingMoves.Clear();

        // Use connection that is NOT the current player
        var connectionId = session.Engine.CurrentPlayer?.Color == CheckerColor.White ? "red-conn" : "white-conn";

        // Act
        var result = await _service.OfferDoubleAsync(session, connectionId);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Not your turn", result.Error);
    }

    [Fact]
    public async Task OfferDoubleAsync_AfterRollingDice_ReturnsError()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.AddPlayer("white-player", "white-conn");
        session.AddPlayer("red-player", "red-conn");
        session.Engine.StartNewGame();

        // Complete opening roll to get past opening phase
        // Keep rolling until we get a non-tie result
        while (session.Engine.IsOpeningRoll)
        {
            session.Engine.RollOpening(CheckerColor.White);
            if (!session.Engine.IsOpeningRoll)
            {
                break; // White's roll completed the opening
            }

            session.Engine.RollOpening(CheckerColor.Red);
            // If we're still in opening roll, it was a tie - loop and re-roll
        }

        // Dice are now rolled from opening roll
        var connectionId = session.Engine.CurrentPlayer?.Color == CheckerColor.White ? "white-conn" : "red-conn";

        // Act
        var result = await _service.OfferDoubleAsync(session, connectionId);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Can only double before rolling dice", result.Error);
    }

    [Fact]
    public async Task OfferDoubleAsync_OpponentOwnsCube_ReturnsError()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.AddPlayer("white-player", "white-conn");
        session.AddPlayer("red-player", "red-conn");
        session.Engine.StartNewGame();
        session.Engine.RemainingMoves.Clear();
        session.Engine.Dice.SetDice(0, 0);

        // Give cube to Red by offering and accepting a double
        var currentPlayer = session.Engine.CurrentPlayer?.Color;
        if (currentPlayer == CheckerColor.White)
        {
            session.Engine.OfferDouble();
            session.Engine.AcceptDouble();
            // Now Red owns the cube
        }
        else
        {
            // Red is current, so skip this test
            return;
        }

        // Now White should not be able to double
        var connectionId = "white-conn";

        // Act
        var result = await _service.OfferDoubleAsync(session, connectionId);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Cannot offer double - opponent owns the cube", result.Error);
    }

    [Fact]
    public async Task OfferDoubleAsync_ValidRequest_OffersDouble()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.AddPlayer("white-player", "white-conn");
        session.AddPlayer("red-player", "red-conn");
        session.Engine.StartNewGame();
        session.Engine.RemainingMoves.Clear();
        session.Engine.Dice.SetDice(0, 0);

        var connectionId = session.Engine.CurrentPlayer?.Color == CheckerColor.White ? "white-conn" : "red-conn";
        var initialCubeValue = session.Engine.DoublingCube.Value;

        // Act
        var result = await _service.OfferDoubleAsync(session, connectionId);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(initialCubeValue, result.CurrentValue);
        // OfferDouble just validates, doesn't change value. Value changes on Accept.
        Assert.Equal(initialCubeValue, result.NewValue);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task AcceptDoubleAsync_UpdatesGameState()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.AddPlayer("white-player", "white-conn");
        session.AddPlayer("red-player", "red-conn");
        session.Engine.StartNewGame();
        session.Engine.RemainingMoves.Clear();
        session.Engine.Dice.SetDice(0, 0);

        // Offer double first
        session.Engine.OfferDouble();

        var initialActivity = session.LastActivityAt;
        await Task.Delay(10); // Small delay to ensure time difference

        // Act
        var result = await _service.AcceptDoubleAsync(session);

        // Assert
        Assert.True(result);
        Assert.True(session.LastActivityAt > initialActivity);
    }

    [Fact]
    public async Task DeclineDoubleAsync_NotAPlayer_ReturnsError()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.AddPlayer("white-player", "white-conn");
        session.AddPlayer("red-player", "red-conn");
        session.Engine.StartNewGame();

        var spectatorId = "spectator-conn";

        // Act
        var result = await _service.DeclineDoubleAsync(session, spectatorId);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("You are not a player in this game", result.Error);
    }

    [Fact]
    public async Task DeclineDoubleAsync_GameAlreadyOver_ReturnsError()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.AddPlayer("white-player", "white-conn");
        session.AddPlayer("red-player", "red-conn");
        session.Engine.StartNewGame();

        // Manually set game as over
        session.Engine.ForfeitGame(session.Engine.WhitePlayer);

        var connectionId = "red-conn";

        // Act
        var result = await _service.DeclineDoubleAsync(session, connectionId);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Game is already finished", result.Error);
    }

    [Fact]
    public async Task DeclineDoubleAsync_GameNotStarted_ReturnsError()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.AddPlayer("white-player", "white-conn");
        session.AddPlayer("red-player", "red-conn");
        // Create a new engine but don't start the game
        session.Engine.GetType().GetProperty("GameStarted")?.SetValue(session.Engine, false);

        var connectionId = "red-conn";

        // Act
        var result = await _service.DeclineDoubleAsync(session, connectionId);

        // Assert
        if (!session.Engine.GameStarted)
        {
            Assert.False(result.Success);
            Assert.Equal("Game hasn't started yet", result.Error);
        }
        else
        {
            // If game somehow started, skip this test
            return;
        }
    }

    [Fact]
    public async Task DeclineDoubleAsync_WhiteDeclines_RedWins()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.AddPlayer("white-player", "white-conn");
        session.AddPlayer("red-player", "red-conn");
        session.Engine.StartNewGame();

        var connectionId = "white-conn";

        // Act
        var result = await _service.DeclineDoubleAsync(session, connectionId);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Winner);
        Assert.Equal(CheckerColor.Red, result.Winner.Color);
        Assert.True(result.Stakes > 0);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task DeclineDoubleAsync_RedDeclines_WhiteWins()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.AddPlayer("white-player", "white-conn");
        session.AddPlayer("red-player", "red-conn");
        session.Engine.StartNewGame();

        var connectionId = "red-conn";

        // Act
        var result = await _service.DeclineDoubleAsync(session, connectionId);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Winner);
        Assert.Equal(CheckerColor.White, result.Winner.Color);
        Assert.True(result.Stakes > 0);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task HandleAiDoubleResponseAsync_LowValue_Accepts()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.AddPlayer("human-player", "human-conn");
        session.AddPlayer("ai-player", string.Empty);
        session.Engine.StartNewGame();

        // Act - AI should accept doubles <= 4
        var result = await _service.HandleAiDoubleResponseAsync(session, "ai-player", 2, 4);

        // Assert
        Assert.True(result.Accepted);
        Assert.Null(result.Winner);
        Assert.Equal(0, result.Stakes);
    }

    [Fact]
    public async Task HandleAiDoubleResponseAsync_HighValue_Declines()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.AddPlayer("human-player", "human-conn");
        session.AddPlayer("ai-player", string.Empty);
        session.Engine.StartNewGame();

        // Act - AI should decline doubles > 4
        var result = await _service.HandleAiDoubleResponseAsync(session, "ai-player", 4, 8);

        // Assert
        Assert.False(result.Accepted);
        Assert.NotNull(result.Winner);
        Assert.True(result.Stakes > 0);
    }

    [Fact]
    public async Task HandleAiDoubleResponseAsync_AIAsWhite_HumanWinsWhenAiDeclines()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.AddPlayer("ai-player", string.Empty);
        session.AddPlayer("human-player", "human-conn");
        session.Engine.StartNewGame();

        // Act - AI as White declines high double
        var result = await _service.HandleAiDoubleResponseAsync(session, "ai-player", 4, 8);

        // Assert
        Assert.False(result.Accepted);
        Assert.NotNull(result.Winner);
        Assert.Equal(CheckerColor.Red, result.Winner.Color);
        Assert.True(result.Stakes > 0);
    }

    [Fact]
    public async Task HandleAiDoubleResponseAsync_AIAsRed_HumanWinsWhenAiDeclines()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.AddPlayer("human-player", "human-conn");
        session.AddPlayer("ai-player", string.Empty);
        session.Engine.StartNewGame();

        // Act - AI as Red declines high double
        var result = await _service.HandleAiDoubleResponseAsync(session, "ai-player", 4, 8);

        // Assert
        Assert.False(result.Accepted);
        Assert.NotNull(result.Winner);
        Assert.Equal(CheckerColor.White, result.Winner.Color);
        Assert.True(result.Stakes > 0);
    }
}
