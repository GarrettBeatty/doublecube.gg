using Tapper;

namespace Backgammon.Server.Models;

/// <summary>
/// Leaderboard entry for player rankings.
/// </summary>
[TranspilationSource]
public class LeaderboardEntryDto
{
    /// <summary>
    /// Rank position (1 = highest).
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    /// User's unique ID.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Username.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Display name shown to other players.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Current ELO rating.
    /// </summary>
    public int Rating { get; set; }

    /// <summary>
    /// Total number of games played.
    /// </summary>
    public int TotalGames { get; set; }

    /// <summary>
    /// Number of wins.
    /// </summary>
    public int Wins { get; set; }

    /// <summary>
    /// Number of losses.
    /// </summary>
    public int Losses { get; set; }

    /// <summary>
    /// Win rate percentage.
    /// </summary>
    public double WinRate { get; set; }

    /// <summary>
    /// Whether this player is currently online.
    /// </summary>
    public bool IsOnline { get; set; }
}
