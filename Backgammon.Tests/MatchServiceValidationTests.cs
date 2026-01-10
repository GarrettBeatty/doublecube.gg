using Backgammon.Server.Models;
using Backgammon.Server.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ServerMatch = Backgammon.Server.Models.Match;

namespace Backgammon.Tests;

public class MatchServiceValidationTests
{
    private readonly Mock<IMatchRepository> _matchRepositoryMock;
    private readonly Mock<IGameRepository> _gameRepositoryMock;
    private readonly Mock<IGameSessionManager> _gameSessionManagerMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IAiMoveService> _aiMoveServiceMock;
    private readonly Mock<ICorrespondenceGameService> _correspondenceGameServiceMock;
    private readonly Mock<ILogger<MatchService>> _loggerMock;
    private readonly MatchService _matchService;

    public MatchServiceValidationTests()
    {
        _matchRepositoryMock = new Mock<IMatchRepository>();
        _gameRepositoryMock = new Mock<IGameRepository>();
        _gameSessionManagerMock = new Mock<IGameSessionManager>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _aiMoveServiceMock = new Mock<IAiMoveService>();
        _correspondenceGameServiceMock = new Mock<ICorrespondenceGameService>();
        _loggerMock = new Mock<ILogger<MatchService>>();

        // Setup GameSessionManager to return a valid session
        _gameSessionManagerMock.Setup(x => x.CreateGame(It.IsAny<string>()))
            .Returns((string gameId) => new GameSession(gameId));

        _matchService = new MatchService(
            _matchRepositoryMock.Object,
            _gameRepositoryMock.Object,
            _gameSessionManagerMock.Object,
            _userRepositoryMock.Object,
            _aiMoveServiceMock.Object,
            _correspondenceGameServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CreateMatchAsync_ThrowsException_WhenPlayer1IdIsNull()
    {
        // Arrange
        string? player1Id = null;
        var player2Id = "player2";
        var targetScore = 7;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _matchService.CreateMatchAsync(player1Id!, targetScore, "Friend", null, player2Id));
        Assert.Contains("Player IDs cannot be null or empty", exception.Message);
    }

    [Fact]
    public async Task CreateMatchAsync_ThrowsException_WhenPlayer1IdIsEmpty()
    {
        // Arrange
        var player1Id = string.Empty;
        var player2Id = "player2";
        var targetScore = 7;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _matchService.CreateMatchAsync(player1Id, targetScore, "Friend", null, player2Id));
        Assert.Contains("Player IDs cannot be null or empty", exception.Message);
    }

