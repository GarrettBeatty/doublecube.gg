using Backgammon.Server.Models;

namespace Backgammon.Server.Services;

/// <summary>
/// Unified service for game creation, joining, and state broadcasting
/// Consolidates IGameCreationService and IGameStateService
/// </summary>
public interface IGameService
{
    // Game Creation & Joining

    /// <summary>
    /// Join an existing game by ID
    /// </summary>
    Task JoinGameAsync(string connectionId, string playerId, string? displayName, string gameId);

    /// <summary>
    /// Leave current game
    /// </summary>
    Task LeaveGameAsync(string connectionId);

    /// <summary>
    /// Create a new game against an AI opponent
    /// </summary>
    Task CreateAiGameAsync(string connectionId, string playerId, string? displayName);

    // Game State Broadcasting

    /// <summary>
    /// Broadcast game state update to all players and spectators
    /// </summary>
    Task BroadcastGameUpdateAsync(GameSession session);

    /// <summary>
    /// Broadcast game start to both players
    /// </summary>
    Task BroadcastGameStartAsync(GameSession session);

    /// <summary>
    /// Broadcast game over to all players
    /// </summary>
    Task BroadcastGameOverAsync(GameSession session);

    /// <summary>
    /// Send game state to a specific connection
    /// </summary>
    Task SendGameStateToConnectionAsync(GameSession session, string connectionId);

    /// <summary>
    /// Broadcast double offer to opponent
    /// </summary>
    Task BroadcastDoubleOfferAsync(GameSession session, string offeringConnectionId, int currentValue, int newValue);

    /// <summary>
    /// Broadcast double accepted to both players
    /// </summary>
    Task BroadcastDoubleAcceptedAsync(GameSession session);

    /// <summary>
    /// Broadcast match update to players
    /// </summary>
    Task BroadcastMatchUpdateAsync(Match match, string gameId);
}
