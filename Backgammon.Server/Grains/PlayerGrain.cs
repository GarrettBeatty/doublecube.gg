using Backgammon.Server.Grains.Interfaces;
using Orleans;

namespace Backgammon.Server.Grains;

/// <summary>
/// Tracks a player's connection-to-game mappings.
/// Replaces the _playerToGame dictionary in GameSessionManager.
/// </summary>
public class PlayerGrain : Grain, IPlayerGrain
{
    private readonly Dictionary<string, string> _connectionToGame = new();

    /// <inheritdoc/>
    public Task TrackConnectionAsync(string connectionId, string gameId)
    {
        _connectionToGame[connectionId] = gameId;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task RemoveConnectionAsync(string connectionId)
    {
        _connectionToGame.Remove(connectionId);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<string?> GetGameIdForConnectionAsync(string connectionId)
    {
        _connectionToGame.TryGetValue(connectionId, out var gameId);
        return Task.FromResult(gameId);
    }

    /// <inheritdoc/>
    public Task<List<string>> GetActiveGameIdsAsync()
    {
        return Task.FromResult(_connectionToGame.Values.Distinct().ToList());
    }
}
