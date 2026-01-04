using Backgammon.Core;
using Backgammon.Server.Extensions;
using Backgammon.Server.Hubs;
using Backgammon.Server.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Match = Backgammon.Server.Models.Match;

namespace Backgammon.Server.Services;

/// <summary>
/// Unified service for game creation, joining, and state broadcasting
/// Consolidates GameCreationService and GameStateService
/// </summary>
public class GameService : IGameService
{
    private readonly IGameSessionManager _sessionManager;
    private readonly IGameRepository _gameRepository;
    private readonly IAiMoveService _aiMoveService;
    private readonly IGameActionOrchestrator _gameActionOrchestrator;
    private readonly IHubContext<GameHub> _hubContext;
    private readonly ILogger<GameService> _logger;

    public GameService(
        IGameSessionManager sessionManager,
        IGameRepository gameRepository,
        IAiMoveService aiMoveService,
        IGameActionOrchestrator gameActionOrchestrator,
        IHubContext<GameHub> hubContext,
        ILogger<GameService> logger)
    {
        _sessionManager = sessionManager;
        _gameRepository = gameRepository;
        _aiMoveService = aiMoveService;
        _gameActionOrchestrator = gameActionOrchestrator;
        _hubContext = hubContext;
        _logger = logger;
    }

    // ==================== Game Creation & Joining ====================

    public async Task JoinGameAsync(string connectionId, string playerId, string? displayName, string gameId)
    {
        var session = _sessionManager.GetGame(gameId);
        if (session == null)
        {
            throw new InvalidOperationException($"Game {gameId} not found");
        }

        await _hubContext.Groups.AddToGroupAsync(connectionId, session.Id);
        _logger.LogInformation(
            "Player {PlayerId} (connection {ConnectionId}) joined game {GameId}",
            playerId,
            connectionId,
            session.Id);

        // Check if game is already in progress (for reconnection detection)
        var wasAlreadyStarted = session.Engine.GameStarted;

        // Try to add as player; if full, add as spectator
        if (!session.AddPlayer(playerId, connectionId))
        {
            session.AddSpectator(connectionId);
            _sessionManager.RegisterPlayerConnection(connectionId, session.Id); // Register spectator
            var spectatorState = session.GetState(null); // No color for spectators
            await _hubContext.Clients.Client(connectionId).SendAsync("SpectatorJoined", spectatorState);
            _logger.LogInformation(
            "Spectator {ConnectionId} joined game {GameId}",
            connectionId,
            session.Id);
            return;
        }

        // Register player connection for game action lookups
        _sessionManager.RegisterPlayerConnection(connectionId, session.Id);

        // Set display name if authenticated
        if (!string.IsNullOrEmpty(displayName))
        {
            session.SetPlayerName(playerId, displayName);
        }

        if (session.IsFull)
        {
            // If game was already started, this is a reconnection - just send current state
            if (wasAlreadyStarted)
            {
                _logger.LogInformation(
                    "Player {PlayerId} reconnecting to in-progress game {GameId}",
                    playerId,
                    session.Id);

                var state = session.GetState(connectionId);
                await _hubContext.Clients.Client(connectionId).SendAsync("GameStart", state);
                return;
            }

            // Game just became full - broadcast start to both players
            await BroadcastGameStartAsync(session);

            // Timer will be started after opening roll completes (see GameActionOrchestrator.RollDiceAsync)

            // Save game state when game starts (progressive save)
            BackgroundTaskHelper.FireAndForget(
                async () =>
                {
                    var game = GameEngineMapper.ToGame(session);
                    await _gameRepository.SaveGameAsync(game);
                },
                _logger,
                $"SaveGameState-{session.Id}");

            // Check if AI should move first (only for non-opening-roll games, i.e., reconnections)
            // For opening roll, AI will roll after human rolls (triggered in RollDiceAsync)
            if (!session.Engine.IsOpeningRoll)
            {
                var currentPlayerId = session.Engine.CurrentPlayer?.Color == CheckerColor.White
                    ? session.WhitePlayerId
                    : session.RedPlayerId;
                if (currentPlayerId != null && _aiMoveService.IsAiPlayer(currentPlayerId))
                {
                    _logger.LogInformation("AI goes first - triggering AI turn for game {GameId}", session.Id);
                    var playerIdForTask = currentPlayerId;
                    BackgroundTaskHelper.FireAndForget(
                        async () =>
                        {
                            await _gameActionOrchestrator.ExecuteAiTurnWithBroadcastAsync(session, playerIdForTask);
                        },
                        _logger,
                        $"AiFirstTurn-{session.Id}");
                }
            }
        }
        else
        {
            // Waiting for opponent - game stays in memory only until opponent joins
            var state = session.GetState(connectionId);
            await _hubContext.Clients.Client(connectionId).SendAsync("GameUpdate", state);
            await _hubContext.Clients.Client(connectionId).SendAsync("WaitingForOpponent", session.Id);
            _logger.LogInformation(
                "Player {PlayerId} waiting in game {GameId} (in-memory only)",
                playerId,
                session.Id);
        }
    }

