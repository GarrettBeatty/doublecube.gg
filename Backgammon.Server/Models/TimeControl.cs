namespace Backgammon.Server.Models;

/// <summary>
/// Time control modes for games (similar to Lichess/chess.com)
/// </summary>
public enum TimeControlMode
{
    Untimed,    // No time limit
    Blitz,      // 30 seconds per player
    Rapid,      // 2 minutes per player
    Classical,  // 5 minutes per player
    Custom      // User-defined time limit
}

/// <summary>
/// Represents time control settings for a game
/// </summary>
public class TimeControl
{
    public TimeControlMode Mode { get; set; }
    public int InitialTimeSeconds { get; set; }
    public long WhiteRemainingMs { get; set; }
    public long RedRemainingMs { get; set; }
    public DateTime? ClockStartTime { get; set; }
    public bool IsClockPaused { get; set; }

    public TimeControl(TimeControlMode mode)
    {
        Mode = mode;
        InitialTimeSeconds = GetInitialTimeForMode(mode);
        WhiteRemainingMs = InitialTimeSeconds * 1000L;
        RedRemainingMs = InitialTimeSeconds * 1000L;
        ClockStartTime = null;
        IsClockPaused = false;
    }

    public TimeControl(int customTimeSeconds)
    {
        Mode = TimeControlMode.Custom;
        InitialTimeSeconds = customTimeSeconds;
        WhiteRemainingMs = customTimeSeconds * 1000L;
        RedRemainingMs = customTimeSeconds * 1000L;
        ClockStartTime = null;
        IsClockPaused = false;
    }

    /// <summary>
    /// Parameterless constructor for deserialization
    /// </summary>
    public TimeControl()
    {
        Mode = TimeControlMode.Untimed;
        InitialTimeSeconds = 0;
        WhiteRemainingMs = 0;
        RedRemainingMs = 0;
        ClockStartTime = null;
        IsClockPaused = false;
    }

    private static int GetInitialTimeForMode(TimeControlMode mode)
    {
        return mode switch
        {
            TimeControlMode.Blitz => 30,
            TimeControlMode.Rapid => 120,
            TimeControlMode.Classical => 300,
            TimeControlMode.Custom => 0,
            TimeControlMode.Untimed => 0,
            _ => 0
        };
    }
}
