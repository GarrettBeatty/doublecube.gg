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
    private readonly IUserRepository _userRepository;
    private readonly IAiMoveService _aiMoveService;
    private readonly IGameActionOrchestrator _gameActionOrchestrator;
    private readonly IHubContext<GameHub> _hubContext;
    private readonly ILogger<GameService> _logger;

    public GameService(
        IGameSessionManager sessionManager,
        IGameRepository gameRepository,
        IUserRepository userRepository,
        IAiMoveService aiMoveService,
        IGameActionOrchestrator gameActionOrchestrator,
        IHubContext<GameHub> hubContext,
        ILogger<GameService> logger)
    {
        _sessionManager = sessionManager;
        _gameRepository = gameRepository;
        _userRepository = userRepository;
        _aiMoveService = aiMoveService;
        _gameActionOrchestrator = gameActionOrchestrator;
        _hubContext = hubContext;
        _logger = logger;
    }

    // ==================== Game Creation & Joining ====================

    public async Task JoinGameAsync(string connectionId, string playerId, string? displayName, string gameId)
    {
        var session = _sessionManager.GetGame(gameId);

        // If not in memory, try loading from database (for completed/abandoned games)
        if (session == null)
        {
            var game = await _gameRepository.GetGameByGameIdAsync(gameId);
            if (game == null)
            {
                throw new InvalidOperationException($"Game {gameId} not found");
            }

            // If game is completed or abandoned, load it as read-only view
            if (game.Status == "Completed" || game.Status == "Abandoned")
            {
                _logger.LogInformation(
                    "Loading {Status} game {GameId} for viewing by {PlayerId}",
                    game.Status,
                    gameId,
                    playerId);

                // Reconstruct session from database for viewing only
                session = GameEngineMapper.FromGame(game);

                // Add to group for notifications but don't add to session manager
                await _hubContext.Groups.AddToGroupAsync(connectionId, session.Id);

                // Send as spectator view (read-only) - use SpectatorJoined event
                var viewState = session.GetState(null);
                await _hubContext.Clients.Client(connectionId).SendAsync("SpectatorJoined", viewState);

                _logger.LogInformation(
                    "Sent {Status} game {GameId} state to viewer {PlayerId}",
                    game.Status,
                    gameId,
                    playerId);

                return;
            }

            // Game exists but is in progress - shouldn't be removed from memory
            throw new InvalidOperationException($"Game {gameId} not found in active games");
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

        // Fetch user data to set both display name and rating
        _logger.LogInformation("Fetching user for playerId: {PlayerId}", playerId);
        var user = await _userRepository.GetByUserIdAsync(playerId);
        if (user != null)
        {
            _logger.LogInformation("Found user {UserId} with rating {Rating} and display name {DisplayName}", user.UserId, user.Rating, user.DisplayName);

            // Set display name from database (prioritize authenticated displayName parameter if provided)
            var effectiveDisplayName = !string.IsNullOrEmpty(displayName) ? displayName : user.DisplayName;
            if (!string.IsNullOrEmpty(effectiveDisplayName))
            {
                session.SetPlayerName(playerId, effectiveDisplayName);
            }

            SetPlayerRating(session, playerId, user.Rating);
        }
        else
        {
            _logger.LogWarning("No user found for playerId: {PlayerId}", playerId);

            // Fall back to authenticated display name if user not found in database
            if (!string.IsNullOrEmpty(displayName))
            {
                session.SetPlayerName(playerId, displayName);
            }
        }

        // Send initial game state to the joining player (even if waiting for opponent)
        // This ensures they see their correct display name immediately
        var initialState = session.GetState(connectionId);
        await _hubContext.Clients.Client(connectionId).SendAsync("GameUpdate", initialState);
        _logger.LogInformation("Sent initial GameState to player {PlayerId} in game {GameId}", playerId, session.Id);

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

            // Save game state when game starts (progressive save) - skip for analysis mode
            if (session.GameMode.ShouldPersist)
            {
                BackgroundTaskHelper.FireAndForget(
                    async () =>
                    {
                        var game = GameEngineMapper.ToGame(session);
                        await _gameRepository.SaveGameAsync(game);
                    },
                    _logger,
                    $"SaveGameState-{session.Id}");
            }

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

        // Register connection so game actions can find this game
        _sessionManager.RegisterPlayerConnection(connectionId, session.Id);

        // Start game immediately and skip opening roll for analysis mode
        if (!session.Engine.GameStarted)
        {
            session.Engine.StartNewGame();

            // Skip opening roll phase in analysis mode - user will set dice manually
            // Force the game out of opening roll state
            session.Engine.GetType()
                .GetProperty("IsOpeningRoll")!
                .SetValue(session.Engine, false);
        }

        await _hubContext.Groups.AddToGroupAsync(connectionId, session.Id);

        var state = session.GetState(connectionId);
        await _hubContext.Clients.Client(connectionId).SendAsync("GameStart", state);

        // Analysis games are not persisted to database

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
        session.IsRated = false; // AI games are always unrated
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

        // Fetch and set player rating
        _logger.LogInformation("Fetching user for playerId: {PlayerId}", playerId);
        var user = await _userRepository.GetByUserIdAsync(playerId);
        if (user != null)
        {
            _logger.LogInformation("Found user {UserId} with rating {Rating}", user.UserId, user.Rating);
            SetPlayerRating(session, playerId, user.Rating);
        }
        else
        {
            _logger.LogWarning("No user found for playerId: {PlayerId}", playerId);
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
            "Created AI game {GameId}. Human: {PlayerId}, AI: {AiPlayerId} (unrated)",
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

    private void SetPlayerRating(GameSession session, string playerId, int rating)
    {
        if (session.WhitePlayerId == playerId)
        {
            session.WhiteRating = rating;
            session.WhiteRatingBefore = rating;
            _logger.LogInformation("Set White player rating to {Rating} for game {GameId}", rating, session.Id);
        }
        else if (session.RedPlayerId == playerId)
        {
            session.RedRating = rating;
            session.RedRatingBefore = rating;
            _logger.LogInformation("Set Red player rating to {Rating} for game {GameId}", rating, session.Id);
        }
        else
        {
            _logger.LogWarning("Could not set rating for playerId {PlayerId} - not found as White or Red in game {GameId}", playerId, session.Id);
        }
    }
}
