using Tapper;

namespace Backgammon.Server.Models;

/// <summary>
/// Configuration for creating a new match
/// </summary>
[TranspilationSource]
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
    /// Time control type: "None", "ChicagoPoint" (defaults to "ChicagoPoint")
    /// All lobby and AI games use ChicagoPoint time control
    /// </summary>
    public string TimeControlType { get; set; } = "ChicagoPoint";

    /// <summary>
    /// Whether the match affects player ratings (defaults to true)
    /// AI matches are always unrated regardless of this setting
    /// </summary>
    public bool IsRated { get; set; } = true;

    /// <summary>
    /// AI type for AI matches: "greedy" or "random" (defaults to "greedy")
    /// Only used when OpponentType is "AI"
    /// </summary>
    public string AiType { get; set; } = "greedy";

    /// <summary>
    /// Whether this is a correspondence (async) match
    /// </summary>
    public bool IsCorrespondence { get; set; }

    /// <summary>
    /// Time allowed per move in days (for correspondence matches)
    /// Valid values: 1, 3, 7, or custom
    /// </summary>
    public int TimePerMoveDays { get; set; } = 3;
}
