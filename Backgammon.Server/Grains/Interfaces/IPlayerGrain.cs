using Orleans;

namespace Backgammon.Server.Grains.Interfaces;

/// <summary>
/// Tracks a player's active game connections.
/// Replaces the _playerToGame dictionary in GameSessionManager.
/// Key = playerId (string).
/// </summary>
public interface IPlayerGrain : IGrainWithStringKey
{
    /// <summary>Record that this player's connection is in a game.</summary>
    Task TrackConnectionAsync(string connectionId, string gameId);

    /// <summary>Remove a connection mapping (on disconnect).</summary>
    Task RemoveConnectionAsync(string connectionId);

    /// <summary>Get the game ID for a specific connection (null if not found).</summary>
    Task<string?> GetGameIdForConnectionAsync(string connectionId);

    /// <summary>Get all active game IDs this player is in (across all connections).</summary>
    Task<List<string>> GetActiveGameIdsAsync();
}
