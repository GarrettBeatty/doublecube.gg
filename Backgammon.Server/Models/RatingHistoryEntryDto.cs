using Tapper;

namespace Backgammon.Server.Models;

/// <summary>
/// DTO for returning rating history to the client
/// </summary>
[TranspilationSource]
public class RatingHistoryEntryDto
{
    /// <summary>
    /// When this rating change occurred
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// The user's rating after this game
    /// </summary>
    public int Rating { get; set; }

    /// <summary>
    /// The rating change from this game (positive or negative)
    /// </summary>
    public int RatingChange { get; set; }

    /// <summary>
    /// The game ID that caused this rating change
    /// </summary>
    public string GameId { get; set; } = string.Empty;

    /// <summary>
    /// The opponent's username
    /// </summary>
    public string? OpponentUsername { get; set; }

    /// <summary>
    /// Whether the user won this game
    /// </summary>
    public bool Won { get; set; }

    /// <summary>
    /// Create DTO from entity
    /// </summary>
    public static RatingHistoryEntryDto FromEntity(RatingHistoryEntry entry)
    {
        return new RatingHistoryEntryDto
        {
            Timestamp = entry.Timestamp,
            Rating = entry.Rating,
            RatingChange = entry.RatingChange,
            GameId = entry.GameId,
            OpponentUsername = entry.OpponentUsername,
            Won = entry.Won
        };
    }
}
