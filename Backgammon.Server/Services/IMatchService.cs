using Backgammon.Server.Models;
using Match = Backgammon.Server.Models.Match;

namespace Backgammon.Server.Services;

/// <summary>
/// Read-only query facade for matches. After Phase 4b, all match mutations
/// (create/join/start-next/complete/abandon and correspondence turn/timeout)
/// live on <see cref="Grains.Interfaces.IMatchGrain"/>. This interface exists
/// so callers can keep using a familiar service API for queries that span many
/// matches and read from the Postgres index tables (kept fresh by the grain's
/// dual-write).
/// </summary>
public interface IMatchService
{
    /// <summary>
    /// Get a match by ID
    /// </summary>
    Task<Match?> GetMatchAsync(string matchId);

    /// <summary>
    /// Get matches for a specific player
    /// </summary>
    Task<List<Match>> GetPlayerMatchesAsync(string playerId, string? status = null);

    /// <summary>
    /// Get active matches
    /// </summary>
    Task<List<Match>> GetActiveMatchesAsync();

    /// <summary>
    /// Get open lobbies waiting for opponents
    /// </summary>
    /// <param name="limit">Maximum number of lobbies to return</param>
    /// <param name="isCorrespondence">Filter by lobby type: true for correspondence, false for regular, null for all</param>
    Task<List<Match>> GetOpenLobbiesAsync(int limit = 50, bool? isCorrespondence = null);

    /// <summary>
    /// Get open regular (non-correspondence) lobbies
    /// </summary>
    Task<List<Match>> GetRegularLobbiesAsync(int limit = 50);

    /// <summary>
    /// Get open correspondence lobbies
    /// </summary>
    Task<List<Match>> GetCorrespondenceLobbiesAsync(int limit = 50);

    /// <summary>
    /// Get match statistics for a player
    /// </summary>
    Task<MatchStats> GetPlayerMatchStatsAsync(string playerId);
}
