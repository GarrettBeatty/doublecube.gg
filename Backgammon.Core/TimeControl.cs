namespace Backgammon.Core;

/// <summary>
/// Type of time control used in a match
/// </summary>
public enum TimeControlType
{
    None, // Casual mode - no time limits
    ChicagoPoint // Chicago Point Simple Delay
}

/// <summary>
/// Configuration for match time controls
/// </summary>
#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type
public class TimeControlConfig
#pragma warning restore SA1402
#pragma warning restore SA1649
{
    public TimeControlType Type { get; set; } = TimeControlType.None;

    // Chicago Point settings (12 seconds delay per move)
    public int DelaySeconds { get; set; } = 12;

    /// <summary>
    /// Calculate initial reserve time for Chicago Point
    /// Formula: 2min per match point - 1min per point scored by either player
    /// </summary>
    public TimeSpan CalculateReserveTime(int targetScore, int player1Score, int player2Score)
    {
        if (Type != TimeControlType.ChicagoPoint)
        {
            return TimeSpan.Zero;
        }

        int totalPointsScored = player1Score + player2Score;
        int reserveMinutes = (2 * targetScore) - totalPointsScored;
        return TimeSpan.FromMinutes(Math.Max(reserveMinutes, 1)); // Minimum 1 minute
    }
}

/// <summary>
/// Tracks time remaining for a player in a game
/// </summary>
#pragma warning disable SA1402 // File may only contain a single type
public class PlayerTimeState
#pragma warning restore SA1402
{
    public TimeSpan ReserveTime { get; set; }

    public DateTime? TurnStartTime { get; set; }

    public bool IsInDelay { get; set; } = true;

    public DateTime? DelayStartTime { get; set; }

    /// <summary>
    /// Calculate if currently in delay period (based on elapsed time)
    /// </summary>
    public bool CalculateIsInDelay(int delaySeconds)
    {
        if (TurnStartTime == null)
        {
            return false;
        }

        var elapsed = DateTime.UtcNow - TurnStartTime.Value;
        return elapsed.TotalSeconds < delaySeconds;
    }

    /// <summary>
    /// Check if player has run out of time
    /// </summary>
    public bool HasTimedOut(int delaySeconds)
    {
        if (TurnStartTime == null)
        {
            return false;
        }

        var elapsed = DateTime.UtcNow - TurnStartTime.Value;

        if (IsInDelay)
        {
            // Still in delay period - check if delay time exceeded
            if (elapsed.TotalSeconds < delaySeconds)
            {
                return false;
            }

            // Delay exceeded, now check reserve
            var reserveUsed = elapsed - TimeSpan.FromSeconds(delaySeconds);
            return ReserveTime - reserveUsed <= TimeSpan.Zero;
        }

        // Already burning reserve time
        var totalReserveUsed = elapsed - TimeSpan.FromSeconds(delaySeconds);
        return ReserveTime - totalReserveUsed <= TimeSpan.Zero;
    }

    /// <summary>
    /// Get reserve time remaining (does not include delay)
    /// </summary>
    public TimeSpan GetRemainingTime(int delaySeconds)
    {
        if (TurnStartTime == null)
        {
            return ReserveTime;
        }

        var elapsed = DateTime.UtcNow - TurnStartTime.Value;
        var delayTime = TimeSpan.FromSeconds(delaySeconds);

        if (elapsed < delayTime)
        {
            // Still in delay - reserve hasn't been touched yet
            return ReserveTime;
        }

        // Burning reserve - return reserve remaining
        var reserveUsed = elapsed - delayTime;
        var remaining = ReserveTime - reserveUsed;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    /// <summary>
    /// Get delay time remaining (returns 0 if not in delay)
    /// </summary>
    public TimeSpan GetDelayRemaining(int delaySeconds)
    {
        if (TurnStartTime == null)
        {
            return TimeSpan.Zero;
        }

        var elapsed = DateTime.UtcNow - TurnStartTime.Value;
        var delayTime = TimeSpan.FromSeconds(delaySeconds);

        if (elapsed < delayTime)
        {
            return delayTime - elapsed;
        }

        return TimeSpan.Zero;
    }

    /// <summary>
    /// Update state after turn completes
    /// </summary>
    public void EndTurn(int delaySeconds)
    {
        if (TurnStartTime == null)
        {
            return;
        }

        var elapsed = DateTime.UtcNow - TurnStartTime.Value;
        var delayTime = TimeSpan.FromSeconds(delaySeconds);

        if (elapsed > delayTime)
        {
            // Used some reserve time
            var reserveUsed = elapsed - delayTime;
            ReserveTime -= reserveUsed;
            if (ReserveTime < TimeSpan.Zero)
            {
                ReserveTime = TimeSpan.Zero;
            }
        }

        // If move completed within delay, no reserve consumed
        TurnStartTime = null;
        DelayStartTime = null;
        IsInDelay = true;
    }

    /// <summary>
    /// Start a new turn
    /// </summary>
    public void StartTurn()
    {
        TurnStartTime = DateTime.UtcNow;
        DelayStartTime = DateTime.UtcNow;
        IsInDelay = true;
    }
}
