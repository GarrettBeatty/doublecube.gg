using Backgammon.Core;
using Backgammon.Server.Models.SignalR;
using Backgammon.Server.Services;
using Microsoft.Extensions.Logging;

namespace Backgammon.Server.Grains;

/// <summary>
/// Partial class for GameGrain: time control tick, timeout handling.
/// </summary>
public partial class GameGrain
{
    private void StartTimeUpdates()
    {
        if (_timeControl == null || _timeControl.Type == TimeControlType.None) return;

        StopTimeUpdates();

        // Orleans 9 RegisterGrainTimer: Func<CancellationToken, Task> overload.
        // Interleave = false ensures single-threaded execution within the grain.
        _timeUpdateTimer = this.RegisterGrainTimer(
            TickTimeUpdateAsync,
            new GrainTimerCreationOptions
            {
                DueTime = TimeSpan.FromSeconds(1),
                Period = TimeSpan.FromSeconds(1),
                Interleave = false
            });
    }

    private void StopTimeUpdates()
    {
        _timeUpdateTimer?.Dispose();
        _timeUpdateTimer = null;
    }

    private async Task TickTimeUpdateAsync(CancellationToken cancellationToken)
    {
        var gameId = this.GetPrimaryKeyString();

        if (_engine.GameOver)
        {
            StopTimeUpdates();
            return;
        }

        if (_engine.WhiteTimeState == null || _engine.RedTimeState == null || _timeControl == null)
        {
            StopTimeUpdates();
            return;
        }

        if (_timeControl.Type == TimeControlType.None)
        {
            StopTimeUpdates();
            return;
        }

        var timeUpdate = new TimeUpdateDto
        {
            GameId = gameId,
            WhiteReserveSeconds = _engine.WhiteTimeState.GetRemainingTime(_timeControl.DelaySeconds).TotalSeconds,
            RedReserveSeconds = _engine.RedTimeState.GetRemainingTime(_timeControl.DelaySeconds).TotalSeconds,
            WhiteIsInDelay = !_engine.IsOpeningRoll
                && _engine.CurrentPlayer?.Color == CheckerColor.White
                && _engine.WhiteTimeState.TurnStartTime != null
                && _engine.WhiteTimeState.CalculateIsInDelay(_timeControl.DelaySeconds),
            RedIsInDelay = !_engine.IsOpeningRoll
                && _engine.CurrentPlayer?.Color == CheckerColor.Red
                && _engine.RedTimeState.TurnStartTime != null
                && _engine.RedTimeState.CalculateIsInDelay(_timeControl.DelaySeconds),
            WhiteDelayRemaining = !_engine.IsOpeningRoll
                && _engine.CurrentPlayer?.Color == CheckerColor.White
                && _engine.WhiteTimeState.TurnStartTime != null
                ? _engine.WhiteTimeState.GetDelayRemaining(_timeControl.DelaySeconds).TotalSeconds
                : 0,
            RedDelayRemaining = !_engine.IsOpeningRoll
                && _engine.CurrentPlayer?.Color == CheckerColor.Red
                && _engine.RedTimeState.TurnStartTime != null
                ? _engine.RedTimeState.GetDelayRemaining(_timeControl.DelaySeconds).TotalSeconds
                : 0
        };

        await _hubContext.Clients.Group(gameId).TimeUpdate(timeUpdate);

        if (_engine.HasCurrentPlayerTimedOut())
        {
            StopTimeUpdates();
            await HandleTimeoutAsync();
        }
    }

    private async Task HandleTimeoutAsync()
    {
        var gameId = this.GetPrimaryKeyString();

        if (_engine.GameOver || _timeControl == null || _timeControl.Type == TimeControlType.None) return;

        var losingPlayer = _engine.CurrentPlayer;
        var winningPlayer = _engine.GetOpponent();

        if (losingPlayer == null || winningPlayer == null) return;

        try
        {
            _engine.ForfeitGame(winningPlayer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to forfeit game {GameId} on timeout", gameId);
            return;
        }

        var timeoutEvent = new PlayerTimedOutDto
        {
            GameId = gameId,
            TimedOutPlayer = losingPlayer.Color.ToString(),
            Winner = winningPlayer.Color.ToString()
        };

        await _hubContext.Clients.Group(gameId).PlayerTimedOut(timeoutEvent);
        await BroadcastGameUpdateAsync();
        await HandleGameCompletionAsync();
    }
}
