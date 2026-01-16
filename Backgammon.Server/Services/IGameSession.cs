using Backgammon.Core;
using Backgammon.Server.Models;
using Backgammon.Server.Models.SignalR;

namespace Backgammon.Server.Services;

/// <summary>
/// Common interface for game sessions (both multiplayer and analysis).
/// </summary>
public interface IGameSession
{
    /// <summary>
    /// Unique session identifier.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// The game engine managing board state and rules.
    /// </summary>
    GameEngine Engine { get; }

    /// <summary>
    /// Current session status.
    /// </summary>
    SessionStatus Status { get; }

    /// <summary>
    /// Lock for game actions that modify state (prevents race conditions with multi-tab access).
    /// </summary>
    SemaphoreSlim GameActionLock { get; }

    /// <summary>
    /// Last time there was activity on this session.
    /// </summary>
    DateTime LastActivityAt { get; }

    /// <summary>
    /// Update the last activity timestamp.
    /// </summary>
    void UpdateActivity();

    /// <summary>
    /// Convert the current engine state to a DTO for clients.
    /// </summary>
    /// <param name="forConnectionId">Optional connection ID to provide player-specific information.</param>
    GameState GetState(string? forConnectionId = null);
}
