using Backgammon.Core;
using Backgammon.Server.Hubs;
using Backgammon.Server.Hubs.Handlers;
using Backgammon.Server.Hubs.Interfaces;
using Backgammon.Server.Services;
using Backgammon.Server.Services.Results;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Backgammon.Tests.Hubs.Handlers;

public class DoublingHandlerTests
{
    private readonly Mock<IGameSessionManager> _mockSessionManager;
    private readonly Mock<IDoubleOfferService> _mockDoubleOfferService;
    private readonly Mock<IGameService> _mockGameService;
    private readonly Mock<IGameRepository> _mockGameRepository;
    private readonly Mock<IPlayerStatsService> _mockPlayerStatsService;
    private readonly Mock<IAiMoveService> _mockAiMoveService;
    private readonly Mock<IHubContext<GameHub, IGameHubClient>> _mockHubContext;
    private readonly Mock<ILogger<DoublingHandler>> _mockLogger;
    private readonly DoublingHandler _handler;

    public DoublingHandlerTests()
    {
        _mockSessionManager = new Mock<IGameSessionManager>();
        _mockDoubleOfferService = new Mock<IDoubleOfferService>();
        _mockGameService = new Mock<IGameService>();
        _mockGameRepository = new Mock<IGameRepository>();
        _mockPlayerStatsService = new Mock<IPlayerStatsService>();
        _mockAiMoveService = new Mock<IAiMoveService>();
        _mockHubContext = new Mock<IHubContext<GameHub, IGameHubClient>>();
        _mockLogger = new Mock<ILogger<DoublingHandler>>();

        _handler = new DoublingHandler(
            _mockSessionManager.Object,
            _mockDoubleOfferService.Object,
            _mockGameService.Object,
            _mockGameRepository.Object,
            _mockPlayerStatsService.Object,
            _mockAiMoveService.Object,
            _mockHubContext.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task OfferDoubleAsync_NoSession_ReturnsFailure()
    {
        // Arrange
        _mockSessionManager.Setup(x => x.GetGameByPlayer("conn-123"))
            .Returns((GameSession?)null);

        // Act
        var result = await _handler.OfferDoubleAsync("conn-123");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ErrorCodes.SessionNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task OfferDoubleAsync_GameCompleted_ReturnsFailure()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.Engine.StartNewGame();
        session.Engine.SetCurrentPlayer(CheckerColor.White);

        // Force a winner by using Forfeit
        session.Engine.ForfeitGame(session.Engine.WhitePlayer);

        _mockSessionManager.Setup(x => x.GetGameByPlayer("conn-123"))
            .Returns(session);

        // Act
        var result = await _handler.OfferDoubleAsync("conn-123");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ErrorCodes.CannotDouble, result.ErrorCode);
        Assert.Contains("already completed", result.Message);
    }

    [Fact]
    public async Task OfferDoubleAsync_DoubleOfferFails_ReturnsFailure()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.AddPlayer("player1", "conn-123");
        session.Engine.StartNewGame();

        _mockSessionManager.Setup(x => x.GetGameByPlayer("conn-123"))
            .Returns(session);
        _mockDoubleOfferService.Setup(x => x.OfferDoubleAsync(session, "conn-123"))
            .ReturnsAsync((false, 1, 2, "Not your turn"));

        // Act
        var result = await _handler.OfferDoubleAsync("conn-123");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ErrorCodes.CannotDouble, result.ErrorCode);
    }

    [Fact]
    public async Task OfferDoubleAsync_Success_BroadcastsToOpponent()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.AddPlayer("player1", "white-conn");
        session.AddPlayer("player2", "red-conn");
        session.Engine.StartNewGame();

        _mockSessionManager.Setup(x => x.GetGameByPlayer("white-conn"))
            .Returns(session);
        _mockDoubleOfferService.Setup(x => x.OfferDoubleAsync(session, "white-conn"))
            .ReturnsAsync((true, 1, 2, null));

        // Act
        var result = await _handler.OfferDoubleAsync("white-conn");

        // Assert
        Assert.True(result.Success);
        _mockGameService.Verify(
            x => x.BroadcastDoubleOfferAsync(session, "white-conn", 1, 2),
            Times.Once);
    }

    [Fact]
    public async Task AcceptDoubleAsync_NoSession_ReturnsFailure()
    {
        // Arrange
        _mockSessionManager.Setup(x => x.GetGameByPlayer("conn-123"))
            .Returns((GameSession?)null);

        // Act
        var result = await _handler.AcceptDoubleAsync("conn-123");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ErrorCodes.SessionNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task AcceptDoubleAsync_GameCompleted_ReturnsFailure()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.Engine.StartNewGame();
        session.Engine.SetCurrentPlayer(CheckerColor.White);

        // Force a winner by using Forfeit
        session.Engine.ForfeitGame(session.Engine.WhitePlayer);

        _mockSessionManager.Setup(x => x.GetGameByPlayer("conn-123"))
            .Returns(session);

        // Act
        var result = await _handler.AcceptDoubleAsync("conn-123");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ErrorCodes.CannotDouble, result.ErrorCode);
    }

    [Fact]
    public async Task AcceptDoubleAsync_Success_BroadcastsAcceptance()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.AddPlayer("player1", "white-conn");
        session.AddPlayer("player2", "red-conn");
        session.Engine.StartNewGame();

        _mockSessionManager.Setup(x => x.GetGameByPlayer("red-conn"))
            .Returns(session);

        // Act
        var result = await _handler.AcceptDoubleAsync("red-conn");

        // Assert
        Assert.True(result.Success);
        _mockDoubleOfferService.Verify(x => x.AcceptDoubleAsync(session), Times.Once);
        _mockGameService.Verify(x => x.BroadcastDoubleAcceptedAsync(session), Times.Once);
    }

    [Fact]
    public async Task DeclineDoubleAsync_NoSession_ReturnsFailure()
    {
        // Arrange
        _mockSessionManager.Setup(x => x.GetGameByPlayer("conn-123"))
            .Returns((GameSession?)null);

        // Act
        var result = await _handler.DeclineDoubleAsync("conn-123");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ErrorCodes.SessionNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task DeclineDoubleAsync_GameCompleted_ReturnsFailure()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.Engine.StartNewGame();
        session.Engine.SetCurrentPlayer(CheckerColor.White);

        // Force a winner by using Forfeit
        session.Engine.ForfeitGame(session.Engine.WhitePlayer);

        _mockSessionManager.Setup(x => x.GetGameByPlayer("conn-123"))
            .Returns(session);

        // Act
        var result = await _handler.DeclineDoubleAsync("conn-123");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ErrorCodes.CannotDouble, result.ErrorCode);
    }

    [Fact]
    public async Task DeclineDoubleAsync_DeclineFails_ReturnsFailure()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.AddPlayer("player1", "white-conn");
        session.AddPlayer("player2", "red-conn");
        session.Engine.StartNewGame();

        _mockSessionManager.Setup(x => x.GetGameByPlayer("red-conn"))
            .Returns(session);
        _mockDoubleOfferService.Setup(x => x.DeclineDoubleAsync(session, "red-conn"))
            .ReturnsAsync((false, (Player?)null, 0, "No pending double offer"));

        // Act
        var result = await _handler.DeclineDoubleAsync("red-conn");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ErrorCodes.NoDoubleOffered, result.ErrorCode);
    }

    [Fact]
    public async Task DeclineDoubleAsync_Success_EndsGameAndRemovesSession()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.AddPlayer("player1", "white-conn");
        session.AddPlayer("player2", "red-conn");
        session.Engine.StartNewGame();

        _mockSessionManager.Setup(x => x.GetGameByPlayer("red-conn"))
            .Returns(session);
        _mockDoubleOfferService.Setup(x => x.DeclineDoubleAsync(session, "red-conn"))
            .ReturnsAsync((true, session.Engine.WhitePlayer, 2, (string?)null));

        // Act
        var result = await _handler.DeclineDoubleAsync("red-conn");

        // Assert
        Assert.True(result.Success);
        _mockGameRepository.Verify(
            x => x.UpdateGameStatusAsync(session.Id, "Completed"),
            Times.Once);
        _mockGameService.Verify(x => x.BroadcastGameOverAsync(session), Times.Once);
        _mockSessionManager.Verify(x => x.RemoveGame(session.Id), Times.Once);
    }
}
