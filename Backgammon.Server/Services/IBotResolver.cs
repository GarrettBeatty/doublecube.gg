using Backgammon.Plugins.Abstractions;

namespace Backgammon.Server.Services;

/// <summary>
/// Interface for resolving game bots by player ID.
/// </summary>
public interface IBotResolver
{
    /// <summary>
    /// Gets the bot instance for the given player ID.
    /// </summary>
    /// <param name="playerId">The player ID (e.g., "ai_greedy_xxx").</param>
    /// <returns>The bot instance, or null if not found.</returns>
    IGameBot? GetBot(string playerId);

    /// <summary>
    /// Checks if the player ID represents a bot.
    /// </summary>
    /// <param name="playerId">The player ID to check.</param>
    /// <returns>True if the player ID is a bot.</returns>
    bool IsBot(string? playerId);
}
