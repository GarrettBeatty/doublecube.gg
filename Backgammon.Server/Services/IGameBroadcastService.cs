using Backgammon.Server.Models;
using Match = Backgammon.Server.Models.Match;

namespace Backgammon.Server.Services;

/// <summary>
/// Service interface for broadcasting game state changes via SignalR.
/// </summary>
public interface IGameBroadcastService
{
    /// <summary>
    /// Broadcasts game state update to all players and spectators.
    /// </summary>
    Task BroadcastGameUpdateAsync(GameSession session);

    /// <summary>
    /// Broadcasts game over event to all players in the game.
    /// </summary>
    Task BroadcastGameOverAsync(GameSession session);

    /// <summary>
    /// Broadcasts game start event to all players in the game.
    /// </summary>
    Task BroadcastGameStartAsync(GameSession session);

    /// <summary>
    /// Broadcasts match score update to all players.
    /// </summary>
    Task BroadcastMatchUpdateAsync(Match match, string gameId);

    /// <summary>
    /// Broadcasts that a new game in a match is starting.
    /// </summary>
    Task BroadcastMatchGameStartingAsync(Match match, string newGameId);

    /// <summary>
    /// Broadcasts game state to a specific connection.
    /// Used for reconnection scenarios.
    /// </summary>
    Task SendGameStateToConnectionAsync(GameSession session, string connectionId);
}
