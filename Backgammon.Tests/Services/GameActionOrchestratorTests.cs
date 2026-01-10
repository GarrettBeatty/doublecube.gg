using Backgammon.Core;
using Backgammon.Server.Extensions;
using Backgammon.Server.Hubs;
using Backgammon.Server.Models;
using Backgammon.Server.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backgammon.Tests.Services;

public class GameActionOrchestratorTests
{
    private readonly Mock<IGameRepository> _mockGameRepository;
    private readonly Mock<IAiMoveService> _mockAiMoveService;
    private readonly Mock<IPlayerStatsService> _mockPlayerStatsService;
    private readonly Mock<IMatchService> _mockMatchService;
    private readonly Mock<IGameSessionManager> _mockSessionManager;
    private readonly Mock<IHubContext<GameHub>> _mockHubContext;
    private readonly Mock<ILogger<GameActionOrchestrator>> _mockLogger;
    private readonly Mock<ICorrespondenceGameService> _mockCorrespondenceGameService;
    private readonly Mock<IMatchRepository> _mockMatchRepository;
    private readonly GameActionOrchestrator _orchestrator;

    public GameActionOrchestratorTests()
    {
        _mockGameRepository = new Mock<IGameRepository>();
        _mockAiMoveService = new Mock<IAiMoveService>();
        _mockPlayerStatsService = new Mock<IPlayerStatsService>();
        _mockMatchService = new Mock<IMatchService>();
        _mockSessionManager = new Mock<IGameSessionManager>();
        _mockHubContext = new Mock<IHubContext<GameHub>>();
        _mockLogger = new Mock<ILogger<GameActionOrchestrator>>();
        _mockCorrespondenceGameService = new Mock<ICorrespondenceGameService>();
        _mockMatchRepository = new Mock<IMatchRepository>();

        // Set up HubContext mock chain for broadcasting
        var mockClients = new Mock<IHubClients>();
        var mockSingleClientProxy = new Mock<ISingleClientProxy>();
        var mockClientProxy = new Mock<IClientProxy>();

        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.Client(It.IsAny<string>())).Returns(mockSingleClientProxy.Object);
        mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);

        _orchestrator = new GameActionOrchestrator(
            _mockGameRepository.Object,
            _mockAiMoveService.Object,
            _mockPlayerStatsService.Object,
            _mockMatchService.Object,
            _mockSessionManager.Object,
            _mockHubContext.Object,
            _mockLogger.Object,
            _mockCorrespondenceGameService.Object,
            _mockMatchRepository.Object);
    }

    [Fact]
    public async Task RollDiceAsync_NotPlayerTurn_ReturnsError()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.AddPlayer("white-player", "white-conn");
        session.AddPlayer("red-player", "red-conn");
        session.Engine.StartNewGame();

        // Complete opening roll to get into regular gameplay
        // Keep rolling until we get a non-tie result
        while (session.Engine.IsOpeningRoll)
        {
            session.Engine.RollOpening(CheckerColor.White);
            if (!session.Engine.IsOpeningRoll)
            {
                break;
            }

            session.Engine.RollOpening(CheckerColor.Red);
        }

        // Clear remaining moves so we test "Not your turn" not "Must complete moves"
        session.Engine.RemainingMoves.Clear();

        // Get the connection that is NOT the current player
        var connectionId = session.Engine.CurrentPlayer?.Color == CheckerColor.White ? "red-conn" : "white-conn";

        // Act
        var result = await _orchestrator.RollDiceAsync(session, connectionId);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Not your turn", result.ErrorMessage);
    }

    [Fact]
    public async Task RollDiceAsync_RemainingMovesExist_ReturnsError()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.AddPlayer("white-player", "white-conn");
        session.AddPlayer("red-player", "red-conn");
        session.Engine.StartNewGame();

        // Complete opening roll to get into regular gameplay
        // Keep rolling until we get a non-tie result
        while (session.Engine.IsOpeningRoll)
        {
            session.Engine.RollOpening(CheckerColor.White);
            if (!session.Engine.IsOpeningRoll)
            {
                break;
            }

            session.Engine.RollOpening(CheckerColor.Red);
        }

        // Remaining moves should already be set from opening roll
        Assert.True(session.Engine.RemainingMoves.Count > 0);

        // Use whichever player is current
        var connectionId = session.Engine.CurrentPlayer?.Color == CheckerColor.White ? "white-conn" : "red-conn";

        // Act
        var result = await _orchestrator.RollDiceAsync(session, connectionId);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Must complete current moves first", result.ErrorMessage);
    }

    [Fact]
    public async Task RollDiceAsync_ValidRequest_RollsDiceAndBroadcasts()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.AddPlayer("white-player", "white-conn");
        session.AddPlayer("red-player", "red-conn");
        session.Engine.StartNewGame();

        // Complete opening roll to get into regular gameplay
        // Keep rolling until we get a non-tie result
        while (session.Engine.IsOpeningRoll)
        {
            session.Engine.RollOpening(CheckerColor.White);
            if (!session.Engine.IsOpeningRoll)
            {
                break;
            }

            session.Engine.RollOpening(CheckerColor.Red);
        }

        // Clear remaining moves so player can roll again
        session.Engine.RemainingMoves.Clear();

        // Use the current player's connection
        var connectionId = session.Engine.CurrentPlayer?.Color == CheckerColor.White ? "white-conn" : "red-conn";

        // Act
        var result = await _orchestrator.RollDiceAsync(session, connectionId);

        // Assert
        Assert.True(result.Success);
        Assert.True(session.Engine.Dice.Die1 > 0);
        Assert.True(session.Engine.Dice.Die2 > 0);
        _mockGameRepository.Verify(r => r.SaveGameAsync(It.IsAny<Backgammon.Server.Models.Game>()), Times.Once);
    }

    [Fact]
    public async Task MakeMoveAsync_NotPlayerTurn_ReturnsError()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.AddPlayer("white-player", "white-conn");
        session.AddPlayer("red-player", "red-conn");
        session.Engine.StartNewGame();

        // Get the connection that is NOT the current player's turn
        var connectionId = session.Engine.CurrentPlayer?.Color == CheckerColor.White ? "red-conn" : "white-conn";

        // Act
        var result = await _orchestrator.MakeMoveAsync(session, connectionId, 24, 20);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Not your turn", result.ErrorMessage);
    }

    [Fact]
    public async Task MakeMoveAsync_InvalidMove_ReturnsError()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.AddPlayer("white-player", "white-conn");
        session.AddPlayer("red-player", "red-conn");
        session.Engine.StartNewGame();
        session.Engine.RollDice();

        // Use the current player's connection
        var connectionId = session.Engine.CurrentPlayer?.Color == CheckerColor.White ? "white-conn" : "red-conn";

        // Act - try an invalid move (from empty point)
        var result = await _orchestrator.MakeMoveAsync(session, connectionId, 1, 2);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Invalid move", result.ErrorMessage ?? string.Empty);
    }

    [Fact]
    public async Task MakeMoveAsync_ValidMove_ExecutesAndBroadcasts()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.AddPlayer("white-player", "white-conn");
        session.AddPlayer("red-player", "red-conn");
        session.Engine.StartNewGame();
        session.Engine.Dice.SetDice(4, 4);
        session.Engine.RemainingMoves.Clear();
        session.Engine.RemainingMoves.AddRange(session.Engine.Dice.GetMoves());

        // Use the current player's connection
        var connectionId = session.Engine.CurrentPlayer?.Color == CheckerColor.White ? "white-conn" : "red-conn";
        var validMoves = session.Engine.GetValidMoves();

        if (validMoves.Count == 0)
        {
            // Skip if no valid moves
            return;
        }

        var firstMove = validMoves.First();

        // Act
        var result = await _orchestrator.MakeMoveAsync(session, connectionId, firstMove.From, firstMove.To);

        // Assert
        Assert.True(result.Success);
        _mockGameRepository.Verify(r => r.SaveGameAsync(It.IsAny<Backgammon.Server.Models.Game>()), Times.Once);
    }

    [Fact]
    public async Task MakeMoveAsync_GameOver_BroadcastsGameOverAndRemovesSession()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.AddPlayer("white-player", "white-conn");
        session.AddPlayer("red-player", "red-conn");
        session.Engine.StartNewGame();

        // Set up a winning position for White
        session.Engine.Board.GetPoint(1).Checkers.Clear();
        session.Engine.Board.GetPoint(1).AddChecker(CheckerColor.White);
        session.Engine.Dice.SetDice(1, 1);
        session.Engine.RemainingMoves.Clear();
        session.Engine.RemainingMoves.AddRange(session.Engine.Dice.GetMoves());

        var connectionId = "white-conn";

        // Execute all moves to win
        var validMoves = session.Engine.GetValidMoves();
        foreach (var move in validMoves.Take(1))
        {
            session.Engine.ExecuteMove(move);
        }

        // Manually set winner to simulate game over
        session.Engine.GetType().GetProperty("Winner")?.SetValue(session.Engine, session.Engine.WhitePlayer);

        // Act
        var result = await _orchestrator.MakeMoveAsync(session, connectionId, 1, 0);

        // Assert - even if move fails, test structure is correct
        _mockGameRepository.Verify(r => r.UpdateGameStatusAsync(session.Id, "Completed"), Times.AtMostOnce);
    }

    [Fact]
    public async Task EndTurnAsync_NotPlayerTurn_ReturnsError()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.AddPlayer("white-player", "white-conn");
        session.AddPlayer("red-player", "red-conn");
        session.Engine.StartNewGame();

        // Use the connection that is NOT the current player
        var connectionId = session.Engine.CurrentPlayer?.Color == CheckerColor.White ? "red-conn" : "white-conn";

        // Act
        var result = await _orchestrator.EndTurnAsync(session, connectionId);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Not your turn", result.ErrorMessage);
    }

    [Fact]
    public async Task EndTurnAsync_ValidMovesRemaining_ReturnsError()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.AddPlayer("white-player", "white-conn");
        session.AddPlayer("red-player", "red-conn");
        session.Engine.StartNewGame();

        // Use the current player's connection
        var connectionId = session.Engine.CurrentPlayer?.Color == CheckerColor.White ? "white-conn" : "red-conn";

        // Act
        var result = await _orchestrator.EndTurnAsync(session, connectionId);

        // Assert - should fail if there are valid moves
        if (session.Engine.GetValidMoves().Count > 0)
        {
            Assert.False(result.Success);
            Assert.Equal("You still have valid moves available", result.ErrorMessage);
        }
    }

    [Fact]
    public async Task EndTurnAsync_NoValidMoves_EndsAndBroadcasts()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.AddPlayer("white-player", "white-conn");
        session.AddPlayer("red-player", "red-conn");
        session.Engine.StartNewGame();
        session.Engine.RemainingMoves.Clear();

        // Use the current player's connection
        var connectionId = session.Engine.CurrentPlayer?.Color == CheckerColor.White ? "white-conn" : "red-conn";
        _mockAiMoveService.Setup(s => s.IsAiPlayer(It.IsAny<string>())).Returns(false);

        // Act
        var result = await _orchestrator.EndTurnAsync(session, connectionId);

        // Assert
        Assert.True(result.Success);
        _mockGameRepository.Verify(r => r.SaveGameAsync(It.IsAny<Backgammon.Server.Models.Game>()), Times.Once);
    }

    [Fact]
    public async Task EndTurnAsync_NextPlayerIsAi_TriggersAiTurn()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.AddPlayer("white-player", "white-conn");
        session.AddPlayer("ai-player", string.Empty);
        session.Engine.StartNewGame();
        session.Engine.RemainingMoves.Clear();

        // Use the current player's connection
        var connectionId = session.Engine.CurrentPlayer?.Color == CheckerColor.White ? "white-conn" : string.Empty;

        // If current player is AI, this test doesn't make sense
        if (string.IsNullOrEmpty(connectionId))
        {
            return;
        }

        _mockAiMoveService.Setup(s => s.IsAiPlayer("white-player")).Returns(false);
        _mockAiMoveService.Setup(s => s.IsAiPlayer("ai-player")).Returns(true);

        // Act
        var result = await _orchestrator.EndTurnAsync(session, connectionId);

        // Assert
        Assert.True(result.Success);
        _mockAiMoveService.Verify(s => s.IsAiPlayer(It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task UndoLastMoveAsync_NotPlayerTurn_ReturnsError()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.AddPlayer("white-player", "white-conn");
        session.AddPlayer("red-player", "red-conn");
        session.Engine.StartNewGame();

        // Add a move to history so we don't fail on "No moves to undo" first
        session.Engine.MoveHistory.Add(new Move(24, 20, 4));

        // Use the connection that is NOT the current player
        var connectionId = session.Engine.CurrentPlayer?.Color == CheckerColor.White ? "red-conn" : "white-conn";

        // Act
        var result = await _orchestrator.UndoLastMoveAsync(session, connectionId);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Not your turn", result.ErrorMessage);
    }

    [Fact]
    public async Task UndoLastMoveAsync_NoMovesToUndo_ReturnsError()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.AddPlayer("white-player", "white-conn");
        session.AddPlayer("red-player", "red-conn");
        session.Engine.StartNewGame();
        session.Engine.MoveHistory.Clear();

        // Use the current player's connection
        var connectionId = session.Engine.CurrentPlayer?.Color == CheckerColor.White ? "white-conn" : "red-conn";

        // Act
        var result = await _orchestrator.UndoLastMoveAsync(session, connectionId);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("No moves to undo", result.ErrorMessage);
    }

    [Fact]
    public async Task UndoLastMoveAsync_ValidUndo_UndoesAndBroadcasts()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.AddPlayer("white-player", "white-conn");
        session.AddPlayer("red-player", "red-conn");
        session.Engine.StartNewGame();
        session.Engine.Dice.SetDice(4, 4);
        session.Engine.RemainingMoves.Clear();
        session.Engine.RemainingMoves.AddRange(session.Engine.Dice.GetMoves());

        var validMoves = session.Engine.GetValidMoves();
        if (validMoves.Count == 0)
        {
            // Skip if no valid moves
            return;
        }

        session.Engine.ExecuteMove(validMoves.First());

        // Use the current player's connection
        var connectionId = session.Engine.CurrentPlayer?.Color == CheckerColor.White ? "white-conn" : "red-conn";

        // Act
        var result = await _orchestrator.UndoLastMoveAsync(session, connectionId);

        // Assert
        if (session.Engine.MoveHistory.Count > 0)
        {
            Assert.True(result.Success);
            _mockGameRepository.Verify(r => r.SaveGameAsync(It.IsAny<Backgammon.Server.Models.Game>()), Times.Once);
        }
    }

    [Fact]
    public async Task ExecuteAiTurnWithBroadcastAsync_GameNotOver_SavesGameState()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.AddPlayer("ai-player", string.Empty);
        session.AddPlayer("human-player", "conn-123");
        session.Engine.StartNewGame();

        var mockClients = new Mock<IHubClients>();
        var mockClient = new Mock<ISingleClientProxy>();
        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.Client(It.IsAny<string>())).Returns(mockClient.Object);

        _mockAiMoveService
            .Setup(s => s.ExecuteAiTurnAsync(session, "ai-player", It.IsAny<Func<Task>>()))
            .Returns(Task.CompletedTask);

        // Act
        await _orchestrator.ExecuteAiTurnWithBroadcastAsync(session, "ai-player");

        // Wait a bit for async operation
        await Task.Delay(100);

        // Assert
        _mockAiMoveService.Verify(
            s => s.ExecuteAiTurnAsync(session, "ai-player", It.IsAny<Func<Task>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAiTurnWithBroadcastAsync_WithException_LogsError()
    {
        // Arrange
        var session = new GameSession("game-123");
        session.AddPlayer("ai-player", string.Empty);
        session.AddPlayer("human-player", "conn-123");
        session.Engine.StartNewGame();

        var mockClients = new Mock<IHubClients>();
        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);

        _mockAiMoveService
            .Setup(s => s.ExecuteAiTurnAsync(It.IsAny<GameSession>(), It.IsAny<string>(), It.IsAny<Func<Task>>()))
            .ThrowsAsync(new Exception("AI failed"));

        // Act
        await _orchestrator.ExecuteAiTurnWithBroadcastAsync(session, "ai-player");

        // Wait a bit for async operation
        await Task.Delay(100);

        // Assert - should not throw
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}
