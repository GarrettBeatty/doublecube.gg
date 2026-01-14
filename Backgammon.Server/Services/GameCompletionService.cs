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
    private readonly IAiMoveService _aiMoveService;
    private readonly IAiPlayerManager _aiPlayerManager;
    private readonly ILogger<GameCompletionService> _logger;

    public GameCompletionService(
        IGameRepository gameRepository,
        IPlayerStatsService playerStatsService,
        IMatchService matchService,
        IGameSessionManager sessionManager,
        IGameSessionFactory sessionFactory,
        IGameBroadcastService broadcastService,
        IAiMoveService aiMoveService,
        IAiPlayerManager aiPlayerManager,
        ILogger<GameCompletionService> logger)
    {
        _gameRepository = gameRepository;
        _playerStatsService = playerStatsService;
        _matchService = matchService;
        _sessionManager = sessionManager;
        _sessionFactory = sessionFactory;
        _broadcastService = broadcastService;
        _aiMoveService = aiMoveService;
        _aiPlayerManager = aiPlayerManager;
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
                "Handling game completion: GameId={GameId}, Winner={Winner}, MatchId={MatchId}",
                session.Id,
                session.Engine.Winner.Name,
                session.MatchId ?? "None");

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
            if (!string.IsNullOrEmpty(session.MatchId))
            {
                await HandleMatchGameCompletionAsync(session);

                // Schedule cleanup after 5 minutes to allow players time to continue
                var gameId = session.Id;
                _ = Task.Delay(TimeSpan.FromMinutes(5)).ContinueWith(_ =>
                {
                    _sessionManager.RemoveGame(gameId);
                    _logger.LogInformation("Cleaned up completed match game {GameId} after timeout", gameId);
                });

                _logger.LogInformation(
                    "Keeping completed match game {GameId} in memory for continuation (will cleanup after 5 minutes)",
                    session.Id);
            }
            else
            {
                // Standalone game - remove immediately
                _sessionManager.RemoveGame(session.Id);
                _logger.LogInformation("Removed completed standalone game {GameId} from memory", session.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling game completion for game {GameId}", session.Id);
            throw;
        }
    }

    /// <summary>
    /// Creates and starts the next game in a match atomically.
    /// This eliminates the "empty session" pattern by creating the game, adding players,
    /// starting the engine, and broadcasting in a single operation.
    /// </summary>
    public async Task<GameSession> CreateAndStartNextMatchGameAsync(
        Match match,
        HashSet<string> player1Connections,
        HashSet<string> player2Connections)
    {
        try
        {
            _logger.LogInformation(
                "========== CreateAndStartNextMatchGame ==========");
            _logger.LogInformation(
                "Match {MatchId} - OpponentType={OpponentType}, P1={P1Id}, P2={P2Id}, Current score: {P1Score}-{P2Score}",
                match.MatchId,
                match.OpponentType,
                match.Player1Id,
                match.Player2Id ?? "null",
                match.Player1Score,
                match.Player2Score);
            _logger.LogInformation(
                "Player1 connections: {P1Connections}, Player2 connections: {P2Connections}",
                player1Connections.Count,
                player2Connections.Count);

            // Create the next game in the database
            var nextGame = await _matchService.StartNextGameAsync(match.MatchId);

            _logger.LogInformation(
                "Created next game {GameId} in database",
                nextGame.GameId);

            // Get the session that was just created
            var session = _sessionManager.GetSession(nextGame.GameId);
            if (session == null)
            {
                _logger.LogError("Session not found for newly created game {GameId}", nextGame.GameId);
                throw new InvalidOperationException($"Session not found for game {nextGame.GameId}");
            }

            _logger.LogInformation(
                "Found session {GameId} in memory",
                nextGame.GameId);

            // Add both players to the session
            AddPlayersToSession(session, match, player1Connections, player2Connections);

            _logger.LogInformation(
                "Added players to session {GameId}",
                nextGame.GameId);

            // Start the game engine
            session.Engine.StartNewGame();

            _logger.LogInformation(
                "Started game engine for {GameId}. Players: P1={P1Id} ({P1Name}), P2={P2Id} ({P2Name})",
                nextGame.GameId,
                match.Player1Id,
                match.Player1Name,
                match.Player2Id ?? "null",
                match.Player2Name ?? "null");

            // Broadcast GameStart to all connected clients
            await _broadcastService.BroadcastGameStartAsync(session);

            _logger.LogInformation(
                "Broadcasted GameStart for {GameId}",
                nextGame.GameId);

            return session;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create and start next game for match {MatchId}", match.MatchId);
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

                // Log match status - next game will be created when players click "Continue"
                if (!match.CoreMatch.IsMatchComplete())
                {
                    _logger.LogInformation(
                        "Match {MatchId} game completed. Score: {P1Score}-{P2Score}. Waiting for players to continue.",
                        match.MatchId,
                        match.Player1Score,
                        match.Player2Score);
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
    /// Helper method to add players to a session based on match configuration.
    /// Handles both human vs human and human vs AI matches.
    /// </summary>
    private void AddPlayersToSession(
        GameSession session,
        Match match,
        HashSet<string> player1Connections,
        HashSet<string> player2Connections)
    {
        // Add player 1 (always human in match context)
        foreach (var connectionId in player1Connections)
        {
            session.AddPlayer(match.Player1Id, connectionId);
        }

        session.SetPlayerName(match.Player1Id, match.Player1Name);

        _logger.LogInformation(
            "Added player 1 to session {GameId}: {PlayerId} ({PlayerName}) with {ConnectionCount} connections",
            session.Id,
            match.Player1Id,
            match.Player1Name,
            player1Connections.Count);

        // Add player 2 (human or AI)
        if (match.OpponentType == "AI")
        {
            // Get AI player for this match (consistent across all games)
            var aiPlayerId = _aiPlayerManager.GetOrCreateAiForMatch(match.MatchId);
            var aiPlayerName = _aiPlayerManager.GetAiNameForMatch(match.MatchId, "Greedy"); // aiType param ignored, uses stored type

            session.AddPlayer(aiPlayerId, string.Empty); // Empty connection ID for AI
            session.SetPlayerName(aiPlayerId, aiPlayerName);

            _logger.LogInformation(
                "Added AI player to session {GameId}: {AiPlayerId} ({AiName})",
                session.Id,
                aiPlayerId,
                aiPlayerName);
        }
        else
        {
            // Add human player 2 with all their connections
            foreach (var connectionId in player2Connections)
            {
                session.AddPlayer(match.Player2Id, connectionId);
            }

            session.SetPlayerName(match.Player2Id, match.Player2Name);

            _logger.LogInformation(
                "Added player 2 to session {GameId}: {PlayerId} ({PlayerName}) with {ConnectionCount} connections",
                session.Id,
                match.Player2Id,
                match.Player2Name,
                player2Connections.Count);
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
