using Tapper;

namespace Backgammon.Server.Models;

/// <summary>
/// Data transfer object for recent opponent information
/// </summary>
[TranspilationSource]
public class RecentOpponentDto
{
    /// <summary>
    /// Opponent's unique ID
    /// </summary>
    public string OpponentId { get; set; } = string.Empty;

    /// <summary>
    /// Opponent's display name
    /// </summary>
    public string OpponentName { get; set; } = string.Empty;

    /// <summary>
    /// Opponent's current rating
    /// </summary>
    public int OpponentRating { get; set; }

    /// <summary>
    /// Total matches played against this opponent
    /// </summary>
    public int TotalMatches { get; set; }

    /// <summary>
    /// User's wins against this opponent
    /// </summary>
    public int Wins { get; set; }

    /// <summary>
    /// User's losses against this opponent
    /// </summary>
    public int Losses { get; set; }

    /// <summary>
    /// Formatted head-to-head record (e.g., "3-2")
    /// </summary>
    public string Record => $"{Wins}-{Losses}";

    /// <summary>
    /// Win rate against this opponent (0-100)
    /// </summary>
    public double WinRate => TotalMatches > 0 ? Math.Round((double)Wins / TotalMatches * 100, 1) : 0;

    /// <summary>
    /// When the last match was played
    /// </summary>
    public DateTime LastPlayedAt { get; set; }

    /// <summary>
    /// Whether the opponent is an AI
    /// </summary>
    public bool IsAi { get; set; }
}
