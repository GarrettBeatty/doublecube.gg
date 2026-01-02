using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Backgammon.Server.Services;

/// <summary>
/// Thread-safe service for tracking player connections.
/// Uses ConcurrentDictionary to avoid race conditions.
/// </summary>
public class PlayerConnectionService : IPlayerConnectionService
{
    private readonly ConcurrentDictionary<string, string> _connections = new();
    private readonly ILogger<PlayerConnectionService> _logger;

    public PlayerConnectionService(ILogger<PlayerConnectionService> logger)
    {
        _logger = logger;
    }

    public void AddConnection(string playerId, string connectionId)
    {
        _connections[playerId] = connectionId;
        _logger.LogInformation(
            "Tracking player connection: playerId={PlayerId}, connectionId={ConnectionId}, total connections: {Count}",
            playerId,
            connectionId,
            _connections.Count);
    }

    public bool RemoveConnection(string playerId)
    {
        var removed = _connections.TryRemove(playerId, out var connectionId);
        if (removed)
        {
            _logger.LogInformation(
                "Removed player connection: playerId={PlayerId}, connectionId={ConnectionId}, remaining connections: {Count}",
                playerId,
                connectionId,
                _connections.Count);
        }

        return removed;
    }

    public string? GetConnectionId(string playerId)
    {
        _connections.TryGetValue(playerId, out var connectionId);
        if (connectionId != null)
        {
            _logger.LogDebug(
                "Found connection for playerId={PlayerId}: connectionId={ConnectionId}",
                playerId,
                connectionId);
        }

        return connectionId;
    }

    public int GetConnectionCount()
    {
        return _connections.Count;
    }
}
