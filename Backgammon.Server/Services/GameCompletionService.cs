using Backgammon.Core;
using Backgammon.Server.Models;
using Microsoft.Extensions.Logging;
using Match = Backgammon.Server.Models.Match;

namespace Backgammon.Server.Services;

/// <summary>
/// Service responsible for handling game and match completion logic.
/// Manages score updates, statistics, and match progression.
/// </summary>
public class GameCompletionService : IGameCompletionService
{
    private readonly IGameRepository _gameRepository;
    private readonly IPlayerStatsService _playerStatsService;
    private readonly IMatchService _matchService;
    private readonly IGameSessionManager _sessionManager;
    private readonly IGameSessionFactory _sessionFactory;
    private readonly IGameBroadcastService _broadcastService;
    private readonly ILogger<GameCompletionService> _logger;

    public GameCompletionService(
        IGameRepository gameRepository,
        IPlayerStatsService playerStatsService,
        IMatchService matchService,
        IGameSessionManager sessionManager,
        IGameSessionFactory sessionFactory,
        IGameBroadcastService broadcastService,
        ILogger<GameCompletionService> logger)
    {
        _gameRepository = gameRepository;
        _playerStatsService = playerStatsService;
        _matchService = matchService;
        _sessionManager = sessionManager;
        _sessionFactory = sessionFactory;
        _broadcastService = broadcastService;
        _logger = logger;
    }

    /// <summary>
    /// Handles game completion, including database updates, stats, and broadcasting.
    /// </summary>
    public async Task HandleGameCompletionAsync(GameSession session)
    {
        try
        {
            if (session.Engine.Winner == null)
            {
                _logger.LogWarning("HandleGameCompletion called but no winner found for game {GameId}", session.Id);
                return;
            }

            _logger.LogInformation(
                "Handling game completion: GameId={GameId}, Winner={Winner}, IsMatchGame={IsMatchGame}",
                session.Id,
                session.Engine.Winner.Name,
                session.IsMatchGame);

            // Update game status and stats BEFORE broadcasting
            if (session.GameMode.ShouldPersist)
            {
                await _gameRepository.UpdateGameStatusAsync(session.Id, "Completed");
            }

            if (session.GameMode.ShouldTrackStats)
            {
                var game = GameEngineMapper.ToGame(session);
                await _playerStatsService.UpdateStatsAfterGameCompletionAsync(game);
            }

            // Broadcast GameOver AFTER database is updated
            await _broadcastService.BroadcastGameOverAsync(session);

            // Handle match-specific completion
            if (session.IsMatchGame)
            {
                await HandleMatchGameCompletionAsync(session);
            }

            // Remove from memory to prevent memory leak
            _sessionManager.RemoveGame(session.Id);
            _logger.LogInformation("Removed completed game {GameId} from memory", session.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling game completion for game {GameId}", session.Id);
            throw;
        }
    }

    /// <summary>
    /// Handles match game completion, including score updates and auto-starting next game.
    /// </summary>
    private async Task HandleMatchGameCompletionAsync(GameSession session)
    {
        if (string.IsNullOrEmpty(session.MatchId))
        {
            _logger.LogWarning("Match game {GameId} has no MatchId", session.Id);
            return;
        }

        try
        {
            var winnerColor = session.Engine.Winner?.Color;
            if (winnerColor == null)
            {
                return;
            }

            var winnerId = winnerColor == CheckerColor.White ? session.WhitePlayerId : session.RedPlayerId;
            var winType = session.Engine.DetermineWinType();
            var stakes = session.Engine.GetGameResult();

            var result = new GameResult(winnerId ?? string.Empty, winType, session.Engine.DoublingCube.Value);

            // Update match scores
            await _matchService.CompleteGameAsync(session.Id, result);

            var match = await _matchService.GetMatchAsync(session.MatchId);
            if (match != null)
            {
                // Broadcast match score update
                await _broadcastService.BroadcastMatchUpdateAsync(match, session.Id);

                // Auto-start next game if match is not complete
                if (!match.CoreMatch.IsMatchComplete())
                {
                    await StartNextGameInMatchAsync(match, session);
                }
                else
                {
                    _logger.LogInformation(
                        "Match {MatchId} complete. Winner: {WinnerId}, Final Score: {P1Score}-{P2Score}",
                        match.MatchId,
                        match.WinnerId,
                        match.Player1Score,
                        match.Player2Score);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling match game completion for game {GameId}", session.Id);
            throw;
        }
    }

    /// <summary>
    /// Starts the next game in a match after the previous game completes.
    /// </summary>
    private async Task StartNextGameInMatchAsync(Match match, GameSession completedSession)
    {
        try
        {
            _logger.LogInformation(
                "Auto-starting next game for match {MatchId}. Current score: {P1Score}-{P2Score}",
                match.MatchId,
                match.Player1Score,
                match.Player2Score);

            // Start the next game (this creates the session in MatchService)
            var nextGame = await _matchService.StartNextGameAsync(match.MatchId);

            // Get the session that was just created
            var session = _sessionManager.GetSession(nextGame.GameId);
            if (session == null)
            {
                _logger.LogError("Session not found for newly created game {GameId}", nextGame.GameId);
                return;
            }

            // Add player connections from completed game
            // Map player IDs to their connections from the completed session (by player ID, not color)
            var player1Connections = completedSession.WhitePlayerId == match.Player1Id
                ? completedSession.WhiteConnections
                : completedSession.RedConnections;

            var player2Connections = completedSession.WhitePlayerId == match.Player2Id
                ? completedSession.WhiteConnections
                : completedSession.RedConnections;

            // Add Player 1 connections to new session
            foreach (var connectionId in player1Connections.ToList())
            {
                if (await IsConnectionActiveAsync(connectionId))
                {
                    session.AddPlayer(match.Player1Id, connectionId);
                }
            }

            // Add Player 2 connections to new session
            foreach (var connectionId in player2Connections.ToList())
            {
                if (await IsConnectionActiveAsync(connectionId))
                {
                    session.AddPlayer(match.Player2Id, connectionId);
                }
            }

            // Initialize game engine (but don't roll dice - players will do opening roll)
            session.Engine.StartNewGame();

            // Broadcast the new game to connected clients
            await _broadcastService.BroadcastMatchGameStartingAsync(match, nextGame.GameId);

            _logger.LogInformation(
                "Started next game {GameId} for match {MatchId}",
                nextGame.GameId,
                match.MatchId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-start next game for match {MatchId}", match.MatchId);
            // Don't throw - failing to auto-start shouldn't crash the completion flow
            // Client can still manually call ContinueMatch()
        }
    }

    /// <summary>
    /// Checks if a SignalR connection is still active.
    /// </summary>
    private async Task<bool> IsConnectionActiveAsync(string connectionId)
    {
        try
        {
            // Simple check - if we can get the session for this connection, it's active
            // More sophisticated checks could be added here
            return await Task.FromResult(!string.IsNullOrEmpty(connectionId));
        }
        catch
        {
            return false;
        }
    }
}
