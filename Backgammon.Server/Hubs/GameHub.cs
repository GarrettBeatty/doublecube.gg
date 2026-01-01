using System.Security.Claims;
using Backgammon.Core;
using Backgammon.Server.Models;
using Backgammon.Server.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ServerGame = Backgammon.Server.Models.Game;
using ServerGameStatus = Backgammon.Server.Models.GameStatus;

namespace Backgammon.Server.Hubs;

/// <summary>
/// SignalR Hub for real-time Backgammon game communication.
/// Handles player connections, game actions, and state synchronization.
///
/// Client Methods (called FROM server TO clients):
/// - GameUpdate(GameState) - Sent when game state changes
/// - OpponentJoined(string) - Sent when second player joins
/// - OpponentLeft() - Sent when opponent disconnects
/// - Error(string) - Sent when an error occurs
/// - GameStart(GameState) - Sent when both players ready
///
/// Server Methods (called FROM clients TO server):
/// - JoinGame(string?) - Join or create game
/// - RollDice() - Request dice roll
/// - MakeMove(int, int) - Execute a move
/// - EndTurn() - Complete current turn
/// - LeaveGame() - Leave current game
/// </summary>
public class GameHub : Hub
{
    private readonly IGameSessionManager _sessionManager;
    private readonly IGameRepository _gameRepository;
    private readonly IUserRepository _userRepository;
    private readonly IFriendshipRepository _friendshipRepository;
    private readonly IAiMoveService _aiMoveService;
    private readonly IHubContext<GameHub> _hubContext;
    private readonly IMemoryCache _cache;
    private readonly HybridCache _hybridCache;
    private readonly IMatchService _matchService;
    private readonly IPlayerConnectionService _playerConnectionService;
    private readonly ILogger<GameHub> _logger;

    public GameHub(
        IGameSessionManager sessionManager,
        IGameRepository gameRepository,
        IUserRepository userRepository,
        IFriendshipRepository friendshipRepository,
        IAiMoveService aiMoveService,
        IHubContext<GameHub> hubContext,
        IMemoryCache cache,
        HybridCache hybridCache,
        IMatchService matchService,
        IPlayerConnectionService playerConnectionService,
        ILogger<GameHub> logger)
    {
        _sessionManager = sessionManager;
        _gameRepository = gameRepository;
        _userRepository = userRepository;
        _friendshipRepository = friendshipRepository;
        _aiMoveService = aiMoveService;
        _hubContext = hubContext;
        _cache = cache;
        _hybridCache = hybridCache;
        _matchService = matchService;
        _playerConnectionService = playerConnectionService;
        _logger = logger;
    }