    public async Task LeaveGameAsync(string connectionId)
    {
        var session = _sessionManager.GetGameByPlayer(connectionId);
        if (session == null)
        {
            return;
        }

        await _hubContext.Groups.RemoveFromGroupAsync(connectionId, session.Id);

        // Check if this is a spectator
        if (session.IsSpectator(connectionId))
        {
            session.RemoveSpectator(connectionId);
            _sessionManager.RemovePlayer(connectionId); // Clean up mapping
            _logger.LogInformation("Spectator {ConnectionId} left game {GameId}", connectionId, session.Id);
        }
        else
        {
            // Notify opponent
            await _hubContext.Clients.Group(session.Id).SendAsync("OpponentLeft");

            _sessionManager.RemovePlayer(connectionId);

            _logger.LogInformation("Player {ConnectionId} left game {GameId}", connectionId, session.Id);
        }
    }

    public async Task CreateAnalysisGameAsync(string connectionId, string userId)
    {
        // Create new session
        var session = _sessionManager.CreateGame();

        // Directly set same player for both sides (bypassing AddPlayer logic)
        // This is necessary because AddPlayer() won't add the same player twice
        session.AddPlayer(userId, connectionId); // Adds as White

        // Manually set Red player to same user (analysis mode special case)
        session.SetRedPlayer(userId, connectionId);

        // Enable analysis mode
        session.EnableAnalysisMode(userId);

        // Start game immediately
        if (!session.Engine.GameStarted)
        {
            session.Engine.StartNewGame();
        }

        await _hubContext.Groups.AddToGroupAsync(connectionId, session.Id);

        var state = session.GetState(connectionId);
        await _hubContext.Clients.Client(connectionId).SendAsync("GameStart", state);

        // Save game state
        BackgroundTaskHelper.FireAndForget(
            async () =>
            {
                var game = GameEngineMapper.ToGame(session);
                await _gameRepository.SaveGameAsync(game);
            },
            _logger,
            $"SaveGameState-{session.Id}");

        _logger.LogInformation(
            "Analysis game {GameId} created by {UserId}",
            session.Id,
            userId);
    }

    public async Task CreateAiGameAsync(string connectionId, string playerId, string? displayName)
    {
        _logger.LogDebug(
            "CreateAiGame called by connection {ConnectionId}, playerId={PlayerId}",
            connectionId,
            playerId);

        _logger.LogDebug(
            "Effective player ID: {EffectivePlayerId}, Display name: {DisplayName}",
            playerId,
            displayName);

        // Create a new game session
        var session = _sessionManager.CreateGame();
        await _hubContext.Groups.AddToGroupAsync(connectionId, session.Id);
        _logger.LogDebug(
            "Created game session {GameId}",
            session.Id);

        // Add human player as White
        session.AddPlayer(playerId, connectionId);
        _sessionManager.RegisterPlayerConnection(connectionId, session.Id); // Register connection for lookup
        if (!string.IsNullOrEmpty(displayName))
        {
            session.SetPlayerName(playerId, displayName);
        }

        _logger.LogDebug(
            "Added human player as White: {PlayerId}, registered connection {ConnectionId}",
            playerId,
            connectionId);

        // Add AI player as Red
        var aiPlayerId = _aiMoveService.GenerateAiPlayerId();
        session.AddPlayer(aiPlayerId, string.Empty); // Empty connection ID for AI
        session.SetPlayerName(aiPlayerId, "Computer");
        _logger.LogDebug(
            "Added AI player as Red: {AiPlayerId}",
            aiPlayerId);

        _logger.LogInformation(
            "Created AI game {GameId}. Human: {PlayerId}, AI: {AiPlayerId}",
            session.Id,
            playerId,
            aiPlayerId);

        // Game is now full - start immediately
        var humanState = session.GetState(connectionId);
        _logger.LogDebug(
            "Sending GameStart to human player. IsYourTurn={IsYourTurn}, CurrentPlayer={CurrentPlayer}, YourColor={YourColor}",
            humanState.IsYourTurn,
            humanState.CurrentPlayer,
            humanState.YourColor);
        await _hubContext.Clients.Client(connectionId).SendAsync("GameStart", humanState);

        // Save initial game state - fire and forget
        BackgroundTaskHelper.FireAndForget(
            async () =>
            {
                var game = GameEngineMapper.ToGame(session);
                await _gameRepository.SaveGameAsync(game);
            },
            _logger,
            $"SaveGameState-{session.Id}");

        // AI will roll after human rolls (triggered in RollDiceAsync)
        _logger.LogInformation(
            "AI game {GameId} started. Opening roll phase - waiting for first player to roll.",
            session.Id);
    }

