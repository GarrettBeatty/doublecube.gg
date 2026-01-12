using Backgammon.Core;
using Backgammon.Server.Models;
using Backgammon.Server.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ServerGame = Backgammon.Server.Models.Game;
using ServerMatch = Backgammon.Server.Models.Match;

namespace Backgammon.Tests;

/// <summary>
/// Integration tests for match game continuation and score tracking.
/// Tests the flow of completing one game and starting the next game in a match.
/// </summary>
public class MatchGameContinuationTests
{
    private readonly Mock<IMatchRepository> _matchRepositoryMock;
    private readonly Mock<IGameRepository> _gameRepositoryMock;
    private readonly Mock<IGameSessionManager> _gameSessionManagerMock;
    private readonly Mock<IGameSessionFactory> _sessionFactoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IAiMoveService> _aiMoveServiceMock;
    private readonly Mock<ICorrespondenceGameService> _correspondenceGameServiceMock;
    private readonly Mock<ILogger<MatchService>> _loggerMock;
    private readonly MatchService _matchService;

    public MatchGameContinuationTests()
    {
        _matchRepositoryMock = new Mock<IMatchRepository>();
        _gameRepositoryMock = new Mock<IGameRepository>();
        _gameSessionManagerMock = new Mock<IGameSessionManager>();
        _sessionFactoryMock = new Mock<IGameSessionFactory>();
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
            _sessionFactoryMock.Object,
            _userRepositoryMock.Object,
            _aiMoveServiceMock.Object,
            _correspondenceGameServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CompleteGameAsync_UpdatesMatchScores_WhenGameCompletes()
    {
        // Arrange
        var matchId = Guid.NewGuid().ToString();
        var gameId = Guid.NewGuid().ToString();
        var player1Id = "player1";
        var player2Id = "ai_opponent";

        var match = new ServerMatch
        {
            MatchId = matchId,
            Player1Id = player1Id,
            Player2Id = player2Id,
            Player1Name = "Player 1",
            Player2Name = "Computer",
            TargetScore = 5,
            Player1Score = 0,
            Player2Score = 0,
            Status = "InProgress",
            IsCrawfordGame = false,
            OpponentType = "AI"
        };

        var game = new ServerGame
        {
            GameId = gameId,
            MatchId = matchId,
            Status = "Completed",
            WhitePlayerId = player1Id,
            RedPlayerId = player2Id
        };

        // Player 1 wins with gammon (2 points with cubeValue=1)
        var result = new GameResult(player1Id, WinType.Gammon, 1);

        _gameRepositoryMock.Setup(x => x.GetGameByGameIdAsync(gameId))
            .ReturnsAsync(game);
        _matchRepositoryMock.Setup(x => x.GetMatchByIdAsync(matchId))
            .ReturnsAsync(match);
        _matchRepositoryMock.Setup(x => x.UpdateMatchAsync(It.IsAny<ServerMatch>()))
            .Callback<ServerMatch>(m =>
            {
                // Simulate database update
                match.Player1Score = m.Player1Score;
                match.Player2Score = m.Player2Score;
                match.LastUpdatedAt = m.LastUpdatedAt;
            })
            .Returns(Task.CompletedTask);
        _gameRepositoryMock.Setup(x => x.SaveGameAsync(It.IsAny<ServerGame>()))
            .Returns(Task.CompletedTask);

        // Act
        await _matchService.CompleteGameAsync(gameId, result);

        // Assert
        Assert.Equal(2, match.Player1Score); // Player 1 should have 2 points
        Assert.Equal(0, match.Player2Score); // Player 2 should still have 0
        _matchRepositoryMock.Verify(
            x => x.UpdateMatchAsync(It.Is<ServerMatch>(m => m.Player1Score == 2 && m.Player2Score == 0)),
            Times.Once);
    }

    [Fact]
    public async Task StartNextGameAsync_CreatesSessionWithCorrectScores_AfterFirstGameCompletes()
    {
        // Arrange
        var matchId = Guid.NewGuid().ToString();
        var player1Id = "player1";
        var player2Id = "ai_opponent";

        // Match after first game (Player 1 won 2 points)
        var matchAfterGame1 = new ServerMatch
        {
            MatchId = matchId,
            Player1Id = player1Id,
            Player2Id = player2Id,
            Player1Name = "Player 1",
            Player2Name = "Computer",
            TargetScore = 5,
            Player1Score = 2,  // Updated score after first game
            Player2Score = 0,
            Status = "InProgress",
            IsCrawfordGame = false,
            OpponentType = "AI"
        };

        GameSession? capturedSession = null;

        _matchRepositoryMock.Setup(x => x.GetMatchByIdAsync(matchId))
            .ReturnsAsync(matchAfterGame1);
        _gameRepositoryMock.Setup(x => x.SaveGameAsync(It.IsAny<ServerGame>()))
            .Returns(Task.CompletedTask);
        _matchRepositoryMock.Setup(x => x.AddGameToMatchAsync(matchId, It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Capture the session created by the factory
        _sessionFactoryMock.Setup(x => x.CreateMatchGameSession(It.IsAny<ServerMatch>(), It.IsAny<string>()))
            .Returns((ServerMatch match, string gameId) =>
            {
                var session = new GameSession(gameId)
                {
                    MatchId = match.MatchId,
                    TargetScore = match.TargetScore,
                    Player1Score = match.Player1Score,
                    Player2Score = match.Player2Score,
                    IsCrawfordGame = match.IsCrawfordGame
                };
                capturedSession = session;
                return session;
            });

        // Act
        var nextGame = await _matchService.StartNextGameAsync(matchId);

        // Assert
        Assert.NotNull(capturedSession);
        Assert.Equal(matchId, capturedSession.MatchId);
        Assert.Equal(5, capturedSession.TargetScore);
        Assert.Equal(2, capturedSession.Player1Score); // Should have updated score
        Assert.Equal(0, capturedSession.Player2Score);
        Assert.False(capturedSession.IsCrawfordGame);

        // Verify the factory was called with correct match data
        _sessionFactoryMock.Verify(
            x => x.CreateMatchGameSession(
                It.Is<ServerMatch>(
                    m =>
                        m.MatchId == matchId &&
                        m.Player1Score == 2 &&
                        m.Player2Score == 0 &&
                        m.TargetScore == 5),
                It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task CompleteGameAsync_ActivatesCrawfordRule_WhenPlayerReachesTargetMinusOne()
    {
        // Arrange
        var matchId = Guid.NewGuid().ToString();
        var gameId = Guid.NewGuid().ToString();
        var player1Id = "player1";
        var player2Id = "ai_opponent";

        var match = new ServerMatch
        {
            MatchId = matchId,
            Player1Id = player1Id,
            Player2Id = player2Id,
            Player1Name = "Player 1",
            Player2Name = "Computer",
            TargetScore = 5,
            Player1Score = 3,  // One point away from target
            Player2Score = 1,
            Status = "InProgress",
            IsCrawfordGame = false,
            OpponentType = "AI"
        };

        var game = new ServerGame
        {
            GameId = gameId,
            MatchId = matchId,
            Status = "Completed",
            WhitePlayerId = player1Id,
            RedPlayerId = player2Id
        };

        // Player 1 wins 1 point (normal win with cubeValue=1)
        var result = new GameResult(player1Id, WinType.Normal, 1);

        _gameRepositoryMock.Setup(x => x.GetGameByGameIdAsync(gameId))
            .ReturnsAsync(game);
        _matchRepositoryMock.Setup(x => x.GetMatchByIdAsync(matchId))
            .ReturnsAsync(match);
        _matchRepositoryMock.Setup(x => x.UpdateMatchAsync(It.IsAny<ServerMatch>()))
            .Callback<ServerMatch>(m =>
            {
                match.Player1Score = m.Player1Score;
                match.IsCrawfordGame = m.IsCrawfordGame;
            })
            .Returns(Task.CompletedTask);
        _gameRepositoryMock.Setup(x => x.SaveGameAsync(It.IsAny<ServerGame>()))
            .Returns(Task.CompletedTask);

        // Act
        await _matchService.CompleteGameAsync(gameId, result);

        // Assert
        Assert.Equal(4, match.Player1Score); // Player 1 at target - 1
        Assert.True(match.IsCrawfordGame); // Crawford rule should be activated
        _matchRepositoryMock.Verify(
            x => x.UpdateMatchAsync(It.Is<ServerMatch>(m => m.Player1Score == 4 && m.IsCrawfordGame)),
            Times.Once);
    }

    [Fact]
    public async Task FullMatchFlow_TracksScoresCorrectly_ThroughMultipleGames()
    {
        // Arrange - simulate a full match to 3 points
        var matchId = Guid.NewGuid().ToString();
        var player1Id = "player1";
        var player2Id = "ai_opponent";

        var match = new ServerMatch
        {
            MatchId = matchId,
            Player1Id = player1Id,
            Player2Id = player2Id,
            Player1Name = "Player 1",
            Player2Name = "Computer",
            TargetScore = 3,
            Player1Score = 0,
            Player2Score = 0,
            Status = "InProgress",
            IsCrawfordGame = false,
            OpponentType = "AI"
        };

        _matchRepositoryMock.Setup(x => x.GetMatchByIdAsync(matchId))
            .ReturnsAsync(() => match);
        _matchRepositoryMock.Setup(x => x.UpdateMatchAsync(It.IsAny<ServerMatch>()))
            .Callback<ServerMatch>(m =>
            {
                match.Player1Score = m.Player1Score;
                match.Player2Score = m.Player2Score;
                match.IsCrawfordGame = m.IsCrawfordGame;
                match.Status = m.Status;
            })
            .Returns(Task.CompletedTask);
        _gameRepositoryMock.Setup(x => x.SaveGameAsync(It.IsAny<ServerGame>()))
            .Returns(Task.CompletedTask);

        // Game 1: Player 1 wins 1 point
        var game1Id = Guid.NewGuid().ToString();
        var game1 = new ServerGame
        {
            GameId = game1Id,
            MatchId = matchId,
            Status = "Completed",
            WhitePlayerId = player1Id,
            RedPlayerId = player2Id
        };
        _gameRepositoryMock.Setup(x => x.GetGameByGameIdAsync(game1Id))
            .ReturnsAsync(game1);

        await _matchService.CompleteGameAsync(game1Id, new GameResult(player1Id, WinType.Normal, 1));
        Assert.Equal(1, match.Player1Score);
        Assert.Equal(0, match.Player2Score);
        Assert.False(match.IsCrawfordGame);
        Assert.Equal("InProgress", match.Status);

        // Game 2: Player 2 wins 2 points (gammon), reaching target - 1
        var game2Id = Guid.NewGuid().ToString();
        var game2 = new ServerGame
        {
            GameId = game2Id,
            MatchId = matchId,
            Status = "Completed",
            WhitePlayerId = player1Id,
            RedPlayerId = player2Id
        };
        _gameRepositoryMock.Setup(x => x.GetGameByGameIdAsync(game2Id))
            .ReturnsAsync(game2);

        await _matchService.CompleteGameAsync(game2Id, new GameResult(player2Id, WinType.Gammon, 1));
        Assert.Equal(1, match.Player1Score);
        Assert.Equal(2, match.Player2Score);
        Assert.True(match.IsCrawfordGame); // Crawford activated (Player 2 at 2, target is 3)
        Assert.Equal("InProgress", match.Status);

        // Game 3 (Crawford game): Player 2 wins 1 point, reaching target
        var game3Id = Guid.NewGuid().ToString();
        var game3 = new ServerGame
        {
            GameId = game3Id,
            MatchId = matchId,
            Status = "Completed",
            WhitePlayerId = player1Id,
            RedPlayerId = player2Id
        };
        _gameRepositoryMock.Setup(x => x.GetGameByGameIdAsync(game3Id))
            .ReturnsAsync(game3);

        await _matchService.CompleteGameAsync(game3Id, new GameResult(player2Id, WinType.Normal, 1));
        Assert.Equal(1, match.Player1Score);
        Assert.Equal(3, match.Player2Score);
        Assert.Equal("Completed", match.Status);
        Assert.Equal(player2Id, match.WinnerId);
    }

    [Fact]
    public async Task StartNextGameAsync_ThrowsException_WhenMatchNotFound()
    {
        // Arrange
        var matchId = Guid.NewGuid().ToString();

        _matchRepositoryMock.Setup(x => x.GetMatchByIdAsync(matchId))
            .ReturnsAsync((ServerMatch?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _matchService.StartNextGameAsync(matchId));
        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task StartNextGameAsync_ThrowsException_WhenMatchAlreadyCompleted()
    {
        // Arrange
        var matchId = Guid.NewGuid().ToString();
        var match = new ServerMatch
        {
            MatchId = matchId,
            Status = "Completed",
            Player1Id = "player1",
            Player2Id = "player2",
            TargetScore = 5
        };

        _matchRepositoryMock.Setup(x => x.GetMatchByIdAsync(matchId))
            .ReturnsAsync(match);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _matchService.StartNextGameAsync(matchId));
        Assert.Contains("Cannot start new game in completed match", exception.Message);
    }

    // NOTE: Test for connection mapping bug fix (GameCompletionService.cs:173-198)
    // The bug where player connections were mapped by color instead of player ID
    // when continuing to the next match game has been fixed. The fix ensures that:
    // - Player1's connections are always mapped to Player1's ID (not their game color)
    // - Player2's connections are always mapped to Player2's ID (not their game color)
    // This prevents the bug where player names would swap and connections would break
    // in game 2+ of a match.
    //
    // Manual test: Create a 3-point match against AI, win the first game, and verify:
    // 1. Your name remains correct in game 2
    // 2. AI opponent name remains correct in game 2
    // 3. You can successfully roll dice and make moves in game 2

    [Fact]
    public async Task StartNextGameAsync_CreatesProperlyInitializedSession()
    {
        // This test verifies that StartNextGameAsync creates a new session
        // with correct match context. The critical fix (in GameCompletionService.cs:200-201)
        // ensures that StartNewGame() is called WITHOUT RollDice(), allowing players
        // to perform a normal opening roll instead of starting with pre-rolled dice.

        // Arrange
        var matchId = Guid.NewGuid().ToString();
        var player1Id = "player1";
        var player2Id = "player2";

        var match = new ServerMatch
        {
            MatchId = matchId,
            Player1Id = player1Id,
            Player2Id = player2Id,
            Player1Name = "Player 1",
            Player2Name = "Player 2",
            TargetScore = 3,
            Player1Score = 1,
            Player2Score = 0,
            Status = "InProgress",
            IsCrawfordGame = false,
            OpponentType = "Friend"
        };

        GameSession? capturedSession = null;

        _matchRepositoryMock.Setup(x => x.GetMatchByIdAsync(matchId))
            .ReturnsAsync(match);
        _gameRepositoryMock.Setup(x => x.SaveGameAsync(It.IsAny<ServerGame>()))
            .Returns(Task.CompletedTask);
        _matchRepositoryMock.Setup(x => x.AddGameToMatchAsync(matchId, It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Capture the session created by the factory
        _sessionFactoryMock.Setup(x => x.CreateMatchGameSession(It.IsAny<ServerMatch>(), It.IsAny<string>()))
            .Returns((ServerMatch m, string gameId) =>
            {
                var session = new GameSession(gameId)
                {
                    MatchId = m.MatchId,
                    TargetScore = m.TargetScore,
                    Player1Score = m.Player1Score,
                    Player2Score = m.Player2Score,
                    IsCrawfordGame = m.IsCrawfordGame
                };
                capturedSession = session;
                return session;
            });

        // Act
        var nextGame = await _matchService.StartNextGameAsync(matchId);

        // Assert
        Assert.NotNull(capturedSession);

        // The session factory creates the session, then StartNextGameInMatchAsync initializes it.
        // We're testing at the MatchService level, so we verify the created session structure.
        // The key verification is that the MatchService.StartNextGameAsync creates a proper
        // game session that will be initialized correctly by GameCompletionService.

        // Verify match context is preserved
        Assert.Equal(matchId, capturedSession.MatchId);
        Assert.Equal(3, capturedSession.TargetScore);
        Assert.Equal(1, capturedSession.Player1Score);
        Assert.Equal(0, capturedSession.Player2Score);

        // Verify it's marked as a match game
        Assert.NotNull(capturedSession.MatchId);
        Assert.False(capturedSession.IsCrawfordGame);
    }
}