    /// <summary>
    /// Get authenticated user ID from JWT token if available
    /// </summary>
    private string? GetAuthenticatedUserId()
    {
        return Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    /// <summary>
    /// Get display name from JWT token if authenticated
    /// </summary>
    private string? GetAuthenticatedDisplayName()
    {
        return Context.User?.FindFirst("displayName")?.Value;
    }

    /// <summary>
    /// Get the effective player ID - authenticated user ID or anonymous ID
    /// </summary>
    private string GetEffectivePlayerId(string anonymousPlayerId)
    {
        return GetAuthenticatedUserId() ?? anonymousPlayerId;
    }

    /// <summary>
    /// Join an existing game by ID or create/join via matchmaking.
    /// Supports both authenticated users (via JWT) and anonymous players.
    /// </summary>
    /// <param name="playerId">Persistent player ID (anonymous ID if not authenticated)</param>
    /// <param name="gameId">Optional game ID. If null, uses matchmaking.</param>
    public async Task JoinGame(string playerId, string? gameId = null)
    {
        try
        {
            var connectionId = Context.ConnectionId;

            // Use authenticated user ID if available, otherwise use provided anonymous ID
            var effectivePlayerId = GetEffectivePlayerId(playerId);
            var displayName = GetAuthenticatedDisplayName();

            var session = await _sessionManager.JoinOrCreateAsync(effectivePlayerId, connectionId, gameId);
            await Groups.AddToGroupAsync(connectionId, session.Id);
            _logger.LogInformation(
                "Player {PlayerId} (connection {ConnectionId}) joined game {GameId}",
                effectivePlayerId, connectionId, session.Id);

            // Try to add as player; if full, add as spectator
            if (!session.AddPlayer(effectivePlayerId, connectionId))
            {
                session.AddSpectator(connectionId);
                var spectatorState = session.GetState(null); // No color for spectators
                await Clients.Caller.SendAsync("SpectatorJoined", spectatorState);
                _logger.LogInformation("Spectator {ConnectionId} joined game {GameId}", connectionId, session.Id);
                return;
            }

            // Set display name if authenticated
            if (!string.IsNullOrEmpty(displayName))
            {
                session.SetPlayerName(effectivePlayerId, displayName);
            }

            if (session.IsFull)
            {
                // Game is ready to start - send personalized state to each player
                if (!string.IsNullOrEmpty(session.WhiteConnectionId))
                {
                    var whiteState = session.GetState(session.WhiteConnectionId);
                    await Clients.Client(session.WhiteConnectionId).SendAsync("GameStart", whiteState);
                }

                if (!string.IsNullOrEmpty(session.RedConnectionId))
                {
                    var redState = session.GetState(session.RedConnectionId);
                    await Clients.Client(session.RedConnectionId).SendAsync("GameStart", redState);
                }

                // Save game state when game starts (progressive save)
                await SaveGameStateAsync(session);

                _logger.LogInformation("Game {GameId} started with both players", session.Id);

                // Check if AI should move first
                var currentPlayerId = GetCurrentPlayerId(session);
                if (_aiMoveService.IsAiPlayer(currentPlayerId))
                {
                    _logger.LogInformation("AI goes first - triggering AI turn for game {GameId}", session.Id);
                    // Trigger AI turn in background with error handling
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await ExecuteAiTurnWithBroadcastAsync(session, currentPlayerId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "AI turn failed for game {GameId}", session.Id);
                        }
                    });
                }
            }
            else
            {
                // Waiting for opponent - save to database so it shows in dashboard
                await SaveGameStateAsync(session);

                var state = session.GetState(connectionId);
                await Clients.Caller.SendAsync("GameUpdate", state);
                await Clients.Caller.SendAsync("WaitingForOpponent", session.Id);
                _logger.LogInformation("Player {PlayerId} waiting in game {GameId}", effectivePlayerId, session.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining game");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    /// <summary>
    /// Create an analysis/practice game where one player controls both sides
    /// </summary>
    public async Task CreateAnalysisGame()
    {
        try
        {
            var connectionId = Context.ConnectionId;
            var userId = GetAuthenticatedUserId() ?? connectionId;

            // Create new session
            var session = await _sessionManager.JoinOrCreateAsync(userId, connectionId, null);

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

            await Groups.AddToGroupAsync(connectionId, session.Id);

            var state = session.GetState(connectionId);
            await Clients.Caller.SendAsync("GameStart", state);

            // Save game state
            await SaveGameStateAsync(session);

            _logger.LogInformation("Analysis game {GameId} created by {UserId}", session.Id, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating analysis game");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    /// <summary>
    /// Create a new game against an AI opponent.
    /// The human player is always White (moves first).
    /// </summary>
    /// <param name="playerId">The human player's persistent ID</param>
    public async Task CreateAiGame(string playerId)
    {
        try
        {
            var connectionId = Context.ConnectionId;
            _logger.LogDebug(
                "CreateAiGame called by connection {ConnectionId}, playerId={PlayerId}",
                connectionId, playerId);

            // Use authenticated user ID if available
            var effectivePlayerId = GetEffectivePlayerId(playerId);
            var displayName = GetAuthenticatedDisplayName();
            _logger.LogDebug(
                "Effective player ID: {EffectivePlayerId}, Display name: {DisplayName}",
                effectivePlayerId, displayName);

            // Create a new game session
            var session = _sessionManager.CreateGame();
            await Groups.AddToGroupAsync(connectionId, session.Id);
            _logger.LogDebug("Created game session {GameId}", session.Id);

            // Add human player as White
            session.AddPlayer(effectivePlayerId, connectionId);
            _sessionManager.RegisterPlayerConnection(connectionId, session.Id); // Register connection for lookup
            if (!string.IsNullOrEmpty(displayName))
            {
                session.SetPlayerName(effectivePlayerId, displayName);
            }

            _logger.LogDebug(
                "Added human player as White: {PlayerId}, registered connection {ConnectionId}",
                effectivePlayerId, connectionId);

            // Add AI player as Red
            var aiPlayerId = _aiMoveService.GenerateAiPlayerId();
            session.AddPlayer(aiPlayerId, string.Empty); // Empty connection ID for AI
            session.SetPlayerName(aiPlayerId, "Computer");
            _logger.LogDebug("Added AI player as Red: {AiPlayerId}", aiPlayerId);

            _logger.LogInformation(
                "Created AI game {GameId}. Human: {PlayerId}, AI: {AiPlayerId}",
                session.Id, effectivePlayerId, aiPlayerId);

            // Game is now full - start immediately
            var humanState = session.GetState(connectionId);
            _logger.LogDebug(
                "Sending GameStart to human player. IsYourTurn={IsYourTurn}, CurrentPlayer={CurrentPlayer}, YourColor={YourColor}",
                humanState.IsYourTurn, humanState.CurrentPlayer, humanState.YourColor);
            await Clients.Caller.SendAsync("GameStart", humanState);

            // Save initial game state
            await SaveGameStateAsync(session);

            // If AI goes first (which would be unusual since White goes first, but handle it)
            var currentPlayerId = GetCurrentPlayerId(session);
            _logger.LogDebug(
                "Current player ID: {CurrentPlayerId}, Is AI: {IsAi}",
                currentPlayerId, _aiMoveService.IsAiPlayer(currentPlayerId));

            if (_aiMoveService.IsAiPlayer(currentPlayerId))
            {
                _logger.LogInformation("AI goes first - triggering AI turn for game {GameId}", session.Id);
                // Trigger AI turn in background with error handling
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ExecuteAiTurnWithBroadcastAsync(session, aiPlayerId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "AI turn failed for game {GameId}", session.Id);
                    }
                });
            }

            _logger.LogInformation("AI game {GameId} started", session.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating AI game");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    /// <summary>
    /// Get list of points that have checkers that can be moved
    /// </summary>
    public async Task<List<int>> GetValidSources()
    {
        try
        {
            var session = _sessionManager.GetGameByPlayer(Context.ConnectionId);
            if (session == null)
            {
                return new List<int>();
            }

            if (!session.IsPlayerTurn(Context.ConnectionId))
            {
                return new List<int>();
            }

            if (session.Engine.RemainingMoves.Count == 0)
            {
                return new List<int>();
            }

            var validMoves = session.Engine.GetValidMoves();
            var sources = validMoves.Select(m => m.From).Distinct().ToList();
            return sources;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting valid sources");
            return new List<int>();
        }
    }

    /// <summary>
    /// Get list of valid destinations from a specific source point
    /// </summary>
    public async Task<List<MoveDto>> GetValidDestinations(int fromPoint)
    {
        try
        {
            _logger.LogInformation("GetValidDestinations called for point {FromPoint}", fromPoint);
            var session = _sessionManager.GetGameByPlayer(Context.ConnectionId);
            if (session == null)
            {
                _logger.LogWarning("No session found for player");
                return new List<MoveDto>();
            }

            if (!session.IsPlayerTurn(Context.ConnectionId))
            {
                _logger.LogWarning("Not player's turn");
                return new List<MoveDto>();
            }

            if (session.Engine.RemainingMoves.Count == 0)
            {
                _logger.LogWarning("No remaining moves");
                return new List<MoveDto>();
            }

            var allValidMoves = session.Engine.GetValidMoves();
            _logger.LogInformation("Total valid moves: {Count}", allValidMoves.Count);
            foreach (var m in allValidMoves)
            {
                _logger.LogInformation("  Valid move: {From} -> {To} (die: {Die})", m.From, m.To, m.DieValue);
            }

            var validMoves = allValidMoves
                .Where(m => m.From == fromPoint)
                .Select(m => new MoveDto
                {
                    From = m.From,
                    To = m.To,
                    DieValue = m.DieValue,
                    IsHit = WillHit(session.Engine, m)
                })
                .ToList();

            _logger.LogInformation("Filtered moves from point {FromPoint}: {Count}", fromPoint, validMoves.Count);
            return validMoves;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting valid destinations");
            return new List<MoveDto>();
        }
    }

    private bool WillHit(GameEngine engine, Move move)
    {
        // Bear-off moves (To = 0 or 25) cannot hit
        if (move.IsBearOff)
        {
            return false;
        }

        var targetPoint = engine.Board.GetPoint(move.To);
        if (targetPoint.Color == null || targetPoint.Count == 0)
        {
            return false;
        }

        return targetPoint.Color != engine.CurrentPlayer?.Color && targetPoint.Count == 1;
    }

    /// <summary>
    /// Roll dice to start turn (only valid when no remaining moves)
    /// </summary>
    public async Task RollDice()
    {
        try
        {
            _logger.LogDebug("RollDice called by connection {ConnectionId}", Context.ConnectionId);

            var session = _sessionManager.GetGameByPlayer(Context.ConnectionId);
            if (session == null)
            {
                _logger.LogWarning("RollDice failed: No session found for connection {ConnectionId}", Context.ConnectionId);
                await Clients.Caller.SendAsync("Error", "Not in a game");
                return;
            }

            _logger.LogDebug(
                "RollDice: Found session {GameId}, CurrentPlayer={CurrentPlayer}, WhiteConn={WhiteConn}, RedConn={RedConn}",
                session.Id, session.Engine.CurrentPlayer?.Color, session.WhiteConnectionId, session.RedConnectionId);

            if (!session.IsPlayerTurn(Context.ConnectionId))
            {
                _logger.LogWarning(
                    "RollDice failed: Not player's turn. Connection={ConnectionId}, Game={GameId}",
                    Context.ConnectionId, session.Id);
                await Clients.Caller.SendAsync("Error", "Not your turn");
                return;
            }

            if (session.Engine.RemainingMoves.Count > 0)
            {
                _logger.LogWarning(
                    "RollDice failed: Remaining moves exist. Count={Count}, Connection={ConnectionId}",
                    session.Engine.RemainingMoves.Count, Context.ConnectionId);
                await Clients.Caller.SendAsync("Error", "Must complete current moves first");
                return;
            }

            session.Engine.RollDice();
            session.UpdateActivity();

            _logger.LogInformation(
                "Player {ConnectionId} rolled dice in game {GameId}: [{Die1}, {Die2}]",
                Context.ConnectionId, session.Id, session.Engine.Dice.Die1, session.Engine.Dice.Die2);

            // Send personalized state to each player
            if (!string.IsNullOrEmpty(session.WhiteConnectionId))
            {
                var whiteState = session.GetState(session.WhiteConnectionId);
                _logger.LogDebug(
                    "Sending GameUpdate to White player (conn={Conn}). IsYourTurn={IsYourTurn}, Dice=[{Die1},{Die2}]",
                    session.WhiteConnectionId, whiteState.IsYourTurn, whiteState.Dice?.FirstOrDefault(), whiteState.Dice?.Skip(1).FirstOrDefault());
                await Clients.Client(session.WhiteConnectionId).SendAsync("GameUpdate", whiteState);
            }
            else
            {
                _logger.LogWarning("White player has no connection ID in game {GameId}", session.Id);
            }

            if (!string.IsNullOrEmpty(session.RedConnectionId))
            {
                var redState = session.GetState(session.RedConnectionId);
                _logger.LogDebug(
                    "Sending GameUpdate to Red player (conn={Conn}). IsYourTurn={IsYourTurn}, Dice=[{Die1},{Die2}]",
                    session.RedConnectionId, redState.IsYourTurn, redState.Dice?.FirstOrDefault(), redState.Dice?.Skip(1).FirstOrDefault());
                await Clients.Client(session.RedConnectionId).SendAsync("GameUpdate", redState);
            }
            else
            {
                _logger.LogDebug("Red player has no connection ID (AI player) in game {GameId}", session.Id);
            }

            // Send updates to all spectators
            var spectatorState = session.GetState(null); // null = spectator view
            foreach (var spectatorId in session.SpectatorConnections)
            {
                await Clients.Client(spectatorId).SendAsync("GameUpdate", spectatorState);
            }

            // Save game state after dice roll (progressive save)
            await SaveGameStateAsync(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rolling dice");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    /// <summary>
    /// Execute a move from one point to another
    /// </summary>
    public async Task MakeMove(int from, int to)
    {
        try
        {
            _logger.LogInformation("MakeMove called: from {From} to {To}", from, to);
            var session = _sessionManager.GetGameByPlayer(Context.ConnectionId);
            if (session == null)
            {
                _logger.LogWarning("No session found for player");
                await Clients.Caller.SendAsync("Error", "Not in a game");
                return;
            }

            if (!session.IsPlayerTurn(Context.ConnectionId))
            {
                _logger.LogWarning("Not player's turn");
                await Clients.Caller.SendAsync("Error", "Not your turn");
                return;
            }

            _logger.LogInformation(
                "Current player: {Player}, Remaining moves: {Moves}",
                session.Engine.CurrentPlayer.Name, string.Join(",", session.Engine.RemainingMoves));

            // Find the correct die value from valid moves
            var validMoves = session.Engine.GetValidMoves();
            var matchingMove = validMoves.FirstOrDefault(m => m.From == from && m.To == to);

            if (matchingMove == null)
            {
                _logger.LogWarning("No valid move found from {From} to {To}", from, to);
                await Clients.Caller.SendAsync("Error", "Invalid move - no matching valid move");
                return;
            }

            _logger.LogInformation("Found valid move with die value: {DieValue}", matchingMove.DieValue);

            var isValid = session.Engine.IsValidMove(matchingMove);
            _logger.LogInformation("Move validity check: {IsValid}", isValid);

            if (!session.Engine.ExecuteMove(matchingMove))
            {
                _logger.LogWarning("ExecuteMove returned false - invalid move");
                await Clients.Caller.SendAsync("Error", "Invalid move");
                return;
            }

            _logger.LogInformation("Move executed successfully");

            session.UpdateActivity();

            // Send personalized state to each player
            if (!string.IsNullOrEmpty(session.WhiteConnectionId))
            {
                var whiteState = session.GetState(session.WhiteConnectionId);
                await Clients.Client(session.WhiteConnectionId).SendAsync("GameUpdate", whiteState);
            }

            if (!string.IsNullOrEmpty(session.RedConnectionId))
            {
                var redState = session.GetState(session.RedConnectionId);
                await Clients.Client(session.RedConnectionId).SendAsync("GameUpdate", redState);
            }

            // Send updates to all spectators
            var spectatorState = session.GetState(null); // null = spectator view
            foreach (var spectatorId in session.SpectatorConnections)
            {
                await Clients.Client(spectatorId).SendAsync("GameUpdate", spectatorState);
            }

            // Save game state after move (progressive save)
            await SaveGameStateAsync(session);

            // Check if game is over
            if (session.Engine.Winner != null)
            {
                var stakes = session.Engine.GetGameResult();
                // Use group send for game over since it's the same for both players
                var finalState = session.GetState();
                await Clients.Group(session.Id).SendAsync("GameOver", finalState);
                _logger.LogInformation(
                    "Game {GameId} completed. Winner: {Winner} (Stakes: {Stakes})",
                    session.Id, session.Engine.Winner.Name, stakes);

                // Handle match game completion if this is a match game
                await HandleMatchGameCompletion(session);

                // Update game status to Completed and update user stats
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Save final game state with Status="Completed" (already done by SaveGameStateAsync above)
                        // Update status explicitly to ensure CompletedAt timestamp is set
                        await _gameRepository.UpdateGameStatusAsync(session.Id, "Completed");

                        // Update user statistics if players are registered (skip for analysis games)
                        if (session.GameMode.ShouldTrackStats)
                        {
                            var game = GameEngineMapper.ToGame(session);
                            await UpdateUserStatsAfterGame(game);

                            // Invalidate player caches
                            await InvalidatePlayerCachesAsync(session.WhitePlayerId, session.RedPlayerId);
                        }
                        else
                        {
                            _logger.LogInformation("Skipping stats tracking for non-competitive game {GameId}", session.Id);
                        }

                        _logger.LogInformation("Updated game {GameId} to Completed status and user stats", session.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to update completion status for game {GameId}", session.Id);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making move");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    /// <summary>
    /// End current turn and switch to opponent
    /// </summary>
    public async Task EndTurn()
    {
        try
        {
            var session = _sessionManager.GetGameByPlayer(Context.ConnectionId);
            if (session == null)
            {
                await Clients.Caller.SendAsync("Error", "Not in a game");
                return;
            }

            if (!session.IsPlayerTurn(Context.ConnectionId))
            {
                await Clients.Caller.SendAsync("Error", "Not your turn");
                return;
            }

            // Validate that turn can be ended
            // Only allow ending turn if:
            // 1. No remaining moves, OR
            // 2. No valid moves are possible
            if (session.Engine.RemainingMoves.Count > 0)
            {
                var validMoves = session.Engine.GetValidMoves();
                if (validMoves.Count > 0)
                {
                    await Clients.Caller.SendAsync("Error", "You still have valid moves available");
                    return;
                }
            }

            session.Engine.EndTurn();
            session.UpdateActivity();

            // Send personalized state to each player
            if (!string.IsNullOrEmpty(session.WhiteConnectionId))
            {
                var whiteState = session.GetState(session.WhiteConnectionId);
                await Clients.Client(session.WhiteConnectionId).SendAsync("GameUpdate", whiteState);
            }

            if (!string.IsNullOrEmpty(session.RedConnectionId))
            {
                var redState = session.GetState(session.RedConnectionId);
                await Clients.Client(session.RedConnectionId).SendAsync("GameUpdate", redState);
            }

            // Send updates to all spectators
            var spectatorStateEndTurn = session.GetState(null); // null = spectator view
            foreach (var spectatorId in session.SpectatorConnections)
            {
                await Clients.Client(spectatorId).SendAsync("GameUpdate", spectatorStateEndTurn);
            }

            // Save game state after turn end (progressive save)
            await SaveGameStateAsync(session);

            _logger.LogInformation(
                "Turn ended in game {GameId}. Current player: {Player}",
                session.Id, session.Engine.CurrentPlayer?.Color.ToString() ?? "Unknown");

            // Check if next player is AI and trigger AI turn
            var nextPlayerId = GetCurrentPlayerId(session);
            if (_aiMoveService.IsAiPlayer(nextPlayerId))
            {
                _logger.LogInformation(
                    "Triggering AI turn for player {AiPlayerId} in game {GameId}",
                    nextPlayerId, session.Id);
                // Trigger AI turn in background with error handling
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ExecuteAiTurnWithBroadcastAsync(session, nextPlayerId!);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "AI turn failed for game {GameId}", session.Id);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending turn");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    /// <summary>
    /// Undo the last move made during the current turn
    /// </summary>
    public async Task UndoLastMove()
    {
        try
        {
            var session = _sessionManager.GetGameByPlayer(Context.ConnectionId);
            if (session == null)
            {
                await Clients.Caller.SendAsync("Error", "Not in a game");
                return;
            }

            if (!session.IsPlayerTurn(Context.ConnectionId))
            {
                await Clients.Caller.SendAsync("Error", "Not your turn");
                return;
            }

            if (session.Engine.MoveHistory.Count == 0)
            {
                await Clients.Caller.SendAsync("Error", "No moves to undo");
                return;
            }

            // Perform undo
            if (!session.Engine.UndoLastMove())
            {
                await Clients.Caller.SendAsync("Error", "Failed to undo move");
                return;
            }

            session.UpdateActivity();

            // Send updated state to both players
            if (!string.IsNullOrEmpty(session.WhiteConnectionId))
            {
                var whiteState = session.GetState(session.WhiteConnectionId);
                await Clients.Client(session.WhiteConnectionId).SendAsync("GameUpdate", whiteState);
            }

            if (!string.IsNullOrEmpty(session.RedConnectionId))
            {
                var redState = session.GetState(session.RedConnectionId);
                await Clients.Client(session.RedConnectionId).SendAsync("GameUpdate", redState);
            }

            // Send updates to spectators
            var spectatorState = session.GetState(null);
            foreach (var spectatorId in session.SpectatorConnections)
            {
                await Clients.Client(spectatorId).SendAsync("GameUpdate", spectatorState);
            }

            // Save updated game state
            await SaveGameStateAsync(session);

            _logger.LogInformation(
                "Player {ConnectionId} undid last move in game {GameId}",
                Context.ConnectionId, session.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error undoing move");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    /// <summary>
    /// Offer to double the stakes to the opponent
    /// </summary>
    public async Task OfferDouble()
    {
        try
        {
            var session = _sessionManager.GetGameByPlayer(Context.ConnectionId);
            if (session == null)
            {
                await Clients.Caller.SendAsync("Error", "Not in a game");
                return;
            }

            if (!session.IsPlayerTurn(Context.ConnectionId))
            {
                await Clients.Caller.SendAsync("Error", "Not your turn");
                return;
            }

            // Can only double before rolling dice
            if (session.Engine.RemainingMoves.Count > 0 || session.Engine.Dice.Die1 != 0)
            {
                await Clients.Caller.SendAsync("Error", "Can only double before rolling dice");
                return;
            }

            // Check if player can offer double
            if (!session.Engine.OfferDouble())
            {
                await Clients.Caller.SendAsync("Error", "Cannot offer double - opponent owns the cube");
                return;
            }

            var currentValue = session.Engine.DoublingCube.Value;
            var newValue = currentValue * 2;

            // Notify opponent of the double offer
            var opponentConnectionId = session.GetPlayerColor(Context.ConnectionId) == CheckerColor.White
                ? session.RedConnectionId
                : session.WhiteConnectionId;

            _logger.LogInformation(
                "OfferDouble: opponentConnectionId={OpponentConnectionId}, WhiteConn={WhiteConn}, RedConn={RedConn}",
                opponentConnectionId ?? "null", session.WhiteConnectionId ?? "null", session.RedConnectionId ?? "null");

            if (opponentConnectionId != null && !string.IsNullOrEmpty(opponentConnectionId))
            {
                await Clients.Client(opponentConnectionId).SendAsync("DoubleOffered", currentValue, newValue);
                _logger.LogInformation(
                    "Player {ConnectionId} offered double in game {GameId}. Stakes: {Current}x → {New}x",
                    Context.ConnectionId, session.Id, currentValue, newValue);
            }
            else
            {
                // Opponent might be an AI (empty connection ID)
                var opponentPlayerId = session.GetPlayerColor(Context.ConnectionId) == CheckerColor.White
                    ? session.RedPlayerId
                    : session.WhitePlayerId;

                _logger.LogInformation(
                    "OfferDouble: Checking AI opponent. OpponentPlayerId={OpponentPlayerId}, IsAiPlayer={IsAi}",
                    opponentPlayerId ?? "null", opponentPlayerId != null && _aiMoveService.IsAiPlayer(opponentPlayerId));

                if (opponentPlayerId != null && _aiMoveService.IsAiPlayer(opponentPlayerId))
                {
                    _logger.LogInformation(
                        "AI opponent {AiPlayerId} evaluating double offer in game {GameId}. Stakes: {Current}x → {New}x",
                        opponentPlayerId, session.Id, currentValue, newValue);

                    // AI decision logic: Accept if new value <= 4, otherwise decline
                    // This is a simple conservative strategy
                    bool aiAccepts = newValue <= 4;

                    // Small delay to make it feel more natural
                    await Task.Delay(1000);

                    if (aiAccepts)
                    {
                        _logger.LogInformation("AI {AiPlayerId} accepted the double", opponentPlayerId);
                        session.Engine.AcceptDouble();
                        session.UpdateActivity();

                        // Send updated state to the human player (use DoubleAccepted for consistency)
                        if (!string.IsNullOrEmpty(Context.ConnectionId))
                        {
                            var state = session.GetState(Context.ConnectionId);
                            await Clients.Caller.SendAsync("DoubleAccepted", state);
                        }

                        // Save game state
                        await SaveGameStateAsync(session);

                        _logger.LogInformation(
                            "AI accepted double in game {GameId}. New stakes: {Stakes}x",
                            session.Id, session.Engine.DoublingCube.Value);
                    }
                    else
                    {
                        _logger.LogInformation("AI {AiPlayerId} declined the double", opponentPlayerId);

                        // Determine human player (opponent of AI)
                        var humanColor = session.GetPlayerColor(Context.ConnectionId);
                        var humanPlayer = humanColor == CheckerColor.White
                            ? session.Engine.WhitePlayer
                            : session.Engine.RedPlayer;

                        // AI declines - human wins at current stakes
                        session.Engine.ForfeitGame(humanPlayer);
                        var stakes = session.Engine.GetGameResult();

                        _logger.LogInformation(
                            "Game {GameId} ended. AI declined double. Winner: {Winner} (Stakes: {Stakes})",
                            session.Id, humanPlayer.Name, stakes);

                        // Send game over event to human player
                        if (!string.IsNullOrEmpty(Context.ConnectionId))
                        {
                            var finalState = session.GetState(Context.ConnectionId);
                            await Clients.Caller.SendAsync("GameOver", finalState);
                        }

                        await Clients.Caller.SendAsync("Info", "Computer declined the double. You win!");

                        // Update database and stats (async)
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await _gameRepository.UpdateGameStatusAsync(session.Id, "Completed");

                                // Skip stats update for non-competitive games
                                if (session.GameMode.ShouldTrackStats)
                                {
                                    var game = GameEngineMapper.ToGame(session);
                                    await UpdateUserStatsAfterGame(game);

                                    // Invalidate player caches
                                    await InvalidatePlayerCachesAsync(session.WhitePlayerId, session.RedPlayerId);
                                }

                                _logger.LogInformation("Updated game {GameId} to Completed status and user stats", session.Id);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to update completion status for game {GameId}", session.Id);
                            }
                        });

                        // Clean up game session
                        _sessionManager.RemoveGame(session.Id);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error offering double");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    /// <summary>
    /// Accept a double offer from the opponent
    /// </summary>
    public async Task AcceptDouble()
    {
        try
        {
            var session = _sessionManager.GetGameByPlayer(Context.ConnectionId);
            if (session == null)
            {
                await Clients.Caller.SendAsync("Error", "Not in a game");
                return;
            }

            // Accept the double
            session.Engine.AcceptDouble();
            session.UpdateActivity();

            // Send updated state to both players
            if (!string.IsNullOrEmpty(session.WhiteConnectionId))
            {
                var whiteState = session.GetState(session.WhiteConnectionId);
                await Clients.Client(session.WhiteConnectionId).SendAsync("DoubleAccepted", whiteState);
            }

            if (!string.IsNullOrEmpty(session.RedConnectionId))
            {
                var redState = session.GetState(session.RedConnectionId);
                await Clients.Client(session.RedConnectionId).SendAsync("DoubleAccepted", redState);
            }

            // Save game state
            await SaveGameStateAsync(session);

            _logger.LogInformation(
                "Player {ConnectionId} accepted double in game {GameId}. New stakes: {Stakes}x",
                Context.ConnectionId, session.Id, session.Engine.DoublingCube.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting double");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    /// <summary>
    /// Decline a double offer (opponent wins at current stakes)
    /// </summary>
    public async Task DeclineDouble()
    {
        try
        {
            var session = _sessionManager.GetGameByPlayer(Context.ConnectionId);
            if (session == null)
            {
                await Clients.Caller.SendAsync("Error", "Not in a game");
                return;
            }

            // Determine declining player and opponent
            var decliningColor = session.GetPlayerColor(Context.ConnectionId);
            if (decliningColor == null)
            {
                await Clients.Caller.SendAsync("Error", "You are not a player in this game");
                return;
            }

            var decliningPlayer = decliningColor == CheckerColor.White
                ? session.Engine.WhitePlayer
                : session.Engine.RedPlayer;
            var opponentPlayer = decliningColor == CheckerColor.White
                ? session.Engine.RedPlayer
                : session.Engine.WhitePlayer;

            // Check if game is already over or hasn't started
            if (session.Engine.GameOver)
            {
                _logger.LogWarning("Game {GameId} is already over, cannot decline double", session.Id);
                await Clients.Caller.SendAsync("Error", "Game is already finished");
                return;
            }

            if (!session.Engine.GameStarted)
            {
                _logger.LogWarning("Game {GameId} hasn't started yet, cannot decline double", session.Id);
                await Clients.Caller.SendAsync("Error", "Game hasn't started yet");
                return;
            }

            // Forfeit game - opponent wins at current stakes
            session.Engine.ForfeitGame(opponentPlayer);

            // Get stakes from current cube value (before the proposed double)
            var stakes = session.Engine.GetGameResult();

            // Broadcast game over
            var finalState = session.GetState();
            await Clients.Group(session.Id).SendAsync("GameOver", finalState);

            _logger.LogInformation(
                "Game {GameId} ended - player declined double. Winner: {Winner} (Stakes: {Stakes})",
                session.Id, opponentPlayer.Name, stakes);

            // Update database and stats (async)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _gameRepository.UpdateGameStatusAsync(session.Id, "Completed");

                    // Skip stats update for non-competitive games
                    if (session.GameMode.ShouldTrackStats)
                    {
                        var game = GameEngineMapper.ToGame(session);
                        await UpdateUserStatsAfterGame(game);

                        // Invalidate player caches
                        await InvalidatePlayerCachesAsync(session.WhitePlayerId, session.RedPlayerId);
                    }
                    else
                    {
                        _logger.LogInformation("Skipping stats tracking for non-competitive game {GameId}", session.Id);
                    }

                    _logger.LogInformation("Updated game {GameId} to Completed status and user stats", session.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update completion status for game {GameId}", session.Id);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error declining double");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    /// <summary>
    /// Abandon the current game. The opponent wins automatically.
    /// </summary>
    public async Task AbandonGame()
    {
        try
        {
            var session = _sessionManager.GetGameByPlayer(Context.ConnectionId);
            if (session == null)
            {
                await Clients.Caller.SendAsync("Error", "Not in a game");
                return;
            }

            // Determine abandoning player and opponent
            var abandoningColor = session.GetPlayerColor(Context.ConnectionId);
            if (abandoningColor == null)
            {
                await Clients.Caller.SendAsync("Error", "You are not a player in this game");
                return;
            }

            // Check if game is still waiting for opponent
            var currentState = session.GetState();
            var isWaitingForPlayer = currentState.Status == ServerGameStatus.WaitingForPlayer;

            if (isWaitingForPlayer)
            {
                // No opponent yet - just cancel the game
                var gameId = session.Id;

                // Remove player from group
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId);

                // Remove game completely from session manager
                _sessionManager.RemoveGame(gameId);

                _logger.LogInformation("Game {GameId} abandoned by player while waiting for opponent", gameId);

                // Update database to mark as abandoned but don't update stats
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _gameRepository.UpdateGameStatusAsync(gameId, "Abandoned");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to update abandoned game {GameId}", gameId);
                    }
                });

                return;
            }

            var abandoningPlayer = abandoningColor == CheckerColor.White
                ? session.Engine.WhitePlayer
                : session.Engine.RedPlayer;
            var opponentPlayer = abandoningColor == CheckerColor.White
                ? session.Engine.RedPlayer
                : session.Engine.WhitePlayer;

            // Check if game is already over or hasn't started
            if (session.Engine.GameOver)
            {
                _logger.LogWarning("Game {GameId} is already over, cannot abandon", session.Id);
                await Clients.Caller.SendAsync("Error", "Game is already finished");
                return;
            }

            if (!session.Engine.GameStarted)
            {
                _logger.LogWarning("Game {GameId} hasn't started yet, cannot forfeit", session.Id);
                await Clients.Caller.SendAsync("Error", "Game hasn't started yet");
                return;
            }

            // Forfeit game - set opponent as winner
            session.Engine.ForfeitGame(opponentPlayer);

            // Get stakes from doubling cube
            var stakes = session.Engine.GetGameResult();

            // Broadcast game over
            var finalState = session.GetState();
            await Clients.Group(session.Id).SendAsync("GameOver", finalState);

            _logger.LogInformation(
                "Game {GameId} abandoned by {Player}. Winner: {Winner} (Stakes: {Stakes})",
                session.Id, abandoningPlayer.Name, opponentPlayer.Name, stakes);

            // Update database and stats (async)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _gameRepository.UpdateGameStatusAsync(session.Id, "Abandoned");

                    // Skip stats update for non-competitive games
                    if (session.GameMode.ShouldTrackStats)
                    {
                        var game = GameEngineMapper.ToGame(session);
                        await UpdateUserStatsAfterGame(game);
                    }
                    else
                    {
                        _logger.LogInformation("Skipping stats tracking for analysis game {GameId}", session.Id);
                    }

                    _logger.LogInformation("Updated game {GameId} to Abandoned status and user stats", session.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update abandoned game {GameId}", session.Id);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error abandoning game");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    /// <summary>
    /// Get current game state
    /// </summary>
    public async Task GetGameState()
    {
        try
        {
            var session = _sessionManager.GetGameByPlayer(Context.ConnectionId);
            if (session == null)
            {
                await Clients.Caller.SendAsync("Error", "Not in a game");
                return;
            }

            var state = session.GetState(Context.ConnectionId);
            await Clients.Caller.SendAsync("GameUpdate", state);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting game state");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    /// <summary>
    /// Send a chat message to all players in the game
    /// </summary>
    public async Task SendChatMessage(string message)
    {
        try
        {
            // Validate message is not empty
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            // Get the game session
            var session = _sessionManager.GetGameByPlayer(Context.ConnectionId);
            if (session == null)
            {
                await Clients.Caller.SendAsync("Error", "Not in a game");
                return;
            }

            // Determine sender's color and name
            var senderColor = session.GetPlayerColor(Context.ConnectionId);
            if (senderColor == null)
            {
                return;
            }

            var senderName = senderColor == CheckerColor.White
                ? (session.WhitePlayerName ?? "White")
                : (session.RedPlayerName ?? "Red");

            // Broadcast to all players in the game
            await Clients.Group(session.Id).SendAsync(
                "ReceiveChatMessage",
                senderName, message, Context.ConnectionId);

            _logger.LogInformation(
                "Chat message from {Sender} in game {GameId}",
                senderName, session.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending chat message");
            await Clients.Caller.SendAsync("Error", "Failed to send message");
        }
    }

    /// <summary>
    /// Leave current game
    /// </summary>
    public async Task LeaveGame()
    {
        await HandleDisconnection(Context.ConnectionId);
    }

    /// <summary>
    /// Handle player disconnection
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Remove from player connections tracking
        var playerId = GetEffectivePlayerId(Context.ConnectionId);
        _playerConnectionService.RemoveConnection(playerId);

        await HandleDisconnection(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    private async Task HandleDisconnection(string connectionId)
    {
        try
        {
            var session = _sessionManager.GetGameByPlayer(connectionId);
            if (session != null)
            {
                await Groups.RemoveFromGroupAsync(connectionId, session.Id);

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
                    await Clients.Group(session.Id).SendAsync("OpponentLeft");

                    _sessionManager.RemovePlayer(connectionId);

                    _logger.LogInformation("Player {ConnectionId} left game {GameId}", connectionId, session.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling disconnection");
        }
    }

    /// <summary>
    /// Save current game state to database (progressive save).
    /// Uses GameEngineMapper to serialize the complete game state.
    /// Fire-and-forget pattern - does not throw exceptions to avoid blocking game flow.
    /// </summary>
    private async Task SaveGameStateAsync(GameSession session)
    {
        try
        {
            var game = GameEngineMapper.ToGame(session);
            await _gameRepository.SaveGameAsync(game);
            _logger.LogDebug("Saved game state for {GameId}, Status={Status}", session.Id, game.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save game state for {GameId}", session.Id);
            // Fire-and-forget - don't throw to avoid disrupting gameplay
        }
    }

    /// <summary>
    /// Check if a player ID looks like a registered user ID (GUID format)
    /// Anonymous IDs have format "player_timestamp_random"
    /// </summary>
    private bool IsRegisteredUserId(string? playerId)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            return false;
        }

        // Registered user IDs are GUIDs
        return Guid.TryParse(playerId, out _);
    }

    /// <summary>
    /// Get the player ID of the current player in the session
    /// </summary>
    private string? GetCurrentPlayerId(GameSession session)
    {
        if (session.Engine.CurrentPlayer == null)
        {
            return null;
        }

        return session.Engine.CurrentPlayer.Color == CheckerColor.White
            ? session.WhitePlayerId
            : session.RedPlayerId;
    }

    /// <summary>
    /// Get the AI player ID from the session (if any)
    /// </summary>
    private string? GetAiPlayerId(GameSession session)
    {
        if (_aiMoveService.IsAiPlayer(session.WhitePlayerId))
        {
            return session.WhitePlayerId;
        }

        if (_aiMoveService.IsAiPlayer(session.RedPlayerId))
        {
            return session.RedPlayerId;
        }

        return null;
    }

    /// <summary>
    /// Execute AI turn and broadcast updates to clients.
    /// Runs as a background task to avoid blocking the hub.
    /// Uses _hubContext instead of Clients because Hub instance may be disposed.
    /// </summary>
    private async Task ExecuteAiTurnWithBroadcastAsync(GameSession session, string aiPlayerId)
    {
        try
        {
            // Broadcast callback - sends state to players and spectators
            // Use _hubContext.Clients instead of this.Clients because Hub may be disposed
            async Task BroadcastUpdate()
            {
                // Send personalized state to each connected player
                if (!string.IsNullOrEmpty(session.WhiteConnectionId))
                {
                    var whiteState = session.GetState(session.WhiteConnectionId);
                    await _hubContext.Clients.Client(session.WhiteConnectionId).SendAsync("GameUpdate", whiteState);
                }

                if (!string.IsNullOrEmpty(session.RedConnectionId))
                {
                    var redState = session.GetState(session.RedConnectionId);
                    await _hubContext.Clients.Client(session.RedConnectionId).SendAsync("GameUpdate", redState);
                }

                // Send updates to all spectators
                var spectatorState = session.GetState(null); // null = spectator view
                foreach (var spectatorId in session.SpectatorConnections)
                {
                    await _hubContext.Clients.Client(spectatorId).SendAsync("GameUpdate", spectatorState);
                }
            }

            // Execute AI turn with delays and broadcasts
            await _aiMoveService.ExecuteAiTurnAsync(session, aiPlayerId, BroadcastUpdate);

            // Save game state after AI turn
            await SaveGameStateAsync(session);

            // Check if game is over
            if (session.Engine.Winner != null)
            {
                var stakes = session.Engine.GetGameResult();
                var finalState = session.GetState();
                await _hubContext.Clients.Group(session.Id).SendAsync("GameOver", finalState);
                _logger.LogInformation(
                    "AI game {GameId} completed. Winner: {Winner} (Stakes: {Stakes})",
                    session.Id, session.Engine.Winner.Name, stakes);

                // Update database status
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _gameRepository.UpdateGameStatusAsync(session.Id, "Completed");
                        var game = GameEngineMapper.ToGame(session);
                        await UpdateUserStatsAfterGame(game);

                        // Invalidate player caches
                        await InvalidatePlayerCachesAsync(session.WhitePlayerId, session.RedPlayerId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to update completion status for AI game {GameId}", session.Id);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing AI turn in game {GameId}", session.Id);
        }
    }

    /// <summary>
    /// Invalidate caches for players after game completion
    /// </summary>
    private async Task InvalidatePlayerCachesAsync(string? whitePlayerId, string? redPlayerId)
    {
        try
        {
            // Invalidate game history and stats caches for both players
            if (!string.IsNullOrEmpty(whitePlayerId))
            {
                await _hybridCache.RemoveByTagAsync($"player:{whitePlayerId}");
                _logger.LogDebug("Invalidated cache for player {PlayerId}", whitePlayerId);
            }

            if (!string.IsNullOrEmpty(redPlayerId))
            {
                await _hybridCache.RemoveByTagAsync($"player:{redPlayerId}");
                _logger.LogDebug("Invalidated cache for player {PlayerId}", redPlayerId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate player caches");
        }
    }

    /// <summary>
    /// Update user statistics after a game is completed
    /// </summary>
    private async Task UpdateUserStatsAfterGame(ServerGame game)
    {
        try
        {
            // Only update stats if game actually had two players
            if (game.Status == "WaitingForPlayer" ||
                string.IsNullOrEmpty(game.RedPlayerId) ||
                string.IsNullOrEmpty(game.WhitePlayerId))
            {
                _logger.LogInformation("Skipping stats update for game {GameId} - no opponent joined", game.GameId);
                return;
            }

            // Skip stats for AI games
            if (game.IsAiOpponent)
            {
                _logger.LogInformation("Skipping stats update for game {GameId} - AI opponent", game.GameId);
                return;
            }

            // Update white player stats if registered
            if (!string.IsNullOrEmpty(game.WhiteUserId))
            {
                var user = await _userRepository.GetByUserIdAsync(game.WhiteUserId);
                if (user != null)
                {
                    var isWinner = game.Winner == "White";
                    UpdateStats(user.Stats, isWinner, game.Stakes);
                    await _userRepository.UpdateStatsAsync(user.UserId, user.Stats);
                }
            }

            // Update red player stats if registered
            if (!string.IsNullOrEmpty(game.RedUserId))
            {
                var user = await _userRepository.GetByUserIdAsync(game.RedUserId);
                if (user != null)
                {
                    var isWinner = game.Winner == "Red";
                    UpdateStats(user.Stats, isWinner, game.Stakes);
                    await _userRepository.UpdateStatsAsync(user.UserId, user.Stats);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user stats after game {GameId}", game.GameId);
        }
    }

    private void UpdateStats(UserStats stats, bool isWinner, int stakes)
    {
        stats.TotalGames++;

        if (isWinner)
        {
            stats.Wins++;
            stats.TotalStakes += stakes;
            stats.WinStreak++;

            if (stats.WinStreak > stats.BestWinStreak)
            {
                stats.BestWinStreak = stats.WinStreak;
            }

            // Track win types
            switch (stakes)
            {
                case 1:
                    stats.NormalWins++;
                    break;
                case 2:
                    stats.GammonWins++;
                    break;
                case 3:
                    stats.BackgammonWins++;
                    break;
            }
        }
        else
        {
            stats.Losses++;
            stats.WinStreak = 0; // Reset streak on loss
        }
    }

    /// <summary>
    /// Export the current game position in SGF format
    /// </summary>
    public async Task<string> ExportPosition()
    {
        var session = _sessionManager.GetGameByPlayer(Context.ConnectionId);

        if (session == null)
        {
            await Clients.Caller.SendAsync("Error", "You are not in a game");
            return string.Empty;
        }

        try
        {
            return SgfSerializer.ExportPosition(session.Engine);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting position");
            await Clients.Caller.SendAsync("Error", $"Failed to export position: {ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>
    /// Import a position from SGF format
    /// </summary>
    public async Task ImportPosition(string sgf)
    {
        var session = _sessionManager.GetGameByPlayer(Context.ConnectionId);

        if (session == null)
        {
            await Clients.Caller.SendAsync("Error", "You are not in a game");
            return;
        }

        // Check if import is allowed in this game mode
        var features = session.GameMode.GetFeatures();
        if (!features.AllowImportExport)
        {
            await Clients.Caller.SendAsync("Error", "Cannot import positions in this game mode");
            return;
        }

        try
        {
            SgfSerializer.ImportPosition(session.Engine, sgf);

            // Send updated game state to all players
            if (!string.IsNullOrEmpty(session.WhiteConnectionId))
            {
                var whiteState = session.GetState(session.WhiteConnectionId);
                await Clients.Client(session.WhiteConnectionId).SendAsync("GameUpdate", whiteState);
            }

            if (!string.IsNullOrEmpty(session.RedConnectionId))
            {
                var redState = session.GetState(session.RedConnectionId);
                await Clients.Client(session.RedConnectionId).SendAsync("GameUpdate", redState);
            }

            _logger.LogInformation("Position imported successfully for game {GameId}", session.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing position");
            await Clients.Caller.SendAsync("Error", $"Failed to import position: {ex.Message}");
        }
    }

    /// <summary>
    /// Get player profile data including stats, recent games, and friends
    /// </summary>
    public async Task<PlayerProfileDto?> GetPlayerProfile(string username)
    {
        try
        {
            // Validate username
            if (string.IsNullOrWhiteSpace(username))
            {
                await Clients.Caller.SendAsync("Error", "Username is required");
                return null;
            }

            // Get the viewing user (might be anonymous)
            var viewingUserId = GetAuthenticatedUserId();

            // Create cache key based on username and viewer
            var cacheKey = $"profile:{username}:viewer:{viewingUserId ?? "anonymous"}";

            // Try to get from cache first
            if (_cache.TryGetValue<PlayerProfileDto>(cacheKey, out var cachedProfile))
            {
                _logger.LogDebug("Returning cached profile for {Username}", username);
                return cachedProfile;
            }

            // Get the target user
            var targetUser = await _userRepository.GetByUsernameAsync(username);
            if (targetUser == null)
            {
                await Clients.Caller.SendAsync("Error", "User not found");
                return null;
            }

            var isOwnProfile = viewingUserId == targetUser.UserId;
            var isFriend = false;

            // Check if viewer is friends with target
            if (!string.IsNullOrEmpty(viewingUserId) && !isOwnProfile)
            {
                var friendships = await _friendshipRepository.GetFriendsAsync(viewingUserId);
                isFriend = friendships.Any(f => f.FriendUserId == targetUser.UserId && f.Status == FriendshipStatus.Accepted);
            }

            // Create profile DTO respecting privacy settings
            var profile = PlayerProfileDto.FromUser(targetUser, isFriend, isOwnProfile);

            // Get recent games if allowed by privacy settings
            if (isOwnProfile ||
                targetUser.GameHistoryPrivacy == ProfilePrivacyLevel.Public ||
                (targetUser.GameHistoryPrivacy == ProfilePrivacyLevel.FriendsOnly && isFriend))
            {
                var recentGames = await _gameRepository.GetPlayerGamesAsync(targetUser.UserId, "Completed", 10);
                profile.RecentGames = recentGames.Select(g => new GameSummaryDto
                {
                    GameId = g.GameId,
                    OpponentUsername = GetOpponentUsername(g, targetUser.UserId),
                    Won = DetermineIfPlayerWon(g, targetUser.UserId),
                    Stakes = g.Stakes,
                    CompletedAt = g.CompletedAt ?? g.LastUpdatedAt,
                    WinType = DetermineWinType(g, targetUser.UserId)
                }).ToList();
            }

            // Get friends list if allowed by privacy settings
            if (isOwnProfile ||
                targetUser.FriendsListPrivacy == ProfilePrivacyLevel.Public ||
                (targetUser.FriendsListPrivacy == ProfilePrivacyLevel.FriendsOnly && isFriend))
            {
                var friendships = await _friendshipRepository.GetFriendsAsync(targetUser.UserId);
                var friendUsers = new List<FriendDto>();

                foreach (var friendship in friendships.Where(f => f.Status == FriendshipStatus.Accepted))
                {
                    var friendUser = await _userRepository.GetByUserIdAsync(friendship.FriendUserId);
                    if (friendUser != null)
                    {
                        var isOnline = _sessionManager.IsPlayerOnline(friendship.FriendUserId);
                        friendUsers.Add(new FriendDto
                        {
                            UserId = friendUser.UserId,
                            Username = friendUser.Username,
                            DisplayName = friendUser.DisplayName,
                            IsOnline = isOnline,
                            Status = friendship.Status,
                            InitiatedBy = friendship.InitiatedBy
                        });
                    }
                }

                profile.Friends = friendUsers;
            }

            _logger.LogInformation(
                "Profile viewed for {TargetUser} by {ViewingUser}",
                targetUser.Username, viewingUserId ?? "anonymous");

            // Cache the profile for 2 minutes (shorter for own profile to reflect updates faster)
            var cacheExpiration = isOwnProfile ? TimeSpan.FromMinutes(1) : TimeSpan.FromMinutes(2);
            _cache.Set(cacheKey, profile, cacheExpiration);

            return profile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting player profile");
            await Clients.Caller.SendAsync("Error", "Failed to load profile");
            return null;
        }
    }

    private string GetOpponentUsername(ServerGame game, string userId)
    {
        if (game.WhiteUserId == userId)
        {
            return game.RedPlayerName ?? "Anonymous";
        }
        else
        {
            return game.WhitePlayerName ?? "Anonymous";
        }
    }

    private bool DetermineIfPlayerWon(ServerGame game, string userId)
    {
        if (game.WhiteUserId == userId)
        {
            return game.Winner == "White";
        }
        else if (game.RedUserId == userId)
        {
            return game.Winner == "Red";
        }

        return false;
    }

    private string? DetermineWinType(ServerGame game, string userId)
    {
        if (!DetermineIfPlayerWon(game, userId))
        {
            return null;
        }

        switch (game.Stakes)
        {
            case 2:
                return "Gammon";
            case 3:
                return "Backgammon";
            default:
                return "Normal";
        }
    }

    /// <summary>
    /// Create a new match between two players
    /// </summary>
    public async Task CreateMatch(string opponentId, int targetScore)
    {
        try
        {
            var playerId = GetEffectivePlayerId(Context.ConnectionId);

            if (playerId == opponentId)
            {
                await Clients.Caller.SendAsync("Error", "Cannot create a match against yourself");
                return;
            }

            // Create the match
            var match = await _matchService.CreateMatchAsync(playerId, opponentId, targetScore);

            // Start the first game
            var firstGame = await _matchService.StartNextGameAsync(match.MatchId);

            // Create game session
            var session = _sessionManager.GetSession(firstGame.GameId);
            if (session != null)
            {
                session.MatchId = match.MatchId;
                session.IsMatchGame = true;
            }

            // Notify both players
            await Clients.Caller.SendAsync("MatchCreated", new
            {
                matchId = match.MatchId,
                targetScore = match.TargetScore,
                opponentName = match.Player2Name,
                gameId = firstGame.GameId
            });

            // Check if opponent is online and notify them
            if (_sessionManager.IsPlayerOnline(opponentId))
            {
                var opponentConnection = GetPlayerConnection(opponentId);
                if (!string.IsNullOrEmpty(opponentConnection))
                {
                    await Clients.Client(opponentConnection).SendAsync("MatchInvite", new
                    {
                        matchId = match.MatchId,
                        targetScore = match.TargetScore,
                        challengerName = match.Player1Name,
                        gameId = firstGame.GameId
                    });
                }
            }

            _logger.LogInformation(
                "Created match {MatchId} between {Player1} and {Player2}",
                match.MatchId, match.Player1Name, match.Player2Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating match");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    /// <summary>
    /// Continue an existing match
    /// </summary>
    public async Task ContinueMatch(string matchId)
    {
        try
        {
            var match = await _matchService.GetMatchAsync(matchId);
            if (match == null)
            {
                await Clients.Caller.SendAsync("Error", "Match not found");
                return;
            }

            if (match.Status != "InProgress")
            {
                await Clients.Caller.SendAsync("Error", "Match is not in progress");
                return;
            }

            var playerId = GetEffectivePlayerId(Context.ConnectionId);
            if (playerId != match.Player1Id && playerId != match.Player2Id)
            {
                await Clients.Caller.SendAsync("Error", "You are not a player in this match");
                return;
            }

            // Start next game in the match
            var nextGame = await _matchService.StartNextGameAsync(matchId);

            // Create game session
            var session = _sessionManager.GetSession(nextGame.GameId);
            if (session != null)
            {
                session.MatchId = match.MatchId;
                session.IsMatchGame = true;

                // For AI matches, add AI player before human joins
                if (match.OpponentType == "AI")
                {
                    var aiPlayerId = _aiMoveService.GenerateAiPlayerId();
                    session.AddPlayer(aiPlayerId, string.Empty); // Empty connection ID for AI
                    session.SetPlayerName(aiPlayerId, "Computer");

                    _logger.LogInformation("Added AI player {AiPlayerId} to next match game {GameId}", aiPlayerId, nextGame.GameId);
                }
            }

            // Send match status update
            await Clients.Caller.SendAsync("MatchContinued", new
            {
                matchId = match.MatchId,
                gameId = nextGame.GameId,
                player1Score = match.Player1Score,
                player2Score = match.Player2Score,
                targetScore = match.TargetScore,
                isCrawfordGame = match.IsCrawfordGame
            });

            // Join the new game
            await JoinGame(playerId, nextGame.GameId);

            _logger.LogInformation(
                "Continued match {MatchId} with game {GameId}",
                matchId, nextGame.GameId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error continuing match");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    /// <summary>
    /// Get match status
    /// </summary>
    public async Task GetMatchStatus(string matchId)
    {
        try
        {
            var playerId = GetEffectivePlayerId(Context.ConnectionId);
            var match = await _matchService.GetMatchAsync(matchId);
            if (match == null)
            {
                await Clients.Caller.SendAsync("Error", "Match not found");
                return;
            }

            // Authorization: only participants can view match status
            if (match.Player1Id != playerId && match.Player2Id != playerId)
            {
                await Clients.Caller.SendAsync("Error", "Access denied");
                _logger.LogWarning("Player {PlayerId} attempted to access match {MatchId} without authorization",
                    playerId, matchId);
                return;
            }

            await Clients.Caller.SendAsync("MatchStatus", new
            {
                matchId = match.MatchId,
                targetScore = match.TargetScore,
                player1Name = match.Player1Name,
                player2Name = match.Player2Name,
                player1Score = match.Player1Score,
                player2Score = match.Player2Score,
                isCrawfordGame = match.IsCrawfordGame,
                hasCrawfordGameBeenPlayed = match.HasCrawfordGameBeenPlayed,
                status = match.Status,
                winnerId = match.WinnerId,
                totalGames = match.GameIds.Count,
                currentGameId = match.CurrentGameId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting match status");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    /// <summary>
    /// Get player's active matches
    /// </summary>
    public async Task GetMyMatches(string? status = null)
    {
        try
        {
            var playerId = GetEffectivePlayerId(Context.ConnectionId);
            var matches = await _matchService.GetPlayerMatchesAsync(playerId, status);

            var matchList = matches.Select(m => new
            {
                matchId = m.MatchId,
                targetScore = m.TargetScore,
                opponentId = m.Player1Id == playerId ? m.Player2Id : m.Player1Id,
                opponentName = m.Player1Id == playerId ? m.Player2Name : m.Player1Name,
                myScore = m.Player1Id == playerId ? m.Player1Score : m.Player2Score,
                opponentScore = m.Player1Id == playerId ? m.Player2Score : m.Player1Score,
                status = m.Status,
                createdAt = m.CreatedAt,
                totalGames = m.GameIds.Count
            }).ToList();

            await Clients.Caller.SendAsync("MyMatches", matchList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting player matches");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    /// <summary>
    /// Create a new match with configuration (lobby-based)
    /// </summary>
    public async Task CreateMatchWithConfig(MatchConfig config)
    {
        try
        {
            var playerId = GetEffectivePlayerId(Context.ConnectionId);

            // Determine if this is an open lobby
            bool isOpenLobby = config.OpponentType == "OpenLobby";
            string? opponentId = null;

            // For friend matches, set the opponent ID
            if (config.OpponentType == "Friend" && !string.IsNullOrEmpty(config.OpponentId))
            {
                opponentId = config.OpponentId;
            }

            // For AI matches, set the AI opponent ID
            else if (config.OpponentType == "AI" && !string.IsNullOrEmpty(config.OpponentId))
            {
                opponentId = config.OpponentId;
            }

            // Create match lobby
            var match = await _matchService.CreateMatchLobbyAsync(
                playerId,
                config.TargetScore,
                config.OpponentType,
                isOpenLobby,
                config.DisplayName,
                opponentId);

            // For AI matches, skip lobby and start immediately
            if (config.OpponentType == "AI")
            {
                var game = await _matchService.StartMatchFirstGameAsync(match.MatchId);

                // Get the game session and add AI player
                var session = _sessionManager.GetGame(game.GameId);
                if (session != null)
                {
                    // Add AI player as Red (human player will be White when they join)
                    var aiPlayerId = _aiMoveService.GenerateAiPlayerId();
                    session.AddPlayer(aiPlayerId, string.Empty); // Empty connection ID for AI
                    session.SetPlayerName(aiPlayerId, "Computer");

                    _logger.LogInformation("Added AI player {AiPlayerId} to match game {GameId}", aiPlayerId, game.GameId);
                }
                else
                {
                    _logger.LogWarning("Could not find game session {GameId} to add AI player", game.GameId);
                }

                // Refresh match data
                var updatedMatch = await _matchService.GetMatchAsync(match.MatchId);

                await Clients.Caller.SendAsync("MatchGameStarting", new
                {
                    matchId = updatedMatch.MatchId,
                    gameId = game.GameId,
                    player1Id = updatedMatch.Player1Id,
                    player2Id = updatedMatch.Player2Id,
                    player1Name = updatedMatch.Player1Name,
                    player2Name = updatedMatch.Player2Name,
                    player1Score = updatedMatch.Player1Score,
                    player2Score = updatedMatch.Player2Score,
                    targetScore = updatedMatch.TargetScore,
                    isCrawfordGame = updatedMatch.IsCrawfordGame
                });

                _logger.LogInformation(
                    "AI match {MatchId} created and started for player {PlayerId}",
                    match.MatchId, playerId);
                return;
            }

            // Send match lobby created event
            await Clients.Caller.SendAsync("MatchLobbyCreated", new
            {
                matchId = match.MatchId,
                targetScore = match.TargetScore,
                opponentType = match.OpponentType,
                isOpenLobby = match.IsOpenLobby,
                player1Name = match.Player1Name,
                player1Id = match.Player1Id,
                player2Name = match.Player2Name,
                player2Id = match.Player2Id,
                lobbyStatus = match.LobbyStatus
            });

            // For friend matches, notify the friend if they're online
            if (config.OpponentType == "Friend" && !string.IsNullOrEmpty(opponentId))
            {
                if (_sessionManager.IsPlayerOnline(opponentId))
                {
                    var opponentConnection = GetPlayerConnection(opponentId);
                    if (!string.IsNullOrEmpty(opponentConnection))
                    {
                        await Clients.Client(opponentConnection).SendAsync("MatchLobbyInvite", new
                        {
                            matchId = match.MatchId,
                            targetScore = match.TargetScore,
                            challengerName = match.Player1Name,
                            challengerId = match.Player1Id
                        });
                    }
                }
            }

            _logger.LogInformation(
                "Match lobby {MatchId} created by {PlayerId} (type: {OpponentType})",
                match.MatchId, playerId, config.OpponentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating match with config");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    /// <summary>
    /// Join a match lobby
    /// </summary>
    public async Task JoinMatchLobby(string matchId, string? displayName)
    {
        try
        {
            var playerId = GetEffectivePlayerId(Context.ConnectionId);

            // Track this player's connection
            _playerConnectionService.AddConnection(playerId, Context.ConnectionId);

            // Get match to check if it exists
            var existingMatch = await _matchService.GetMatchLobbyAsync(matchId);
            if (existingMatch == null)
            {
                await Clients.Caller.SendAsync("Error", "Match not found");
                return;
            }

            _logger.LogInformation(
                "Player {PlayerId} attempting to join match {MatchId}. Match state: IsOpenLobby={IsOpenLobby}, OpponentType={OpponentType}, Player1Id={Player1Id}, Player2Id={Player2Id}, LobbyStatus={LobbyStatus}",
                playerId, matchId, existingMatch.IsOpenLobby, existingMatch.OpponentType, existingMatch.Player1Id, existingMatch.Player2Id ?? "null", existingMatch.LobbyStatus);

            // If player is the creator, just send them the lobby state
            if (existingMatch.Player1Id == playerId)
            {
                await Clients.Caller.SendAsync("MatchLobbyJoined", new
                {
                    matchId = existingMatch.MatchId,
                    targetScore = existingMatch.TargetScore,
                    opponentType = existingMatch.OpponentType,
                    isOpenLobby = existingMatch.IsOpenLobby,
                    player1Name = existingMatch.Player1Name,
                    player1Id = existingMatch.Player1Id,
                    player2Name = existingMatch.Player2Name,
                    player2Id = existingMatch.Player2Id,
                    lobbyStatus = existingMatch.LobbyStatus
                });
                return;
            }

            // Check if this is an open lobby with an empty slot
            if (existingMatch.IsOpenLobby && string.IsNullOrEmpty(existingMatch.Player2Id))
            {
                // New player joining open lobby
                var match = await _matchService.JoinOpenLobbyAsync(matchId, playerId, displayName);

                // Notify the joiner
                await Clients.Caller.SendAsync("MatchLobbyJoined", new
                {
                    matchId = match.MatchId,
                    targetScore = match.TargetScore,
                    opponentType = match.OpponentType,
                    isOpenLobby = match.IsOpenLobby,
                    player1Name = match.Player1Name,
                    player1Id = match.Player1Id,
                    player2Name = match.Player2Name,
                    player2Id = match.Player2Id,
                    lobbyStatus = match.LobbyStatus
                });

                // Notify the creator
                var creatorConnection = GetPlayerConnection(match.Player1Id);
                _logger.LogInformation(
                    "Looking up creator connection for Player1Id={Player1Id}, found connectionId={ConnectionId}",
                    match.Player1Id, creatorConnection ?? "NULL");

                if (!string.IsNullOrEmpty(creatorConnection))
                {
                    _logger.LogInformation("Sending MatchLobbyPlayerJoined to creator at connection {ConnectionId}", creatorConnection);
                    await Clients.Client(creatorConnection).SendAsync("MatchLobbyPlayerJoined", new
                    {
                        matchId = match.MatchId,
                        player2Name = match.Player2Name,
                        player2Id = match.Player2Id,
                        lobbyStatus = match.LobbyStatus
                    });
                }
                else
                {
                    _logger.LogWarning("Could not find connection for creator {Player1Id}", match.Player1Id);
                }

                _logger.LogInformation("Player {PlayerId} joined match lobby {MatchId}", playerId, matchId);

                // Auto-start the match now that both players are ready
                _logger.LogInformation(
                    "Auto-starting match {MatchId} with both players ready. Player1Id={Player1Id}, Player2Id={Player2Id}",
                    matchId, match.Player1Id, match.Player2Id);
                try
                {
                    // Use the match data we just got from JoinOpenLobbyAsync (it's fresh)
                    if (string.IsNullOrEmpty(match.Player2Id))
                    {
                        _logger.LogWarning(
                            "Cannot auto-start match {MatchId} - Player2Id not set in returned match data. Player1Id={Player1Id}, Player2Id={Player2Id}",
                            matchId, match.Player1Id, match.Player2Id);
                        return;
                    }

                    // Start the first game (pass the match object to avoid DB reload)
                    var firstGame = await _matchService.StartMatchFirstGameAsync(match);

                    // Get updated match state
                    var updatedMatch = await _matchService.GetMatchAsync(matchId);

                    // Notify both players
                    var player1Connection = GetPlayerConnection(match.Player1Id);
                    var player2Connection = GetPlayerConnection(match.Player2Id);

                    var matchGameData = new
                    {
                        matchId = updatedMatch.MatchId,
                        gameId = firstGame.GameId,
                        player1Id = updatedMatch.Player1Id,
                        player2Id = updatedMatch.Player2Id,
                        player1Name = updatedMatch.Player1Name,
                        player2Name = updatedMatch.Player2Name,
                        player1Score = updatedMatch.Player1Score,
                        player2Score = updatedMatch.Player2Score,
                        targetScore = updatedMatch.TargetScore,
                        isCrawfordGame = updatedMatch.IsCrawfordGame
                    };

                    if (!string.IsNullOrEmpty(player1Connection))
                    {
                        await Clients.Client(player1Connection).SendAsync("MatchGameStarting", matchGameData);
                    }

                    if (!string.IsNullOrEmpty(player2Connection))
                    {
                        await Clients.Client(player2Connection).SendAsync("MatchGameStarting", matchGameData);
                    }

                    _logger.LogInformation(
                        "Match {MatchId} first game {GameId} auto-started",
                        matchId, firstGame.GameId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error auto-starting match {MatchId}", matchId);
                    // Don't throw - just log the error and let players manually start if needed
                }
            }
            else if (existingMatch.Player2Id == playerId)
            {
                // Player is already in this match (rejoining), send current state
                await Clients.Caller.SendAsync("MatchLobbyJoined", new
                {
                    matchId = existingMatch.MatchId,
                    targetScore = existingMatch.TargetScore,
                    opponentType = existingMatch.OpponentType,
                    isOpenLobby = existingMatch.IsOpenLobby,
                    player1Name = existingMatch.Player1Name,
                    player1Id = existingMatch.Player1Id,
                    player2Name = existingMatch.Player2Name,
                    player2Id = existingMatch.Player2Id,
                    lobbyStatus = existingMatch.LobbyStatus
                });
                _logger.LogInformation("Player {PlayerId} rejoined match lobby {MatchId}", playerId, matchId);
            }
            else
            {
                // Provide detailed error message for debugging
                string reason = existingMatch.IsOpenLobby
                    ? "Match lobby is full"
                    : "Match is not an open lobby (it's invite-only)";

                _logger.LogWarning(
                    "Player {PlayerId} cannot join match {MatchId}. Reason: {Reason}. IsOpenLobby={IsOpenLobby}, Player2Id={Player2Id}",
                    playerId, matchId, reason, existingMatch.IsOpenLobby, existingMatch.Player2Id);

                await Clients.Caller.SendAsync("Error", $"Cannot join this match: {reason}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining match lobby {MatchId}", matchId);
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    /// <summary>
    /// Start the first game in a match
    /// </summary>
    public async Task StartMatchGame(string matchId)
    {
        try
        {
            var playerId = GetEffectivePlayerId(Context.ConnectionId);

            var match = await _matchService.GetMatchLobbyAsync(matchId);
            if (match == null)
            {
                await Clients.Caller.SendAsync("Error", "Match not found");
                return;
            }

            // Only the creator can start the match
            if (match.Player1Id != playerId)
            {
                await Clients.Caller.SendAsync("Error", "Only the match creator can start the game");
                return;
            }

            // Ensure both players are present
            if (string.IsNullOrEmpty(match.Player2Id))
            {
                await Clients.Caller.SendAsync("Error", "Waiting for opponent to join");
                return;
            }

            // Start the first game
            var game = await _matchService.StartMatchFirstGameAsync(matchId);

            // Refresh match data to get updated state
            var updatedMatch = await _matchService.GetMatchAsync(matchId);

            // Notify both players
            var player1Connection = GetPlayerConnection(match.Player1Id);
            var player2Connection = GetPlayerConnection(match.Player2Id);

            var matchData = new
            {
                matchId = updatedMatch.MatchId,
                gameId = game.GameId,
                player1Id = updatedMatch.Player1Id,
                player2Id = updatedMatch.Player2Id,
                player1Name = updatedMatch.Player1Name,
                player2Name = updatedMatch.Player2Name,
                player1Score = updatedMatch.Player1Score,
                player2Score = updatedMatch.Player2Score,
                targetScore = updatedMatch.TargetScore,
                isCrawfordGame = updatedMatch.IsCrawfordGame
            };

            if (!string.IsNullOrEmpty(player1Connection))
            {
                await Clients.Client(player1Connection).SendAsync("MatchGameStarting", matchData);
            }

            if (!string.IsNullOrEmpty(player2Connection))
            {
                await Clients.Client(player2Connection).SendAsync("MatchGameStarting", matchData);
            }

            _logger.LogInformation(
                "Match {MatchId} first game {GameId} started by {PlayerId}",
                matchId, game.GameId, playerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting match game {MatchId}", matchId);
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    /// <summary>
    /// Leave a match lobby
    /// </summary>
    public async Task LeaveMatchLobby(string matchId)
    {
        try
        {
            var playerId = GetEffectivePlayerId(Context.ConnectionId);

            var match = await _matchService.GetMatchLobbyAsync(matchId);
            if (match == null)
            {
                return;
            }

            await _matchService.LeaveMatchLobbyAsync(matchId, playerId);

            // Notify the caller
            await Clients.Caller.SendAsync("MatchLobbyLeft", new { matchId });

            // If player 2 left, notify player 1
            if (match.Player2Id == playerId && !string.IsNullOrEmpty(match.Player1Id))
            {
                var player1Connection = GetPlayerConnection(match.Player1Id);
                if (!string.IsNullOrEmpty(player1Connection))
                {
                    await Clients.Client(player1Connection).SendAsync("MatchLobbyPlayerLeft", new
                    {
                        matchId,
                        playerId
                    });
                }
            }

            _logger.LogInformation("Player {PlayerId} left match lobby {MatchId}", playerId, matchId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving match lobby {MatchId}", matchId);
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    /// <summary>
    /// Override the existing MakeMove to handle match game completion
    /// </summary>
    private async Task HandleMatchGameCompletion(GameSession session)
    {
        if (!session.IsMatchGame || string.IsNullOrEmpty(session.MatchId))
        {
            return;
        }

        try
        {
            // Get game result
            var winnerColor = session.Engine.Winner?.Color;
            if (winnerColor == null)
            {
                return;
            }

            var winnerId = winnerColor == CheckerColor.White ? session.WhitePlayerId : session.RedPlayerId;
            var winType = session.Engine.DetermineWinType();
            var stakes = session.Engine.GetGameResult();

            var result = new GameResult(winnerId ?? string.Empty, winType, session.Engine.DoublingCube.Value);

            // Complete the game in the match
            await _matchService.CompleteGameAsync(session.Id, result);

            // Check if match is complete
            var match = await _matchService.GetMatchAsync(session.MatchId);
            if (match == null)
            {
                return;
            }

            // Notify players of match update
            await Clients.Group(session.Id).SendAsync("MatchUpdate", new
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling match game completion");
        }
    }

    /// <summary>
    /// Get the connection ID for a specific player
    /// </summary>
    private string? GetPlayerConnection(string playerId)
    {
        // First check our connection tracking service (for lobby players)
        var connectionId = _playerConnectionService.GetConnectionId(playerId);
        if (connectionId != null)
        {
            _logger.LogDebug("GetPlayerConnection: Found playerId={PlayerId} via connection service with connectionId={ConnectionId}",
                playerId, connectionId);
            return connectionId;
        }

        _logger.LogDebug("GetPlayerConnection: playerId={PlayerId} not found in connection service, checking game sessions", playerId);

        // Fall back to checking game sessions (for players in active games)
        var sessions = _sessionManager.GetPlayerGames(playerId);
        var session = sessions.FirstOrDefault();
        if (session == null)
        {
            _logger.LogDebug("GetPlayerConnection: No game session found for playerId={PlayerId}", playerId);
            return null;
        }

        if (session.WhitePlayerId == playerId)
        {
            return session.WhiteConnectionId;
        }

        if (session.RedPlayerId == playerId)
        {
            return session.RedConnectionId;
        }

        return null;
    }
}

/// <summary>
/// Configuration for creating a new match
/// </summary>
public class MatchConfig
{
    /// <summary>
    /// Opponent type: "Friend", "AI", "OpenLobby"
    /// </summary>
    public string OpponentType { get; set; } = string.Empty;

    /// <summary>
    /// Opponent ID (for Friend/AI modes)
    /// </summary>
    public string? OpponentId { get; set; }

    /// <summary>
    /// Target score to win the match
    /// </summary>
    public int TargetScore { get; set; } = 7;

    /// <summary>
    /// Display name for anonymous players
    /// </summary>
    public string? DisplayName { get; set; }
}
