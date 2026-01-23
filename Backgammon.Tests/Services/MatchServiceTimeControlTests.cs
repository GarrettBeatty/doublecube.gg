using Backgammon.Core;
using Backgammon.Server.Models;
using Backgammon.Server.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Backgammon.Tests.Services;

/// <summary>
/// Tests to verify that all lobby and AI games enforce ChicagoPoint time control
/// </summary>
public class MatchServiceTimeControlTests
{
    private readonly Mock<IMatchRepository> _mockMatchRepo;
    private readonly Mock<IGameRepository> _mockGameRepo;
    private readonly Mock<IGameSessionManager> _mockGameSessionManager;
    private readonly Mock<IGameSessionFactory> _mockGameSessionFactory;
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly Mock<IAiMoveService> _mockAiMoveService;
    private readonly Mock<IAiPlayerManager> _mockAiPlayerManager;
    private readonly Mock<ICorrespondenceGameService> _mockCorrespondenceGameService;
    private readonly Mock<IChatService> _mockChatService;
    private readonly Mock<ILogger<MatchService>> _mockLogger;
    private readonly MatchService _matchService;

    public MatchServiceTimeControlTests()
    {
        _mockMatchRepo = new Mock<IMatchRepository>();
        _mockGameRepo = new Mock<IGameRepository>();
        _mockGameSessionManager = new Mock<IGameSessionManager>();
        _mockGameSessionFactory = new Mock<IGameSessionFactory>();
        _mockUserRepo = new Mock<IUserRepository>();
        _mockAiMoveService = new Mock<IAiMoveService>();
        _mockAiPlayerManager = new Mock<IAiPlayerManager>();
        _mockCorrespondenceGameService = new Mock<ICorrespondenceGameService>();
        _mockChatService = new Mock<IChatService>();
        _mockLogger = new Mock<ILogger<MatchService>>();

        _matchService = new MatchService(
            _mockMatchRepo.Object,
            _mockGameRepo.Object,
            _mockGameSessionManager.Object,
            _mockGameSessionFactory.Object,
            _mockUserRepo.Object,
            _mockAiMoveService.Object,
            _mockAiPlayerManager.Object,
            _mockCorrespondenceGameService.Object,
            _mockChatService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task CreateMatch_WithNullTimeControl_DefaultsToChicagoPoint()
    {
        // Arrange
        var playerId = "player1";
        var targetScore = 5;

        _mockUserRepo.Setup(x => x.GetByUserIdAsync(playerId))
            .ReturnsAsync(new User { UserId = playerId, Username = "TestPlayer", DisplayName = "Test Player" });

        // Act
        var (match, game) = await _matchService.CreateMatchAsync(
            playerId,
            targetScore,
            "OpenLobby",
            "Test Player",
            null,
            null, // No time control specified
            true,
            "greedy");

        // Assert
        Assert.NotNull(match.TimeControl);
        Assert.Equal(TimeControlType.ChicagoPoint, match.TimeControl.Type);
        Assert.Equal(12, match.TimeControl.DelaySeconds);
    }

    [Fact]
    public async Task CreateMatch_AI_AlwaysChicagoPoint()
    {
        // Arrange
        var playerId = "player1";
        var targetScore = 3;
        var aiType = "greedy";

        _mockUserRepo.Setup(x => x.GetByUserIdAsync(playerId))
            .ReturnsAsync(new User { UserId = playerId, Username = "TestPlayer", DisplayName = "Test Player" });

        _mockAiPlayerManager.Setup(x => x.GetOrCreateAiForMatch(It.IsAny<string>(), aiType))
            .Returns("ai-player-1");
        _mockAiPlayerManager.Setup(x => x.GetAiNameForMatch(It.IsAny<string>(), aiType))
            .Returns("Greedy Bot");

        // Act - Create with null time control
        var (match, game) = await _matchService.CreateMatchAsync(
            playerId,
            targetScore,
            "AI",
            "Test Player",
            null,
            null, // No time control specified
            true,
            aiType);

        // Assert
        Assert.NotNull(match.TimeControl);
        Assert.Equal(TimeControlType.ChicagoPoint, match.TimeControl.Type);
        Assert.Equal(12, match.TimeControl.DelaySeconds);
    }

    [Fact]
    public async Task CreateMatch_OpenLobby_EnforcesChicagoPoint()
    {
        // Arrange
        var playerId = "player1";
        var targetScore = 7;

        _mockUserRepo.Setup(x => x.GetByUserIdAsync(playerId))
            .ReturnsAsync(new User { UserId = playerId, Username = "TestPlayer", DisplayName = "Test Player" });

        // Act
        var (match, game) = await _matchService.CreateMatchAsync(
            playerId,
            targetScore,
            "OpenLobby",
            "Test Player",
            null,
            null, // No time control specified - should default to ChicagoPoint
            true,
            "greedy");

        // Assert
        Assert.NotNull(match.TimeControl);
        Assert.Equal(TimeControlType.ChicagoPoint, match.TimeControl.Type);
        Assert.Equal(12, match.TimeControl.DelaySeconds);
        Assert.True(match.IsOpenLobby);
    }

    [Fact]
    public async Task CreateMatch_Friend_AlwaysChicagoPoint()
    {
        // Arrange
        var player1Id = "player1";
        var player2Id = "player2";
        var targetScore = 5;

        _mockUserRepo.Setup(x => x.GetByUserIdAsync(player1Id))
            .ReturnsAsync(new User { UserId = player1Id, Username = "Player1", DisplayName = "Player One" });
        _mockUserRepo.Setup(x => x.GetByUserIdAsync(player2Id))
            .ReturnsAsync(new User { UserId = player2Id, Username = "Player2", DisplayName = "Player Two" });

        // Act
        var (match, game) = await _matchService.CreateMatchAsync(
            player1Id,
            targetScore,
            "Friend",
            "Player One",
            player2Id, // Friend opponent
            null, // No time control specified
            true,
            "greedy");

        // Assert
        Assert.NotNull(match.TimeControl);
        Assert.Equal(TimeControlType.ChicagoPoint, match.TimeControl.Type);
        Assert.Equal(12, match.TimeControl.DelaySeconds);
    }

    [Fact]
    public async Task CreateMatch_ExplicitChicagoPoint_Preserved()
    {
        // Arrange
        var playerId = "player1";
        var targetScore = 5;
        var explicitTimeControl = new TimeControlConfig
        {
            Type = TimeControlType.ChicagoPoint,
            DelaySeconds = 12
        };

        _mockUserRepo.Setup(x => x.GetByUserIdAsync(playerId))
            .ReturnsAsync(new User { UserId = playerId, Username = "TestPlayer", DisplayName = "Test Player" });

        // Act
        var (match, game) = await _matchService.CreateMatchAsync(
            playerId,
            targetScore,
            "OpenLobby",
            "Test Player",
            null,
            explicitTimeControl, // Explicitly specify ChicagoPoint
            true,
            "greedy");

        // Assert
        Assert.NotNull(match.TimeControl);
        Assert.Equal(TimeControlType.ChicagoPoint, match.TimeControl.Type);
        Assert.Equal(12, match.TimeControl.DelaySeconds);
    }
}
