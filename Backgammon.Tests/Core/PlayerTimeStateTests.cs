using Backgammon.Core;
using Xunit;

namespace Backgammon.Tests.Core;

/// <summary>
/// Tests for PlayerTimeState - time control logic for backgammon games.
/// </summary>
public class PlayerTimeStateTests
{
    private const int DefaultDelaySeconds = 12;

    [Fact]
    public void StartTurn_SetsTimestamps()
    {
        // Arrange
        var timeState = new PlayerTimeState
        {
            ReserveTime = TimeSpan.FromMinutes(10)
        };

        // Act
        timeState.StartTurn();

        // Assert
        Assert.NotNull(timeState.TurnStartTime);
        Assert.NotNull(timeState.DelayStartTime);
        Assert.True(timeState.IsInDelay);
    }

    [Fact]
    public void EndTurn_WithoutStartTurn_DoesNothing()
    {
        // Arrange
        var timeState = new PlayerTimeState
        {
            ReserveTime = TimeSpan.FromMinutes(10)
        };

        // Act
        timeState.EndTurn(DefaultDelaySeconds);

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(10), timeState.ReserveTime);
        Assert.Null(timeState.TurnStartTime);
    }

    [Fact]
    public void GetRemainingTime_BeforeTurnStart_ReturnsFullReserve()
    {
        // Arrange
        var timeState = new PlayerTimeState
        {
            ReserveTime = TimeSpan.FromMinutes(10)
        };

        // Act
        var remaining = timeState.GetRemainingTime(DefaultDelaySeconds);

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(10), remaining);
    }

    [Fact]
    public void GetDelayRemaining_BeforeTurnStart_ReturnsZero()
    {
        // Arrange
        var timeState = new PlayerTimeState
        {
            ReserveTime = TimeSpan.FromMinutes(10)
        };

        // Act
        var delayRemaining = timeState.GetDelayRemaining(DefaultDelaySeconds);

        // Assert
        Assert.Equal(TimeSpan.Zero, delayRemaining);
    }

    [Fact]
    public void CalculateIsInDelay_BeforeTurnStart_ReturnsFalse()
    {
        // Arrange
        var timeState = new PlayerTimeState
        {
            ReserveTime = TimeSpan.FromMinutes(10)
        };

        // Act
        var isInDelay = timeState.CalculateIsInDelay(DefaultDelaySeconds);

        // Assert
        Assert.False(isInDelay);
    }

    [Fact]
    public void HasTimedOut_BeforeTurnStart_ReturnsFalse()
    {
        // Arrange
        var timeState = new PlayerTimeState
        {
            ReserveTime = TimeSpan.FromMinutes(10)
        };

        // Act
        var timedOut = timeState.HasTimedOut(DefaultDelaySeconds);

        // Assert
        Assert.False(timedOut);
    }

    [Fact]
    public void CalculateIsInDelay_JustStarted_ReturnsTrue()
    {
        // Arrange
        var timeState = new PlayerTimeState
        {
            ReserveTime = TimeSpan.FromMinutes(10)
        };
        timeState.StartTurn();

        // Act
        var isInDelay = timeState.CalculateIsInDelay(DefaultDelaySeconds);

        // Assert
        Assert.True(isInDelay);
    }

    [Fact]
    public void GetDelayRemaining_JustStarted_ReturnsPositiveValue()
    {
        // Arrange
        var timeState = new PlayerTimeState
        {
            ReserveTime = TimeSpan.FromMinutes(10)
        };
        timeState.StartTurn();

        // Act
        var delayRemaining = timeState.GetDelayRemaining(DefaultDelaySeconds);

        // Assert
        Assert.True(delayRemaining > TimeSpan.Zero);
        Assert.True(delayRemaining <= TimeSpan.FromSeconds(DefaultDelaySeconds));
    }

    [Fact]
    public void GetRemainingTime_DuringDelay_ReturnsFullReserve()
    {
        // Arrange
        var timeState = new PlayerTimeState
        {
            ReserveTime = TimeSpan.FromMinutes(10)
        };
        timeState.StartTurn();

        // Act - check immediately (should still be in delay)
        var remaining = timeState.GetRemainingTime(DefaultDelaySeconds);

        // Assert - reserve should not be consumed during delay
        Assert.Equal(TimeSpan.FromMinutes(10), remaining);
    }

    [Fact]
    public void HasTimedOut_WithReserveRemaining_ReturnsFalse()
    {
        // Arrange
        var timeState = new PlayerTimeState
        {
            ReserveTime = TimeSpan.FromMinutes(10)
        };
        timeState.StartTurn();

        // Act
        var timedOut = timeState.HasTimedOut(DefaultDelaySeconds);

        // Assert
        Assert.False(timedOut);
    }

    [Fact]
    public void EndTurn_ResetsTimestamps()
    {
        // Arrange
        var timeState = new PlayerTimeState
        {
            ReserveTime = TimeSpan.FromMinutes(10)
        };
        timeState.StartTurn();

        // Act
        timeState.EndTurn(DefaultDelaySeconds);

        // Assert
        Assert.Null(timeState.TurnStartTime);
        Assert.Null(timeState.DelayStartTime);
        Assert.True(timeState.IsInDelay);
    }
}
