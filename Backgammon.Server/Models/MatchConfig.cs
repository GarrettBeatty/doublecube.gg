namespace Backgammon.Server.Models;

/// <summary>
/// Configuration for creating a new match
/// </summary>
public class MatchConfig
{
    /// <summary>
    /// Opponent type: "Friend", "AI", "OpenLobby"
    /// </summary>
    public string OpponentType { get; set; } = string.Empty;

    /// <summary>
    /// Opponent ID (for Friend/AI modes)
    /// </summary>
    public string? OpponentId { get; set; }

    /// <summary>
    /// Target score to win the match
    /// </summary>
    public int TargetScore { get; set; } = 7;

    /// <summary>
    /// Display name for anonymous players
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Time control type: "None", "ChicagoPoint" (defaults to "None")
    /// </summary>
    public string TimeControlType { get; set; } = "None";

    /// <summary>
    /// Whether the match affects player ratings (defaults to true)
    /// AI matches are always unrated regardless of this setting
    /// </summary>
    public bool IsRated { get; set; } = true;
}
