using Backgammon.Server.Models;
using Microsoft.AspNetCore.SignalR;

namespace Backgammon.Server.Services;

/// <summary>
/// Service for broadcasting game state updates to clients
/// </summary>
public interface IGameStateService
{
    /// <summary>
    /// Broadcast game state update to all players and spectators
    /// </summary>
    Task BroadcastGameUpdateAsync(GameSession session, IHubCallerClients clients);

    /// <summary>
    /// Broadcast game start to both players
    /// </summary>
    Task BroadcastGameStartAsync(GameSession session, IHubCallerClients clients);

    /// <summary>
    /// Broadcast game over to all players
    /// </summary>
    Task BroadcastGameOverAsync(GameSession session, IHubCallerClients clients);

    /// <summary>
    /// Send game state to a specific connection
    /// </summary>
    Task SendGameStateToConnectionAsync(GameSession session, string connectionId, IHubCallerClients clients);

    /// <summary>
    /// Broadcast double offer to opponent
    /// </summary>
    Task BroadcastDoubleOfferAsync(GameSession session, string offeringConnectionId, int currentValue, int newValue, IHubCallerClients clients);

    /// <summary>
    /// Broadcast double accepted to both players
    /// </summary>
    Task BroadcastDoubleAcceptedAsync(GameSession session, IHubCallerClients clients);

    /// <summary>
    /// Broadcast match update to players
    /// </summary>
    Task BroadcastMatchUpdateAsync(Match match, string gameId, IHubCallerClients clients);
}
