using Backgammon.Server.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backgammon.Tests.Services;

public class EloRatingServiceTests
{
    private readonly Mock<ILogger<EloRatingService>> _mockLogger;
    private readonly IConfiguration _configuration;
    private readonly EloRatingService _service;

    public EloRatingServiceTests()
    {
        _mockLogger = new Mock<ILogger<EloRatingService>>();

        // Set up default configuration
        var configData = new Dictionary<string, string?>
        {
            { "EloRating:StartingRating", "1500" },
            { "EloRating:KFactorNew", "32" },
            { "EloRating:KFactorEstablished", "24" },
            { "EloRating:GamesForEstablished", "30" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _service = new EloRatingService(_configuration, _mockLogger.Object);
    }

    [Fact]
    public void GetKFactor_NewPlayer_Returns32()
    {
        // Act
        var kFactor = _service.GetKFactor(0);

        // Assert
        Assert.Equal(32, kFactor);
    }

    [Fact]
    public void GetKFactor_Player29Games_Returns32()
    {
        // Act
        var kFactor = _service.GetKFactor(29);

        // Assert
        Assert.Equal(32, kFactor);
    }

    [Fact]
    public void GetKFactor_Player30Games_Returns24()
    {
        // Act
        var kFactor = _service.GetKFactor(30);

        // Assert
        Assert.Equal(24, kFactor);
    }

    [Fact]
    public void GetKFactor_EstablishedPlayer_Returns24()
    {
        // Act
        var kFactor = _service.GetKFactor(100);

        // Assert
        Assert.Equal(24, kFactor);
    }

    [Fact]
    public void CalculateExpectedScore_EqualRatings_Returns0Point5()
    {
        // Act
        var expected = _service.CalculateExpectedScore(1500, 1500);

        // Assert
        Assert.Equal(0.5, expected, 4); // 4 decimal places precision
    }

    [Fact]
    public void CalculateExpectedScore_PlayerHigher200_ReturnsAbout0Point76()
    {
        // Expected score when player is 200 points higher
        // Formula: 1 / (1 + 10^((1500-1700)/400)) = 1 / (1 + 10^(-0.5)) ≈ 0.76

        // Act
        var expected = _service.CalculateExpectedScore(1700, 1500);

        // Assert
        Assert.Equal(0.76, expected, 2);
    }

    [Fact]
    public void CalculateExpectedScore_PlayerLower200_ReturnsAbout0Point24()
    {
        // Expected score when player is 200 points lower
        // Formula: 1 / (1 + 10^((1700-1500)/400)) = 1 / (1 + 10^(0.5)) ≈ 0.24

        // Act
        var expected = _service.CalculateExpectedScore(1500, 1700);

        // Assert
        Assert.Equal(0.24, expected, 2);
    }

    [Fact]
    public void CalculateExpectedScore_ExtremeRatingDifference_ReturnsNearZero()
    {
        // Very weak player vs very strong player
        // Act
        var expected = _service.CalculateExpectedScore(800, 2400);

        // Assert - should be close to 0
        Assert.True(expected < 0.01);
    }

    [Fact]
    public void CalculateExpectedScore_ExtremeRatingDifference_ReturnsNearOne()
    {
        // Very strong player vs very weak player
        // Act
        var expected = _service.CalculateExpectedScore(2400, 800);

        // Assert - should be close to 1
        Assert.True(expected > 0.99);
    }

    [Fact]
    public void CalculateNewRatings_EqualPlayersNewPlayers_WhiteWins_CorrectChange()
    {
        // Two new players (K=32), equal rating, white wins
        // Expected: 0.5, Actual: 1.0, Change = 32 * (1.0 - 0.5) = 16

        // Act
        var (whiteNew, redNew) = _service.CalculateNewRatings(
            whiteRating: 1500,
            redRating: 1500,
            whiteRatedGames: 0,
            redRatedGames: 0,
            whiteWon: true);

        // Assert
        Assert.Equal(1516, whiteNew); // 1500 + 16
        Assert.Equal(1484, redNew);   // 1500 - 16
    }

    [Fact]
    public void CalculateNewRatings_EqualPlayersNewPlayers_RedWins_CorrectChange()
    {
        // Two new players (K=32), equal rating, red wins

        // Act
        var (whiteNew, redNew) = _service.CalculateNewRatings(
            whiteRating: 1500,
            redRating: 1500,
            whiteRatedGames: 0,
            redRatedGames: 0,
            whiteWon: false);

        // Assert
        Assert.Equal(1484, whiteNew); // 1500 - 16
        Assert.Equal(1516, redNew);   // 1500 + 16
    }

    [Fact]
    public void CalculateNewRatings_EqualPlayersEstablished_WhiteWins_SmallerChange()
    {
        // Two established players (K=24), equal rating, white wins
        // Expected: 0.5, Actual: 1.0, Change = 24 * (1.0 - 0.5) = 12

        // Act
        var (whiteNew, redNew) = _service.CalculateNewRatings(
            whiteRating: 1500,
            redRating: 1500,
            whiteRatedGames: 30,
            redRatedGames: 30,
            whiteWon: true);

        // Assert
        Assert.Equal(1512, whiteNew); // 1500 + 12
        Assert.Equal(1488, redNew);   // 1500 - 12
    }

    [Fact]
    public void CalculateNewRatings_UnderdogWins_LargeRatingGain()
    {
        // Weak player (1200) beats strong player (1800) - both established
        // Expected for weak: ~0.03, Actual: 1.0, Change ≈ 24 * 0.97 ≈ 23

        // Act
        var (whiteNew, redNew) = _service.CalculateNewRatings(
            whiteRating: 1200,
            redRating: 1800,
            whiteRatedGames: 30,
            redRatedGames: 30,
            whiteWon: true);

        // Assert
        // Weak player should gain significantly
        Assert.True(whiteNew > 1220, $"Expected white rating > 1220, got {whiteNew}");

        // Strong player should lose significantly
        Assert.True(redNew < 1780, $"Expected red rating < 1780, got {redNew}");
    }

    [Fact]
    public void CalculateNewRatings_FavoriteWins_SmallRatingGain()
    {
        // Strong player (1800) beats weak player (1200) - both established
        // Expected for strong: ~0.97, Actual: 1.0, Change ≈ 24 * 0.03 ≈ 1

        // Act
        var (whiteNew, redNew) = _service.CalculateNewRatings(
            whiteRating: 1800,
            redRating: 1200,
            whiteRatedGames: 30,
            redRatedGames: 30,
            whiteWon: true);

        // Assert
        // Strong player should gain very little
        Assert.True(whiteNew < 1805, $"Expected white rating < 1805, got {whiteNew}");
        Assert.True(whiteNew > 1800, $"Expected white rating > 1800, got {whiteNew}");

        // Weak player should lose very little (in absolute terms)
        Assert.True(redNew > 1195, $"Expected red rating > 1195, got {redNew}");
        Assert.True(redNew < 1200, $"Expected red rating < 1200, got {redNew}");
    }

    [Fact]
    public void CalculateNewRatings_MixedExperience_DifferentKFactors()
    {
        // New white player (K=32) vs established red player (K=24)
        // Equal rating, white wins

        // Act
        var (whiteNew, redNew) = _service.CalculateNewRatings(
            whiteRating: 1500,
            redRating: 1500,
            whiteRatedGames: 0,  // New player
            redRatedGames: 50,   // Established player
            whiteWon: true);

        // Assert
        // White gain = 32 * 0.5 = 16
        Assert.Equal(1516, whiteNew);

        // Red loss = 24 * 0.5 = 12
        Assert.Equal(1488, redNew);
    }

    [Fact]
    public void CalculateNewRatings_ExpectedScoresSumToOne()
    {
        // The sum of both players' expected scores should always equal 1

        // Act
        var whiteExpected = _service.CalculateExpectedScore(1600, 1400);
        var redExpected = _service.CalculateExpectedScore(1400, 1600);

        // Assert
        Assert.Equal(1.0, whiteExpected + redExpected, 4);
    }

    [Fact]
    public void CalculateNewRatings_RatingConservation_WithEqualKFactors()
    {
        // When both players have same K-factor and equal ratings,
        // total rating change should be zero (one gains X, other loses X)

        // Act
        var (whiteNew, redNew) = _service.CalculateNewRatings(
            whiteRating: 1500,
            redRating: 1500,
            whiteRatedGames: 30,
            redRatedGames: 30,
            whiteWon: true);

        // Assert
        var totalBefore = 1500 + 1500;
        var totalAfter = whiteNew + redNew;

        Assert.Equal(totalBefore, totalAfter);
    }

    [Fact]
    public void CalculateNewRatings_VeryHighRatings_StillWorks()
    {
        // Test with very high ratings to ensure no overflow or calculation issues

        // Act
        var (whiteNew, redNew) = _service.CalculateNewRatings(
            whiteRating: 2800,
            redRating: 2750,
            whiteRatedGames: 100,
            redRatedGames: 100,
            whiteWon: true);

        // Assert - should produce reasonable results
        Assert.True(whiteNew > 2800);
        Assert.True(redNew < 2750);
        Assert.True(whiteNew < 2850); // Shouldn't gain too much
        Assert.True(redNew > 2700); // Shouldn't lose too much
    }

    [Fact]
    public void CalculateNewRatings_VeryLowRatings_StillWorks()
    {
        // Test with very low ratings (though in practice we might enforce a floor)

        // Act
        var (whiteNew, redNew) = _service.CalculateNewRatings(
            whiteRating: 200,
            redRating: 250,
            whiteRatedGames: 30,
            redRatedGames: 30,
            whiteWon: true);

        // Assert - should produce reasonable results
        Assert.True(whiteNew > 200);
        Assert.True(redNew < 250);
    }

    [Fact]
    public void Constructor_CustomConfiguration_UsesCustomValues()
    {
        // Arrange
        var customConfig = new Dictionary<string, string?>
        {
            { "EloRating:KFactorNew", "40" },
            { "EloRating:KFactorEstablished", "20" },
            { "EloRating:GamesForEstablished", "50" }
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(customConfig)
            .Build();

        var service = new EloRatingService(config, _mockLogger.Object);

        // Act
        var kFactorNew = service.GetKFactor(0);
        var kFactorEstablished = service.GetKFactor(50);

        // Assert
        Assert.Equal(40, kFactorNew);
        Assert.Equal(20, kFactorEstablished);
    }

    [Fact]
    public void Constructor_MissingConfiguration_UsesDefaults()
    {
        // Arrange - empty configuration
        var emptyConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var service = new EloRatingService(emptyConfig, _mockLogger.Object);

        // Act
        var kFactorNew = service.GetKFactor(0);
        var kFactorEstablished = service.GetKFactor(30);

        // Assert - should use defaults
        Assert.Equal(32, kFactorNew);
        Assert.Equal(24, kFactorEstablished);
    }

    [Fact]
    public void CalculateNewRatings_LoserRatingWouldDropBelow100_ReturnsRawValue()
    {
        // Player with rating 110 loses to equal-rated opponent
        // Will drop by ~12 points (110 - 12 = 98)
        // Note: Rating floor enforcement happens in PlayerStatsService, not here
        // Act
        var (whiteNew, redNew) = _service.CalculateNewRatings(
            whiteRating: 110,
            redRating: 110,
            whiteRatedGames: 30,
            redRatedGames: 30,
            whiteWon: false);

        // Assert - returns raw calculated value (floor enforcement is in PlayerStatsService)
        Assert.Equal(98, whiteNew);
        Assert.True(redNew > 110); // Red should gain ~12 points
    }

    [Fact]
    public void CalculateNewRatings_WinnerAtRatingFloor_CanIncrease()
    {
        // Player at minimum rating (100) can still gain rating when they win
        // Act
        var (whiteNew, redNew) = _service.CalculateNewRatings(
            whiteRating: 100,
            redRating: 1500,
            whiteRatedGames: 30,
            redRatedGames: 30,
            whiteWon: true);

        // Assert - white should be able to increase from floor
        Assert.True(whiteNew > 100, $"Expected white rating > 100, got {whiteNew}");
        Assert.True(redNew < 1500);
    }

    [Fact]
    public void CalculateNewRatings_BothPlayersAtFloor_ReturnsRawValues()
    {
        // Both players at minimum rating (100)
        // Note: Rating floor enforcement happens in PlayerStatsService, not here
        // Act
        var (whiteNew, redNew) = _service.CalculateNewRatings(
            whiteRating: 100,
            redRating: 100,
            whiteRatedGames: 30,
            redRatedGames: 30,
            whiteWon: true);

        // Assert - returns raw calculated values
        Assert.True(whiteNew > 100, "Winner should gain rating from floor");
        Assert.True(redNew < 100, "Loser drops below 100 in raw calculation (floor applied in PlayerStatsService)");
    }
}
