using Backgammon.Core;
using Backgammon.Server.Hubs.Handlers;
using Backgammon.Server.Models;
using Backgammon.Server.Models.SignalR;
using Backgammon.Server.Services;
using Backgammon.Server.Services.Results;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Backgammon.Tests.Hubs.Handlers;

public class GameActionHandlerTests
{
    private readonly Mock<IGameSessionManager> _mockSessionManager;
    private readonly Mock<IGameActionOrchestrator> _mockOrchestrator;
    private readonly Mock<IMoveQueryService> _mockMoveQueryService;
    private readonly Mock<IGameService> _mockGameService;
    private readonly Mock<ILogger<GameActionHandler>> _mockLogger;
    private readonly GameActionHandler _handler;

    public GameActionHandlerTests()
    {
        _mockSessionManager = new Mock<IGameSessionManager>();
        _mockOrchestrator = new Mock<IGameActionOrchestrator>();
        _mockMoveQueryService = new Mock<IMoveQueryService>();
        _mockGameService = new Mock<IGameService>();
        _mockLogger = new Mock<ILogger<GameActionHandler>>();

        _handler = new GameActionHandler(
            _mockSessionManager.Object,
            _mockOrchestrator.Object,
            _mockMoveQueryService.Object,
            _mockGameService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task RollDiceAsync_NoSession_ReturnsFailure()
    {
        // Arrange
        _mockSessionManager.Setup(x => x.GetGameByPlayer("conn-123"))
            .Returns((GameSession?)null);

        // Act
        var result = await _handler.RollDiceAsync("conn-123");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ErrorCodes.SessionNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task RollDiceAsync_OrchestratorFails_ReturnsFailure()
    {
        // Arrange
        var session = new GameSession("game-123");
        _mockSessionManager.Setup(x => x.GetGameByPlayer("conn-123"))
            .Returns(session);
        _mockOrchestrator.Setup(x => x.RollDiceAsync(session, "conn-123"))
            .ReturnsAsync(ActionResult.Error("Not your turn"));

        // Act
        var result = await _handler.RollDiceAsync("conn-123");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ErrorCodes.InvalidMove, result.ErrorCode);
    }

    [Fact]
    public async Task RollDiceAsync_Success_ReturnsOk()
    {
        // Arrange
        var session = new GameSession("game-123");
        _mockSessionManager.Setup(x => x.GetGameByPlayer("conn-123"))
            .Returns(session);
        _mockOrchestrator.Setup(x => x.RollDiceAsync(session, "conn-123"))
            .ReturnsAsync(ActionResult.Ok());

        // Act
        var result = await _handler.RollDiceAsync("conn-123");

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public async Task MakeMoveAsync_NoSession_ReturnsFailure()
    {
        // Arrange
        _mockSessionManager.Setup(x => x.GetGameByPlayer("conn-123"))
            .Returns((GameSession?)null);

        // Act
        var result = await _handler.MakeMoveAsync("conn-123", 24, 20);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ErrorCodes.SessionNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task MakeMoveAsync_OrchestratorFails_ReturnsFailure()
    {
        // Arrange
        var session = new GameSession("game-123");
        _mockSessionManager.Setup(x => x.GetGameByPlayer("conn-123"))
            .Returns(session);
        _mockOrchestrator.Setup(x => x.MakeMoveAsync(session, "conn-123", 24, 20))
            .ReturnsAsync(ActionResult.Error("Invalid move"));

        // Act
        var result = await _handler.MakeMoveAsync("conn-123", 24, 20);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ErrorCodes.InvalidMove, result.ErrorCode);
    }

    [Fact]
    public async Task MakeMoveAsync_Success_ReturnsOk()
    {
        // Arrange
        var session = new GameSession("game-123");
        _mockSessionManager.Setup(x => x.GetGameByPlayer("conn-123"))
            .Returns(session);
        _mockOrchestrator.Setup(x => x.MakeMoveAsync(session, "conn-123", 24, 20))
            .ReturnsAsync(ActionResult.Ok());

        // Act
        var result = await _handler.MakeMoveAsync("conn-123", 24, 20);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public async Task MakeCombinedMoveAsync_NoSession_ReturnsFailure()
    {
        // Arrange
        _mockSessionManager.Setup(x => x.GetGameByPlayer("conn-123"))
            .Returns((GameSession?)null);

        // Act
        var result = await _handler.MakeCombinedMoveAsync("conn-123", 24, 17, new[] { 20 });

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ErrorCodes.SessionNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task MakeCombinedMoveAsync_Success_ReturnsOk()
    {
        // Arrange
        var session = new GameSession("game-123");
        _mockSessionManager.Setup(x => x.GetGameByPlayer("conn-123"))
            .Returns(session);
        _mockOrchestrator.Setup(x => x.MakeCombinedMoveAsync(session, "conn-123", 24, 17, It.IsAny<int[]>()))
            .ReturnsAsync(ActionResult.Ok());

        // Act
        var result = await _handler.MakeCombinedMoveAsync("conn-123", 24, 17, new[] { 20 });

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public async Task EndTurnAsync_NoSession_ReturnsFailure()
    {
        // Arrange
        _mockSessionManager.Setup(x => x.GetGameByPlayer("conn-123"))
            .Returns((GameSession?)null);

        // Act
        var result = await _handler.EndTurnAsync("conn-123");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ErrorCodes.SessionNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task EndTurnAsync_OrchestratorFails_ReturnsFailure()
    {
        // Arrange
        var session = new GameSession("game-123");
        _mockSessionManager.Setup(x => x.GetGameByPlayer("conn-123"))
            .Returns(session);
        _mockOrchestrator.Setup(x => x.EndTurnAsync(session, "conn-123"))
            .ReturnsAsync(ActionResult.Error("Cannot end turn"));

        // Act
        var result = await _handler.EndTurnAsync("conn-123");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ErrorCodes.InvalidMove, result.ErrorCode);
    }

    [Fact]
    public async Task EndTurnAsync_Success_ReturnsOk()
    {
        // Arrange
        var session = new GameSession("game-123");
        _mockSessionManager.Setup(x => x.GetGameByPlayer("conn-123"))
            .Returns(session);
        _mockOrchestrator.Setup(x => x.EndTurnAsync(session, "conn-123"))
            .ReturnsAsync(ActionResult.Ok());

        // Act
        var result = await _handler.EndTurnAsync("conn-123");

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public async Task UndoLastMoveAsync_NoSession_ReturnsFailure()
    {
        // Arrange
        _mockSessionManager.Setup(x => x.GetGameByPlayer("conn-123"))
            .Returns((GameSession?)null);

        // Act
        var result = await _handler.UndoLastMoveAsync("conn-123");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ErrorCodes.SessionNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task UndoLastMoveAsync_Success_ReturnsOk()
    {
        // Arrange
        var session = new GameSession("game-123");
        _mockSessionManager.Setup(x => x.GetGameByPlayer("conn-123"))
            .Returns(session);
        _mockOrchestrator.Setup(x => x.UndoLastMoveAsync(session, "conn-123"))
            .ReturnsAsync(ActionResult.Ok());

        // Act
        var result = await _handler.UndoLastMoveAsync("conn-123");

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public async Task SetDiceAsync_NoSession_ReturnsFailure()
    {
        // Arrange
        _mockSessionManager.Setup(x => x.GetGameByPlayer("conn-123"))
            .Returns((GameSession?)null);

        // Act
        var result = await _handler.SetDiceAsync("conn-123", 3, 4);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ErrorCodes.SessionNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task SetDiceAsync_NotAnalysisMode_ReturnsFailure()
    {
        // Arrange
        var session = new GameSession("game-123");
        // IsAnalysisMode is false by default
        _mockSessionManager.Setup(x => x.GetGameByPlayer("conn-123"))
            .Returns(session);

        // Act
        var result = await _handler.SetDiceAsync("conn-123", 3, 4);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ErrorCodes.NotAuthorized, result.ErrorCode);
    }

    [Fact]
    public async Task SetDiceAsync_InvalidDice_ReturnsFailure()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.EnableAnalysisMode("player-1");
        _mockSessionManager.Setup(x => x.GetGameByPlayer("conn-123"))
            .Returns(session);

        // Act
        var result = await _handler.SetDiceAsync("conn-123", 0, 7);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ErrorCodes.InvalidMove, result.ErrorCode);
        Assert.Contains("between 1 and 6", result.Message);
    }

    [Fact]
    public async Task SetDiceAsync_AnalysisMode_Success()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.EnableAnalysisMode("player-1");
        session.Engine.StartNewGame();
        session.Engine.RemainingMoves.Clear(); // No moves left
        _mockSessionManager.Setup(x => x.GetGameByPlayer("conn-123"))
            .Returns(session);

        // Act
        var result = await _handler.SetDiceAsync("conn-123", 3, 4);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(3, session.Engine.Dice.Die1);
        Assert.Equal(4, session.Engine.Dice.Die2);
        _mockGameService.Verify(x => x.BroadcastGameUpdateAsync(session), Times.Once);
    }

    [Fact]
    public async Task MoveCheckerDirectlyAsync_NoSession_ReturnsFailure()
    {
        // Arrange
        _mockSessionManager.Setup(x => x.GetGameByPlayer("conn-123"))
            .Returns((GameSession?)null);

        // Act
        var result = await _handler.MoveCheckerDirectlyAsync("conn-123", 1, 2);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ErrorCodes.SessionNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task MoveCheckerDirectlyAsync_NotAnalysisMode_ReturnsFailure()
    {
        // Arrange
        var session = new GameSession("game-123");
        _mockSessionManager.Setup(x => x.GetGameByPlayer("conn-123"))
            .Returns(session);

        // Act
        var result = await _handler.MoveCheckerDirectlyAsync("conn-123", 1, 2);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ErrorCodes.NotAuthorized, result.ErrorCode);
    }

    [Fact]
    public async Task MoveCheckerDirectlyAsync_InvalidMove_ReturnsFailure()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.EnableAnalysisMode("player-1");
        session.Engine.StartNewGame();
        _mockSessionManager.Setup(x => x.GetGameByPlayer("conn-123"))
            .Returns(session);

        // Act - Invalid: from same to same
        var result = await _handler.MoveCheckerDirectlyAsync("conn-123", 5, 5);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ErrorCodes.InvalidMove, result.ErrorCode);
    }

    [Fact]
    public async Task MoveCheckerDirectlyAsync_Success()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.EnableAnalysisMode("player-1");
        session.Engine.StartNewGame();
        _mockSessionManager.Setup(x => x.GetGameByPlayer("conn-123"))
            .Returns(session);

        // Move white checker from point 6 to point 5 (valid in analysis mode)
        int initialCount = session.Engine.Board.GetPoint(6).Count;

        // Act
        var result = await _handler.MoveCheckerDirectlyAsync("conn-123", 6, 5);

        // Assert
        Assert.True(result.Success);
        _mockGameService.Verify(x => x.BroadcastGameUpdateAsync(session), Times.Once);
    }

    [Fact]
    public async Task SetCurrentPlayerAsync_NoSession_ReturnsFailure()
    {
        // Arrange
        _mockSessionManager.Setup(x => x.GetGameByPlayer("conn-123"))
            .Returns((GameSession?)null);

        // Act
        var result = await _handler.SetCurrentPlayerAsync("conn-123", CheckerColor.White);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ErrorCodes.SessionNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task SetCurrentPlayerAsync_NotAnalysisMode_ReturnsFailure()
    {
        // Arrange
        var session = new GameSession("game-123");
        _mockSessionManager.Setup(x => x.GetGameByPlayer("conn-123"))
            .Returns(session);

        // Act
        var result = await _handler.SetCurrentPlayerAsync("conn-123", CheckerColor.White);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ErrorCodes.NotAuthorized, result.ErrorCode);
    }

    [Fact]
    public async Task SetCurrentPlayerAsync_Success()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.EnableAnalysisMode("player-1");
        session.Engine.StartNewGame();
        _mockSessionManager.Setup(x => x.GetGameByPlayer("conn-123"))
            .Returns(session);

        // Act
        var result = await _handler.SetCurrentPlayerAsync("conn-123", CheckerColor.Red);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(CheckerColor.Red, session.Engine.CurrentPlayer?.Color);
        _mockGameService.Verify(x => x.BroadcastGameUpdateAsync(session), Times.Once);
    }

    [Fact]
    public void GetValidSources_CallsMoveQueryService()
    {
        // Arrange
        var expectedSources = new List<int> { 6, 8, 13 };
        _mockMoveQueryService.Setup(x => x.GetValidSources("conn-123"))
            .Returns(expectedSources);

        // Act
        var result = _handler.GetValidSources("conn-123");

        // Assert
        Assert.Equal(expectedSources, result);
        _mockMoveQueryService.Verify(x => x.GetValidSources("conn-123"), Times.Once);
    }

    [Fact]
    public void GetValidDestinations_CallsMoveQueryService()
    {
        // Arrange
        var expectedDests = new List<MoveDto>
        {
            new() { From = 6, To = 3, DieValue = 3 },
            new() { From = 6, To = 2, DieValue = 4 }
        };
        _mockMoveQueryService.Setup(x => x.GetValidDestinations("conn-123", 6))
            .Returns(expectedDests);

        // Act
        var result = _handler.GetValidDestinations("conn-123", 6);

        // Assert
        Assert.Equal(2, result.Count);
        _mockMoveQueryService.Verify(x => x.GetValidDestinations("conn-123", 6), Times.Once);
    }
}
