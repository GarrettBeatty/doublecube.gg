using System.Text.Json.Serialization;

namespace Backgammon.Server.Models;

/// <summary>
/// Data transfer object for puzzle submission result.
/// </summary>
public class PuzzleResultDto
{
    /// <summary>
    /// Whether the submitted solution was correct
    /// </summary>
    [JsonPropertyName("isCorrect")]
    public bool IsCorrect { get; set; }

    /// <summary>
    /// Equity loss compared to optimal (0 = perfect, higher = worse)
    /// </summary>
    [JsonPropertyName("equityLoss")]
    public double EquityLoss { get; set; }

    /// <summary>
    /// Human-readable feedback message
    /// </summary>
    [JsonPropertyName("feedback")]
    public string Feedback { get; set; } = string.Empty;

    /// <summary>
    /// The optimal moves - only revealed when correct or after giving up
    /// </summary>
    [JsonPropertyName("bestMoves")]
    public List<MoveDto>? BestMoves { get; set; }

    /// <summary>
    /// Best moves in notation format
    /// </summary>
    [JsonPropertyName("bestMovesNotation")]
    public string? BestMovesNotation { get; set; }

    /// <summary>
    /// User's current streak after this attempt
    /// </summary>
    [JsonPropertyName("currentStreak")]
    public int CurrentStreak { get; set; }

    /// <summary>
    /// Whether the streak was broken (missed a day)
    /// </summary>
    [JsonPropertyName("streakBroken")]
    public bool StreakBroken { get; set; }

    /// <summary>
    /// Total number of attempts on this puzzle
    /// </summary>
    [JsonPropertyName("attemptCount")]
    public int AttemptCount { get; set; }
}
