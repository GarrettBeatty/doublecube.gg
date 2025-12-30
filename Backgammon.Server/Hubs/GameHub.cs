using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Backgammon.Core;
using Backgammon.Server.Services;
using Backgammon.Server.Models;

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
    private readonly IAiMoveService _aiMoveService;
    private readonly IHubContext<GameHub> _hubContext;
    private readonly ILogger<GameHub> _logger;

    public GameHub(
        IGameSessionManager sessionManager,
        IGameRepository gameRepository,
        IUserRepository userRepository,
        IAiMoveService aiMoveService,
        IHubContext<GameHub> hubContext,
        ILogger<GameHub> logger)
    {
        _sessionManager = sessionManager;
        _gameRepository = gameRepository;
        _userRepository = userRepository;
        _aiMoveService = aiMoveService;
        _hubContext = hubContext;
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

            var session = _sessionManager.JoinOrCreate(effectivePlayerId, connectionId, gameId);
            await Groups.AddToGroupAsync(connectionId, session.Id);
            _logger.LogInformation("Player {PlayerId} (connection {ConnectionId}) joined game {GameId}",
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
            }
            else
            {
                // Waiting for opponent
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
            var session = _sessionManager.JoinOrCreate(userId, connectionId, null);
            session.IsAnalysisMode = true;

            // Directly set same player for both sides (bypassing AddPlayer logic)
            // This is necessary because AddPlayer() won't add the same player twice
            if (session.WhitePlayerId == null)
            {
                session.AddPlayer(userId, connectionId); // Adds as White
            }

            // Manually set Red player to same user (analysis mode special case)
            session.SetRedPlayer(userId, connectionId);

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
            _logger.LogDebug("CreateAiGame called by connection {ConnectionId}, playerId={PlayerId}",
                connectionId, playerId);

            // Use authenticated user ID if available
            var effectivePlayerId = GetEffectivePlayerId(playerId);
            var displayName = GetAuthenticatedDisplayName();
            _logger.LogDebug("Effective player ID: {EffectivePlayerId}, Display name: {DisplayName}",
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
            _logger.LogDebug("Added human player as White: {PlayerId}, registered connection {ConnectionId}",
                effectivePlayerId, connectionId);

            // Add AI player as Red
            var aiPlayerId = _aiMoveService.GenerateAiPlayerId();
            session.AddPlayer(aiPlayerId, ""); // Empty connection ID for AI
            session.SetPlayerName(aiPlayerId, "Computer");
            _logger.LogDebug("Added AI player as Red: {AiPlayerId}", aiPlayerId);

            _logger.LogInformation("Created AI game {GameId}. Human: {PlayerId}, AI: {AiPlayerId}",
                session.Id, effectivePlayerId, aiPlayerId);

            // Game is now full - start immediately
            var humanState = session.GetState(connectionId);
            _logger.LogDebug("Sending GameStart to human player. IsYourTurn={IsYourTurn}, CurrentPlayer={CurrentPlayer}, YourColor={YourColor}",
                humanState.IsYourTurn, humanState.CurrentPlayer, humanState.YourColor);
            await Clients.Caller.SendAsync("GameStart", humanState);

            // Save initial game state
            await SaveGameStateAsync(session);

            // If AI goes first (which would be unusual since White goes first, but handle it)
            var currentPlayerId = GetCurrentPlayerId(session);
            _logger.LogDebug("Current player ID: {CurrentPlayerId}, Is AI: {IsAi}",
                currentPlayerId, _aiMoveService.IsAiPlayer(currentPlayerId));

            if (_aiMoveService.IsAiPlayer(currentPlayerId))
            {
                _logger.LogInformation("AI goes first - triggering AI turn for game {GameId}", session.Id);
                // Trigger AI turn in background
                _ = ExecuteAiTurnWithBroadcastAsync(session, aiPlayerId);
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
                return new List<int>();

            if (!session.IsPlayerTurn(Context.ConnectionId))
                return new List<int>();

            if (session.Engine.RemainingMoves.Count == 0)
                return new List<int>();

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
        var targetPoint = engine.Board.GetPoint(move.To);
        if (targetPoint.Color == null || targetPoint.Count == 0)
            return false;
            
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

            _logger.LogDebug("RollDice: Found session {GameId}, CurrentPlayer={CurrentPlayer}, WhiteConn={WhiteConn}, RedConn={RedConn}",
                session.Id, session.Engine.CurrentPlayer?.Color, session.WhiteConnectionId, session.RedConnectionId);

            if (!session.IsPlayerTurn(Context.ConnectionId))
            {
                _logger.LogWarning("RollDice failed: Not player's turn. Connection={ConnectionId}, Game={GameId}",
                    Context.ConnectionId, session.Id);
                await Clients.Caller.SendAsync("Error", "Not your turn");
                return;
            }

            if (session.Engine.RemainingMoves.Count > 0)
            {
                _logger.LogWarning("RollDice failed: Remaining moves exist. Count={Count}, Connection={ConnectionId}",
                    session.Engine.RemainingMoves.Count, Context.ConnectionId);
                await Clients.Caller.SendAsync("Error", "Must complete current moves first");
                return;
            }

            session.Engine.RollDice();
            session.UpdateActivity();

            _logger.LogInformation("Player {ConnectionId} rolled dice in game {GameId}: [{Die1}, {Die2}]",
                Context.ConnectionId, session.Id, session.Engine.Dice.Die1, session.Engine.Dice.Die2);

            // Send personalized state to each player
            if (!string.IsNullOrEmpty(session.WhiteConnectionId))
            {
                var whiteState = session.GetState(session.WhiteConnectionId);
                _logger.LogDebug("Sending GameUpdate to White player (conn={Conn}). IsYourTurn={IsYourTurn}, Dice=[{Die1},{Die2}]",
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
                _logger.LogDebug("Sending GameUpdate to Red player (conn={Conn}). IsYourTurn={IsYourTurn}, Dice=[{Die1},{Die2}]",
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

            _logger.LogInformation("Current player: {Player}, Remaining moves: {Moves}",
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
                _logger.LogInformation("Game {GameId} completed. Winner: {Winner} (Stakes: {Stakes})",
                    session.Id, session.Engine.Winner.Name, stakes);

                // Update game status to Completed and update user stats
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Save final game state with Status="Completed" (already done by SaveGameStateAsync above)
                        // Update status explicitly to ensure CompletedAt timestamp is set
                        await _gameRepository.UpdateGameStatusAsync(session.Id, "Completed");

                        // Update user statistics if players are registered (skip for analysis games)
                        if (!session.IsAnalysisMode)
                        {
                            var game = GameEngineMapper.ToGame(session);
                            await UpdateUserStatsAfterGame(game);
                        }
                        else
                        {
                            _logger.LogInformation("Skipping stats tracking for analysis game {GameId}", session.Id);
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
    /// Undo the last move made during this turn
    /// </summary>
    public async Task UndoMove()
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

            if (!session.Engine.UndoLastMove())
            {
                await Clients.Caller.SendAsync("Error", "No moves to undo");
                return;
            }

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
            
            _logger.LogInformation("Player {ConnectionId} undid last move in game {GameId}", 
                Context.ConnectionId, session.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error undoing move");
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

            _logger.LogInformation("Turn ended in game {GameId}. Current player: {Player}",
                session.Id, session.Engine.CurrentPlayer?.Color.ToString() ?? "Unknown");

            // Check if next player is AI and trigger AI turn
            var nextPlayerId = GetCurrentPlayerId(session);
            if (_aiMoveService.IsAiPlayer(nextPlayerId))
            {
                _logger.LogInformation("Triggering AI turn for player {AiPlayerId} in game {GameId}",
                    nextPlayerId, session.Id);
                _ = ExecuteAiTurnWithBroadcastAsync(session, nextPlayerId!);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending turn");
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

            if (opponentConnectionId != null)
            {
                await Clients.Client(opponentConnectionId).SendAsync("DoubleOffered", currentValue, newValue);
                _logger.LogInformation("Player {ConnectionId} offered double in game {GameId}. Stakes: {Current}x → {New}x",
                    Context.ConnectionId, session.Id, currentValue, newValue);
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

            _logger.LogInformation("Player {ConnectionId} accepted double in game {GameId}. New stakes: {Stakes}x",
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

            _logger.LogInformation("Game {GameId} ended - player declined double. Winner: {Winner} (Stakes: {Stakes})",
                session.Id, opponentPlayer.Name, stakes);

            // Update database and stats (async)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _gameRepository.UpdateGameStatusAsync(session.Id, "Completed");

                    // Skip stats update for analysis games
                    if (!session.IsAnalysisMode)
                    {
                        var game = GameEngineMapper.ToGame(session);
                        await UpdateUserStatsAfterGame(game);
                    }
                    else
                    {
                        _logger.LogInformation("Skipping stats tracking for analysis game {GameId}", session.Id);
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
            var isWaitingForPlayer = currentState.Status == GameStatus.WaitingForPlayer;

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

            _logger.LogInformation("Game {GameId} abandoned by {Player}. Winner: {Winner} (Stakes: {Stakes})",
                session.Id, abandoningPlayer.Name, opponentPlayer.Name, stakes);

            // Update database and stats (async)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _gameRepository.UpdateGameStatusAsync(session.Id, "Abandoned");

                    // Skip stats update for analysis games
                    if (!session.IsAnalysisMode)
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
                return;

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
                return;

            var senderName = senderColor == CheckerColor.White
                ? (session.WhitePlayerName ?? "White")
                : (session.RedPlayerName ?? "Red");

            // Broadcast to all players in the game
            await Clients.Group(session.Id).SendAsync("ReceiveChatMessage",
                senderName, message, Context.ConnectionId);

            _logger.LogInformation("Chat message from {Sender} in game {GameId}",
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
            return false;

        // Registered user IDs are GUIDs
        return Guid.TryParse(playerId, out _);
    }

    /// <summary>
    /// Get the player ID of the current player in the session
    /// </summary>
    private string? GetCurrentPlayerId(GameSession session)
    {
        if (session.Engine.CurrentPlayer == null)
            return null;

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
            return session.WhitePlayerId;
        if (_aiMoveService.IsAiPlayer(session.RedPlayerId))
            return session.RedPlayerId;
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
                _logger.LogInformation("AI game {GameId} completed. Winner: {Winner} (Stakes: {Stakes})",
                    session.Id, session.Engine.Winner.Name, stakes);

                // Update database status
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _gameRepository.UpdateGameStatusAsync(session.Id, "Completed");
                        var game = GameEngineMapper.ToGame(session);
                        await UpdateUserStatsAfterGame(game);
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
    /// Update user statistics after a game is completed
    /// </summary>
    private async Task UpdateUserStatsAfterGame(Game game)
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
                stats.BestWinStreak = stats.WinStreak;

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

        // Allow import in analysis mode OR when same player controls both sides
        if (!session.IsAnalysisMode &&
            session.WhitePlayerId != null && session.RedPlayerId != null &&
            session.WhitePlayerId != session.RedPlayerId)
        {
            await Clients.Caller.SendAsync("Error", "Cannot import positions in multiplayer games");
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
}