    [Fact]
    public async Task CreateMatchAsync_ThrowsException_WhenPlayer1IdIsWhitespace()
    {
        // Arrange
        var player1Id = "   ";
        var player2Id = "player2";
        var targetScore = 7;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _matchService.CreateMatchAsync(player1Id, targetScore, "Friend", null, player2Id));
        Assert.Contains("Player IDs cannot be null or empty", exception.Message);
    }

    [Fact]
    public async Task CreateMatchAsync_ThrowsException_WhenPlayer2IdIsNull()
    {
        // Arrange
        var player1Id = "player1";
        string? player2Id = null;
        var targetScore = 7;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _matchService.CreateMatchAsync(player1Id, targetScore, "Friend", null, player2Id!));
        Assert.Contains("Player IDs cannot be null or empty", exception.Message);
    }

    [Fact]
    public async Task CreateMatchAsync_ThrowsException_WhenPlayer2IdIsEmpty()
    {
        // Arrange
        var player1Id = "player1";
        var player2Id = string.Empty;
        var targetScore = 7;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _matchService.CreateMatchAsync(player1Id, targetScore, "Friend", null, player2Id));
        Assert.Contains("Player IDs cannot be null or empty", exception.Message);
    }

    [Fact]
    public async Task CreateMatchAsync_ThrowsException_WhenTargetScoreIsZero()
    {
        // Arrange
        var player1Id = "player1";
        var player2Id = "player2";
        var targetScore = 0;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _matchService.CreateMatchAsync(player1Id, targetScore, "Friend", null, player2Id));
        Assert.Contains("Target score must be between 1 and 25", exception.Message);
    }

    [Fact]
    public async Task CreateMatchAsync_ThrowsException_WhenTargetScoreIsNegative()
    {
        // Arrange
        var player1Id = "player1";
        var player2Id = "player2";
        var targetScore = -5;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _matchService.CreateMatchAsync(player1Id, targetScore, "Friend", null, player2Id));
        Assert.Contains("Target score must be between 1 and 25", exception.Message);
    }

    [Fact]
    public async Task CreateMatchAsync_ThrowsException_WhenTargetScoreExceeds25()
    {
        // Arrange
        var player1Id = "player1";
        var player2Id = "player2";
        var targetScore = 26;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _matchService.CreateMatchAsync(player1Id, targetScore, "Friend", null, player2Id));
        Assert.Contains("Target score must be between 1 and 25", exception.Message);
    }

    [Fact]
    public async Task CreateMatchAsync_AcceptsTargetScore_WhenExactly1()
    {
        // Arrange
        var player1Id = "player1";
        var player2Id = "player2";
        var targetScore = 1;

        _userRepositoryMock.Setup(x => x.GetByUserIdAsync(player1Id))
            .ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(x => x.GetByUserIdAsync(player2Id))
            .ReturnsAsync((User?)null);
        _matchRepositoryMock.Setup(x => x.SaveMatchAsync(It.IsAny<ServerMatch>()))
            .Returns(Task.CompletedTask);

        // Act
        var (match, firstGame) = await _matchService.CreateMatchAsync(player1Id, targetScore, "Friend", null, player2Id);

        // Assert
        Assert.Equal(targetScore, match.TargetScore);
    }

    [Fact]
    public async Task CreateMatchAsync_AcceptsTargetScore_WhenExactly25()
    {
        // Arrange
        var player1Id = "player1";
        var player2Id = "player2";
        var targetScore = 25;

        _userRepositoryMock.Setup(x => x.GetByUserIdAsync(player1Id))
            .ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(x => x.GetByUserIdAsync(player2Id))
            .ReturnsAsync((User?)null);
        _matchRepositoryMock.Setup(x => x.SaveMatchAsync(It.IsAny<ServerMatch>()))
            .Returns(Task.CompletedTask);

        // Act
        var (match, firstGame) = await _matchService.CreateMatchAsync(player1Id, targetScore, "Friend", null, player2Id);

        // Assert
        Assert.Equal(targetScore, match.TargetScore);
    }

    [Fact]
    public async Task CreateMatchAsync_ThrowsException_WhenPlayerIdsAreIdentical()
    {
        // Arrange
        var playerId = "player1";
        var targetScore = 7;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _matchService.CreateMatchAsync(playerId, targetScore, "Friend", null, playerId));
        Assert.Contains("Player IDs cannot be identical", exception.Message);
    }

    [Fact]
    public async Task CreateMatchAsync_HandlesShortPlayerIds_WithoutSubstringError()
    {
        // Arrange - Test player IDs shorter than 8 characters
        var player1Id = "p1";
        var player2Id = "p2";
        var targetScore = 7;

        _userRepositoryMock.Setup(x => x.GetByUserIdAsync(player1Id))
            .ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(x => x.GetByUserIdAsync(player2Id))
            .ReturnsAsync((User?)null);
        _matchRepositoryMock.Setup(x => x.SaveMatchAsync(It.IsAny<ServerMatch>()))
            .Returns(Task.CompletedTask);

        // Act
        var (match, firstGame) = await _matchService.CreateMatchAsync(player1Id, targetScore, "Friend", null, player2Id);

        // Assert
        Assert.Equal("Unknown", match.Player1Name);
        Assert.Equal("Unknown", match.Player2Name);
    }

    [Fact]
    public async Task CreateMatchAsync_UsesSubstring_ForLongPlayerIds()
    {
        // Arrange - Test player IDs longer than 8 characters
        var player1Id = "player123456789";
        var player2Id = "player987654321";
        var targetScore = 7;

        _userRepositoryMock.Setup(x => x.GetByUserIdAsync(player1Id))
            .ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(x => x.GetByUserIdAsync(player2Id))
            .ReturnsAsync((User?)null);
        _matchRepositoryMock.Setup(x => x.SaveMatchAsync(It.IsAny<ServerMatch>()))
            .Returns(Task.CompletedTask);

        // Act
        var (match, firstGame) = await _matchService.CreateMatchAsync(player1Id, targetScore, "Friend", null, player2Id);

        // Assert
        Assert.Equal("Unknown", match.Player1Name);
        Assert.Equal("Unknown", match.Player2Name);
    }

    [Fact]
    public async Task CreateMatchAsync_UsesDisplayName_WhenUserExists()
    {
        // Arrange
        var player1Id = "player1";
        var player2Id = "player2";
        var targetScore = 7;

        var user1 = new User { UserId = player1Id, DisplayName = "Alice" };
        var user2 = new User { UserId = player2Id, DisplayName = "Bob" };

        _userRepositoryMock.Setup(x => x.GetByUserIdAsync(player1Id))
            .ReturnsAsync(user1);
        _userRepositoryMock.Setup(x => x.GetByUserIdAsync(player2Id))
            .ReturnsAsync(user2);
        _matchRepositoryMock.Setup(x => x.SaveMatchAsync(It.IsAny<ServerMatch>()))
            .Returns(Task.CompletedTask);

        // Act
        var (match, firstGame) = await _matchService.CreateMatchAsync(player1Id, targetScore, "Friend", null, player2Id);

        // Assert
        Assert.Equal("Alice", match.Player1Name);
        Assert.Equal("Bob", match.Player2Name);
    }

    [Fact]
    public async Task CreateMatchAsync_CreatesValidMatch_WithValidInputs()
    {
        // Arrange
        var player1Id = "player1";
        var player2Id = "player2";
        var targetScore = 7;

        _userRepositoryMock.Setup(x => x.GetByUserIdAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);
        _matchRepositoryMock.Setup(x => x.SaveMatchAsync(It.IsAny<ServerMatch>()))
            .Returns(Task.CompletedTask);

        // Act
        var (match, firstGame) = await _matchService.CreateMatchAsync(player1Id, targetScore, "Friend", null, player2Id);

        // Assert
        Assert.NotNull(match.MatchId);
        Assert.Equal(player1Id, match.Player1Id);
        Assert.Equal(player2Id, match.Player2Id);
        Assert.Equal(targetScore, match.TargetScore);
        Assert.Equal("InProgress", match.Status);

        // Verify repository was called
        _matchRepositoryMock.Verify(x => x.SaveMatchAsync(It.IsAny<ServerMatch>()), Times.Once);
    }

    [Fact]
    public async Task CreateMatchAsync_LogsError_WhenExceptionOccurs()
    {
        // Arrange
        var player1Id = "player1";
        var player2Id = "player2";
        var targetScore = 7;

        _userRepositoryMock.Setup(x => x.GetByUserIdAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(
            () => _matchService.CreateMatchAsync(player1Id, targetScore, "Friend", null, player2Id));

        // Verify error was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to create match")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(7)]
    [InlineData(11)]
    [InlineData(15)]
    [InlineData(25)]
    public async Task CreateMatchAsync_AcceptsCommonTargetScores(int targetScore)
    {
        // Arrange
        var player1Id = "player1";
        var player2Id = "player2";

        _userRepositoryMock.Setup(x => x.GetByUserIdAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);
        _matchRepositoryMock.Setup(x => x.SaveMatchAsync(It.IsAny<ServerMatch>()))
            .Returns(Task.CompletedTask);

        // Act
        var (match, firstGame) = await _matchService.CreateMatchAsync(player1Id, targetScore, "Friend", null, player2Id);

        // Assert
        Assert.Equal(targetScore, match.TargetScore);
    }
}
