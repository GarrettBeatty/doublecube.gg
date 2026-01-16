using Backgammon.Core;
using Backgammon.Server.Models;
using Backgammon.Server.Models.SignalR;
using Backgammon.Server.Services.Results;

namespace Backgammon.Server.Hubs.Handlers;

/// <summary>
/// Handler for core game actions: roll dice, make move, end turn, undo.
/// Encapsulates the game action orchestration and error handling.
/// </summary>
public interface IGameActionHandler
{
    /// <summary>
    /// Roll dice to start a turn.
    /// </summary>
    Task<Result> RollDiceAsync(string connectionId);

    /// <summary>
    /// Execute a single move.
    /// </summary>
    Task<Result> MakeMoveAsync(string connectionId, int from, int to);

    /// <summary>
    /// Execute a combined move (using multiple dice) atomically.
    /// </summary>
    Task<Result> MakeCombinedMoveAsync(string connectionId, int from, int to, int[] intermediatePoints);

    /// <summary>
    /// End the current turn.
    /// </summary>
    Task<Result> EndTurnAsync(string connectionId);

    /// <summary>
    /// Undo the last move.
    /// </summary>
    Task<Result> UndoLastMoveAsync(string connectionId);

    /// <summary>
    /// Get valid source points for moves.
    /// </summary>
    List<int> GetValidSources(string connectionId);

    /// <summary>
    /// Get valid destinations from a source point.
    /// </summary>
    List<MoveDto> GetValidDestinations(string connectionId, int fromPoint);
}
