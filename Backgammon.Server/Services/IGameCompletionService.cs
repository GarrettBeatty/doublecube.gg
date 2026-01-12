using Backgammon.Server.Models;

namespace Backgammon.Server.Services;

/// <summary>
/// Service interface for handling game and match completion.
/// </summary>
public interface IGameCompletionService
{
    /// <summary>
    /// Handles game completion, including database updates, stats, match progression, and broadcasting.
    /// </summary>
    Task HandleGameCompletionAsync(GameSession session);
}
