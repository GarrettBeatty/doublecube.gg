using Backgammon.Server.Models;

namespace Backgammon.Server.Services;

/// <summary>
/// Service for managing player statistics after game completion
/// </summary>
public interface IPlayerStatsService
{
    /// <summary>
    /// Update user statistics after a game is completed
    /// </summary>
    Task UpdateStatsAfterGameCompletionAsync(Game game);
}
