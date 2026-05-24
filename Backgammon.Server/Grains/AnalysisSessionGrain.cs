using Backgammon.Core;
using Backgammon.Server.Grains.Interfaces;
using Backgammon.Server.Models;
using Backgammon.Server.Services;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Backgammon.Server.Grains;

/// <summary>
/// Per-user analysis session grain (key = userId). Owns the user's
/// <see cref="AnalysisSession"/> objects in-memory and the connection→session
/// map. Replaces the singleton AnalysisSessionManager + the per-session
/// SemaphoreSlim that used to live on each AnalysisSession.
/// </summary>
public class AnalysisSessionGrain : Grain, IAnalysisSessionGrain
{
    private readonly Dictionary<string, AnalysisSession> _sessions = new();
    private readonly Dictionary<string, string> _connectionToSession = new();
    private readonly IAnalysisService _analysisService;
    private readonly ILogger<AnalysisSessionGrain> _logger;

    public AnalysisSessionGrain(IAnalysisService analysisService, ILogger<AnalysisSessionGrain> logger)
    {
        _analysisService = analysisService;
        _logger = logger;
    }

    // ==================== Lifecycle ====================

    /// <inheritdoc/>
    public Task<AnalysisActionResult> CreateSessionAsync(string connectionId)
    {
        var userId = this.GetPrimaryKeyString();
        var sessionId = Guid.NewGuid().ToString();
        var session = new AnalysisSession(sessionId, userId);
        session.AddConnection(connectionId);

        _sessions[sessionId] = session;
        _connectionToSession[connectionId] = sessionId;

        _logger.LogInformation("Created analysis session {SessionId} for user {UserId}", sessionId, userId);

        return Task.FromResult(new AnalysisActionResult
        {
            SessionId = sessionId,
            State = session.GetState(connectionId),
        });
    }

    /// <inheritdoc/>
    public Task<AnalysisActionResult> JoinSessionAsync(string sessionId, string connectionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            return Task.FromResult(new AnalysisActionResult { Error = "Analysis session not found or you don't have access" });
        }

        // The grain key is the userId — by routing the caller here we've already
        // proven they own the session. Defensive check anyway.
        if (session.UserId != this.GetPrimaryKeyString())
        {
            return Task.FromResult(new AnalysisActionResult { Error = "Analysis session not found or you don't have access" });
        }

        session.AddConnection(connectionId);
        _connectionToSession[connectionId] = sessionId;

        _logger.LogInformation("Connection {ConnectionId} joined analysis session {SessionId}", connectionId, sessionId);

