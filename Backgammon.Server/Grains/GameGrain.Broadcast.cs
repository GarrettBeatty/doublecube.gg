using Backgammon.Server.Models.SignalR;
using Match = Backgammon.Server.Models.Match;
using Microsoft.Extensions.Logging;

namespace Backgammon.Server.Grains;

/// <summary>
/// Partial class for GameGrain: SignalR broadcast helpers.
/// </summary>
public partial class GameGrain
{
    private async Task BroadcastGameUpdateAsync()
    {
        var gameId = this.GetPrimaryKeyString();
        try
        {
            foreach (var connId in _whiteConnections)
                await _hubContext.Clients.Client(connId).GameUpdate(GetStateInternal(connId));

            foreach (var connId in _redConnections)
                await _hubContext.Clients.Client(connId).GameUpdate(GetStateInternal(connId));

            var spectatorState = GetStateInternal(null);
            foreach (var connId in _spectatorConnections)
                await _hubContext.Clients.Client(connId).GameUpdate(spectatorState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting update for game {GameId}", gameId);
        }
    }

    private async Task BroadcastGameStartAsync()
    {
        try
        {
            foreach (var connId in _whiteConnections)
                await _hubContext.Clients.Client(connId).GameStart(GetStateInternal(connId));

            foreach (var connId in _redConnections)
                await _hubContext.Clients.Client(connId).GameStart(GetStateInternal(connId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting game start for {GameId}", this.GetPrimaryKeyString());
        }
    }

    private async Task BroadcastGameOverAsync()
    {
        try
        {
            var finalState = GetStateInternal(null);
            await _hubContext.Clients.Group(this.GetPrimaryKeyString()).GameOver(finalState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting game over for {GameId}", this.GetPrimaryKeyString());
        }
    }

    private async Task BroadcastDoubleAcceptedAsync()
    {
        try
        {
            foreach (var connId in _whiteConnections)
                await _hubContext.Clients.Client(connId).DoubleAccepted(GetStateInternal(connId));

            foreach (var connId in _redConnections)
                await _hubContext.Clients.Client(connId).DoubleAccepted(GetStateInternal(connId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting double accepted for {GameId}", this.GetPrimaryKeyString());
        }
    }

    private async Task BroadcastMatchUpdateAsync(Match match)
    {
        try
        {
            var gameId = this.GetPrimaryKeyString();
            await _hubContext.Clients.Group(gameId).MatchUpdate(new MatchUpdateDto
            {
                MatchId = match.MatchId,
                Player1Score = match.Player1Score,
                Player2Score = match.Player2Score,
                TargetScore = match.TargetScore,
                IsCrawfordGame = match.IsCrawfordGame,
                MatchComplete = match.CoreMatch.Status == Core.MatchStatus.Completed,
                MatchWinner = match.WinnerId,
                NextGameId = match.CurrentGameId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting match update for {GameId}", this.GetPrimaryKeyString());
        }
    }
}
