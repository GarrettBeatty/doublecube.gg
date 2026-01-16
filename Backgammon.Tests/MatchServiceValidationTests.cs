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
    private readonly Mock<IGameSessionFactory> _sessionFactoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IAiMoveService> _aiMoveServiceMock;
    private readonly Mock<IAiPlayerManager> _aiPlayerManagerMock;
    private readonly Mock<ICorrespondenceGameService> _correspondenceGameServiceMock;
    private readonly Mock<IChatService> _chatServiceMock;
    private readonly Mock<ILogger<MatchService>> _loggerMock;
    private readonly MatchService _matchService;

    public MatchServiceValidationTests()
    {
        _matchRepositoryMock = new Mock<IMatchRepository>();
        _gameRepositoryMock = new Mock<IGameRepository>();
        _gameSessionManagerMock = new Mock<IGameSessionManager>();
        _sessionFactoryMock = new Mock<IGameSessionFactory>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _aiMoveServiceMock = new Mock<IAiMoveService>();
        _aiPlayerManagerMock = new Mock<IAiPlayerManager>();
        _correspondenceGameServiceMock = new Mock<ICorrespondenceGameService>();
        _chatServiceMock = new Mock<IChatService>();
        _loggerMock = new Mock<ILogger<MatchService>>();

        // Setup GameSessionManager to return a valid session
        _gameSessionManagerMock.Setup(x => x.CreateGame(It.IsAny<string>()))
            .Returns((string gameId) => new GameSession(gameId));

        // Setup SessionFactory to return a valid session
        _sessionFactoryMock.Setup(x => x.CreateMatchGameSession(It.IsAny<ServerMatch>(), It.IsAny<string>()))
            .Returns((ServerMatch match, string gameId) => new GameSession(gameId));

        // Setup AiPlayerManager to return consistent AI player IDs and names
        _aiPlayerManagerMock.Setup(x => x.GetOrCreateAiForMatch(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string matchId, string aiType) => $"ai_{aiType.ToLower()}_{Guid.NewGuid()}");
        _aiPlayerManagerMock.Setup(x => x.GetAiNameForMatch(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string matchId, string aiType) => $"{aiType} Bot");

        _matchService = new MatchService(
            _matchRepositoryMock.Object,
            _gameRepositoryMock.Object,
            _gameSessionManagerMock.Object,
            _sessionFactoryMock.Object,
            _userRepositoryMock.Object,
            _aiMoveServiceMock.Object,
            _aiPlayerManagerMock.Object,
            _correspondenceGameServiceMock.Object,
            _chatServiceMock.Object,
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

    [Fact]
    public async Task CreateMatchAsync_InvalidOpponentType_ThrowsException()
    {
        // Arrange
        var player1Id = "player1";
        var targetScore = 7;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _matchService.CreateMatchAsync(player1Id, targetScore, "InvalidType"));
        Assert.Contains("OpponentType must be 'AI', 'OpenLobby', or 'Friend'", exception.Message);
    }

    [Fact]
    public async Task CreateMatchAsync_AI_CreatesAIMatch()
    {
        // Arrange
        var player1Id = "player1";
        var targetScore = 7;

        _userRepositoryMock.Setup(x => x.GetByUserIdAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);
        _matchRepositoryMock.Setup(x => x.SaveMatchAsync(It.IsAny<ServerMatch>()))
            .Returns(Task.CompletedTask);

        // Act
        var (match, firstGame) = await _matchService.CreateMatchAsync(player1Id, targetScore, "AI");

        // Assert
        Assert.NotNull(match.Player2Id);
        Assert.Equal("InProgress", match.Status);
        Assert.False(match.IsOpenLobby);
        Assert.False(match.IsRated); // AI matches are always unrated
    }

    [Fact]
    public async Task CreateMatchAsync_OpenLobby_CreatesWaitingMatch()
    {
        // Arrange
        var player1Id = "player1";
        var targetScore = 7;

        _userRepositoryMock.Setup(x => x.GetByUserIdAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);
        _matchRepositoryMock.Setup(x => x.SaveMatchAsync(It.IsAny<ServerMatch>()))
            .Returns(Task.CompletedTask);

        // Act
        var (match, firstGame) = await _matchService.CreateMatchAsync(player1Id, targetScore, "OpenLobby");

        // Assert
        Assert.True(string.IsNullOrEmpty(match.Player2Id)); // OpenLobby has no Player2 yet
        Assert.Equal("WaitingForPlayers", match.Status);
        Assert.True(match.IsOpenLobby);
    }

    [Fact]
    public async Task GetMatchAsync_ReturnsMatch()
    {
        // Arrange
        var matchId = "match-123";
        var expectedMatch = new ServerMatch { MatchId = matchId };
        _matchRepositoryMock.Setup(x => x.GetMatchByIdAsync(matchId))
            .ReturnsAsync(expectedMatch);

        // Act
        var result = await _matchService.GetMatchAsync(matchId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(matchId, result.MatchId);
    }

    [Fact]
    public async Task GetMatchAsync_ReturnsNullForNonexistent()
    {
        // Arrange
        var matchId = "nonexistent-match";
        _matchRepositoryMock.Setup(x => x.GetMatchByIdAsync(matchId))
            .ReturnsAsync((ServerMatch?)null);

        // Act
        var result = await _matchService.GetMatchAsync(matchId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateMatchAsync_CallsRepository()
    {
        // Arrange
        var match = new ServerMatch { MatchId = "match-123" };

        // Act
        await _matchService.UpdateMatchAsync(match);

        // Assert
        _matchRepositoryMock.Verify(x => x.UpdateMatchAsync(match), Times.Once);
    }

    [Fact]
    public async Task StartNextGameAsync_MatchNotFound_ThrowsException()
    {
        // Arrange
        var matchId = "nonexistent";
        _matchRepositoryMock.Setup(x => x.GetMatchByIdAsync(matchId))
            .ReturnsAsync((ServerMatch?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _matchService.StartNextGameAsync(matchId));
        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task StartNextGameAsync_MatchCompleted_ThrowsException()
    {
        // Arrange
        var matchId = "match-123";
        var match = new ServerMatch { MatchId = matchId };
        match.CoreMatch.Status = Backgammon.Core.MatchStatus.Completed;

        _matchRepositoryMock.Setup(x => x.GetMatchByIdAsync(matchId))
            .ReturnsAsync(match);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _matchService.StartNextGameAsync(matchId));
        Assert.Contains("Cannot start new game in completed match", exception.Message);
    }

    [Fact]
    public async Task StartNextGameAsync_Success_CreatesGame()
    {
        // Arrange
        var matchId = "match-123";
        var match = new ServerMatch
        {
            MatchId = matchId,
            Player1Id = "player1",
            Player2Id = "player2",
            Player1Name = "Player 1",
            Player2Name = "Player 2"
        };
        match.CoreMatch.Status = Backgammon.Core.MatchStatus.InProgress;

        _matchRepositoryMock.Setup(x => x.GetMatchByIdAsync(matchId))
            .ReturnsAsync(match);

        // Act
        var game = await _matchService.StartNextGameAsync(matchId);

        // Assert
        Assert.NotNull(game);
        Assert.Equal(matchId, game.MatchId);
        Assert.Equal("player1", game.WhitePlayerId);
        Assert.Equal("player2", game.RedPlayerId);
        _gameRepositoryMock.Verify(x => x.SaveGameAsync(It.IsAny<Backgammon.Server.Models.Game>()), Times.Once);
        _matchRepositoryMock.Verify(x => x.AddGameToMatchAsync(matchId, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task IsMatchCompleteAsync_Completed_ReturnsTrue()
    {
        // Arrange
        var matchId = "match-123";
        var match = new ServerMatch { MatchId = matchId };
        match.CoreMatch.Status = Backgammon.Core.MatchStatus.Completed;

        _matchRepositoryMock.Setup(x => x.GetMatchByIdAsync(matchId))
            .ReturnsAsync(match);

        // Act
        var result = await _matchService.IsMatchCompleteAsync(matchId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsMatchCompleteAsync_InProgress_ReturnsFalse()
    {
        // Arrange
        var matchId = "match-123";
        var match = new ServerMatch { MatchId = matchId };
        match.CoreMatch.Status = Backgammon.Core.MatchStatus.InProgress;

        _matchRepositoryMock.Setup(x => x.GetMatchByIdAsync(matchId))
            .ReturnsAsync(match);

        // Act
        var result = await _matchService.IsMatchCompleteAsync(matchId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetPlayerMatchesAsync_CallsRepository()
    {
        // Arrange
        var playerId = "player1";
        var matches = new List<ServerMatch>
        {
            new() { MatchId = "match-1" },
            new() { MatchId = "match-2" }
        };
        _matchRepositoryMock.Setup(x => x.GetPlayerMatchesAsync(playerId, null))
            .ReturnsAsync(matches);

        // Act
        var result = await _matchService.GetPlayerMatchesAsync(playerId);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetActiveMatchesAsync_CallsRepository()
    {
        // Arrange
        var matches = new List<ServerMatch>
        {
            new() { MatchId = "match-1" }
        };
        _matchRepositoryMock.Setup(x => x.GetActiveMatchesAsync())
            .ReturnsAsync(matches);

        // Act
        var result = await _matchService.GetActiveMatchesAsync();

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public async Task GetOpenLobbiesAsync_CallsRepository()
    {
        // Arrange
        var matches = new List<ServerMatch>
        {
            new() { MatchId = "lobby-1", IsOpenLobby = true }
        };
        _matchRepositoryMock.Setup(x => x.GetOpenLobbiesAsync(50, null))
            .ReturnsAsync(matches);

        // Act
        var result = await _matchService.GetOpenLobbiesAsync();

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public async Task GetRegularLobbiesAsync_CallsRepositoryWithFalse()
    {
        // Arrange
        var matches = new List<ServerMatch>();
        _matchRepositoryMock.Setup(x => x.GetOpenLobbiesAsync(50, false))
            .ReturnsAsync(matches);

        // Act
        await _matchService.GetRegularLobbiesAsync();

        // Assert
        _matchRepositoryMock.Verify(x => x.GetOpenLobbiesAsync(50, false), Times.Once);
    }

    [Fact]
    public async Task GetCorrespondenceLobbiesAsync_CallsRepositoryWithTrue()
    {
        // Arrange
        var matches = new List<ServerMatch>();
        _matchRepositoryMock.Setup(x => x.GetOpenLobbiesAsync(50, true))
            .ReturnsAsync(matches);

        // Act
        await _matchService.GetCorrespondenceLobbiesAsync();

        // Assert
        _matchRepositoryMock.Verify(x => x.GetOpenLobbiesAsync(50, true), Times.Once);
    }

    [Fact]
    public async Task AbandonMatchAsync_MatchNotFound_ReturnsEarly()
    {
        // Arrange
        var matchId = "nonexistent";
        _matchRepositoryMock.Setup(x => x.GetMatchByIdAsync(matchId))
            .ReturnsAsync((ServerMatch?)null);

        // Act
        await _matchService.AbandonMatchAsync(matchId, "player1");

        // Assert
        _matchRepositoryMock.Verify(x => x.UpdateMatchAsync(It.IsAny<ServerMatch>()), Times.Never);
    }

    [Fact]
    public async Task AbandonMatchAsync_MatchCompleted_ReturnsEarly()
    {
        // Arrange
        var matchId = "match-123";
        var match = new ServerMatch { MatchId = matchId };
        match.CoreMatch.Status = Backgammon.Core.MatchStatus.Completed;

        _matchRepositoryMock.Setup(x => x.GetMatchByIdAsync(matchId))
            .ReturnsAsync(match);

        // Act
        await _matchService.AbandonMatchAsync(matchId, "player1");

        // Assert
        _matchRepositoryMock.Verify(x => x.UpdateMatchAsync(It.IsAny<ServerMatch>()), Times.Never);
    }

    [Fact]
    public async Task AbandonMatchAsync_InProgress_SetsOpponentAsWinner()
    {
        // Arrange
        var matchId = "match-123";
        var match = new ServerMatch
        {
            MatchId = matchId,
            Player1Id = "player1",
            Player2Id = "player2"
        };
        match.CoreMatch.Status = Backgammon.Core.MatchStatus.InProgress;

        _matchRepositoryMock.Setup(x => x.GetMatchByIdAsync(matchId))
            .ReturnsAsync(match);

        // Act
        await _matchService.AbandonMatchAsync(matchId, "player1");

        // Assert
        Assert.Equal("Abandoned", match.Status);
        Assert.Equal("player2", match.WinnerId); // Opponent wins
        _matchRepositoryMock.Verify(x => x.UpdateMatchAsync(match), Times.Once);
    }

    [Fact]
    public async Task AbandonMatchAsync_WaitingNoPlayer2_NoWinner()
    {
        // Arrange
        var matchId = "match-123";
        var match = new ServerMatch
        {
            MatchId = matchId,
            Player1Id = "player1"
        };
        match.CoreMatch.Status = Backgammon.Core.MatchStatus.WaitingForPlayers;

        _matchRepositoryMock.Setup(x => x.GetMatchByIdAsync(matchId))
            .ReturnsAsync(match);

        // Act
        await _matchService.AbandonMatchAsync(matchId, "player1");

        // Assert
        Assert.Equal("Abandoned", match.Status);
        Assert.Null(match.WinnerId); // No winner if match never started
    }

    [Fact]
    public async Task GetPlayerMatchStatsAsync_CallsRepository()
    {
        // Arrange
        var playerId = "player1";
        var stats = new MatchStats { TotalMatches = 10, MatchesWon = 5 };
        _matchRepositoryMock.Setup(x => x.GetPlayerMatchStatsAsync(playerId))
            .ReturnsAsync(stats);

        // Act
        var result = await _matchService.GetPlayerMatchStatsAsync(playerId);

        // Assert
        Assert.Equal(10, result.TotalMatches);
        Assert.Equal(5, result.MatchesWon);
    }

    [Fact]
    public async Task JoinMatchAsync_MatchNotFound_ThrowsException()
    {
        // Arrange
        var matchId = "nonexistent";
        _matchRepositoryMock.Setup(x => x.GetMatchByIdAsync(matchId))
            .ReturnsAsync((ServerMatch?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _matchService.JoinMatchAsync(matchId, "player2"));
        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task JoinMatchAsync_AIMatch_ThrowsException()
    {
        // Arrange
        var matchId = "match-123";
        var match = new ServerMatch
        {
            MatchId = matchId,
            OpponentType = "AI"
        };

        _matchRepositoryMock.Setup(x => x.GetMatchByIdAsync(matchId))
            .ReturnsAsync(match);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _matchService.JoinMatchAsync(matchId, "player2"));
        Assert.Contains("does not allow joining", exception.Message);
    }

    [Fact]
    public async Task JoinMatchAsync_AlreadyHasPlayer2_ThrowsException()
    {
        // Arrange
        var matchId = "match-123";
        var match = new ServerMatch
        {
            MatchId = matchId,
            OpponentType = "OpenLobby",
            Player2Id = "existing-player"
        };

        _matchRepositoryMock.Setup(x => x.GetMatchByIdAsync(matchId))
            .ReturnsAsync(match);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _matchService.JoinMatchAsync(matchId, "player2"));
        Assert.Contains("already has a second player", exception.Message);
    }

    [Fact]
    public async Task JoinMatchAsync_NotWaiting_ThrowsException()
    {
        // Arrange
        var matchId = "match-123";
        var match = new ServerMatch
        {
            MatchId = matchId,
            OpponentType = "OpenLobby"
        };
        match.CoreMatch.Status = Backgammon.Core.MatchStatus.InProgress;

        _matchRepositoryMock.Setup(x => x.GetMatchByIdAsync(matchId))
            .ReturnsAsync(match);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _matchService.JoinMatchAsync(matchId, "player2"));
        Assert.Contains("not accepting players", exception.Message);
    }

    [Fact]
    public async Task JoinMatchAsync_Success_SetsPlayer2AndInProgress()
    {
        // Arrange
        var matchId = "match-123";
        var player2Id = "player2";
        var match = new ServerMatch
        {
            MatchId = matchId,
            Player1Id = "player1",
            OpponentType = "OpenLobby"
        };
        match.CoreMatch.Status = Backgammon.Core.MatchStatus.WaitingForPlayers;

        var user2 = new User { UserId = player2Id, DisplayName = "Player Two" };

        _matchRepositoryMock.Setup(x => x.GetMatchByIdAsync(matchId))
            .ReturnsAsync(match);
        _userRepositoryMock.Setup(x => x.GetByUserIdAsync(player2Id))
            .ReturnsAsync(user2);

        // Act
        var result = await _matchService.JoinMatchAsync(matchId, player2Id);

        // Assert
        Assert.Equal(player2Id, result.Player2Id);
        Assert.Equal("Player Two", result.Player2Name);
        Assert.Equal("InProgress", result.Status);
        _matchRepositoryMock.Verify(x => x.UpdateMatchAsync(match), Times.Once);
        _matchRepositoryMock.Verify(
            x => x.CreatePlayerMatchIndexAsync(
                player2Id,
                matchId,
                match.Player1Id,
                "InProgress",
                match.CreatedAt),
            Times.Once);
    }
}
