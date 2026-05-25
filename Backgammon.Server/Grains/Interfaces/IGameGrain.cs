using Backgammon.Core;
using Backgammon.Server.Models;
using Backgammon.Server.Models.SignalR;
using Backgammon.Server.Services;
using Orleans;

namespace Backgammon.Server.Grains.Interfaces;

/// <summary>
/// Orleans grain interface for a single active game session.
/// Replaces GameSession + GameSessionManager - no SemaphoreSlim needed (Orleans is single-threaded per grain).
/// Key = gameId (string).
/// </summary>
public interface IGameGrain : IGrainWithStringKey
{
    /// <summary>Join or reconnect to the game. Returns initial state for the joining player.</summary>
    Task<GameState?> JoinAsync(string playerId, string connectionId, string? displayName);

    /// <summary>Leave the game (called on disconnect).</summary>
    Task LeaveAsync(string connectionId);

    /// <summary>Get current game state, personalized for a connection.</summary>
    Task<GameState> GetStateAsync(string? connectionId);

    /// <summary>Get session status without full state computation.</summary>
    Task<SessionStatus> GetStatusAsync();

    /// <summary>Check if a player ID is a participant in this game.</summary>
    Task<bool> IsPlayerAsync(string playerId);

    /// <summary>Get all connection IDs for all players and spectators.</summary>
    Task<List<string>> GetAllConnectionsAsync();

    // ===== Match game initialization =====
    /// <summary>Pre-assign white player (for match games created before players connect).</summary>
    Task SetWhitePlayerAsync(string playerId, string? displayName = null, int? rating = null);

    /// <summary>Pre-assign red player (for match games created before players connect).</summary>
    Task SetRedPlayerAsync(string playerId, string? displayName = null, int? rating = null, string? connectionId = null);

    /// <summary>Set match context (matchId, scores, Crawford rule).</summary>
    Task SetMatchInfoAsync(string matchId, int? targetScore, int? p1Score, int? p2Score, bool? isCrawford);

    /// <summary>Set time control configuration.</summary>
    Task SetTimeControlAsync(TimeControlConfig? config);

    /// <summary>Set correspondence game mode.</summary>
    Task SetCorrespondenceAsync(bool isCorrespondence, int? timePerMoveDays = null);

    /// <summary>Mark as AI game (unrated).</summary>
    Task SetAiGameAsync(bool isBotGame);

    // ===== Gameplay =====
    Task<ActionResult> RollDiceAsync(string connectionId);
    Task<ActionResult> MakeMoveAsync(string connectionId, int from, int to);
    Task<ActionResult> MakeCombinedMoveAsync(string connectionId, int from, int to, int[] intermediatePoints);
    Task<ActionResult> EndTurnAsync(string connectionId);
    Task<ActionResult> UndoLastMoveAsync(string connectionId);

    // ===== Doubling cube =====
    Task<ActionResult> OfferDoubleAsync(string connectionId);
    Task<ActionResult> AcceptDoubleAsync(string connectionId);
    Task<ActionResult> DeclineDoubleAsync(string connectionId);

    // ===== Game lifecycle =====
    Task<ActionResult> AbandonAsync(string connectionId);
    Task MarkCompletedAsync();

    /// <summary>
    /// Run an AI player's full turn on the activation. Invoked from background
    /// continuations via a grain reference so Orleans schedules execution on
    /// the activation context (calling the private implementation directly
    /// from a fire-and-forget task would run on a thread-pool thread and
    /// throw an activation-access-violation on state access).
    /// </summary>
    Task TriggerAiTurnAsync(string aiPlayerId);

    // ===== Analysis mode (study/analysis sessions only) =====
    Task<ActionResult> SetDiceAsync(string connectionId, int die1, int die2);
    Task<ActionResult> MoveCheckerDirectlyAsync(string connectionId, int from, int to);
    Task<ActionResult> SetCurrentPlayerAsync(string connectionId, string color);
    Task<string?> ExportPositionAsync();
    Task<string?> ExportGameSgfAsync();
    Task<ActionResult> ImportPositionAsync(string sgf);
}
