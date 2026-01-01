using Backgammon.Server.Models;

namespace Backgammon.Server.Services;

/// <summary>
/// Storage abstraction for backgammon games (both in-progress and completed).
/// Allows swapping database implementations (MongoDB, DynamoDB, CosmosDB, etc.)
/// without changing business logic.
/// </summary>
public interface IGameRepository
{
    /// <summary>
    /// Save or update a game (in-progress or completed) to persistent storage
    /// Uses upsert - creates if doesn't exist, updates if it does
    /// </summary>
    Task SaveGameAsync(Game game);

    /// <summary>
    /// Retrieve a specific game by its game ID
    /// </summary>
    Task<Game?> GetGameByGameIdAsync(string gameId);

    /// <summary>
    /// Get all active (in-progress) games for server restart loading
    /// </summary>
    Task<List<Game>> GetActiveGamesAsync();

    /// <summary>
    /// Update the status of a game (e.g., mark as Completed or Abandoned)
    /// </summary>
    Task UpdateGameStatusAsync(string gameId, string status);

    /// <summary>
    /// Get all games for a specific player (as either color)
    /// </summary>
    /// <param name="playerId">Player identifier</param>
    /// <param name="status">Optional status filter ("InProgress", "Completed", "Abandoned")</param>
    /// <param name="limit">Maximum number of games to return</param>
    /// <param name="skip">Number of games to skip (for pagination)</param>
    Task<List<Game>> GetPlayerGamesAsync(string playerId, string? status = null, int limit = 50, int skip = 0);

    /// <summary>
    /// Get player statistics (wins, losses, stakes earned)
    /// Only counts completed games
    /// </summary>
    Task<PlayerStats> GetPlayerStatsAsync(string playerId);

    /// <summary>
    /// Get recent games across all players
    /// </summary>
    /// <param name="status">Optional status filter</param>
    /// <param name="limit">Maximum number of games to return</param>
    Task<List<Game>> GetRecentGamesAsync(string? status = "Completed", int limit = 20);

    /// <summary>
    /// Count total games in database
    /// </summary>
    /// <param name="status">Optional status filter</param>
    Task<long> GetTotalGameCountAsync(string? status = null);

    /// <summary>
    /// Delete a game from the database (use sparingly - prefer status updates)
    /// </summary>
    Task DeleteGameAsync(string gameId);

    /// <summary>
    /// Get all games with a specific status that were last updated before a given timestamp
    /// Used for cleanup/abandonment detection
    /// </summary>
    /// <param name="timestamp">The timestamp cutoff</param>
    /// <param name="status">Game status to filter by</param>
    Task<List<Game>> GetGamesLastUpdatedBeforeAsync(DateTime timestamp, string status);
}

/// <summary>
/// Aggregated player statistics
/// </summary>
public class PlayerStats
{
    public string PlayerId { get; set; } = string.Empty;

    public int TotalGames { get; set; }

    public int Wins { get; set; }

    public int Losses { get; set; }

    public int TotalStakes { get; set; }

    public int NormalWins { get; set; }

    public int GammonWins { get; set; }

    public int BackgammonWins { get; set; }

    public double WinRate => TotalGames > 0 ? (double)Wins / TotalGames : 0;
}
