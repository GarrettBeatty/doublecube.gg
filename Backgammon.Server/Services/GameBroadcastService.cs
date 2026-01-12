using Backgammon.Server.Hubs;
using Backgammon.Server.Hubs.Interfaces;
using Backgammon.Server.Models;
using Backgammon.Server.Models.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Match = Backgammon.Server.Models.Match;

namespace Backgammon.Server.Services;

/// <summary>
/// Service responsible for broadcasting game state changes via SignalR.
/// Handles all real-time communication to connected clients.
/// </summary>
public class GameBroadcastService : IGameBroadcastService
{
    private readonly IHubContext<GameHub, IGameHubClient> _hubContext;
    private readonly IGameSessionManager _sessionManager;
    private readonly ILogger<GameBroadcastService> _logger;

    public GameBroadcastService(
        IHubContext<GameHub, IGameHubClient> hubContext,
        IGameSessionManager sessionManager,
        ILogger<GameBroadcastService> logger)
    {
        _hubContext = hubContext;
        _sessionManager = sessionManager;
        _logger = logger;
    }

    /// <summary>
    /// Broadcasts game state update to all players and spectators.
    /// Sends personalized state to each connection.
    /// </summary>
    public async Task BroadcastGameUpdateAsync(GameSession session)
    {
        try
        {
            // Send personalized state to each White player connection
            foreach (var connectionId in session.WhiteConnections)
            {
                var whiteState = session.GetState(connectionId);
                await _hubContext.Clients.Client(connectionId).GameUpdate(whiteState);
            }

            // Send personalized state to each Red player connection
            foreach (var connectionId in session.RedConnections)
            {
                var redState = session.GetState(connectionId);
                await _hubContext.Clients.Client(connectionId).GameUpdate(redState);
            }

            // Send updates to all spectators
            var spectatorState = session.GetState(null);
            foreach (var spectatorId in session.SpectatorConnections)
            {
                await _hubContext.Clients.Client(spectatorId).GameUpdate(spectatorState);
            }

            _logger.LogDebug("Broadcasted game update for game {GameId}", session.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting game update for game {GameId}", session.Id);
            throw;
        }
    }

    /// <summary>
    /// Broadcasts game over event to all players in the game.
    /// </summary>
    public async Task BroadcastGameOverAsync(GameSession session)
    {
        try
        {
            var finalState = session.GetState();
            await _hubContext.Clients.Group(session.Id).GameOver(finalState);

            _logger.LogInformation(
                "Broadcasted GameOver for game {GameId}, Winner: {Winner}",
                session.Id,
                session.Engine.Winner?.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting game over for game {GameId}", session.Id);
            throw;
        }
    }

    /// <summary>
    /// Broadcasts match score update to all players.
    /// </summary>
    public async Task BroadcastMatchUpdateAsync(Match match, string gameId)
    {
        try
        {
            await _hubContext.Clients.Group(gameId).MatchUpdate(new MatchUpdateDto
            {
                MatchId = match.MatchId,
                Player1Score = match.Player1Score,
                Player2Score = match.Player2Score,
                TargetScore = match.TargetScore,
                IsCrawfordGame = match.IsCrawfordGame,
                MatchComplete = match.Status == "Completed",
                MatchWinner = match.WinnerId
            });

            _logger.LogInformation(
                "Broadcasted match update for match {MatchId}: {P1Score}-{P2Score}",
                match.MatchId,
                match.Player1Score,
                match.Player2Score);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting match update for match {MatchId}", match.MatchId);
            throw;
        }
    }

    /// <summary>
    /// Broadcasts that a new game in a match is starting.
    /// Sends to all connected players with personalized state.
    /// </summary>
    public async Task BroadcastMatchGameStartingAsync(Match match, string newGameId)
    {
        try
        {
            var session = _sessionManager.GetSession(newGameId);
            if (session == null)
            {
                _logger.LogWarning("Cannot broadcast game starting - session {GameId} not found", newGameId);
                return;
            }

            // Send to all connected players
            foreach (var connectionId in session.WhiteConnections.Concat(session.RedConnections))
            {
                var state = session.GetState(connectionId);
                var dto = new MatchGameStartingDto
                {
                    MatchId = match.MatchId,
                    GameId = newGameId,
                    GameNumber = match.TotalGamesPlayed + 1,
                    CurrentScore = new MatchScoreDto
                    {
                        Player1 = match.Player1Score,
                        Player2 = match.Player2Score
                    },
                    IsCrawfordGame = match.IsCrawfordGame,
                    State = state
                };

                await _hubContext.Clients.Client(connectionId).MatchGameStarting(dto);
            }

            _logger.LogInformation(
                "Broadcasted MatchGameStarting for game {GameId} in match {MatchId}",
                newGameId,
                match.MatchId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting match game starting for game {GameId}", newGameId);
            throw;
        }
    }

    /// <summary>
    /// Broadcasts game state to a specific connection.
    /// Used for reconnection scenarios.
    /// </summary>
    public async Task SendGameStateToConnectionAsync(GameSession session, string connectionId)
    {
        try
        {
            var state = session.GetState(connectionId);
            await _hubContext.Clients.Client(connectionId).GameUpdate(state);

            _logger.LogDebug(
                "Sent game state for game {GameId} to connection {ConnectionId}",
                session.Id,
                connectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error sending game state for game {GameId} to connection {ConnectionId}",
                session.Id,
                connectionId);
            throw;
        }
    }
}