    // ==================== Game State Broadcasting ====================

    public async Task BroadcastGameUpdateAsync(GameSession session)
    {
        // Send personalized state to each player connection
        foreach (var connectionId in session.WhiteConnections)
        {
            var whiteState = session.GetState(connectionId);
            await _hubContext.Clients.Client(connectionId).SendAsync("GameUpdate", whiteState);
        }

        foreach (var connectionId in session.RedConnections)
        {
            var redState = session.GetState(connectionId);
            await _hubContext.Clients.Client(connectionId).SendAsync("GameUpdate", redState);
        }

        // Send updates to all spectators
        var spectatorState = session.GetState(null); // null = spectator view
        foreach (var spectatorId in session.SpectatorConnections)
        {
            await _hubContext.Clients.Client(spectatorId).SendAsync("GameUpdate", spectatorState);
        }
    }

    public async Task BroadcastGameStartAsync(GameSession session)
    {
        // Game is ready to start - send personalized state to each player connection
        foreach (var connectionId in session.WhiteConnections)
        {
            var whiteState = session.GetState(connectionId);
            await _hubContext.Clients.Client(connectionId).SendAsync("GameStart", whiteState);
        }

        foreach (var connectionId in session.RedConnections)
        {
            var redState = session.GetState(connectionId);
            await _hubContext.Clients.Client(connectionId).SendAsync("GameStart", redState);
        }

        _logger.LogInformation("Game {GameId} started with both players", session.Id);
    }

    public async Task BroadcastGameOverAsync(GameSession session)
    {
        var finalState = session.GetState();
        await _hubContext.Clients.Group(session.Id).SendAsync("GameOver", finalState);

        _logger.LogInformation(
            "Game {GameId} completed. Winner: {Winner}",
            session.Id,
            session.Engine.Winner?.Name ?? "Unknown");
    }

    public async Task SendGameStateToConnectionAsync(GameSession session, string connectionId)
    {
        var state = session.GetState(connectionId);
        await _hubContext.Clients.Client(connectionId).SendAsync("GameUpdate", state);
    }

    public async Task BroadcastDoubleOfferAsync(
        GameSession session,
        string offeringConnectionId,
        int currentValue,
        int newValue)
    {
        // Determine opponent connections
        var opponentConnections = session.GetPlayerColor(offeringConnectionId) == CheckerColor.White
            ? session.RedConnections
            : session.WhiteConnections;

        foreach (var opponentConnectionId in opponentConnections)
        {
            await _hubContext.Clients.Client(opponentConnectionId).SendAsync("DoubleOffered", currentValue, newValue);
        }
    }

    public async Task BroadcastDoubleAcceptedAsync(GameSession session)
    {
        // Send updated state to all player connections
        foreach (var connectionId in session.WhiteConnections)
        {
            var whiteState = session.GetState(connectionId);
            await _hubContext.Clients.Client(connectionId).SendAsync("DoubleAccepted", whiteState);
        }

        foreach (var connectionId in session.RedConnections)
        {
            var redState = session.GetState(connectionId);
            await _hubContext.Clients.Client(connectionId).SendAsync("DoubleAccepted", redState);
        }
    }

    public async Task BroadcastMatchUpdateAsync(Match match, string gameId)
    {
        await _hubContext.Clients.Group(gameId).SendAsync("MatchUpdate", new
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
