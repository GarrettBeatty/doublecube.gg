using Backgammon.Server.Models;

namespace Backgammon.Server.Services;

/// <summary>
/// Repository interface for daily puzzle persistence operations.
/// </summary>
public interface IPuzzleRepository
{
    /// <summary>
    /// Save a new daily puzzle.
    /// </summary>
    Task SavePuzzleAsync(DailyPuzzle puzzle);

    /// <summary>
    /// Get puzzle by date (yyyy-MM-dd format).
    /// </summary>
    Task<DailyPuzzle?> GetPuzzleByDateAsync(string date);

    /// <summary>
    /// Check if a puzzle exists for the given date.
    /// </summary>
    Task<bool> PuzzleExistsAsync(string date);

    /// <summary>
    /// Increment the solved count for a puzzle.
    /// </summary>
    Task IncrementSolvedCountAsync(string puzzleDate);

    /// <summary>
    /// Increment the attempt count for a puzzle.
    /// </summary>
    Task IncrementAttemptCountAsync(string puzzleDate);

    /// <summary>
    /// Save a user's puzzle attempt.
    /// </summary>
    Task SaveAttemptAsync(PuzzleAttempt attempt);

    /// <summary>
    /// Get a user's attempt for a specific puzzle date.
    /// </summary>
    Task<PuzzleAttempt?> GetAttemptAsync(string userId, string puzzleDate);

    /// <summary>
    /// Update an existing attempt (e.g., when user retries or solves).
    /// </summary>
    Task UpdateAttemptAsync(PuzzleAttempt attempt);

    /// <summary>
    /// Get user's puzzle streak info.
    /// </summary>
    Task<PuzzleStreakInfo?> GetStreakInfoAsync(string userId);

    /// <summary>
    /// Save or update user's puzzle streak info.
    /// </summary>
    Task SaveStreakInfoAsync(PuzzleStreakInfo streakInfo);
}
