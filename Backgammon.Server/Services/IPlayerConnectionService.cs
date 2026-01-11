namespace Backgammon.Server.Services;

/// <summary>
/// Service for tracking player connections in a thread-safe manner.
/// Maps player IDs to their SignalR connection IDs.
/// </summary>
public interface IPlayerConnectionService
{
    /// <summary>
    /// Track a player connection
    /// </summary>
    void AddConnection(string playerId, string connectionId);

    /// <summary>
    /// Remove a player connection
    /// </summary>
    bool RemoveConnection(string playerId);

    /// <summary>
    /// Get connection ID for a player
    /// </summary>
    string? GetConnectionId(string playerId);

    /// <summary>
    /// Get all tracked connections (for debugging)
    /// </summary>
    int GetConnectionCount();

    /// <summary>
    /// Get all currently connected player IDs
    /// </summary>
    IEnumerable<string> GetAllConnectedPlayerIds();

    /// <summary>
    /// Check if a player is currently connected
    /// </summary>
    bool IsPlayerConnected(string playerId);
}
