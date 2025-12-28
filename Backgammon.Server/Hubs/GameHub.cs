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
    private readonly ILogger<GameHub> _logger;

    public GameHub(
        IGameSessionManager sessionManager,
        IGameRepository gameRepository,
        IUserRepository userRepository,
        ILogger<GameHub> logger)
    {
        _sessionManager = sessionManager;
        _gameRepository = gameRepository;
        _userRepository = userRepository;
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
                if (session.WhiteConnectionId != null)
                {
                    var whiteState = session.GetState(session.WhiteConnectionId);
                    await Clients.Client(session.WhiteConnectionId).SendAsync("GameStart", whiteState);
                }
                if (session.RedConnectionId != null)
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

            if (session.Engine.RemainingMoves.Count > 0)
            {
                await Clients.Caller.SendAsync("Error", "Must complete current moves first");
                return;
            }

            session.Engine.RollDice();
            session.UpdateActivity();
            
            // Send personalized state to each player
            if (session.WhiteConnectionId != null)
            {
                var whiteState = session.GetState(session.WhiteConnectionId);
                await Clients.Client(session.WhiteConnectionId).SendAsync("GameUpdate", whiteState);
            }
            if (session.RedConnectionId != null)
            {
                var redState = session.GetState(session.RedConnectionId);
                await Clients.Client(session.RedConnectionId).SendAsync("GameUpdate", redState);
            }

            // Save game state after dice roll (progressive save)
            await SaveGameStateAsync(session);

            _logger.LogInformation("Player {ConnectionId} rolled dice in game {GameId}",
                Context.ConnectionId, session.Id);
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
            if (session.WhiteConnectionId != null)
            {
                var whiteState = session.GetState(session.WhiteConnectionId);
                await Clients.Client(session.WhiteConnectionId).SendAsync("GameUpdate", whiteState);
            }
            if (session.RedConnectionId != null)
            {
                var redState = session.GetState(session.RedConnectionId);
                await Clients.Client(session.RedConnectionId).SendAsync("GameUpdate", redState);
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

                        // Update user statistics if players are registered
                        var game = GameEngineMapper.ToGame(session);
                        await UpdateUserStatsAfterGame(game);

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
            if (session.WhiteConnectionId != null)
            {
                var whiteState = session.GetState(session.WhiteConnectionId);
                await Clients.Client(session.WhiteConnectionId).SendAsync("GameUpdate", whiteState);
            }
            if (session.RedConnectionId != null)
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
            if (session.WhiteConnectionId != null)
            {
                var whiteState = session.GetState(session.WhiteConnectionId);
                await Clients.Client(session.WhiteConnectionId).SendAsync("GameUpdate", whiteState);
            }
            if (session.RedConnectionId != null)
            {
                var redState = session.GetState(session.RedConnectionId);
                await Clients.Client(session.RedConnectionId).SendAsync("GameUpdate", redState);
            }

            // Save game state after turn end (progressive save)
            await SaveGameStateAsync(session);

            _logger.LogInformation("Turn ended in game {GameId}. Current player: {Player}",
                session.Id, session.Engine.CurrentPlayer?.Color.ToString() ?? "Unknown");
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
            if (session.WhiteConnectionId != null)
            {
                var whiteState = session.GetState(session.WhiteConnectionId);
                await Clients.Client(session.WhiteConnectionId).SendAsync("DoubleAccepted", whiteState);
            }
            if (session.RedConnectionId != null)
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
                    var game = GameEngineMapper.ToGame(session);
                    await UpdateUserStatsAfterGame(game);
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
                    var game = GameEngineMapper.ToGame(session);
                    await UpdateUserStatsAfterGame(game);
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

                // Notify opponent
                await Clients.Group(session.Id).SendAsync("OpponentLeft");

                _sessionManager.RemovePlayer(connectionId);

                _logger.LogInformation("Player {ConnectionId} left game {GameId}", connectionId, session.Id);
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
}
