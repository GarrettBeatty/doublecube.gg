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
    /// Execute a combined move (using 2+ dice) atomically through intermediate points.
    /// Either all moves succeed or none are applied (rollback on failure).
    /// </summary>
    /// <param name="session">The game session</param>
    /// <param name="connectionId">The player's connection ID</param>
    /// <param name="from">Starting point</param>
    /// <param name="to">Final destination point</param>
    /// <param name="intermediatePoints">Points the checker passes through before reaching final destination</param>
    Task<ActionResult> MakeCombinedMoveAsync(
        GameSession session,
        string connectionId,
        int from,
        int to,
        int[] intermediatePoints);

    /// <summary>
    /// End the current player's turn
    /// </summary>
    Task<ActionResult> EndTurnAsync(GameSession session, string connectionId);

    /// <summary>
    /// Undo the last move made during the current turn
    /// </summary>
    Task<ActionResult> UndoLastMoveAsync(GameSession session, string connectionId);

    /// <summary>
    /// Execute a complete AI turn (roll, move, end turn) with broadcasting
    /// </summary>
    Task ExecuteAiTurnWithBroadcastAsync(GameSession session, string aiPlayerId);
}
