using Backgammon.Server.Grains.Interfaces;
using Orleans;

namespace Backgammon.Server.Grains;

/// <summary>
/// Tracks in-memory match state: current game and player connections.
/// </summary>
public class MatchGrain : Grain, IMatchGrain
{
    private string? _currentGameId;
    private readonly Dictionary<string, HashSet<string>> _playerConnections = new();

    /// <inheritdoc/>
    public Task<string?> GetCurrentGameIdAsync() => Task.FromResult(_currentGameId);

    /// <inheritdoc/>
    public Task SetCurrentGameIdAsync(string gameId)
    {
        _currentGameId = gameId;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<List<string>> GetPlayerConnectionsAsync(string playerId)
    {
        if (_playerConnections.TryGetValue(playerId, out var connections))
        {
            return Task.FromResult(connections.ToList());
        }

        return Task.FromResult(new List<string>());
    }

    /// <inheritdoc/>
    public Task TrackPlayerConnectionAsync(string playerId, string connectionId)
    {
        if (!_playerConnections.TryGetValue(playerId, out var connections))
        {
            connections = new HashSet<string>();
            _playerConnections[playerId] = connections;
        }

        connections.Add(connectionId);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task RemovePlayerConnectionAsync(string playerId, string connectionId)
    {
        if (_playerConnections.TryGetValue(playerId, out var connections))
        {
            connections.Remove(connectionId);
        }

        return Task.CompletedTask;
    }
}
