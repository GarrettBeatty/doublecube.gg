using Backgammon.Core;
using Backgammon.Server.Hubs.Interfaces;
using Backgammon.Server.Models;
using Backgammon.Server.Models.SignalR;
using Backgammon.Server.Services;
using Backgammon.Server.Services.Results;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Backgammon.Server.Hubs.Handlers;

/// <summary>
/// Handler for core game actions: roll dice, make move, end turn, undo.
/// Consolidates game action services and provides unified error handling.
/// </summary>
public class GameActionHandler : IGameActionHandler
{
    private readonly IGameSessionManager _sessionManager;
    private readonly IGameActionOrchestrator _gameActionOrchestrator;
    private readonly IMoveQueryService _moveQueryService;
    private readonly IGameService _gameService;
    private readonly ILogger<GameActionHandler> _logger;

    public GameActionHandler(
        IGameSessionManager sessionManager,
        IGameActionOrchestrator gameActionOrchestrator,
        IMoveQueryService moveQueryService,
        IGameService gameService,
        ILogger<GameActionHandler> logger)
    {
        _sessionManager = sessionManager;
        _gameActionOrchestrator = gameActionOrchestrator;
        _moveQueryService = moveQueryService;
        _gameService = gameService;
        _logger = logger;
    }

    public async Task<Result> RollDiceAsync(string connectionId)
    {
        var session = _sessionManager.GetGameByPlayer(connectionId);
        if (session == null)
        {
            return Result.Failure(ErrorCodes.SessionNotFound, "Not in a game");
        }

        var result = await _gameActionOrchestrator.RollDiceAsync(session, connectionId);
        if (!result.Success)
        {
            return Result.Failure(ErrorCodes.InvalidMove, result.ErrorMessage ?? "Failed to roll dice");
        }

        return Result.Ok();
    }

    public async Task<Result> MakeMoveAsync(string connectionId, int from, int to)
    {
        var session = _sessionManager.GetGameByPlayer(connectionId);
        if (session == null)
        {
            return Result.Failure(ErrorCodes.SessionNotFound, "Not in a game");
        }

        var result = await _gameActionOrchestrator.MakeMoveAsync(session, connectionId, from, to);
        if (!result.Success)
        {
            return Result.Failure(ErrorCodes.InvalidMove, result.ErrorMessage ?? "Invalid move");
        }

        return Result.Ok();
    }

    public async Task<Result> MakeCombinedMoveAsync(string connectionId, int from, int to, int[] intermediatePoints)
    {
        var session = _sessionManager.GetGameByPlayer(connectionId);
        if (session == null)
        {
            return Result.Failure(ErrorCodes.SessionNotFound, "Not in a game");
        }

        var result = await _gameActionOrchestrator.MakeCombinedMoveAsync(
            session,
            connectionId,
            from,
            to,
            intermediatePoints);

        if (!result.Success)
        {
            return Result.Failure(ErrorCodes.InvalidMove, result.ErrorMessage ?? "Invalid combined move");
        }

        return Result.Ok();
    }

    public async Task<Result> EndTurnAsync(string connectionId)
    {
        var session = _sessionManager.GetGameByPlayer(connectionId);
        if (session == null)
        {
            return Result.Failure(ErrorCodes.SessionNotFound, "Not in a game");
        }

        var result = await _gameActionOrchestrator.EndTurnAsync(session, connectionId);
        if (!result.Success)
        {
            return Result.Failure(ErrorCodes.InvalidMove, result.ErrorMessage ?? "Cannot end turn");
        }

        return Result.Ok();
    }

    public async Task<Result> UndoLastMoveAsync(string connectionId)
    {
        var session = _sessionManager.GetGameByPlayer(connectionId);
        if (session == null)
        {
            return Result.Failure(ErrorCodes.SessionNotFound, "Not in a game");
        }

        var result = await _gameActionOrchestrator.UndoLastMoveAsync(session, connectionId);
        if (!result.Success)
        {
            return Result.Failure(ErrorCodes.InvalidMove, result.ErrorMessage ?? "Cannot undo move");
        }

        return Result.Ok();
    }

    public async Task<Result> SetDiceAsync(string connectionId, int die1, int die2)
    {
        var session = _sessionManager.GetGameByPlayer(connectionId);
        if (session == null)
        {
            return Result.Failure(ErrorCodes.SessionNotFound, "Not in a game");
        }

        if (!session.IsAnalysisMode)
        {
            return Result.Failure(ErrorCodes.NotAuthorized, "Dice can only be set in analysis mode");
        }

        if (die1 < 1 || die1 > 6 || die2 < 1 || die2 > 6)
        {
            return Result.Failure(ErrorCodes.InvalidMove, "Dice values must be between 1 and 6");
        }

        await session.GameActionLock.WaitAsync();
        try
        {
            var initialDiceCount = session.Engine.Dice.GetMoves().Count;
            var currentRemainingCount = session.Engine.RemainingMoves.Count;
            var noMovesLeft = currentRemainingCount == 0;
            var noMovesMadeYet = currentRemainingCount == initialDiceCount;

            if (!noMovesLeft && !noMovesMadeYet)
            {
                return Result.Failure(ErrorCodes.InvalidMove, "End your turn or undo moves before setting new dice");
            }

            session.Engine.Dice.SetDice(die1, die2);
            session.Engine.RemainingMoves.Clear();
            session.Engine.RemainingMoves.AddRange(session.Engine.Dice.GetMoves());

            _logger.LogInformation("Set dice to [{Die1}, {Die2}] in analysis game {GameId}", die1, die2, session.Id);
        }
        finally
        {
            session.GameActionLock.Release();
        }

        await _gameService.BroadcastGameUpdateAsync(session);
        return Result.Ok();
    }

