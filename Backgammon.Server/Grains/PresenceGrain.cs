using Backgammon.Server.Grains.Interfaces;
using Orleans;

namespace Backgammon.Server.Grains;

/// <summary>
/// Singleton presence registry (resolve with key <c>"global"</c>). Replaces
/// PlayerConnectionService + PlayerGrain. Ephemeral — no IPersistentState; presence
/// is reset on silo restart and clients re-register when they reconnect.
/// </summary>
public class PresenceGrain : Grain, IPresenceGrain
{
    private readonly Dictionary<string, HashSet<string>> _playerConnections = new();
    private readonly Dictionary<string, string> _connectionToPlayer = new();
    private readonly Dictionary<string, string> _connectionToGame = new();

    /// <inheritdoc/>
    public Task SetOnlineAsync(string playerId, string connectionId)
    {
        _connectionToPlayer[connectionId] = playerId;

        if (!_playerConnections.TryGetValue(playerId, out var connections))
        {
            connections = new HashSet<string>();
            _playerConnections[playerId] = connections;
        }

        connections.Add(connectionId);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SetOfflineAsync(string connectionId)
    {
        if (_connectionToPlayer.TryGetValue(connectionId, out var playerId))
        {
            _connectionToPlayer.Remove(connectionId);
            if (_playerConnections.TryGetValue(playerId, out var connections))
            {
                connections.Remove(connectionId);
                if (connections.Count == 0)
                {
                    _playerConnections.Remove(playerId);
                }
            }
        }

        _connectionToGame.Remove(connectionId);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SetConnectionGameAsync(string connectionId, string gameId)
    {
        _connectionToGame[connectionId] = gameId;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task ClearConnectionGameAsync(string connectionId)
    {
        _connectionToGame.Remove(connectionId);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<string?> GetConnectionIdAsync(string playerId)
    {
        if (_playerConnections.TryGetValue(playerId, out var connections))
        {
            return Task.FromResult<string?>(connections.FirstOrDefault());
        }

        return Task.FromResult<string?>(null);
    }

    /// <inheritdoc/>
    public Task<bool> IsPlayerOnlineAsync(string playerId)
    {
        return Task.FromResult(_playerConnections.ContainsKey(playerId));
    }

    /// <inheritdoc/>
    public Task<List<string>> GetAllOnlinePlayerIdsAsync()
    {
        return Task.FromResult(_playerConnections.Keys.ToList());
    }

    /// <inheritdoc/>
    public Task<string?> GetGameIdForConnectionAsync(string connectionId)
    {
        _connectionToGame.TryGetValue(connectionId, out var gameId);
        return Task.FromResult(gameId);
    }

    /// <inheritdoc/>
    public Task<List<string>> GetActiveGameIdsAsync(string playerId)
    {
        if (!_playerConnections.TryGetValue(playerId, out var connections))
        {
            return Task.FromResult(new List<string>());
        }

        var games = connections
            .Where(c => _connectionToGame.ContainsKey(c))
            .Select(c => _connectionToGame[c])
            .Distinct()
            .ToList();

        return Task.FromResult(games);
    }
}
