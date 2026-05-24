using Backgammon.Core;
using Backgammon.Server.Models;
using Orleans;

namespace Backgammon.Server.Grains.Interfaces;

/// <summary>
/// Per-user analysis session grain (key = userId). Owns the user's analysis
/// sessions in-memory and the connection→session map. Subsumes the old
/// singleton <c>AnalysisSessionManager</c>; the grain's single-threaded
/// execution replaces the per-session SemaphoreSlim that used to live on
/// each AnalysisSession.
///
/// Ephemeral — no <c>IPersistentState</c>; analysis state is session-scoped
/// and silo restart wipes it.
/// </summary>
public interface IAnalysisSessionGrain : IGrainWithStringKey
{
    // ==================== Lifecycle ====================

    /// <summary>Create a new analysis session and seat the connection in it.</summary>
    Task<AnalysisActionResult> CreateSessionAsync(string connectionId);

    /// <summary>Join an existing session (multi-tab support).</summary>
    Task<AnalysisActionResult> JoinSessionAsync(string sessionId, string connectionId);

    /// <summary>Remove the connection from its session; returns the sessionId so the caller can clean up SignalR groups.</summary>
    Task<string?> LeaveSessionAsync(string connectionId);

    /// <summary>Returns the session ID the connection is in, or null.</summary>
    Task<string?> GetSessionIdForConnectionAsync(string connectionId);

    // ==================== Engine actions ====================

    /// <summary>Roll dice (rejects if remaining moves &gt; 0).</summary>
    Task<AnalysisActionResult> RollDiceAsync(string sessionId);

    /// <summary>Execute a single move (validated against engine rules).</summary>
    Task<AnalysisActionResult> MakeMoveAsync(string sessionId, int from, int to);

    /// <summary>Execute a combined (multi-die) move atomically.</summary>
    Task<AnalysisActionResult> MakeCombinedMoveAsync(string sessionId, int from, int to, int[] intermediatePoints);

    /// <summary>End the current turn, advancing to the other player.</summary>
    Task<AnalysisActionResult> EndTurnAsync(string sessionId);

    /// <summary>Undo the last move of the current turn.</summary>
    Task<AnalysisActionResult> UndoLastMoveAsync(string sessionId);

    /// <summary>Manually set the dice values (analysis only).</summary>
    Task<AnalysisActionResult> SetDiceAsync(string sessionId, int die1, int die2);

    /// <summary>Import a position from SGF (raw or base64-encoded).</summary>
    Task<AnalysisActionResult> ImportPositionAsync(string sessionId, string positionData);

    /// <summary>Move a checker directly between points, bypassing engine rules (analysis only).</summary>
    Task<AnalysisActionResult> MoveCheckerDirectlyAsync(string sessionId, int from, int to);

    /// <summary>Set which player is on roll (clears remaining moves).</summary>
    Task<AnalysisActionResult> SetCurrentPlayerAsync(string sessionId, CheckerColor color);

    // ==================== Reads ====================

    /// <summary>Export the current position as a base64-encoded SGF string (or empty if no session).</summary>
    Task<string> ExportPositionAsync(string sessionId);

    /// <summary>Export the full game SGF with move history.</summary>
    Task<string> ExportGameSgfAsync(string sessionId);

    /// <summary>Run a position evaluation against the session's engine state.</summary>
    Task<PositionEvaluationDto?> AnalyzePositionAsync(string sessionId, string? evaluatorType);

    /// <summary>Find the best moves for the session's current position.</summary>
    Task<BestMovesAnalysisDto?> FindBestMovesAsync(string sessionId, string? evaluatorType);
}
