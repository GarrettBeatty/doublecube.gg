using System.Text.Json.Serialization;

namespace Backgammon.Server.Models;

/// <summary>
/// Time control modes for game timing
/// </summary>
public enum TimeControlMode
{
    /// <summary>No time limit - classic untimed play</summary>
    Untimed,

    /// <summary>30 seconds per player - fast-paced</summary>
    Blitz,

    /// <summary>2 minutes per player - standard competitive</summary>
    Rapid,

    /// <summary>5 minutes per player - longer format</summary>
    Classical,

    /// <summary>User-defined time limit</summary>
    Custom
}

/// <summary>
/// Chess-clock style time control for timed games.
/// Tracks remaining time for each player and manages the active clock.
/// </summary>
public class TimeControl
{
    /// <summary>
    /// Time control mode
    /// </summary>
    [JsonPropertyName("mode")]
    public TimeControlMode Mode { get; set; } = TimeControlMode.Untimed;

    /// <summary>
    /// Initial time in seconds for each player (e.g., 30, 120, 300)
    /// </summary>
    [JsonPropertyName("initialTimeSeconds")]
    public int InitialTimeSeconds { get; set; }

    /// <summary>
    /// White player's remaining time in milliseconds
    /// </summary>
    [JsonPropertyName("whiteRemainingMs")]
    public long WhiteRemainingMs { get; set; }

    /// <summary>
    /// Red player's remaining time in milliseconds
    /// </summary>
    [JsonPropertyName("redRemainingMs")]
    public long RedRemainingMs { get; set; }

    /// <summary>
    /// When the current player's clock started (null if not running)
    /// </summary>
    [JsonPropertyName("clockStartTime")]
    public DateTime? ClockStartTime { get; set; }

    /// <summary>
    /// Whether the clock is currently paused (e.g., during disconnection)
    /// </summary>
    [JsonPropertyName("isPaused")]
    public bool IsPaused { get; set; }

    /// <summary>
    /// Create an untimed game control
    /// </summary>
    public static TimeControl Untimed()
    {
        return new TimeControl
        {
            Mode = TimeControlMode.Untimed,
            InitialTimeSeconds = 0,
            WhiteRemainingMs = 0,
            RedRemainingMs = 0
        };
    }

    /// <summary>
    /// Create a blitz time control (30 seconds per player)
    /// </summary>
    public static TimeControl Blitz()
    {
        const int seconds = 30;
        return new TimeControl
        {
            Mode = TimeControlMode.Blitz,
            InitialTimeSeconds = seconds,
            WhiteRemainingMs = seconds * 1000,
            RedRemainingMs = seconds * 1000
        };
    }

    /// <summary>
    /// Create a rapid time control (2 minutes per player)
    /// </summary>
    public static TimeControl Rapid()
    {
        const int seconds = 120;
        return new TimeControl
        {
            Mode = TimeControlMode.Rapid,
            InitialTimeSeconds = seconds,
            WhiteRemainingMs = seconds * 1000,
            RedRemainingMs = seconds * 1000
        };
    }

    /// <summary>
    /// Create a classical time control (5 minutes per player)
    /// </summary>
    public static TimeControl Classical()
    {
        const int seconds = 300;
        return new TimeControl
        {
            Mode = TimeControlMode.Classical,
            InitialTimeSeconds = seconds,
            WhiteRemainingMs = seconds * 1000,
            RedRemainingMs = seconds * 1000
        };
    }

    /// <summary>
    /// Create a custom time control with specified seconds per player
    /// </summary>
    public static TimeControl Custom(int secondsPerPlayer)
    {
        return new TimeControl
        {
            Mode = TimeControlMode.Custom,
            InitialTimeSeconds = secondsPerPlayer,
            WhiteRemainingMs = secondsPerPlayer * 1000,
            RedRemainingMs = secondsPerPlayer * 1000
        };
    }

    /// <summary>
    /// Create a time control from mode
    /// </summary>
    public static TimeControl FromMode(TimeControlMode mode, int? customSeconds = null)
    {
        return mode switch
        {
            TimeControlMode.Blitz => Blitz(),
            TimeControlMode.Rapid => Rapid(),
            TimeControlMode.Classical => Classical(),
            TimeControlMode.Custom => Custom(customSeconds ?? 120),
            _ => Untimed()
        };
    }
}
