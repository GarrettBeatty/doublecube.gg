using Backgammon.Core;
using Backgammon.Server.Hubs;
using Backgammon.Server.Hubs.Interfaces;
using Backgammon.Server.Models;
using Backgammon.Server.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backgammon.Tests.Services;

public class GameImportExportServiceTests
{
    private readonly Mock<IGameSessionManager> _mockSessionManager;
    private readonly Mock<IHubContext<GameHub, IGameHubClient>> _mockHubContext;
    private readonly Mock<ILogger<GameImportExportService>> _mockLogger;
    private readonly GameImportExportService _service;

    public GameImportExportServiceTests()
    {
        _mockSessionManager = new Mock<IGameSessionManager>();
        _mockHubContext = new Mock<IHubContext<GameHub, IGameHubClient>>();
        _mockLogger = new Mock<ILogger<GameImportExportService>>();

        _service = new GameImportExportService(
            _mockSessionManager.Object,
            _mockHubContext.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ExportPositionAsync_NoSession_ReturnsEmptyStringAndSendsError()
    {
        // Arrange
        var connectionId = "conn-123";
        var mockClients = new Mock<IHubClients<IGameHubClient>>();
        var mockCallerClient = new Mock<IGameHubClient>();

        _mockSessionManager
            .Setup(m => m.GetGameByPlayer(connectionId))
            .Returns((GameSession?)null);

        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.Client(connectionId)).Returns(mockCallerClient.Object);

        // Act
        var result = await _service.ExportPositionAsync(connectionId);

        // Assert
        Assert.Empty(result);
        mockCallerClient.Verify(
            c => c.Error("You are not in a game"),
            Times.Once);
    }

    [Fact]
    public async Task ExportPositionAsync_ValidSession_ReturnsSgfString()
    {
        // Arrange
        var connectionId = "conn-123";
        var session = new GameSession("game-123");
        session.Engine.StartNewGame();

        _mockSessionManager
            .Setup(m => m.GetGameByPlayer(connectionId))
            .Returns(session);

        // Act
        var result = await _service.ExportPositionAsync(connectionId);

        // Assert
        Assert.NotEmpty(result);

        // Decode from base64 and verify it's valid SGF
        var bytes = Convert.FromBase64String(result);
        var sgf = System.Text.Encoding.UTF8.GetString(bytes);
        Assert.StartsWith("(;FF[4]GM[6]CA[UTF-8]", sgf); // Standard SGF format with headers
    }

    [Fact]
    public async Task ImportPositionAsync_NoSession_SendsError()
    {
        // Arrange
        var connectionId = "conn-123";
        var sgf = "(;GM[6])";
        var mockClients = new Mock<IHubClients<IGameHubClient>>();
        var mockCallerClient = new Mock<IGameHubClient>();

        _mockSessionManager
            .Setup(m => m.GetGameByPlayer(connectionId))
            .Returns((GameSession?)null);

        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.Client(connectionId)).Returns(mockCallerClient.Object);

        // Act
        await _service.ImportPositionAsync(connectionId, sgf);

        // Assert
        mockCallerClient.Verify(
            c => c.Error("You are not in a game"),
            Times.Once);
    }

    [Fact]
    public async Task ImportPositionAsync_GameModeDoesNotAllowImport_SendsError()
    {
        // Arrange
        var connectionId = "conn-123";
        var sgf = "(;GM[6])";
        var session = new GameSession("game-123");
        // Default game mode (competitive) doesn't allow import/export

        var mockClients = new Mock<IHubClients<IGameHubClient>>();
        var mockCallerClient = new Mock<IGameHubClient>();

        _mockSessionManager
            .Setup(m => m.GetGameByPlayer(connectionId))
            .Returns(session);

        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.Client(connectionId)).Returns(mockCallerClient.Object);

        // Act
        await _service.ImportPositionAsync(connectionId, sgf);

        // Assert
        mockCallerClient.Verify(
            c => c.Error("Cannot import positions in this game mode"),
            Times.Once);
    }

    [Fact]
    public async Task ImportPositionAsync_ValidSgf_UpdatesGameAndBroadcasts()
    {
        // Arrange
        var connectionId = "conn-123";
        var whiteConnectionId = "white-conn";
        var redConnectionId = "red-conn";
        var session = new GameSession("game-123");
        session.AddPlayer("white-player", whiteConnectionId);
        session.AddPlayer("red-player", redConnectionId);
        session.EnableAnalysisMode("white-player"); // Analysis mode allows import/export

        var sgf = "(;GM[6]SZ[24:2]AP[bgammon];B[24/2])";

        var mockClients = new Mock<IHubClients<IGameHubClient>>();
        var mockWhiteClient = new Mock<IGameHubClient>();
        var mockRedClient = new Mock<IGameHubClient>();

        _mockSessionManager
            .Setup(m => m.GetGameByPlayer(connectionId))
            .Returns(session);

        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.Client(whiteConnectionId)).Returns(mockWhiteClient.Object);
        mockClients.Setup(c => c.Client(redConnectionId)).Returns(mockRedClient.Object);

        // Act
        await _service.ImportPositionAsync(connectionId, sgf);

        // Assert
        mockWhiteClient.Verify(
            c => c.GameUpdate(It.IsAny<GameState>()),
            Times.Once);
        mockRedClient.Verify(
            c => c.GameUpdate(It.IsAny<GameState>()),
            Times.Once);
    }

    [Fact]
    public async Task ImportPositionAsync_InvalidSgf_SendsError()
    {
        // Arrange
        var connectionId = "conn-123";
        var session = new GameSession("game-123");
        session.AddPlayer("player-1", connectionId);
        session.EnableAnalysisMode("player-1");

        var invalidSgf = "invalid sgf data";

        var mockClients = new Mock<IHubClients<IGameHubClient>>();
        var mockCallerClient = new Mock<IGameHubClient>();

        _mockSessionManager
            .Setup(m => m.GetGameByPlayer(connectionId))
            .Returns(session);

        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.Client(connectionId)).Returns(mockCallerClient.Object);

        // Act
        await _service.ImportPositionAsync(connectionId, invalidSgf);

        // Assert
        mockCallerClient.Verify(
            c => c.Error(It.Is<string>(s => s.StartsWith("Failed to import position:"))),
            Times.Once);
    }
}
