using Orleans;

namespace Backgammon.Server.Grains.Interfaces;

/// <summary>
/// Process-wide presence registry: tracks which players are online, the SignalR
/// connection IDs for each, and which game (if any) each connection is currently in.
/// Subsumes the old <c>PlayerConnectionService</c> singleton and <c>PlayerGrain</c>
/// per-player grain.
///
/// Convention: use the well-known key <c>"global"</c> to resolve the single
/// instance: <c>_grainFactory.GetGrain&lt;IPresenceGrain&gt;("global")</c>.
///
/// Not persisted — presence is inherently ephemeral. After silo restart, the map
/// is empty and clients re-register on reconnect.
/// </summary>
public interface IPresenceGrain : IGrainWithStringKey
{
    /// <summary>Well-known key to resolve the singleton presence grain.</summary>
    const string Key = "global";

    /// <summary>Register that a SignalR connection is online for a player.</summary>
    Task SetOnlineAsync(string playerId, string connectionId);

    /// <summary>Remove a single connection (player goes offline only if no connections remain).</summary>
    Task SetOfflineAsync(string connectionId);

    /// <summary>Associate a connection with a game it has joined.</summary>
    Task SetConnectionGameAsync(string connectionId, string gameId);

    /// <summary>Clear the game association for a connection (e.g. on LeaveGame).</summary>
    Task ClearConnectionGameAsync(string connectionId);

    /// <summary>Get any one connection ID for the player (null if not online).</summary>
    Task<string?> GetConnectionIdAsync(string playerId);

    /// <summary>True if the player has at least one tracked connection.</summary>
    Task<bool> IsPlayerOnlineAsync(string playerId);

    /// <summary>Snapshot of all currently online player IDs.</summary>
    Task<List<string>> GetAllOnlinePlayerIdsAsync();

    /// <summary>Game ID this connection is in, or null.</summary>
    Task<string?> GetGameIdForConnectionAsync(string connectionId);

    /// <summary>Distinct game IDs the player is currently in across all their connections.</summary>
    Task<List<string>> GetActiveGameIdsAsync(string playerId);
}
