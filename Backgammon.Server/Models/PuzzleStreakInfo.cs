using System.Text.Json.Serialization;

namespace Backgammon.Server.Models;

/// <summary>
/// Entity model for a user's puzzle streak information.
/// Stored in DynamoDB with PK=USER#{userId}, SK=PUZZLE_STREAK
/// </summary>
public class PuzzleStreakInfo
{
    /// <summary>
    /// User ID
    /// </summary>
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Current consecutive days streak
    /// </summary>
    [JsonPropertyName("currentStreak")]
    public int CurrentStreak { get; set; }

    /// <summary>
    /// Best streak ever achieved
    /// </summary>
    [JsonPropertyName("bestStreak")]
    public int BestStreak { get; set; }

    /// <summary>
    /// Date of the last puzzle solved (yyyy-MM-dd format)
    /// </summary>
    [JsonPropertyName("lastSolvedDate")]
    public string? LastSolvedDate { get; set; }

    /// <summary>
    /// Total number of puzzles solved
    /// </summary>
    [JsonPropertyName("totalSolved")]
    public int TotalSolved { get; set; }

    /// <summary>
    /// Total number of puzzle attempts (including incorrect)
    /// </summary>
    [JsonPropertyName("totalAttempts")]
    public int TotalAttempts { get; set; }
}
