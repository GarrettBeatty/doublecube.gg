using Backgammon.Core;
using Xunit;

namespace Backgammon.Tests.Core;

/// <summary>
/// Tests for TimeControlConfig - time control configuration.
/// </summary>
public class TimeControlConfigTests
{
    [Fact]
    public void CalculateReserveTime_NoTimeControl_ReturnsZero()
    {
        // Arrange
        var config = new TimeControlConfig
        {
            Type = TimeControlType.None
        };

        // Act
        var reserve = config.CalculateReserveTime(targetScore: 5, player1Score: 0, player2Score: 0);

        // Assert
        Assert.Equal(TimeSpan.Zero, reserve);
    }

    [Fact]
    public void CalculateReserveTime_ChicagoPoint_ZeroScores_CalculatesCorrectly()
    {
        // Arrange
        var config = new TimeControlConfig
        {
            Type = TimeControlType.ChicagoPoint
        };

        // Act - 5 point match, no points scored
        // Formula: 2min × 5 points - (0 + 0) = 10 minutes
        var reserve = config.CalculateReserveTime(targetScore: 5, player1Score: 0, player2Score: 0);

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(10), reserve);
    }

    [Fact]
    public void CalculateReserveTime_ChicagoPoint_WithScores_ReducesTime()
    {
        // Arrange
        var config = new TimeControlConfig
        {
            Type = TimeControlType.ChicagoPoint
        };

        // Act - 5 point match, 2-1 score
        // Formula: 2min × 5 points - (2 + 1) = 10 - 3 = 7 minutes
        var reserve = config.CalculateReserveTime(targetScore: 5, player1Score: 2, player2Score: 1);

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(7), reserve);
    }

    [Fact]
    public void CalculateReserveTime_ChicagoPoint_NearMatchEnd_ReturnsMinimum()
    {
        // Arrange
        var config = new TimeControlConfig
        {
            Type = TimeControlType.ChicagoPoint
        };

        // Act - 5 point match, 4-4 score (only 1 point from match end)
        // Formula: 2min × 5 points - (4 + 4) = 10 - 8 = 2 minutes
        var reserve = config.CalculateReserveTime(targetScore: 5, player1Score: 4, player2Score: 4);

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(2), reserve);
    }

    [Fact]
    public void CalculateReserveTime_ChicagoPoint_WouldGoNegative_ReturnsMinimum()
    {
        // Arrange
        var config = new TimeControlConfig
        {
            Type = TimeControlType.ChicagoPoint
        };

        // Act - Edge case: total points scored exceeds formula result
        // Formula would give negative, but should return minimum 1 minute
        var reserve = config.CalculateReserveTime(targetScore: 3, player1Score: 4, player2Score: 4);

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(1), reserve);
    }

    [Fact]
    public void DefaultDelaySeconds_IsTwelve()
    {
        // Arrange & Act
        var config = new TimeControlConfig();

        // Assert
        Assert.Equal(12, config.DelaySeconds);
    }

    [Fact]
    public void DefaultType_IsNone()
    {
        // Arrange & Act
        var config = new TimeControlConfig();

        // Assert
        Assert.Equal(TimeControlType.None, config.Type);
    }
}
