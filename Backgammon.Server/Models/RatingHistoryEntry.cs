using System.Text.Json.Serialization;

namespace Backgammon.Server.Models;

/// <summary>
/// Represents a single rating change in a user's rating history.
/// Stored in DynamoDB with PK=USER#{userId}, SK=RATING#{timestamp}
/// </summary>
public class RatingHistoryEntry
{
    /// <summary>
    /// The user ID this rating entry belongs to
    /// </summary>
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// When this rating change occurred
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// The user's rating after this game
    /// </summary>
    [JsonPropertyName("rating")]
    public int Rating { get; set; }

    /// <summary>
    /// The rating change from this game (positive or negative)
    /// </summary>
    [JsonPropertyName("ratingChange")]
    public int RatingChange { get; set; }

    /// <summary>
    /// The game ID that caused this rating change
    /// </summary>
    [JsonPropertyName("gameId")]
    public string GameId { get; set; } = string.Empty;

    /// <summary>
    /// The opponent's user ID
    /// </summary>
    [JsonPropertyName("opponentUserId")]
    public string? OpponentUserId { get; set; }

    /// <summary>
    /// The opponent's username (denormalized for display)
    /// </summary>
    [JsonPropertyName("opponentUsername")]
    public string? OpponentUsername { get; set; }

    /// <summary>
    /// Whether the user won this game
    /// </summary>
    [JsonPropertyName("won")]
    public bool Won { get; set; }
}
