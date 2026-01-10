using Backgammon.Server.Models;
using Backgammon.Server.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backgammon.Tests.Services;

public class PlayerStatsServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IEloRatingService> _mockEloRatingService;
    private readonly Mock<ILogger<PlayerStatsService>> _mockLogger;
    private readonly PlayerStatsService _service;

    public PlayerStatsServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockEloRatingService = new Mock<IEloRatingService>();
        _mockLogger = new Mock<ILogger<PlayerStatsService>>();
        _service = new PlayerStatsService(
            _mockUserRepository.Object,
            _mockEloRatingService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task UpdateStatsAfterGameCompletionAsync_WaitingForPlayer_SkipsUpdate()
    {
        // Arrange
        var game = new Game
        {
            GameId = "game-123",
            WhitePlayerId = "player-1",
            RedPlayerId = null // No red player = waiting
        };

        // Act
        await _service.UpdateStatsAfterGameCompletionAsync(game);

        // Assert - should skip because no red player
        _mockUserRepository.Verify(
            r => r.GetByUserIdAsync(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateStatsAfterGameCompletionAsync_NoRedPlayer_SkipsUpdate()
    {
        // Arrange
        var game = new Game
        {
            GameId = "game-123",
            WhitePlayerId = "player-1",
            RedPlayerId = string.Empty
        };

        // Act
        await _service.UpdateStatsAfterGameCompletionAsync(game);

        // Assert
        _mockUserRepository.Verify(
            r => r.GetByUserIdAsync(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateStatsAfterGameCompletionAsync_AiOpponent_SkipsUpdate()
    {
        // Arrange
        var game = new Game
        {
            GameId = "game-123",
            WhitePlayerId = "player-1",
            RedPlayerId = "ai-123",
            IsAiOpponent = true
        };

        // Act
        await _service.UpdateStatsAfterGameCompletionAsync(game);

        // Assert
        _mockUserRepository.Verify(
            r => r.GetByUserIdAsync(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateStatsAfterGameCompletionAsync_WhiteWins_UpdatesBothPlayersCorrectly()
    {
        // Arrange
        var whiteUser = new User
        {
            UserId = "white-user",
            Stats = new UserStats()
        };
        var redUser = new User
        {
            UserId = "red-user",
            Stats = new UserStats()
        };

        var game = new Game
        {
            GameId = "game-123",
            WhitePlayerId = "white-player",
            RedPlayerId = "red-player",
            WhiteUserId = "white-user",
            RedUserId = "red-user",
            Winner = "White",
            Stakes = 1,
            IsAiOpponent = false
        };

        _mockUserRepository.Setup(r => r.GetByUserIdAsync("white-user")).ReturnsAsync(whiteUser);
        _mockUserRepository.Setup(r => r.GetByUserIdAsync("red-user")).ReturnsAsync(redUser);

        // Act
        await _service.UpdateStatsAfterGameCompletionAsync(game);

        // Assert - White player (winner)
        Assert.Equal(1, whiteUser.Stats.TotalGames);
        Assert.Equal(1, whiteUser.Stats.Wins);
        Assert.Equal(0, whiteUser.Stats.Losses);
        Assert.Equal(1, whiteUser.Stats.TotalStakes);
        Assert.Equal(1, whiteUser.Stats.WinStreak);
        Assert.Equal(1, whiteUser.Stats.NormalWins);

        // Assert - Red player (loser)
        Assert.Equal(1, redUser.Stats.TotalGames);
        Assert.Equal(0, redUser.Stats.Wins);
        Assert.Equal(1, redUser.Stats.Losses);
        Assert.Equal(0, redUser.Stats.WinStreak);

        _mockUserRepository.Verify(r => r.UpdateUserAsync(whiteUser), Times.Once);
        _mockUserRepository.Verify(r => r.UpdateUserAsync(redUser), Times.Once);
    }

    [Fact]
    public async Task UpdateStatsAfterGameCompletionAsync_GammonWin_RecordsCorrectWinType()
    {
        // Arrange
        var user = new User
        {
            UserId = "user-123",
            Stats = new UserStats()
        };

        var game = new Game
        {
            GameId = "game-123",
            WhitePlayerId = "white-player",
            RedPlayerId = "red-player",
            WhiteUserId = "user-123",
            Winner = "White",
            Stakes = 2, // Gammon
            IsAiOpponent = false
        };

        _mockUserRepository.Setup(r => r.GetByUserIdAsync("user-123")).ReturnsAsync(user);

        // Act
        await _service.UpdateStatsAfterGameCompletionAsync(game);

        // Assert
        Assert.Equal(1, user.Stats.Wins);
        Assert.Equal(2, user.Stats.TotalStakes);
        Assert.Equal(1, user.Stats.GammonWins);
        Assert.Equal(0, user.Stats.NormalWins);
        Assert.Equal(0, user.Stats.BackgammonWins);
    }

    [Fact]
    public async Task UpdateStatsAfterGameCompletionAsync_BackgammonWin_RecordsCorrectWinType()
    {
        // Arrange
        var user = new User
        {
            UserId = "user-123",
            Stats = new UserStats()
        };

        var game = new Game
        {
            GameId = "game-123",
            WhitePlayerId = "white-player",
            RedPlayerId = "red-player",
            WhiteUserId = "user-123",
            Winner = "White",
            Stakes = 3, // Backgammon
            IsAiOpponent = false
        };

        _mockUserRepository.Setup(r => r.GetByUserIdAsync("user-123")).ReturnsAsync(user);

        // Act
        await _service.UpdateStatsAfterGameCompletionAsync(game);

        // Assert
        Assert.Equal(1, user.Stats.Wins);
        Assert.Equal(3, user.Stats.TotalStakes);
        Assert.Equal(1, user.Stats.BackgammonWins);
        Assert.Equal(0, user.Stats.NormalWins);
        Assert.Equal(0, user.Stats.GammonWins);
    }

    [Fact]
    public async Task UpdateStatsAfterGameCompletionAsync_WinStreak_UpdatesCorrectly()
    {
        // Arrange
        var user = new User
        {
            UserId = "user-123",
            Stats = new UserStats
            {
                WinStreak = 2,
                BestWinStreak = 2
            }
        };

        var game = new Game
        {
            GameId = "game-123",
            WhitePlayerId = "white-player",
            RedPlayerId = "red-player",
            WhiteUserId = "user-123",
            Winner = "White",
            Stakes = 1,
            IsAiOpponent = false
        };

        _mockUserRepository.Setup(r => r.GetByUserIdAsync("user-123")).ReturnsAsync(user);

        // Act
        await _service.UpdateStatsAfterGameCompletionAsync(game);

        // Assert
        Assert.Equal(3, user.Stats.WinStreak);
        Assert.Equal(3, user.Stats.BestWinStreak);
    }

    [Fact]
    public async Task UpdateStatsAfterGameCompletionAsync_Loss_ResetsWinStreak()
    {
        // Arrange
        var user = new User
        {
            UserId = "user-123",
            Stats = new UserStats
            {
                WinStreak = 5,
                BestWinStreak = 5
            }
        };

        var game = new Game
        {
            GameId = "game-123",
            WhitePlayerId = "white-player",
            RedPlayerId = "red-player",
            WhiteUserId = "user-123",
            Winner = "Red", // User loses
            Stakes = 1,
            IsAiOpponent = false
        };

        _mockUserRepository.Setup(r => r.GetByUserIdAsync("user-123")).ReturnsAsync(user);

        // Act
        await _service.UpdateStatsAfterGameCompletionAsync(game);

        // Assert
        Assert.Equal(1, user.Stats.Losses);
        Assert.Equal(0, user.Stats.WinStreak);
        Assert.Equal(5, user.Stats.BestWinStreak); // Best streak unchanged
    }

    [Fact]
    public async Task UpdateStatsAfterGameCompletionAsync_UserNotFound_DoesNotThrow()
    {
        // Arrange
        var game = new Game
        {
            GameId = "game-123",
            WhitePlayerId = "white-player",
            RedPlayerId = "red-player",
            WhiteUserId = "non-existent-user",
            Winner = "White",
            Stakes = 1,
            IsAiOpponent = false
        };

        _mockUserRepository.Setup(r => r.GetByUserIdAsync("non-existent-user")).ReturnsAsync((User?)null);

        // Act & Assert - should not throw
        await _service.UpdateStatsAfterGameCompletionAsync(game);

        _mockUserRepository.Verify(
            r => r.UpdateUserAsync(It.IsAny<User>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateStatsAfterGameCompletionAsync_OnlyWhiteUserRegistered_UpdatesOnlyWhite()
    {
        // Arrange
        var whiteUser = new User
        {
            UserId = "white-user",
            Stats = new UserStats()
        };

        var game = new Game
        {
            GameId = "game-123",
            WhitePlayerId = "white-player",
            RedPlayerId = "red-player",
            WhiteUserId = "white-user",
            RedUserId = null, // Red not registered
            Winner = "White",
            Stakes = 1,
            IsAiOpponent = false
        };

        _mockUserRepository.Setup(r => r.GetByUserIdAsync("white-user")).ReturnsAsync(whiteUser);

        // Act
        await _service.UpdateStatsAfterGameCompletionAsync(game);

        // Assert
        _mockUserRepository.Verify(r => r.UpdateUserAsync(whiteUser), Times.Once);
        Assert.Equal(1, whiteUser.Stats.Wins);
    }

    [Fact]
    public async Task UpdateStatsAfterGameCompletionAsync_UnratedGame_DoesNotUpdateRatings()
    {
        // Arrange
        var whiteUser = new User
        {
            UserId = "white-user",
            Rating = 1500,
            RatedGamesCount = 0,
            Stats = new UserStats()
        };
        var redUser = new User
        {
            UserId = "red-user",
            Rating = 1500,
            RatedGamesCount = 0,
            Stats = new UserStats()
        };

        var game = new Game
        {
            GameId = "game-123",
            WhitePlayerId = "white-player",
            RedPlayerId = "red-player",
            WhiteUserId = "white-user",
            RedUserId = "red-user",
            Winner = "White",
            Stakes = 1,
            IsAiOpponent = false,
            IsRated = false // Unrated game
        };

        _mockUserRepository.Setup(r => r.GetByUserIdAsync("white-user")).ReturnsAsync(whiteUser);
        _mockUserRepository.Setup(r => r.GetByUserIdAsync("red-user")).ReturnsAsync(redUser);

        // Act
        await _service.UpdateStatsAfterGameCompletionAsync(game);

        // Assert - ratings should not change for unrated games
        Assert.Equal(1500, whiteUser.Rating);
        Assert.Equal(1500, redUser.Rating);
        Assert.Equal(0, whiteUser.RatedGamesCount);
        Assert.Equal(0, redUser.RatedGamesCount);

        // Stats should still update (wins/losses)
        Assert.Equal(1, whiteUser.Stats.Wins);
        Assert.Equal(1, redUser.Stats.Losses);

        // ELO service should not be called for unrated games
        _mockEloRatingService.Verify(
            s => s.CalculateNewRatings(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateStatsAfterGameCompletionAsync_RatedGame_UpdatesRatingsAndIncrementsCoun()
    {
        // Arrange
        var whiteUser = new User
        {
            UserId = "white-user",
            Rating = 1500,
            RatedGamesCount = 0,
            PeakRating = 1500,
            Stats = new UserStats()
        };
        var redUser = new User
        {
            UserId = "red-user",
            Rating = 1500,
            RatedGamesCount = 0,
            PeakRating = 1500,
            Stats = new UserStats()
        };

        var game = new Game
        {
            GameId = "game-123",
            WhitePlayerId = "white-player",
            RedPlayerId = "red-player",
            WhiteUserId = "white-user",
            RedUserId = "red-user",
            Winner = "White",
            Stakes = 1,
            IsAiOpponent = false,
            IsRated = true // Rated game
        };

        _mockUserRepository.Setup(r => r.GetByUserIdAsync("white-user")).ReturnsAsync(whiteUser);
        _mockUserRepository.Setup(r => r.GetByUserIdAsync("red-user")).ReturnsAsync(redUser);

        // Mock ELO calculation
        _mockEloRatingService
            .Setup(s => s.CalculateNewRatings(1500, 1500, 0, 0, true))
            .Returns((1516, 1484));

        // Act
        await _service.UpdateStatsAfterGameCompletionAsync(game);

        // Assert - ratings should be updated
        Assert.Equal(1516, whiteUser.Rating);
        Assert.Equal(1484, redUser.Rating);
        Assert.Equal(1, whiteUser.RatedGamesCount);
        Assert.Equal(1, redUser.RatedGamesCount);
        Assert.NotNull(whiteUser.RatingLastUpdatedAt);
        Assert.NotNull(redUser.RatingLastUpdatedAt);

        // Game should have rating snapshots
        Assert.Equal(1500, game.WhiteRatingBefore);
        Assert.Equal(1516, game.WhiteRatingAfter);
        Assert.Equal(1500, game.RedRatingBefore);
        Assert.Equal(1484, game.RedRatingAfter);

        // ELO service should be called
        _mockEloRatingService.Verify(
            s => s.CalculateNewRatings(1500, 1500, 0, 0, true),
            Times.Once);
    }

    [Fact]
    public async Task UpdateStatsAfterGameCompletionAsync_PeakRating_IncreasesWhenRatingGoesUp()
    {
        // Arrange
        var whiteUser = new User
        {
            UserId = "white-user",
            Rating = 1500,
            RatedGamesCount = 5,
            PeakRating = 1520, // Previous peak
            Stats = new UserStats()
        };
        var redUser = new User
        {
            UserId = "red-user",
            Rating = 1500,
            RatedGamesCount = 5,
            PeakRating = 1550,
            Stats = new UserStats()
        };

        var game = new Game
        {
            GameId = "game-123",
            WhitePlayerId = "white-player",
            RedPlayerId = "red-player",
            WhiteUserId = "white-user",
            RedUserId = "red-user",
            Winner = "White",
            Stakes = 1,
            IsAiOpponent = false,
            IsRated = true
        };

        _mockUserRepository.Setup(r => r.GetByUserIdAsync("white-user")).ReturnsAsync(whiteUser);
        _mockUserRepository.Setup(r => r.GetByUserIdAsync("red-user")).ReturnsAsync(redUser);

        // White wins and goes to 1530 (new peak)
        _mockEloRatingService
            .Setup(s => s.CalculateNewRatings(1500, 1500, 5, 5, true))
            .Returns((1530, 1488));

        // Act
        await _service.UpdateStatsAfterGameCompletionAsync(game);

        // Assert - white's peak should increase
        Assert.Equal(1530, whiteUser.Rating);
        Assert.Equal(1530, whiteUser.PeakRating); // New peak

        // Red's peak should stay the same (rating decreased)
        Assert.Equal(1488, redUser.Rating);
        Assert.Equal(1550, redUser.PeakRating); // Unchanged
    }

    [Fact]
    public async Task UpdateStatsAfterGameCompletionAsync_PeakRating_DoesNotDecreaseWhenRatingDrops()
    {
        // Arrange
        var user = new User
        {
            UserId = "user-123",
            Rating = 1600,
            RatedGamesCount = 10,
            PeakRating = 1650, // Historical peak
            Stats = new UserStats()
        };
        var opponent = new User
        {
            UserId = "opponent-123",
            Rating = 1600,
            RatedGamesCount = 10,
            PeakRating = 1600,
            Stats = new UserStats()
        };

        var game = new Game
        {
            GameId = "game-123",
            WhitePlayerId = "white-player",
            RedPlayerId = "red-player",
            WhiteUserId = "user-123",
            RedUserId = "opponent-123",
            Winner = "Red", // User loses
            Stakes = 1,
            IsAiOpponent = false,
            IsRated = true
        };

        _mockUserRepository.Setup(r => r.GetByUserIdAsync("user-123")).ReturnsAsync(user);
        _mockUserRepository.Setup(r => r.GetByUserIdAsync("opponent-123")).ReturnsAsync(opponent);

        // User loses and drops to 1588
        _mockEloRatingService
            .Setup(s => s.CalculateNewRatings(1600, 1600, 10, 10, false))
            .Returns((1588, 1612));

        // Act
        await _service.UpdateStatsAfterGameCompletionAsync(game);

        // Assert - user's peak should NOT decrease
        Assert.Equal(1588, user.Rating);
        Assert.Equal(1650, user.PeakRating); // Unchanged from historical peak
    }

    [Fact]
    public async Task UpdateStatsAfterGameCompletionAsync_RatingFloorEnforcement_InPlayerStatsService()
    {
        // Arrange - User at very low rating
        var whiteUser = new User
        {
            UserId = "white-user",
            Rating = 105,
            RatedGamesCount = 30,
            PeakRating = 500,
            Stats = new UserStats()
        };
        var redUser = new User
        {
            UserId = "red-user",
            Rating = 1500,
            RatedGamesCount = 30,
            PeakRating = 1500,
            Stats = new UserStats()
        };

        var game = new Game
        {
            GameId = "game-123",
            WhitePlayerId = "white-player",
            RedPlayerId = "red-player",
            WhiteUserId = "white-user",
            RedUserId = "red-user",
            Winner = "Red", // White loses
            Stakes = 1,
            IsAiOpponent = false,
            IsRated = true
        };

        _mockUserRepository.Setup(r => r.GetByUserIdAsync("white-user")).ReturnsAsync(whiteUser);
        _mockUserRepository.Setup(r => r.GetByUserIdAsync("red-user")).ReturnsAsync(redUser);

        // ELO service would return 89 (below floor), but PlayerStatsService should enforce minimum of 100
        _mockEloRatingService
            .Setup(s => s.CalculateNewRatings(105, 1500, 30, 30, false))
            .Returns((89, 1516));

        // Act
        await _service.UpdateStatsAfterGameCompletionAsync(game);

        // Assert - PlayerStatsService should enforce the floor at 100 (defense in depth)
        Assert.Equal(100, whiteUser.Rating); // Enforced by PlayerStatsService
        Assert.Equal(1516, redUser.Rating);
    }

    [Fact]
    public async Task UpdateStatsAfterGameCompletionAsync_ValidStakes_NoWarningLogged()
    {
        // Arrange
        var whiteUser = new User
        {
            UserId = "white-user",
            Rating = 1500,
            RatedGamesCount = 0,
            PeakRating = 1500,
            Stats = new UserStats()
        };
        var redUser = new User
        {
            UserId = "red-user",
            Rating = 1500,
            RatedGamesCount = 0,
            PeakRating = 1500,
            Stats = new UserStats()
        };

        var game = new Game
        {
            GameId = "game-123",
            WhitePlayerId = "white-player",
            RedPlayerId = "red-player",
            WhiteUserId = "white-user",
            RedUserId = "red-user",
            Winner = "White",
            WinType = "Normal",
            DoublingCubeValue = 2,
            Stakes = 2, // Correct: Normal (1) × Cube (2) = 2
            IsAiOpponent = false,
            IsRated = true
        };

        _mockUserRepository.Setup(r => r.GetByUserIdAsync("white-user")).ReturnsAsync(whiteUser);
        _mockUserRepository.Setup(r => r.GetByUserIdAsync("red-user")).ReturnsAsync(redUser);
        _mockEloRatingService
            .Setup(s => s.CalculateNewRatings(1500, 1500, 0, 0, true))
            .Returns((1516, 1484));

        // Act
        await _service.UpdateStatsAfterGameCompletionAsync(game);

        // Assert - No warning should be logged for valid stakes
        Assert.Equal(2, game.Stakes); // Stakes unchanged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Stakes mismatch")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateStatsAfterGameCompletionAsync_InvalidStakes_WarningLoggedAndCorrected()
    {
        // Arrange
        var whiteUser = new User
        {
            UserId = "white-user",
            Rating = 1500,
            RatedGamesCount = 0,
            PeakRating = 1500,
            Stats = new UserStats()
        };
        var redUser = new User
        {
            UserId = "red-user",
            Rating = 1500,
            RatedGamesCount = 0,
            PeakRating = 1500,
            Stats = new UserStats()
        };

        var game = new Game
        {
            GameId = "game-123",
            WhitePlayerId = "white-player",
            RedPlayerId = "red-player",
            WhiteUserId = "white-user",
            RedUserId = "red-user",
            Winner = "White",
            WinType = "Gammon",
            DoublingCubeValue = 2,
            Stakes = 3, // WRONG: Should be Gammon (2) × Cube (2) = 4
            IsAiOpponent = false,
            IsRated = true
        };

        _mockUserRepository.Setup(r => r.GetByUserIdAsync("white-user")).ReturnsAsync(whiteUser);
        _mockUserRepository.Setup(r => r.GetByUserIdAsync("red-user")).ReturnsAsync(redUser);
        _mockEloRatingService
            .Setup(s => s.CalculateNewRatings(1500, 1500, 0, 0, true))
            .Returns((1516, 1484));

        // Act
        await _service.UpdateStatsAfterGameCompletionAsync(game);

        // Assert - Stakes should be corrected
        Assert.Equal(4, game.Stakes); // Corrected to expected value

        // Assert - Warning should be logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Stakes mismatch")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateStatsAfterGameCompletionAsync_BackgammonWithDoublingCube_ValidatesCorrectly()
    {
        // Arrange
        var whiteUser = new User
        {
            UserId = "white-user",
            Rating = 1500,
            RatedGamesCount = 0,
            PeakRating = 1500,
            Stats = new UserStats()
        };
        var redUser = new User
        {
            UserId = "red-user",
            Rating = 1500,
            RatedGamesCount = 0,
            PeakRating = 1500,
            Stats = new UserStats()
        };

        var game = new Game
        {
            GameId = "game-123",
            WhitePlayerId = "white-player",
            RedPlayerId = "red-player",
            WhiteUserId = "white-user",
            RedUserId = "red-user",
            Winner = "White",
            WinType = "Backgammon",
            DoublingCubeValue = 4,
            Stakes = 12, // Correct: Backgammon (3) × Cube (4) = 12
            IsAiOpponent = false,
            IsRated = true
        };

        _mockUserRepository.Setup(r => r.GetByUserIdAsync("white-user")).ReturnsAsync(whiteUser);
        _mockUserRepository.Setup(r => r.GetByUserIdAsync("red-user")).ReturnsAsync(redUser);
        _mockEloRatingService
            .Setup(s => s.CalculateNewRatings(1500, 1500, 0, 0, true))
            .Returns((1516, 1484));

        // Act
        await _service.UpdateStatsAfterGameCompletionAsync(game);

        // Assert - No correction needed, stakes already correct
        Assert.Equal(12, game.Stakes);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Stakes mismatch")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateStatsAfterGameCompletionAsync_NormalWinCube1_ValidatesCorrectly()
    {
        // Arrange
        var whiteUser = new User
        {
            UserId = "white-user",
            Rating = 1500,
            RatedGamesCount = 0,
            PeakRating = 1500,
            Stats = new UserStats()
        };
        var redUser = new User
        {
            UserId = "red-user",
            Rating = 1500,
            RatedGamesCount = 0,
            PeakRating = 1500,
            Stats = new UserStats()
        };

        var game = new Game
        {
            GameId = "game-123",
            WhitePlayerId = "white-player",
            RedPlayerId = "red-player",
            WhiteUserId = "white-user",
            RedUserId = "red-user",
            Winner = "White",
            WinType = "Normal",
            DoublingCubeValue = 1,
            Stakes = 1, // Correct: Normal (1) × Cube (1) = 1
            IsAiOpponent = false,
            IsRated = true
        };

        _mockUserRepository.Setup(r => r.GetByUserIdAsync("white-user")).ReturnsAsync(whiteUser);
        _mockUserRepository.Setup(r => r.GetByUserIdAsync("red-user")).ReturnsAsync(redUser);
        _mockEloRatingService
            .Setup(s => s.CalculateNewRatings(1500, 1500, 0, 0, true))
            .Returns((1516, 1484));

        // Act
        await _service.UpdateStatsAfterGameCompletionAsync(game);

        // Assert - Stakes correct, no warning
        Assert.Equal(1, game.Stakes);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Stakes mismatch")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateStatsAfterGameCompletionAsync_UnratedGame_SkipsStakesValidation()
    {
        // Arrange
        var whiteUser = new User
        {
            UserId = "white-user",
            Rating = 1500,
            Stats = new UserStats()
        };
        var redUser = new User
        {
            UserId = "red-user",
            Rating = 1500,
            Stats = new UserStats()
        };

        var game = new Game
        {
            GameId = "game-123",
            WhitePlayerId = "white-player",
            RedPlayerId = "red-player",
            WhiteUserId = "white-user",
            RedUserId = "red-user",
            Winner = "White",
            WinType = "Normal",
            DoublingCubeValue = 1,
            Stakes = 999, // Invalid, but should be ignored for unrated games
            IsAiOpponent = false,
            IsRated = false
        };

        _mockUserRepository.Setup(r => r.GetByUserIdAsync("white-user")).ReturnsAsync(whiteUser);
        _mockUserRepository.Setup(r => r.GetByUserIdAsync("red-user")).ReturnsAsync(redUser);

        // Act
        await _service.UpdateStatsAfterGameCompletionAsync(game);

        // Assert - Validation not performed for unrated games
        Assert.Equal(999, game.Stakes); // Unchanged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Stakes mismatch")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }
}
