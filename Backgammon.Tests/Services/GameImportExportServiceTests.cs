using Backgammon.Core;
using Backgammon.Server.Hubs;
using Backgammon.Server.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backgammon.Tests.Services;

public class GameImportExportServiceTests
{
    private readonly Mock<IGameSessionManager> _mockSessionManager;
    private readonly Mock<IHubContext<GameHub>> _mockHubContext;
    private readonly Mock<ILogger<GameImportExportService>> _mockLogger;
    private readonly GameImportExportService _service;

    public GameImportExportServiceTests()
    {
        _mockSessionManager = new Mock<IGameSessionManager>();
        _mockHubContext = new Mock<IHubContext<GameHub>>();
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
        var mockClients = new Mock<IHubClients>();
        var mockCallerClients = new Mock<ISingleClientProxy>();

        _mockSessionManager
            .Setup(m => m.GetGameByPlayer(connectionId))
            .Returns((GameSession?)null);

        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.Client(connectionId)).Returns(mockCallerClients.Object);

        // Act
        var result = await _service.ExportPositionAsync(connectionId);

        // Assert
        Assert.Empty(result);
        mockCallerClients.Verify(
            c => c.SendCoreAsync(
                "Error",
                It.Is<object[]>(args => args.Length == 1 && (string)args[0] == "You are not in a game"),
                default),
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
        var mockClients = new Mock<IHubClients>();
        var mockCallerClients = new Mock<ISingleClientProxy>();

        _mockSessionManager
            .Setup(m => m.GetGameByPlayer(connectionId))
            .Returns((GameSession?)null);

        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.Client(connectionId)).Returns(mockCallerClients.Object);

        // Act
        await _service.ImportPositionAsync(connectionId, sgf);

        // Assert
        mockCallerClients.Verify(
            c => c.SendCoreAsync(
                "Error",
                It.Is<object[]>(args => args.Length == 1 && (string)args[0] == "You are not in a game"),
                default),
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

        var mockClients = new Mock<IHubClients>();
        var mockCallerClients = new Mock<ISingleClientProxy>();

        _mockSessionManager
            .Setup(m => m.GetGameByPlayer(connectionId))
            .Returns(session);

        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.Client(connectionId)).Returns(mockCallerClients.Object);

        // Act
        await _service.ImportPositionAsync(connectionId, sgf);

        // Assert
        mockCallerClients.Verify(
            c => c.SendCoreAsync(
                "Error",
                It.Is<object[]>(args => args.Length == 1 && (string)args[0] == "Cannot import positions in this game mode"),
                default),
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

        var mockClients = new Mock<IHubClients>();
        var mockWhiteClient = new Mock<ISingleClientProxy>();
        var mockRedClient = new Mock<ISingleClientProxy>();

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
            c => c.SendCoreAsync(
                "GameUpdate",
                It.IsAny<object[]>(),
                default),
            Times.Once);
        mockRedClient.Verify(
            c => c.SendCoreAsync(
                "GameUpdate",
                It.IsAny<object[]>(),
                default),
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

        var mockClients = new Mock<IHubClients>();
        var mockCallerClients = new Mock<ISingleClientProxy>();

        _mockSessionManager
            .Setup(m => m.GetGameByPlayer(connectionId))
            .Returns(session);

        _mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.Client(connectionId)).Returns(mockCallerClients.Object);

        // Act
        await _service.ImportPositionAsync(connectionId, invalidSgf);

        // Assert
        mockCallerClients.Verify(
            c => c.SendCoreAsync(
                "Error",
                It.Is<object[]>(args => args.Length == 1 && ((string)args[0]).StartsWith("Failed to import position:")),
                default),
            Times.Once);
    }
}
