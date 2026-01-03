using Backgammon.Core;
using Backgammon.Server.Extensions;
using Backgammon.Server.Hubs;
using Backgammon.Server.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Backgammon.Server.Services;

/// <summary>
/// Handles game creation and joining logic
/// </summary>
public class GameCreationService : IGameCreationService
{
    private readonly IGameSessionManager _sessionManager;
    private readonly IGameStateService _gameStateService;
    private readonly IGameRepository _gameRepository;
    private readonly IAiMoveService _aiMoveService;
    private readonly IGameActionOrchestrator _gameActionOrchestrator;
    private readonly IHubContext<GameHub> _hubContext;
    private readonly ILogger<GameCreationService> _logger;

    public GameCreationService(
        IGameSessionManager sessionManager,
        IGameStateService gameStateService,
        IGameRepository gameRepository,
        IAiMoveService aiMoveService,
        IGameActionOrchestrator gameActionOrchestrator,
        IHubContext<GameHub> hubContext,
        ILogger<GameCreationService> logger)
    {
        _sessionManager = sessionManager;
        _gameStateService = gameStateService;
        _gameRepository = gameRepository;
        _aiMoveService = aiMoveService;
        _gameActionOrchestrator = gameActionOrchestrator;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task JoinGameAsync(string connectionId, string playerId, string? displayName, string? gameId = null)
    {
        var session = await _sessionManager.JoinOrCreateAsync(
            playerId,
            connectionId,
            gameId);
        await _hubContext.Groups.AddToGroupAsync(connectionId, session.Id);
        _logger.LogInformation(
            "Player {PlayerId} (connection {ConnectionId}) joined game {GameId}",
            playerId,
            connectionId,
            session.Id);

        // Try to add as player; if full, add as spectator
        if (!session.AddPlayer(playerId, connectionId))
        {
            session.AddSpectator(connectionId);
            var spectatorState = session.GetState(null); // No color for spectators
            await _hubContext.Clients.Client(connectionId).SendAsync("SpectatorJoined", spectatorState);
            _logger.LogInformation(
            "Spectator {ConnectionId} joined game {GameId}",
            connectionId,
            session.Id);
            return;
        }

        // Set display name if authenticated
        if (!string.IsNullOrEmpty(displayName))
        {
            session.SetPlayerName(playerId, displayName);
        }

        if (session.IsFull)
        {
            // Game is ready to start - send personalized state to each player
            await _gameStateService.BroadcastGameStartAsync(session);

            // Save game state when game starts (progressive save)
            BackgroundTaskHelper.FireAndForget(
                async () =>
                {
                    var game = GameEngineMapper.ToGame(session);
                    await _gameRepository.SaveGameAsync(game);
                },
                _logger,
                $"SaveGameState-{session.Id}");

            // Check if AI should move first
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

    public async Task CreateAnalysisGameAsync(string connectionId, string userId)
    {
        // Create new session
        var session = await _sessionManager.JoinOrCreateAsync(
            userId,
            connectionId,
            null);

        // Directly set same player for both sides (bypassing AddPlayer logic)
        // This is necessary because AddPlayer() won't add the same player twice
        if (session.WhitePlayerId == null)
        {
            session.AddPlayer(userId, connectionId); // Adds as White
        }

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

        // If AI goes first (which would be unusual since White goes first, but handle it)
        var currentPlayerId = session.Engine.CurrentPlayer?.Color == CheckerColor.White
            ? session.WhitePlayerId
            : session.RedPlayerId;
        _logger.LogDebug(
            "Current player ID: {CurrentPlayerId}, Is AI: {IsAi}",
            currentPlayerId,
            _aiMoveService.IsAiPlayer(currentPlayerId));

        if (_aiMoveService.IsAiPlayer(currentPlayerId))
        {
            _logger.LogInformation("AI goes first - triggering AI turn for game {GameId}", session.Id);
            BackgroundTaskHelper.FireAndForget(
                async () =>
                {
                    await _gameActionOrchestrator.ExecuteAiTurnWithBroadcastAsync(session, aiPlayerId);
                },
                _logger,
                $"AiFirstTurn-{session.Id}");
        }

        _logger.LogInformation(
            "AI game {GameId} started",
            session.Id);
    }
}
