using Tapper;

namespace Backgammon.Server.Services;

/// <summary>
/// DTO for correspondence game information
/// </summary>
[TranspilationSource]
public class CorrespondenceGameDto
{
    /// <summary>
    /// The unique identifier for the match
    /// </summary>
    public string MatchId { get; set; } = string.Empty;

    /// <summary>
    /// The unique identifier for the current game within the match
    /// </summary>
    public string GameId { get; set; } = string.Empty;

    /// <summary>
    /// The opponent player's unique identifier
    /// </summary>
    public string OpponentId { get; set; } = string.Empty;

    /// <summary>
    /// The opponent player's display name
    /// </summary>
    public string OpponentName { get; set; } = string.Empty;

    /// <summary>
    /// The opponent player's ELO rating
    /// </summary>
    public int OpponentRating { get; set; }

    /// <summary>
    /// True if it's the current player's turn to move
    /// </summary>
    public bool IsYourTurn { get; set; }

    /// <summary>
    /// Number of days allowed per move
    /// </summary>
    public int TimePerMoveDays { get; set; }

    /// <summary>
    /// The absolute deadline (UTC) for the current turn
    /// </summary>
    public DateTime? TurnDeadline { get; set; }

    /// <summary>
    /// Time remaining until the turn deadline expires (formatted as "d.hh:mm:ss")
    /// </summary>
    public string? TimeRemaining { get; set; }

    /// <summary>
    /// Total number of moves made in the current game
    /// </summary>
    public int MoveCount { get; set; }

    /// <summary>
    /// Current match score (e.g., "2-1")
    /// </summary>
    public string MatchScore { get; set; } = string.Empty;

    /// <summary>
    /// Points required to win the match
    /// </summary>
    public int TargetScore { get; set; }

    /// <summary>
    /// True if this is a rated match (affects ELO ratings)
    /// </summary>
    public bool IsRated { get; set; }

    /// <summary>
    /// Timestamp of the last update to this match
    /// </summary>
    public DateTime LastUpdatedAt { get; set; }
}
