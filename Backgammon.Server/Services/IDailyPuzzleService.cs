using Backgammon.Server.Models;

namespace Backgammon.Server.Services;

/// <summary>
/// Service interface for daily puzzle operations.
/// </summary>
public interface IDailyPuzzleService
{
    /// <summary>
    /// Get today's puzzle for a user.
    /// Includes whether they've already solved it and their attempt count.
    /// </summary>
    Task<DailyPuzzleDto?> GetTodaysPuzzleAsync(string? userId);

    /// <summary>
    /// Get a puzzle for a specific date.
    /// </summary>
    Task<DailyPuzzleDto?> GetPuzzleByDateAsync(string date, string? userId);

    /// <summary>
    /// Submit an answer to a puzzle.
    /// </summary>
    Task<PuzzleResultDto> SubmitAnswerAsync(string userId, string puzzleDate, List<MoveDto> moves);

    /// <summary>
    /// Get user's streak information.
    /// </summary>
    Task<PuzzleStreakInfo> GetStreakInfoAsync(string userId);

    /// <summary>
    /// Generate and save a new puzzle for a specific date.
    /// </summary>
    Task<DailyPuzzle> GeneratePuzzleForDateAsync(string date);

    /// <summary>
    /// Check if a puzzle exists for a specific date.
    /// </summary>
    Task<bool> PuzzleExistsAsync(string date);
}
