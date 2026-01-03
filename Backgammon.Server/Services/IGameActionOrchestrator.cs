using Backgammon.Core;
using Backgammon.Server.Models;

namespace Backgammon.Server.Services;

/// <summary>
/// Orchestrates game actions with common patterns:
/// validation, execution, broadcasting, persistence, and AI turn triggering
/// </summary>
public interface IGameActionOrchestrator
{
    /// <summary>
    /// Execute a dice roll for the current player
    /// </summary>
    Task<ActionResult> RollDiceAsync(GameSession session, string connectionId);

    /// <summary>
    /// Execute a move from one point to another
    /// </summary>
    Task<ActionResult> MakeMoveAsync(GameSession session, string connectionId, int from, int to);

    /// <summary>
    /// End the current player's turn
    /// </summary>
    Task<ActionResult> EndTurnAsync(GameSession session, string connectionId);

    /// <summary>
    /// Undo the last move made during the current turn
    /// </summary>
    Task<ActionResult> UndoLastMoveAsync(GameSession session, string connectionId);
}
