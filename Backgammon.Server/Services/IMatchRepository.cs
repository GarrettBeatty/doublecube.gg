using Backgammon.Server.Models;

namespace Backgammon.Server.Services;

/// <summary>
/// Repository interface for match persistence operations
/// </summary>
public interface IMatchRepository
{
    /// <summary>
    /// Save a match to the database
    /// </summary>
    Task SaveMatchAsync(Match match);

    /// <summary>
    /// Get a match by its ID
    /// </summary>
    Task<Match?> GetMatchByIdAsync(string matchId);

    /// <summary>
    /// Update match scores and status
    /// </summary>
    Task UpdateMatchAsync(Match match);

    /// <summary>
    /// Get matches for a specific player
    /// </summary>
    Task<List<Match>> GetPlayerMatchesAsync(string playerId, string? status = null, int limit = 50, int skip = 0);

    /// <summary>
    /// Get active matches (in progress)
    /// </summary>
    Task<List<Match>> GetActiveMatchesAsync();

    /// <summary>
    /// Get recent matches
    /// </summary>
    Task<List<Match>> GetRecentMatchesAsync(string? status = "Completed", int limit = 20);

    /// <summary>
    /// Get match statistics for a player
    /// </summary>
    Task<MatchStats> GetPlayerMatchStatsAsync(string playerId);

    /// <summary>
    /// Delete a match and all associated data
    /// </summary>
    Task DeleteMatchAsync(string matchId);

    /// <summary>
    /// Add a game to a match
    /// </summary>
    Task AddGameToMatchAsync(string matchId, string gameId);

    /// <summary>
    /// Update match status
    /// </summary>
    Task UpdateMatchStatusAsync(string matchId, string status);
}
