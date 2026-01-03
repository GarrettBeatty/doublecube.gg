using Backgammon.Core;
using Backgammon.Server.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Backgammon.Tests;

public class DoubleOfferServiceTests
{
    private readonly Mock<ILogger<DoubleOfferService>> _loggerMock;
    private readonly DoubleOfferService _service;

    public DoubleOfferServiceTests()
    {
        _loggerMock = new Mock<ILogger<DoubleOfferService>>();
        _service = new DoubleOfferService(_loggerMock.Object);
    }

    [Fact]
    public async Task OfferDoubleAsync_NotPlayerTurn_ReturnsFalse()
    {
        // Arrange
        var session = CreateTestGameSession();
        var wrongConnectionId = "not-current-player";

        // Act
        var (success, _, _, error) = await _service.OfferDoubleAsync(session, wrongConnectionId);

        // Assert
        Assert.False(success);
        Assert.Equal("Not your turn", error);
    }

    [Fact]
    public async Task OfferDoubleAsync_AfterRollingDice_ReturnsFalse()
    {
        // Arrange
        var session = CreateTestGameSession();
        session.Engine.StartNewGame();
        session.Engine.RollDice();

        // Get the connection ID of the current player (who just rolled)
        var currentPlayerConnection = session.Engine.CurrentPlayer?.Color == CheckerColor.White
            ? session.WhiteConnectionId!
            : session.RedConnectionId!;

        // Act
        var (success, _, _, error) = await _service.OfferDoubleAsync(session, currentPlayerConnection);

        // Assert
        Assert.False(success);
        Assert.Equal("Can only double before rolling dice", error);
    }

    [Fact]
    public async Task AcceptDoubleAsync_UpdatesGameState()
    {
        // Arrange
        var session = CreateTestGameSession();
        session.Engine.StartNewGame();
        session.Engine.OfferDouble();

        // Act
        var result = await _service.AcceptDoubleAsync(session);

        // Assert
        Assert.True(result);
        Assert.Equal(2, session.Engine.DoublingCube.Value);
    }

    [Fact]
    public async Task DeclineDoubleAsync_GameNotStarted_ReturnsFalse()
    {
        // Arrange
        var session = new GameSession("test-game-id");
        session.AddPlayer("white-player", "white-connection");
        // Don't add second player - game won't auto-start
        var connectionId = session.WhiteConnectionId!;

        // Act
        var (success, _, _, error) = await _service.DeclineDoubleAsync(session, connectionId);

        // Assert
        Assert.False(success);
        Assert.Equal("Game hasn't started yet", error);
    }

    [Fact]
    public async Task DeclineDoubleAsync_GameStarted_OpponentWins()
    {
        // Arrange
        var session = CreateTestGameSession();
        session.Engine.StartNewGame();
        var whiteConnectionId = session.WhiteConnectionId!;

        // Act
        var (success, winner, stakes, error) = await _service.DeclineDoubleAsync(session, whiteConnectionId);

        // Assert
        Assert.True(success);
        Assert.NotNull(winner);
        Assert.Equal(CheckerColor.Red, winner.Color);
        // Starting position is a backgammon (loser has checkers in winner's home board)
        Assert.Equal(3, stakes);
    }

    [Fact]
    public async Task HandleAiDoubleResponseAsync_LowValue_AiAccepts()
    {
        // Arrange
        var session = CreateTestGameSession();
        session.Engine.StartNewGame();
        var aiPlayerId = "ai-player";

        // Act
        var (accepted, winner, stakes) = await _service.HandleAiDoubleResponseAsync(
            session, aiPlayerId, 1, 2);

        // Assert
        Assert.True(accepted);
        Assert.Null(winner);
        Assert.Equal(0, stakes);
    }

    [Fact]
    public async Task HandleAiDoubleResponseAsync_HighValue_AiDeclines()
    {
        // Arrange
        var session = CreateTestGameSession();
        session.Engine.StartNewGame();
        var aiPlayerId = session.RedPlayerId!;

        // Act
        var (accepted, winner, stakes) = await _service.HandleAiDoubleResponseAsync(
            session, aiPlayerId, 4, 8);

        // Assert
        Assert.False(accepted);
        Assert.NotNull(winner);
        Assert.Equal(CheckerColor.White, winner.Color);
    }

    private GameSession CreateTestGameSession()
    {
        var session = new GameSession("test-game-id");
        session.AddPlayer("white-player", "white-connection");
        session.AddPlayer("red-player", "red-connection");
        return session;
    }
}
