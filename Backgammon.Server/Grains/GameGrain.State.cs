using Backgammon.Core;
using Backgammon.Server.Models;
using Backgammon.Server.Models.SignalR;
using Backgammon.Server.Services;
using ServerGameStatus = Backgammon.Server.Models.GameStatus;

namespace Backgammon.Server.Grains;

/// <summary>
/// Partial class for GameGrain: state computation (GetState logic).
/// </summary>
public partial class GameGrain
{
    private GameState GetStateInternal(string? forConnectionId = null)
    {
        CheckerColor? playerColor = forConnectionId != null ? GetPlayerColorInternal(forConnectionId) : null;
        var features = _gameMode.GetFeatures();

        var state = new GameState
        {
            GameId = this.GetPrimaryKeyString(),
            WhitePlayerId = _whitePlayerId ?? string.Empty,
            RedPlayerId = _redPlayerId ?? string.Empty,
            WhitePlayerName = _whitePlayerName ?? _whitePlayerId ?? "Waiting...",
            RedPlayerName = _redPlayerName ?? _redPlayerId ?? "Waiting...",
            WhiteRating = _whiteRating,
            RedRating = _redRating,
            WhiteRatingChange = _whiteRating.HasValue && _whiteRatingBefore.HasValue ? _whiteRating - _whiteRatingBefore : null,
            RedRatingChange = _redRating.HasValue && _redRatingBefore.HasValue ? _redRating - _redRatingBefore : null,
            CurrentPlayer = _engine.CurrentPlayer?.Color ?? CheckerColor.White,
            YourColor = playerColor,
            IsYourTurn = forConnectionId != null && IsPlayerTurnInternal(forConnectionId),
            Dice = new[] { _engine.Dice.Die1, _engine.Dice.Die2 },
            RemainingMoves = _engine.RemainingMoves.ToArray(),
            MovesMadeThisTurn = _engine.MoveHistory.Count,
            TurnHistory = _engine.History.Turns.Select(TurnSnapshotDto.FromCore).ToList(),
            CurrentTurnMoves = _engine.CurrentTurnMoves.Select(m => m.ToNotation()).ToList(),
            Board = GetBoardStateInternal(),
            WhiteCheckersOnBar = _engine.WhitePlayer.CheckersOnBar,
            RedCheckersOnBar = _engine.RedPlayer.CheckersOnBar,
            WhiteBornOff = _engine.WhitePlayer.CheckersBornOff,
            RedBornOff = _engine.RedPlayer.CheckersBornOff,
            WhitePipCount = CalculatePipCount(CheckerColor.White),
            RedPipCount = CalculatePipCount(CheckerColor.Red),
            Status = _status switch
            {
                SessionStatus.WaitingForOpponent => ServerGameStatus.WaitingForPlayer,
                SessionStatus.InProgress => ServerGameStatus.InProgress,
                SessionStatus.Completed => ServerGameStatus.Completed,
                _ => throw new InvalidOperationException($"Unknown status: {_status}")
            },
            Winner = _engine.Winner?.Color,
            DoublingCubeValue = _engine.DoublingCube.Value,
            DoublingCubeOwner = _engine.DoublingCube.Owner?.ToString(),
            CanDouble = playerColor.HasValue
                && IsFull
                && !_engine.IsOpeningRoll
                && !(_isCrawfordGame ?? false)
                && !_hasPendingDoubleOffer
                && _engine.CurrentPlayer?.Color == playerColor.Value
                && _engine.DoublingCube.CanDouble(playerColor.Value),
            HasPendingDoubleOffer = _hasPendingDoubleOffer,
            IsAwaitingDoubleResponse = _hasPendingDoubleOffer && GetPlayerIdForColor(playerColor) == _pendingDoubleOfferedBy,
            HasReceivedDoubleOffer = _hasPendingDoubleOffer && GetPlayerIdForColor(playerColor) != _pendingDoubleOfferedBy && playerColor.HasValue,
            PendingDoubleNewValue = _hasPendingDoubleOffer ? _engine.DoublingCube.Value * 2 : 0,
            IsAnalysisMode = features.ShowAnalysisBadge,
            IsRated = _isRated,
            MatchId = _matchId,
            TargetScore = _targetScore,
            Player1Score = _player1Score,
            Player2Score = _player2Score,
            IsCrawfordGame = _isCrawfordGame,
            IsOpeningRoll = _engine.IsOpeningRoll,
            WhiteOpeningRoll = _engine.WhiteOpeningRoll,
            RedOpeningRoll = _engine.RedOpeningRoll,
            IsOpeningRollTie = _engine.IsOpeningRollTie,
            LeaveGameAction = GetLeaveGameActionInternal(),
            TimeControlType = _timeControl?.Type.ToString(),
            DelaySeconds = _timeControl?.DelaySeconds,
            WhiteReserveSeconds = _engine.WhiteTimeState?.GetRemainingTime(_timeControl?.DelaySeconds ?? 0).TotalSeconds,
            RedReserveSeconds = _engine.RedTimeState?.GetRemainingTime(_timeControl?.DelaySeconds ?? 0).TotalSeconds,
            WhiteIsInDelay = !_engine.IsOpeningRoll
                && _engine.CurrentPlayer?.Color == CheckerColor.White
                && _engine.WhiteTimeState?.TurnStartTime != null
                && _engine.WhiteTimeState.CalculateIsInDelay(_timeControl?.DelaySeconds ?? 0),
            RedIsInDelay = !_engine.IsOpeningRoll
                && _engine.CurrentPlayer?.Color == CheckerColor.Red
                && _engine.RedTimeState?.TurnStartTime != null
                && _engine.RedTimeState.CalculateIsInDelay(_timeControl?.DelaySeconds ?? 0),
            WhiteDelayRemaining = !_engine.IsOpeningRoll
                && _engine.CurrentPlayer?.Color == CheckerColor.White
                && _engine.WhiteTimeState?.TurnStartTime != null
                ? _engine.WhiteTimeState.GetDelayRemaining(_timeControl?.DelaySeconds ?? 0).TotalSeconds
                : 0,
            RedDelayRemaining = !_engine.IsOpeningRoll
                && _engine.CurrentPlayer?.Color == CheckerColor.Red
                && _engine.RedTimeState?.TurnStartTime != null
                ? _engine.RedTimeState.GetDelayRemaining(_timeControl?.DelaySeconds ?? 0).TotalSeconds
                : 0,
            IsCorrespondence = _isCorrespondence,
            TimePerMoveDays = _timePerMoveDays,
            TurnDeadline = _turnDeadline
        };

        var validMoves = _engine.GetValidMoves(includeCombined: true);
        state.ValidMoves = validMoves.Select(m => new MoveDto
        {
            From = m.From,
            To = m.To,
            DieValue = m.DieValue,
            IsHit = m.IsHit || WillHit(m),
            IsCombinedMove = m.IsCombined,
            DiceUsed = m.DiceUsed,
            IntermediatePoints = m.IntermediatePoints
        }).ToList();

        state.HasValidMoves = validMoves.Count > 0;

        if (_engine.Winner != null)
        {
            var stakes = _engine.GetGameResult();
            var multiplier = stakes / _engine.DoublingCube.Value;
            state.WinType = multiplier switch
            {
                3 => "Backgammon",
                2 => "Gammon",
                _ => "Normal"
            };
        }

        return state;
    }

