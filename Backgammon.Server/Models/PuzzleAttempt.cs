using System.Text.Json.Serialization;

namespace Backgammon.Server.Models;

/// <summary>
/// Entity model for a user's attempt at a daily puzzle.
/// Stored in DynamoDB with PK=USER#{userId}, SK=PUZZLE_ATTEMPT#{date}
/// </summary>
public class PuzzleAttempt
{
    /// <summary>
    /// User ID who made the attempt
    /// </summary>
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Puzzle ID attempted
    /// </summary>
    [JsonPropertyName("puzzleId")]
    public string PuzzleId { get; set; } = string.Empty;

    /// <summary>
    /// Date of the puzzle in yyyy-MM-dd format
    /// </summary>
    [JsonPropertyName("puzzleDate")]
    public string PuzzleDate { get; set; } = string.Empty;

    /// <summary>
    /// The moves the user submitted
    /// </summary>
    [JsonPropertyName("submittedMoves")]
    public List<MoveDto> SubmittedMoves { get; set; } = new();

    /// <summary>
    /// Submitted moves in notation format
    /// </summary>
    [JsonPropertyName("submittedNotation")]
    public string SubmittedNotation { get; set; } = string.Empty;

    /// <summary>
    /// Whether the user's solution was correct (within tolerance)
    /// </summary>
    [JsonPropertyName("isCorrect")]
    public bool IsCorrect { get; set; }

    /// <summary>
    /// Equity loss compared to the optimal move (0 = perfect)
    /// </summary>
    [JsonPropertyName("equityLoss")]
    public double EquityLoss { get; set; }

    /// <summary>
    /// Number of attempts the user made before getting it right (or giving up)
    /// </summary>
    [JsonPropertyName("attemptCount")]
    public int AttemptCount { get; set; }

    /// <summary>
    /// When the user first attempted this puzzle
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the user solved this puzzle (null if never solved)
    /// </summary>
    [JsonPropertyName("solvedAt")]
    public DateTime? SolvedAt { get; set; }
}
