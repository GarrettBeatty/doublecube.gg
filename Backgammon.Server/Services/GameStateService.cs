using Backgammon.Core;
using Backgammon.Server.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Backgammon.Server.Services;

/// <summary>
/// Implementation of game state broadcasting
/// </summary>
public class GameStateService : IGameStateService
{
    private readonly ILogger<GameStateService> _logger;

    public GameStateService(ILogger<GameStateService> logger)
    {
        _logger = logger;
    }

    public async Task BroadcastGameUpdateAsync(GameSession session, IHubCallerClients clients)
    {
        // Send personalized state to each player
        if (!string.IsNullOrEmpty(session.WhiteConnectionId))
        {
            var whiteState = session.GetState(session.WhiteConnectionId);
            await clients.Client(session.WhiteConnectionId).SendAsync("GameUpdate", whiteState);
        }

        if (!string.IsNullOrEmpty(session.RedConnectionId))
        {
            var redState = session.GetState(session.RedConnectionId);
            await clients.Client(session.RedConnectionId).SendAsync("GameUpdate", redState);
        }

        // Send updates to all spectators
        var spectatorState = session.GetState(null); // null = spectator view
        foreach (var spectatorId in session.SpectatorConnections)
        {
            await clients.Client(spectatorId).SendAsync("GameUpdate", spectatorState);
        }
    }

    public async Task BroadcastGameStartAsync(GameSession session, IHubCallerClients clients)
    {
        // Game is ready to start - send personalized state to each player
        if (!string.IsNullOrEmpty(session.WhiteConnectionId))
        {
            var whiteState = session.GetState(session.WhiteConnectionId);
            await clients.Client(session.WhiteConnectionId).SendAsync("GameStart", whiteState);
        }

        if (!string.IsNullOrEmpty(session.RedConnectionId))
        {
            var redState = session.GetState(session.RedConnectionId);
            await clients.Client(session.RedConnectionId).SendAsync("GameStart", redState);
        }

        _logger.LogInformation("Game {GameId} started with both players", session.Id);
    }

    public async Task BroadcastGameOverAsync(GameSession session, IHubCallerClients clients)
    {
        var finalState = session.GetState();
        await clients.Group(session.Id).SendAsync("GameOver", finalState);

        _logger.LogInformation(
            "Game {GameId} completed. Winner: {Winner}",
            session.Id,
            session.Engine.Winner?.Name ?? "Unknown");
    }

    public async Task SendGameStateToConnectionAsync(GameSession session, string connectionId, IHubCallerClients clients)
    {
        var state = session.GetState(connectionId);
        await clients.Client(connectionId).SendAsync("GameUpdate", state);
    }

    public async Task BroadcastDoubleOfferAsync(
        GameSession session,
        string offeringConnectionId,
        int currentValue,
        int newValue,
        IHubCallerClients clients)
    {
        // Determine opponent connection
        var opponentConnectionId = session.GetPlayerColor(offeringConnectionId) == CheckerColor.White
            ? session.RedConnectionId
            : session.WhiteConnectionId;

        if (opponentConnectionId != null && !string.IsNullOrEmpty(opponentConnectionId))
        {
            await clients.Client(opponentConnectionId).SendAsync("DoubleOffered", currentValue, newValue);
        }
    }

    public async Task BroadcastDoubleAcceptedAsync(GameSession session, IHubCallerClients clients)
    {
        // Send updated state to both players
        if (!string.IsNullOrEmpty(session.WhiteConnectionId))
        {
            var whiteState = session.GetState(session.WhiteConnectionId);
            await clients.Client(session.WhiteConnectionId).SendAsync("DoubleAccepted", whiteState);
        }

        if (!string.IsNullOrEmpty(session.RedConnectionId))
        {
            var redState = session.GetState(session.RedConnectionId);
            await clients.Client(session.RedConnectionId).SendAsync("DoubleAccepted", redState);
        }
    }

    public async Task BroadcastMatchUpdateAsync(Models.Match match, string gameId, IHubCallerClients clients)
    {
        await clients.Group(gameId).SendAsync("MatchUpdate", new
        {
            matchId = match.MatchId,
            player1Score = match.Player1Score,
            player2Score = match.Player2Score,
            targetScore = match.TargetScore,
            isCrawfordGame = match.IsCrawfordGame,
            matchComplete = match.Status == "Completed",
            matchWinner = match.WinnerId
        });

        if (match.Status == "Completed")
        {
            _logger.LogInformation("Match {MatchId} completed. Winner: {WinnerId}", match.MatchId, match.WinnerId);
        }
    }
}
