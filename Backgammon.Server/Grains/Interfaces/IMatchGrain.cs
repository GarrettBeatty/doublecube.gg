using Orleans;

namespace Backgammon.Server.Grains.Interfaces;

/// <summary>
/// Manages in-memory match state (complement to DB-persisted MatchService).
/// Key = matchId (string).
/// </summary>
public interface IMatchGrain : IGrainWithStringKey
{
    /// <summary>Get the current game ID for this match.</summary>
    Task<string?> GetCurrentGameIdAsync();

    /// <summary>Set the current active game ID.</summary>
    Task SetCurrentGameIdAsync(string gameId);

    /// <summary>Get all player connection IDs for a player in this match.</summary>
    Task<List<string>> GetPlayerConnectionsAsync(string playerId);

    /// <summary>Update connection tracking for a player in this match.</summary>
    Task TrackPlayerConnectionAsync(string playerId, string connectionId);

    /// <summary>Remove a player connection from tracking.</summary>
    Task RemovePlayerConnectionAsync(string playerId, string connectionId);
}