        return Task.FromResult(new AnalysisActionResult
        {
            SessionId = sessionId,
            State = session.GetState(connectionId),
        });
    }

    /// <inheritdoc/>
    public Task<string?> LeaveSessionAsync(string connectionId)
    {
        if (!_connectionToSession.TryGetValue(connectionId, out var sessionId))
        {
            return Task.FromResult<string?>(null);
        }

        _connectionToSession.Remove(connectionId);
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.RemoveConnection(connectionId);
            // Note: sessions with zero connections are intentionally retained so the user
            // can rejoin from another tab; idle cleanup belongs to a future reminder timer.
        }

        _logger.LogInformation("Connection {ConnectionId} left analysis session {SessionId}", connectionId, sessionId);
        return Task.FromResult<string?>(sessionId);
    }

    /// <inheritdoc/>
    public Task<string?> GetSessionIdForConnectionAsync(string connectionId)
    {
        _connectionToSession.TryGetValue(connectionId, out var sessionId);
        return Task.FromResult(sessionId);
    }

    // ==================== Engine actions ====================

    /// <inheritdoc/>
    public Task<AnalysisActionResult> RollDiceAsync(string sessionId)
    {
        if (!TryGetSession(sessionId, out var session, out var error))
        {
            return Task.FromResult(error!);
        }

        if (session.Engine.RemainingMoves.Count > 0)
        {
            return Task.FromResult(new AnalysisActionResult
            {
                SessionId = sessionId,
                Error = "Complete or undo your moves before rolling again",
            });
        }

        session.Engine.RollDice();
        session.UpdateActivity();
        return Task.FromResult(Success(sessionId, session));
    }

    /// <inheritdoc/>
    public Task<AnalysisActionResult> MakeMoveAsync(string sessionId, int from, int to)
    {
        if (!TryGetSession(sessionId, out var session, out var error))
        {
            return Task.FromResult(error!);
        }

        var validMoves = session.Engine.GetValidMoves();
        var move = validMoves.FirstOrDefault(m => m.From == from && m.To == to);
        if (move == null)
        {
            return Task.FromResult(new AnalysisActionResult { SessionId = sessionId, Error = "Invalid move" });
        }

        session.Engine.ExecuteMove(move);
        session.UpdateActivity();
        return Task.FromResult(Success(sessionId, session));
    }

    /// <inheritdoc/>
    public Task<AnalysisActionResult> MakeCombinedMoveAsync(string sessionId, int from, int to, int[] intermediatePoints)
    {
        if (!TryGetSession(sessionId, out var session, out var error))
        {
            return Task.FromResult(error!);
        }

        var sequence = new List<(int From, int To)>();
        var currentFrom = from;
        foreach (var intermediate in intermediatePoints)
        {
            sequence.Add((currentFrom, intermediate));
            currentFrom = intermediate;
        }

        sequence.Add((currentFrom, to));

        foreach (var (moveFrom, moveTo) in sequence)
        {
            var validMoves = session.Engine.GetValidMoves();
            var move = validMoves.FirstOrDefault(m => m.From == moveFrom && m.To == moveTo);
            if (move == null)
            {
                return Task.FromResult(new AnalysisActionResult
                {
                    SessionId = sessionId,
                    Error = $"Invalid move from {moveFrom} to {moveTo}",
                });
            }

            session.Engine.ExecuteMove(move);
        }

        session.UpdateActivity();
        return Task.FromResult(Success(sessionId, session));
    }

    /// <inheritdoc/>
    public Task<AnalysisActionResult> EndTurnAsync(string sessionId)
    {
        if (!TryGetSession(sessionId, out var session, out var error))
        {
            return Task.FromResult(error!);
        }

        session.Engine.EndTurn();
        session.UpdateActivity();
        return Task.FromResult(Success(sessionId, session));
    }

    /// <inheritdoc/>
    public Task<AnalysisActionResult> UndoLastMoveAsync(string sessionId)
    {
        if (!TryGetSession(sessionId, out var session, out var error))
        {
            return Task.FromResult(error!);
        }

        if (!session.Engine.UndoLastMove())
        {
            return Task.FromResult(new AnalysisActionResult { SessionId = sessionId, Error = "Nothing to undo" });
        }

        session.UpdateActivity();
        return Task.FromResult(Success(sessionId, session));
    }

    /// <inheritdoc/>
    public Task<AnalysisActionResult> SetDiceAsync(string sessionId, int die1, int die2)
    {
        if (!TryGetSession(sessionId, out var session, out var error))
        {
            return Task.FromResult(error!);
        }

        if (die1 < 1 || die1 > 6 || die2 < 1 || die2 > 6)
        {
            return Task.FromResult(new AnalysisActionResult { SessionId = sessionId, Error = "Dice values must be between 1 and 6" });
        }

        var initialDiceCount = session.Engine.Dice.GetMoves().Count;
        var currentRemainingCount = session.Engine.RemainingMoves.Count;
        var noMovesLeft = currentRemainingCount == 0;
        var noMovesMadeYet = currentRemainingCount == initialDiceCount;

        if (!noMovesLeft && !noMovesMadeYet)
        {
            return Task.FromResult(new AnalysisActionResult
            {
                SessionId = sessionId,
                Error = "End your turn or undo moves before setting new dice",
            });
        }

        session.Engine.StartTurnWithDice(die1, die2);
        session.UpdateActivity();
        return Task.FromResult(Success(sessionId, session));
    }

    /// <inheritdoc/>
    public Task<AnalysisActionResult> ImportPositionAsync(string sessionId, string positionData)
    {
        if (!TryGetSession(sessionId, out var session, out var error))
        {
            return Task.FromResult(error!);
        }

        try
        {
            string sgf = positionData.StartsWith("(;")
                ? positionData
                : System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(positionData));

            SgfSerializer.ImportPosition(session.Engine, sgf);
            session.UpdateActivity();
            return Task.FromResult(Success(sessionId, session));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing position into session {SessionId}", sessionId);
            return Task.FromResult(new AnalysisActionResult
            {
                SessionId = sessionId,
                Error = "Failed to import position: invalid format",
            });
        }
    }

    /// <inheritdoc/>
    public Task<AnalysisActionResult> MoveCheckerDirectlyAsync(string sessionId, int from, int to)
    {
        if (!TryGetSession(sessionId, out var session, out var error))
        {
            return Task.FromResult(error!);
        }

        if (!IsValidDirectMove(session.Engine, from, to))
        {
            return Task.FromResult(new AnalysisActionResult
            {
                SessionId = sessionId,
                Error = "Invalid move: check piece placement rules",
            });
        }

        ExecuteDirectMove(session.Engine, from, to);
        session.UpdateActivity();
        return Task.FromResult(Success(sessionId, session));
    }

    /// <inheritdoc/>
    public Task<AnalysisActionResult> SetCurrentPlayerAsync(string sessionId, CheckerColor color)
    {
        if (!TryGetSession(sessionId, out var session, out var error))
        {
            return Task.FromResult(error!);
        }

        session.Engine.SetCurrentPlayer(color);
        session.Engine.RemainingMoves.Clear();
        session.UpdateActivity();
        return Task.FromResult(Success(sessionId, session));
    }

    // ==================== Reads ====================

    /// <inheritdoc/>
    public Task<string> ExportPositionAsync(string sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            return Task.FromResult(string.Empty);
        }

        var sgf = SgfSerializer.ExportPosition(session.Engine);
        var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(sgf));
        return Task.FromResult(base64);
    }

    /// <inheritdoc/>
    public Task<string> ExportGameSgfAsync(string sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            return Task.FromResult(string.Empty);
        }

        return Task.FromResult(session.Engine.GameSgf ?? string.Empty);
    }

    /// <inheritdoc/>
    public async Task<PositionEvaluationDto?> AnalyzePositionAsync(string sessionId, string? evaluatorType)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            return null;
        }

        return await _analysisService.EvaluatePositionAsync(session.Engine, evaluatorType);
    }

    /// <inheritdoc/>
    public async Task<BestMovesAnalysisDto?> FindBestMovesAsync(string sessionId, string? evaluatorType)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            return null;
        }

        if (session.Engine.RemainingMoves.Count == 0)
        {
            return null;
        }

        return await _analysisService.FindBestMovesAsync(session.Engine, evaluatorType);
    }

    // ==================== Helpers ====================

    private bool TryGetSession(string sessionId, out AnalysisSession session, out AnalysisActionResult? error)
    {
        if (_sessions.TryGetValue(sessionId, out var found))
        {
            session = found;
            error = null;
            return true;
        }

        session = null!;
        error = new AnalysisActionResult { SessionId = sessionId, Error = "Analysis session not found" };
        return false;
    }

    private static AnalysisActionResult Success(string sessionId, AnalysisSession session) => new()
    {
        SessionId = sessionId,
        State = session.GetState(),
    };

    // ----- direct-move helpers (moved from GameHub.cs) -----

    private static bool IsValidDirectMove(GameEngine engine, int from, int to)
    {
        if (from < 0 || from > 25 || to < 0 || to > 25) return false;
        if (from == to) return false;

        var sourceColor = GetCheckerColorAtPoint(engine, from);
        if (sourceColor == null) return false;

        if (to >= 1 && to <= 24)
        {
            var destPoint = engine.Board.GetPoint(to);
            if (destPoint.Color != null && destPoint.Color != sourceColor) return false;
        }

        return CountCheckers(engine, sourceColor.Value) <= 15;
    }

    private static void ExecuteDirectMove(GameEngine engine, int from, int to)
    {
        var color = RemoveCheckerFrom(engine, from);
        AddCheckerTo(engine, to, color);
        engine.RemainingMoves.Clear();
    }

    private static CheckerColor? GetCheckerColorAtPoint(GameEngine engine, int point)
    {
        if (point == 0)
        {
            if (engine.WhitePlayer.CheckersOnBar > 0) return CheckerColor.White;
            if (engine.RedPlayer.CheckersOnBar > 0) return CheckerColor.Red;
        }
        else if (point >= 1 && point <= 24)
        {
            return engine.Board.GetPoint(point).Color;
        }
        else if (point == 25)
        {
            if (engine.WhitePlayer.CheckersBornOff > 0) return CheckerColor.White;
            if (engine.RedPlayer.CheckersBornOff > 0) return CheckerColor.Red;
        }

        return null;
    }

    private static CheckerColor RemoveCheckerFrom(GameEngine engine, int point)
    {
        if (point == 0)
        {
            if (engine.WhitePlayer.CheckersOnBar > 0)
            {
                engine.WhitePlayer.CheckersOnBar--;
                return CheckerColor.White;
            }

            engine.RedPlayer.CheckersOnBar--;
            return CheckerColor.Red;
        }

        if (point >= 1 && point <= 24)
        {
            var boardPoint = engine.Board.GetPoint(point);
            var color = boardPoint.Color!.Value;
            boardPoint.Checkers.RemoveAt(boardPoint.Checkers.Count - 1);
            return color;
        }

        // Bear-off
        if (engine.WhitePlayer.CheckersBornOff > 0)
        {
            engine.WhitePlayer.CheckersBornOff--;
            return CheckerColor.White;
        }

        engine.RedPlayer.CheckersBornOff--;
        return CheckerColor.Red;
    }

    private static void AddCheckerTo(GameEngine engine, int point, CheckerColor color)
    {
        if (point == 0)
        {
            var player = color == CheckerColor.White ? engine.WhitePlayer : engine.RedPlayer;
            player.CheckersOnBar++;
        }
        else if (point >= 1 && point <= 24)
        {
            engine.Board.GetPoint(point).AddChecker(color);
        }
        else if (point == 25)
        {
            var player = color == CheckerColor.White ? engine.WhitePlayer : engine.RedPlayer;
            player.CheckersBornOff++;
        }
    }

    private static int CountCheckers(GameEngine engine, CheckerColor color)
    {
        int count = 0;
        for (int i = 1; i <= 24; i++)
        {
            var point = engine.Board.GetPoint(i);
            if (point.Color == color) count += point.Count;
        }

        var player = color == CheckerColor.White ? engine.WhitePlayer : engine.RedPlayer;
        return count + player.CheckersOnBar + player.CheckersBornOff;
    }
}