    public async Task<Result> MoveCheckerDirectlyAsync(string connectionId, int from, int to)
    {
        var session = _sessionManager.GetGameByPlayer(connectionId);
        if (session == null)
        {
            return Result.Failure(ErrorCodes.SessionNotFound, "Not in a game");
        }

        if (!session.IsAnalysisMode)
        {
            return Result.Failure(ErrorCodes.NotAuthorized, "Direct moves only allowed in analysis mode");
        }

        if (!IsValidDirectMove(session.Engine, from, to))
        {
            return Result.Failure(ErrorCodes.InvalidMove, "Invalid move: check piece placement rules");
        }

        try
        {
            ExecuteDirectMove(session.Engine, from, to);
            await _gameService.BroadcastGameUpdateAsync(session);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing direct move in analysis mode");
            return Result.Failure(ErrorCodes.InvalidMove, "Failed to move checker");
        }
    }

    public async Task<Result> SetCurrentPlayerAsync(string connectionId, CheckerColor color)
    {
        var session = _sessionManager.GetGameByPlayer(connectionId);
        if (session == null)
        {
            return Result.Failure(ErrorCodes.SessionNotFound, "Not in a game");
        }

        if (!session.IsAnalysisMode)
        {
            return Result.Failure(ErrorCodes.NotAuthorized, "Can only set player in analysis mode");
        }

        try
        {
            session.Engine.SetCurrentPlayer(color);
            session.Engine.RemainingMoves.Clear();
            await _gameService.BroadcastGameUpdateAsync(session);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting current player in analysis mode");
            return Result.Failure(ErrorCodes.InvalidMove, "Failed to set current player");
        }
    }

    public List<int> GetValidSources(string connectionId)
    {
        return _moveQueryService.GetValidSources(connectionId);
    }

    public List<MoveDto> GetValidDestinations(string connectionId, int fromPoint)
    {
        return _moveQueryService.GetValidDestinations(connectionId, fromPoint);
    }

    // ==================== Private Helper Methods ====================

    private static bool IsValidDirectMove(GameEngine engine, int from, int to)
    {
        if (from < 0 || from > 25 || to < 0 || to > 25)
        {
            return false;
        }

        if (from == to)
        {
            return false;
        }

        CheckerColor? sourceColor = GetCheckerColorAtPoint(engine, from);
        if (sourceColor == null)
        {
            return false;
        }

        if (to >= 1 && to <= 24)
        {
            var destPoint = engine.Board.GetPoint(to);
            if (destPoint.Color != null && destPoint.Color != sourceColor)
            {
                return false;
            }
        }

        return CountCheckers(engine, sourceColor.Value) <= 15;
    }

    private static void ExecuteDirectMove(GameEngine engine, int from, int to)
    {
        CheckerColor color = RemoveCheckerFrom(engine, from);
        AddCheckerTo(engine, to, color);
        engine.RemainingMoves.Clear();
    }

    private static CheckerColor? GetCheckerColorAtPoint(GameEngine engine, int point)
    {
        if (point == 0)
        {
            if (engine.WhitePlayer.CheckersOnBar > 0)
            {
                return CheckerColor.White;
            }

            if (engine.RedPlayer.CheckersOnBar > 0)
            {
                return CheckerColor.Red;
            }
        }
        else if (point >= 1 && point <= 24)
        {
            return engine.Board.GetPoint(point).Color;
        }
        else if (point == 25)
        {
            if (engine.WhitePlayer.CheckersBornOff > 0)
            {
                return CheckerColor.White;
            }

            if (engine.RedPlayer.CheckersBornOff > 0)
            {
                return CheckerColor.Red;
            }
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
            else
            {
                engine.RedPlayer.CheckersOnBar--;
                return CheckerColor.Red;
            }
        }
        else if (point >= 1 && point <= 24)
        {
            var boardPoint = engine.Board.GetPoint(point);
            CheckerColor color = boardPoint.Color!.Value;
            boardPoint.Checkers.RemoveAt(boardPoint.Checkers.Count - 1);
            return color;
        }
        else
        {
            if (engine.WhitePlayer.CheckersBornOff > 0)
            {
                engine.WhitePlayer.CheckersBornOff--;
                return CheckerColor.White;
            }
            else
            {
                engine.RedPlayer.CheckersBornOff--;
                return CheckerColor.Red;
            }
        }
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
            if (point.Color == color)
            {
                count += point.Count;
            }
        }

        var player = color == CheckerColor.White ? engine.WhitePlayer : engine.RedPlayer;
        return count + player.CheckersOnBar + player.CheckersBornOff;
    }
}