    private PointState[] GetBoardStateInternal()
    {
        var points = new List<PointState>();
        for (int i = 1; i <= 24; i++)
        {
            var point = _engine.Board.GetPoint(i);
            points.Add(new PointState { Position = i, Color = point.Color, Count = point.Count });
        }

        return points.ToArray();
    }

    private bool WillHit(Move move)
    {
        if (move.IsBearOff) return false;
        var targetPoint = _engine.Board.GetPoint(move.To);
        if (targetPoint.Color == null || targetPoint.Count == 0) return false;
        return targetPoint.Color != _engine.CurrentPlayer?.Color && targetPoint.Count == 1;
    }

    private string? GetPlayerIdForColor(CheckerColor? color) => color switch
    {
        CheckerColor.White => _whitePlayerId,
        CheckerColor.Red => _redPlayerId,
        _ => null
    };

    private int CalculatePipCount(CheckerColor color)
    {
        int pips = 0;
        for (int pointNum = 1; pointNum <= 24; pointNum++)
        {
            var point = _engine.Board.GetPoint(pointNum);
            if (point.Color == color && point.Count > 0)
            {
                pips += color == CheckerColor.White
                    ? point.Count * pointNum
                    : point.Count * (25 - pointNum);
            }
        }

        pips += color == CheckerColor.White
            ? _engine.WhitePlayer.CheckersOnBar * 25
            : _engine.RedPlayer.CheckersOnBar * 25;

        return pips;
    }

    private string GetLeaveGameActionInternal()
    {
        if (!_engine.GameStarted || _engine.IsOpeningRoll) return "Abandon";
        return "Forfeit";
    }
}
