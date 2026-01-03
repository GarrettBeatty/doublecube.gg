using Backgammon.Server.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Match = Backgammon.Server.Models.Match;
using MatchConfig = Backgammon.Server.Models.MatchConfig;

namespace Backgammon.Tests;

public class MatchLobbyServiceTests
{
    private readonly Mock<IMatchService> _matchServiceMock;
    private readonly Mock<IAiMoveService> _aiMoveServiceMock;
    private readonly Mock<IGameSessionManager> _sessionManagerMock;
    private readonly Mock<ILogger<MatchLobbyService>> _loggerMock;
    private readonly MatchLobbyService _service;

    public MatchLobbyServiceTests()
    {
        _matchServiceMock = new Mock<IMatchService>();
        _aiMoveServiceMock = new Mock<IAiMoveService>();
        _sessionManagerMock = new Mock<IGameSessionManager>();
        _loggerMock = new Mock<ILogger<MatchLobbyService>>();

        _service = new MatchLobbyService(
            _matchServiceMock.Object,
            _aiMoveServiceMock.Object,
            _sessionManagerMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CreateMatchLobbyAsync_OpenLobby_CreatesMatch()
    {
        // Arrange
        var playerId = "player123";
        var config = new MatchConfig
        {
            OpponentType = "OpenLobby",
            TargetScore = 7
        };
        var expectedMatch = new Match
        {
            MatchId = "match123",
            Player1Id = playerId,
            TargetScore = 7,
            IsOpenLobby = true
        };

        _matchServiceMock.Setup(m => m.CreateMatchLobbyAsync(
                playerId, 7, "OpenLobby", true, null, null))
            .ReturnsAsync(expectedMatch);

        // Act
        var result = await _service.CreateMatchLobbyAsync(playerId, config, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("match123", result.MatchId);
        Assert.True(result.IsOpenLobby);
    }

    [Fact]
    public async Task JoinMatchLobbyAsync_MatchNotFound_ReturnsError()
    {
        // Arrange
        _matchServiceMock.Setup(m => m.GetMatchLobbyAsync("nonexistent"))
            .ReturnsAsync((Match?)null);

        // Act
        var (success, match, error) = await _service.JoinMatchLobbyAsync("nonexistent", "player123", null);

        // Assert
        Assert.False(success);
        Assert.Null(match);
        Assert.Equal("Match not found", error);
    }

    [Fact]
    public async Task JoinMatchLobbyAsync_PlayerIsCreator_ReturnsCurrentState()
    {
        // Arrange
        var matchId = "match123";
        var playerId = "player123";
        var existingMatch = new Match
        {
            MatchId = matchId,
            Player1Id = playerId,
            IsOpenLobby = true
        };

        _matchServiceMock.Setup(m => m.GetMatchLobbyAsync(matchId))
            .ReturnsAsync(existingMatch);

        // Act
        var (success, match, error) = await _service.JoinMatchLobbyAsync(matchId, playerId, null);

        // Assert
        Assert.True(success);
        Assert.NotNull(match);
        Assert.Null(error);
        Assert.Equal(matchId, match.MatchId);
    }

    [Fact]
    public async Task JoinMatchLobbyAsync_OpenLobbyWithEmptySlot_JoinsSuccessfully()
    {
        // Arrange
        var matchId = "match123";
        var playerId = "player456";
        var existingMatch = new Match
        {
            MatchId = matchId,
            Player1Id = "player123",
            Player2Id = string.Empty,
            IsOpenLobby = true
        };
        var updatedMatch = new Match
        {
            MatchId = matchId,
            Player1Id = "player123",
            Player2Id = playerId,
            IsOpenLobby = true
        };

        _matchServiceMock.Setup(m => m.GetMatchLobbyAsync(matchId))
            .ReturnsAsync(existingMatch);
        _matchServiceMock.Setup(m => m.JoinOpenLobbyAsync(matchId, playerId, null))
            .ReturnsAsync(updatedMatch);

        // Act
        var (success, match, error) = await _service.JoinMatchLobbyAsync(matchId, playerId, null);

        // Assert
        Assert.True(success);
        Assert.NotNull(match);
        Assert.Null(error);
        Assert.Equal(playerId, match.Player2Id);
    }

    [Fact]
    public async Task StartMatchGameAsync_NotCreator_ReturnsError()
    {
        // Arrange
        var matchId = "match123";
        var playerId = "not-creator";
        var match = new Match
        {
            MatchId = matchId,
            Player1Id = "player123", // Different from playerId
            Player2Id = playerId
        };

        _matchServiceMock.Setup(m => m.GetMatchLobbyAsync(matchId))
            .ReturnsAsync(match);

        // Act
        var (success, game, resultMatch, error) = await _service.StartMatchGameAsync(matchId, playerId);

        // Assert
        Assert.False(success);
        Assert.Null(game);
        Assert.Null(resultMatch);
        Assert.Equal("Only the match creator can start the game", error);
    }

    [Fact]
    public async Task StartMatchGameAsync_NoOpponent_ReturnsError()
    {
        // Arrange
        var matchId = "match123";
        var playerId = "player123";
        var match = new Match
        {
            MatchId = matchId,
            Player1Id = playerId,
            Player2Id = string.Empty // No opponent
        };

        _matchServiceMock.Setup(m => m.GetMatchLobbyAsync(matchId))
            .ReturnsAsync(match);

        // Act
        var (success, game, resultMatch, error) = await _service.StartMatchGameAsync(matchId, playerId);

        // Assert
        Assert.False(success);
        Assert.Null(game);
        Assert.Null(resultMatch);
        Assert.Equal("Waiting for opponent to join", error);
    }

    [Fact]
    public async Task LeaveMatchLobbyAsync_CallsMatchService()
    {
        // Arrange
        var matchId = "match123";
        var playerId = "player123";

        _matchServiceMock.Setup(m => m.LeaveMatchLobbyAsync(matchId, playerId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.LeaveMatchLobbyAsync(matchId, playerId);

        // Assert
        Assert.True(result);
        _matchServiceMock.Verify(m => m.LeaveMatchLobbyAsync(matchId, playerId), Times.Once);
    }
}
