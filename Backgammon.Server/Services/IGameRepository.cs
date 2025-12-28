using Backgammon.Server.Models;

namespace Backgammon.Server.Services;

/// <summary>
/// Storage abstraction for completed backgammon games.
/// Allows swapping database implementations (MongoDB, DynamoDB, CosmosDB, etc.)
/// without changing business logic.
/// </summary>
public interface IGameRepository
{
    /// <summary>
    /// Save a completed game to persistent storage
    /// </summary>
    Task SaveCompletedGameAsync(CompletedGame game);
    
    /// <summary>
    /// Retrieve a specific game by its ID
    /// </summary>
    Task<CompletedGame?> GetGameByIdAsync(string gameId);
    
    /// <summary>
    /// Get all games for a specific player (as either color)
    /// </summary>
    /// <param name="playerId">Player identifier</param>
    /// <param name="limit">Maximum number of games to return</param>
    /// <param name="skip">Number of games to skip (for pagination)</param>
    Task<List<CompletedGame>> GetPlayerGamesAsync(string playerId, int limit = 50, int skip = 0);
    
    /// <summary>
    /// Get player statistics (wins, losses, stakes earned)
    /// </summary>
    Task<PlayerStats> GetPlayerStatsAsync(string playerId);
    
    /// <summary>
    /// Get recent games across all players
    /// </summary>
    Task<List<CompletedGame>> GetRecentGamesAsync(int limit = 20);
    
    /// <summary>
    /// Count total games in database
    /// </summary>
    Task<long> GetTotalGameCountAsync();
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
