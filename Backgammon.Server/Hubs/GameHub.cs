using System.Security.Claims;
using Backgammon.Core;
using Backgammon.Server.Extensions;
using Backgammon.Server.Hubs.Interfaces;
using Backgammon.Server.Models;
using Backgammon.Server.Models.SignalR;
using Backgammon.Server.Services;
using Microsoft.AspNetCore.SignalR;
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
public class GameHub : Hub<IGameHubClient>
{
    private readonly IGameSessionManager _sessionManager;
    private readonly IGameRepository _gameRepository;
    private readonly IAiMoveService _aiMoveService;
    private readonly IEloRatingService _eloRatingService;
    private readonly IHubContext<GameHub, IGameHubClient> _hubContext;
    private readonly IMatchService _matchService;
    private readonly IPlayerConnectionService _playerConnectionService;
    private readonly IDoubleOfferService _doubleOfferService;
    private readonly IGameService _gameService;
    private readonly IPlayerProfileService _playerProfileService;
    private readonly IGameActionOrchestrator _gameActionOrchestrator;
    private readonly IPlayerStatsService _playerStatsService;
    private readonly IMoveQueryService _moveQueryService;
    private readonly IGameImportExportService _gameImportExportService;
    private readonly IChatService _chatService;
    private readonly ILogger<GameHub> _logger;
    private readonly AnalysisService _analysisService;
    private readonly IUserRepository _userRepository;
    private readonly IFriendService _friendService;
    private readonly ICorrespondenceGameService _correspondenceGameService;
    private readonly IAuthService _authService;
    private readonly IDailyPuzzleService _dailyPuzzleService;

    public GameHub(
        IGameSessionManager sessionManager,
        IGameRepository gameRepository,
        IAiMoveService aiMoveService,
        IEloRatingService eloRatingService,
        IHubContext<GameHub, IGameHubClient> hubContext,
        IMatchService matchService,
        IPlayerConnectionService playerConnectionService,
        IDoubleOfferService doubleOfferService,
        IGameService gameService,
        IPlayerProfileService playerProfileService,
        IGameActionOrchestrator gameActionOrchestrator,
        IPlayerStatsService playerStatsService,
        IMoveQueryService moveQueryService,
        IGameImportExportService gameImportExportService,
        IChatService chatService,
        ILogger<GameHub> logger,
        AnalysisService analysisService,
        IUserRepository userRepository,
        IFriendService friendService,
        ICorrespondenceGameService correspondenceGameService,
        IAuthService authService,
        IDailyPuzzleService dailyPuzzleService)
    {
        _sessionManager = sessionManager;
        _gameRepository = gameRepository;
        _aiMoveService = aiMoveService;
        _eloRatingService = eloRatingService;
        _hubContext = hubContext;
        _matchService = matchService;
        _playerConnectionService = playerConnectionService;
        _doubleOfferService = doubleOfferService;
        _gameService = gameService;
        _playerProfileService = playerProfileService;
        _gameActionOrchestrator = gameActionOrchestrator;
        _playerStatsService = playerStatsService;
        _moveQueryService = moveQueryService;
        _gameImportExportService = gameImportExportService;
        _chatService = chatService;
        _logger = logger;
        _analysisService = analysisService;
        _userRepository = userRepository;
        _friendService = friendService;
        _correspondenceGameService = correspondenceGameService;
        _authService = authService;
        _dailyPuzzleService = dailyPuzzleService;
    }

    /// <summary>
    /// Called when a client connects to the hub.
    /// Validates that the user exists and updates their last seen timestamp.
    /// User creation must happen via HTTP /api/auth/register-anonymous BEFORE connecting.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        try
        {
            var jwtUserId = GetAuthenticatedUserId();
            var jwtDisplayName = GetAuthenticatedDisplayName();
            var connectionId = Context.ConnectionId;

            _logger.LogInformation("========== SignalR Connection ==========");
            _logger.LogInformation("Connection ID: {ConnectionId}", connectionId);
            _logger.LogInformation("JWT User ID: {JwtUserId}", jwtUserId ?? "null");
            _logger.LogInformation("JWT Display Name: {JwtDisplayName}", jwtDisplayName ?? "null");
            _logger.LogInformation("=========================================");

            // Validate authentication - user must have valid JWT
            if (string.IsNullOrEmpty(jwtUserId))
            {
                _logger.LogWarning("SignalR connection rejected - no JWT user ID for connection {ConnectionId}", connectionId);
                throw new HubException("Authentication required. Please ensure you're registered before connecting.");
            }

            // Validate user exists in database (should always exist if JWT is valid)
            var user = await _userRepository.GetByUserIdAsync(jwtUserId);

            if (user == null)
            {
                _logger.LogError(
                    "SignalR connection rejected - user {UserId} from JWT not found in database (connection {ConnectionId})",
                    jwtUserId,
                    connectionId);
                throw new HubException("Invalid authentication token. User not found.");
            }

            _logger.LogInformation(
                "User {UserId} ({DisplayName}) connected successfully - IsAnonymous: {IsAnonymous}",
                user.UserId,
                user.DisplayName,
                user.IsAnonymous);
        }
        catch (HubException)
        {
            // Re-throw HubExceptions to client
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnConnectedAsync for connection {ConnectionId}", Context.ConnectionId);
            throw new HubException("Connection failed. Please try again.");
        }

        await base.OnConnectedAsync();
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
            var effectivePlayerId = GetEffectivePlayerId(playerId);
            var displayName = GetEffectiveDisplayNameAsync(effectivePlayerId);

            _logger.LogInformation("========== JoinGame Request ==========");
            _logger.LogInformation("Connection ID: {ConnectionId}", connectionId);
            _logger.LogInformation("Player ID (from client): {PlayerId}", playerId);
            _logger.LogInformation("Effective Player ID: {EffectivePlayerId}", effectivePlayerId);
            _logger.LogInformation("Display Name (resolved): {DisplayName}", displayName ?? "null");
            _logger.LogInformation("Game ID: {GameId}", gameId ?? "null");
            _logger.LogInformation("======================================");

            if (string.IsNullOrEmpty(gameId))
            {
                await Clients.Caller.Error("Game ID is required");
                return;
            }

            await _gameService.JoinGameAsync(connectionId, effectivePlayerId, displayName, gameId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining game");
            await Clients.Caller.Error(ex.Message);
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

            await _gameService.CreateAnalysisGameAsync(connectionId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating analysis game");
            await Clients.Caller.Error(ex.Message);
        }
    }

    /// <summary>
    /// Set dice values manually (analysis mode only)
    /// </summary>
    public async Task SetDice(int die1, int die2)
    {
        try
        {
            var session = _sessionManager.GetGameByPlayer(Context.ConnectionId);
            if (session == null)
            {
                await Clients.Caller.Error("Not in a game");
                return;
            }

            // Only allow in analysis mode
            if (!session.IsAnalysisMode)
            {
                await Clients.Caller.Error("Dice can only be set in analysis mode");
                return;
            }

            // Validate dice values early (before acquiring lock)
            if (die1 < 1 || die1 > 6 || die2 < 1 || die2 > 6)
            {
                await Clients.Caller.Error("Dice values must be between 1 and 6");
                return;
            }

            // Acquire lock to prevent race conditions with multi-tab access
            await session.GameActionLock.WaitAsync();
            try
            {
                // Get initial dice count to detect if moves were made
                var initialDiceCount = session.Engine.Dice.GetMoves().Count;
                var currentRemainingCount = session.Engine.RemainingMoves.Count;

                // Allow setting dice if:
                // 1. No remaining moves (turn ended), OR
                // 2. All moves still available (no moves made yet)
                var noMovesLeft = currentRemainingCount == 0;
                var noMovesMadeYet = currentRemainingCount == initialDiceCount;

                if (!noMovesLeft && !noMovesMadeYet)
                {
                    await Clients.Caller.Error("End your turn or undo moves before setting new dice");
                    return;
                }

                // Set the dice (now atomic with validation)
                session.Engine.Dice.SetDice(die1, die2);
                session.Engine.RemainingMoves.Clear();
                session.Engine.RemainingMoves.AddRange(session.Engine.Dice.GetMoves());

                _logger.LogInformation(
                    "Set dice to [{Die1}, {Die2}] in analysis game {GameId}",
                    die1,
                    die2,
                    session.Id);
            }
            finally
            {
                session.GameActionLock.Release();
            }

            // Broadcast update (outside lock to prevent potential deadlocks)
            await _gameService.BroadcastGameUpdateAsync(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting dice");
            await Clients.Caller.Error(ex.Message);
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
            var effectivePlayerId = GetEffectivePlayerId(playerId);
            var displayName = GetAuthenticatedDisplayName();

            await _gameService.CreateAiGameAsync(connectionId, effectivePlayerId, displayName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating AI game");
            await Clients.Caller.Error(ex.Message);
        }
    }

    /// <summary>
    /// Get list of points that have checkers that can be moved
    /// </summary>
    public async Task<List<int>> GetValidSources()
    {
        return _moveQueryService.GetValidSources(Context.ConnectionId);
    }

    /// <summary>
    /// Get list of valid destinations from a specific source point
    /// </summary>
    public async Task<List<MoveDto>> GetValidDestinations(int fromPoint)
    {
        return _moveQueryService.GetValidDestinations(Context.ConnectionId, fromPoint);
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
                await Clients.Caller.Error("Not in a game");
                return;
            }

            var result = await _gameActionOrchestrator.RollDiceAsync(session, Context.ConnectionId);
            if (!result.Success)
            {
                await Clients.Caller.Error(result.ErrorMessage ?? "An error occurred");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rolling dice");
            await Clients.Caller.Error(ex.Message);
        }
    }

    /// <summary>
    /// Execute a move from one point to another
    /// </summary>
    public async Task MakeMove(int from, int to)
    {
        try
        {
            var session = _sessionManager.GetGameByPlayer(Context.ConnectionId);
            if (session == null)
            {
                await Clients.Caller.Error("Not in a game");
                return;
            }

            var result = await _gameActionOrchestrator.MakeMoveAsync(session, Context.ConnectionId, from, to);
            if (!result.Success)
            {
                await Clients.Caller.Error(result.ErrorMessage ?? "An error occurred");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making move");
            await Clients.Caller.Error(ex.Message);
        }
    }

    /// <summary>
    /// Execute a combined move (using 2+ dice) atomically through intermediate points.
    /// Either all moves succeed or none are applied.
    /// </summary>
    /// <param name="from">Starting point</param>
    /// <param name="to">Final destination point</param>
    /// <param name="intermediatePoints">Points the checker passes through</param>
    public async Task MakeCombinedMove(int from, int to, int[] intermediatePoints)
    {
        try
        {
            var session = _sessionManager.GetGameByPlayer(Context.ConnectionId);
            if (session == null)
            {
                await Clients.Caller.Error("Not in a game");
                return;
            }

            var result = await _gameActionOrchestrator.MakeCombinedMoveAsync(
                session,
                Context.ConnectionId,
                from,
                to,
                intermediatePoints);

            if (!result.Success)
            {
                await Clients.Caller.Error(result.ErrorMessage ?? "An error occurred");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making combined move");
            await Clients.Caller.Error(ex.Message);
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
                await Clients.Caller.Error("Not in a game");
                return;
            }

            var result = await _gameActionOrchestrator.EndTurnAsync(session, Context.ConnectionId);
            if (!result.Success)
            {
                await Clients.Caller.Error(result.ErrorMessage ?? "An error occurred");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending turn");
            await Clients.Caller.Error(ex.Message);
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
                await Clients.Caller.Error("Not in a game");
                return;
            }

            var result = await _gameActionOrchestrator.UndoLastMoveAsync(session, Context.ConnectionId);
            if (!result.Success)
            {
                await Clients.Caller.Error(result.ErrorMessage ?? "An error occurred");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error undoing move");
            await Clients.Caller.Error(ex.Message);
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
                await Clients.Caller.Error("Not in a game");
                return;
            }

            var (success, currentValue, newValue, error) = await _doubleOfferService.OfferDoubleAsync(session, Context.ConnectionId);
            if (!success)
            {
                await Clients.Caller.Error(error ?? "Failed to offer double");
                return;
            }

            // Notify opponent of the double offer
            var opponentConnectionId = session.GetPlayerColor(Context.ConnectionId) == CheckerColor.White
                ? session.RedConnectionId
                : session.WhiteConnectionId;

            if (opponentConnectionId != null && !string.IsNullOrEmpty(opponentConnectionId))
            {
                await _gameService.BroadcastDoubleOfferAsync(session, Context.ConnectionId, currentValue, newValue);
            }
            else
            {
                // Opponent might be an AI (empty connection ID)
                var opponentPlayerId = session.GetPlayerColor(Context.ConnectionId) == CheckerColor.White
                    ? session.RedPlayerId
                    : session.WhitePlayerId;

                if (opponentPlayerId != null && _aiMoveService.IsAiPlayer(opponentPlayerId))
                {
                    var (accepted, winner, stakes) = await _doubleOfferService.HandleAiDoubleResponseAsync(
                        session, opponentPlayerId, currentValue, newValue);

                    if (accepted)
                    {
                        // AI accepted - send updated state to human player
                        if (!string.IsNullOrEmpty(Context.ConnectionId))
                        {
                            var state = session.GetState(Context.ConnectionId);
                            await Clients.Caller.DoubleAccepted(state);
                        }

                        BackgroundTaskHelper.FireAndForget(
                            async () =>
                            {
                                var game = GameEngineMapper.ToGame(session);
                                await _gameRepository.SaveGameAsync(game);
                            },
                            _logger,
                            $"SaveGameState-{session.Id}");
                    }
                    else
                    {
                        // AI declined - human wins
                        await Clients.Caller.Info("Computer declined the double. You win!");

                        // Update database and stats BEFORE broadcasting GameOver (prevents race condition)
                        await _gameRepository.UpdateGameStatusAsync(session.Id, "Completed");

                        if (session.GameMode.ShouldTrackStats)
                        {
                            var game = GameEngineMapper.ToGame(session);
                            await _playerStatsService.UpdateStatsAfterGameCompletionAsync(game);
                        }

                        _logger.LogInformation("Updated game {GameId} to Completed status and user stats", session.Id);

                        // Broadcast GameOver AFTER database is updated
                        if (!string.IsNullOrEmpty(Context.ConnectionId))
                        {
                            var finalState = session.GetState(Context.ConnectionId);
                            await Clients.Caller.GameOver(finalState);
                        }

                        _sessionManager.RemoveGame(session.Id);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error offering double");
            await Clients.Caller.Error(ex.Message);
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
                await Clients.Caller.Error("Not in a game");
                return;
            }

            // Accept the double
            await _doubleOfferService.AcceptDoubleAsync(session);

            // Broadcast double accepted to both players
            await _gameService.BroadcastDoubleAcceptedAsync(session);

            // Save game state
            BackgroundTaskHelper.FireAndForget(
                async () =>
                {
                    var game = GameEngineMapper.ToGame(session);
                    await _gameRepository.SaveGameAsync(game);
                },
                _logger,
                $"SaveGameState-{session.Id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting double");
            await Clients.Caller.Error(ex.Message);
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
                await Clients.Caller.Error("Not in a game");
                return;
            }

            var (success, winner, stakes, error) = await _doubleOfferService.DeclineDoubleAsync(session, Context.ConnectionId);
            if (!success)
            {
                await Clients.Caller.Error(error ?? "Failed to decline double");
                return;
            }

            // Update database and stats BEFORE broadcasting GameOver (prevents race condition)
            await _gameRepository.UpdateGameStatusAsync(session.Id, "Completed");

            if (session.GameMode.ShouldTrackStats)
            {
                var game = GameEngineMapper.ToGame(session);
                await _playerStatsService.UpdateStatsAfterGameCompletionAsync(game);
            }
            else
            {
                _logger.LogInformation("Skipping stats tracking for non-competitive game {GameId}", session.Id);
            }

            _logger.LogInformation("Updated game {GameId} to Completed status and user stats", session.Id);

            // Broadcast game over AFTER database is updated
            await _gameService.BroadcastGameOverAsync(session);

            // Remove from memory to prevent memory leak
            _sessionManager.RemoveGame(session.Id);
            _logger.LogInformation("Removed completed game {GameId} from memory (declined double)", session.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error declining double");
            await Clients.Caller.Error(ex.Message);
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
                await Clients.Caller.Error("Not in a game");
                return;
            }

            // Determine abandoning player and opponent
            var abandoningColor = session.GetPlayerColor(Context.ConnectionId);
            if (abandoningColor == null)
            {
                await Clients.Caller.Error("You are not a player in this game");
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

                // Remove game completely from session manager (no DB update needed - game was never persisted)
                _sessionManager.RemoveGame(gameId);

                _logger.LogInformation("Game {GameId} cancelled by player while waiting for opponent (removed from memory)", gameId);

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
                await Clients.Caller.Error("Game is already finished");
                return;
            }

            if (!session.Engine.GameStarted)
            {
                _logger.LogWarning("Game {GameId} hasn't started yet, cannot forfeit", session.Id);
                await Clients.Caller.Error("Game hasn't started yet");
                return;
            }

            // Forfeit game - set opponent as winner
            session.Engine.ForfeitGame(opponentPlayer);

            // Get stakes from doubling cube
            var stakes = session.Engine.GetGameResult();

            _logger.LogInformation(
                "Game {GameId} abandoned by {Player}. Winner: {Winner} (Stakes: {Stakes})",
                session.Id,
                abandoningPlayer.Name,
                opponentPlayer.Name,
                stakes);

            // Update database and stats BEFORE broadcasting GameOver (prevents race condition)
            await _gameRepository.UpdateGameStatusAsync(session.Id, "Abandoned");

            // Skip stats update for non-competitive games
            if (session.GameMode.ShouldTrackStats)
            {
                var game = GameEngineMapper.ToGame(session);
                await _playerStatsService.UpdateStatsAfterGameCompletionAsync(game);
            }
            else
            {
                _logger.LogInformation("Skipping stats tracking for analysis game {GameId}", session.Id);
            }

            _logger.LogInformation("Updated game {GameId} to Abandoned status and user stats", session.Id);

            // Broadcast game over AFTER database is updated
            var finalState = session.GetState();
            await Clients.Group(session.Id).GameOver(finalState);

            // Remove from memory to prevent memory leak
            _sessionManager.RemoveGame(session.Id);
            _logger.LogInformation("Removed abandoned game {GameId} from memory", session.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error abandoning game");
            await Clients.Caller.Error(ex.Message);
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
                await Clients.Caller.Error("Not in a game");
                return;
            }

            var state = session.GetState(Context.ConnectionId);
            await Clients.Caller.GameUpdate(state);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting game state");
            await Clients.Caller.Error(ex.Message);
        }
    }

    /// <summary>
    /// Send a chat message to all players in the game
    /// </summary>
    public async Task SendChatMessage(string message)
    {
        try
        {
            await _chatService.SendChatMessageAsync(Context.ConnectionId, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending chat message");
            await Clients.Caller.Error("Failed to send message");
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

        // Clean up chat rate limit history
        _chatService.CleanupConnection(Context.ConnectionId);

        await HandleDisconnection(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Export the current game position (base64-encoded SGF - used for URLs)
    /// </summary>
    public async Task<string> ExportPosition()
    {
        return await _gameImportExportService.ExportPositionAsync(Context.ConnectionId);
    }

    /// <summary>
    /// Import a position (auto-detects raw SGF or base64-encoded SGF)
    /// </summary>
    public async Task ImportPosition(string positionData)
    {
        await _gameImportExportService.ImportPositionAsync(Context.ConnectionId, positionData);
    }

    /// <summary>
    /// Move a checker directly from one point to another in analysis mode (bypasses game rules)
    /// </summary>
    public async Task MoveCheckerDirectly(int from, int to)
    {
        var session = _sessionManager.GetGameByPlayer(Context.ConnectionId);
        if (session == null)
        {
            await Clients.Caller.Error("You are not in a game");
            return;
        }

        // Only allow in analysis mode
        if (!session.IsAnalysisMode)
        {
            await Clients.Caller.Error("Direct moves only allowed in analysis mode");
            return;
        }

        // Validate basic constraints
        if (!IsValidDirectMove(session.Engine, from, to))
        {
            await Clients.Caller.Error("Invalid move: check piece placement rules");
            return;
        }

        try
        {
            // Execute move bypassing game rules
            ExecuteDirectMove(session.Engine, from, to);

            // Broadcast update to all connections
            await _gameService.BroadcastGameUpdateAsync(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing direct move in analysis mode");
            await Clients.Caller.Error("Failed to move checker");
        }
    }

    /// <summary>
    /// Set the current player in analysis mode
    /// </summary>
    public async Task SetCurrentPlayer(CheckerColor color)
    {
        var session = _sessionManager.GetGameByPlayer(Context.ConnectionId);
        if (session == null)
        {
            await Clients.Caller.Error("You are not in a game");
            return;
        }

        if (!session.IsAnalysisMode)
        {
            await Clients.Caller.Error("Can only set player in analysis mode");
            return;
        }

        try
        {
            // Update current player
            session.Engine.SetCurrentPlayer(color);

            // Clear remaining moves (reset turn state)
            session.Engine.RemainingMoves.Clear();

            await _gameService.BroadcastGameUpdateAsync(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting current player in analysis mode");
            await Clients.Caller.Error("Failed to set current player");
        }
    }

    /// <summary>
    /// Get player profile data including stats, recent games, and friends
    /// </summary>
    public async Task<PlayerProfileDto?> GetPlayerProfile(string username)
    {
        try
        {
            var viewingUserId = GetAuthenticatedUserId();
            var (profile, error) = await _playerProfileService.GetPlayerProfileAsync(username, viewingUserId);

            if (error != null)
            {
                await Clients.Caller.Error(error);
                return null;
            }

            return profile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting player profile");
            await Clients.Caller.Error("Failed to load profile");
            return null;
        }
    }

    // ==================== Friends Methods ====================

    /// <summary>
    /// Get the current user's friends list with online status
    /// </summary>
    public async Task<List<FriendDto>> GetFriends()
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("GetFriends called without authentication");
                return new List<FriendDto>();
            }

            var friends = await _friendService.GetFriendsAsync(userId);
            return friends;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting friends list");
            await Clients.Caller.Error("Failed to load friends");
            return new List<FriendDto>();
        }
    }

    /// <summary>
    /// Get pending friend requests for the current user
    /// </summary>
    public async Task<List<FriendDto>> GetFriendRequests()
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("GetFriendRequests called without authentication");
                return new List<FriendDto>();
            }

            var requests = await _friendService.GetPendingRequestsAsync(userId);
            return requests;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting friend requests");
            await Clients.Caller.Error("Failed to load friend requests");
            return new List<FriendDto>();
        }
    }

    /// <summary>
    /// Search for players by username
    /// </summary>
    public async Task<List<PlayerSearchResultDto>> SearchPlayers(string query)
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("SearchPlayers called without authentication");
                return new List<PlayerSearchResultDto>();
            }

            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                return new List<PlayerSearchResultDto>();
            }

            var users = await _userRepository.SearchUsersAsync(query);

            // Exclude the current user from search results
            var results = users
                .Where(u => u.UserId != userId)
                .Select(u => new PlayerSearchResultDto
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    DisplayName = u.DisplayName ?? u.Username
                })
                .ToList();

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching players");
            await Clients.Caller.Error("Failed to search players");
            return new List<PlayerSearchResultDto>();
        }
    }

    /// <summary>
    /// Send a friend request to another user
    /// </summary>
    public async Task<bool> SendFriendRequest(string toUserId)
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            if (string.IsNullOrEmpty(userId))
            {
                await Clients.Caller.Error("Must be logged in to send friend requests");
                return false;
            }

            var (success, error) = await _friendService.SendFriendRequestAsync(userId, toUserId);

            if (!success)
            {
                await Clients.Caller.Error(error ?? "Failed to send friend request");
                return false;
            }

            _logger.LogInformation("User {UserId} sent friend request to {ToUserId}", userId, toUserId);

            // Notify the recipient if they're online
            var recipientConnection = GetPlayerConnection(toUserId);
            if (!string.IsNullOrEmpty(recipientConnection))
            {
                await Clients.Client(recipientConnection).FriendRequestReceived();
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending friend request");
            await Clients.Caller.Error("Failed to send friend request");
            return false;
        }
    }

    /// <summary>
    /// Accept a friend request
    /// </summary>
    public async Task<bool> AcceptFriendRequest(string friendUserId)
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            if (string.IsNullOrEmpty(userId))
            {
                await Clients.Caller.Error("Must be logged in to accept friend requests");
                return false;
            }

            var (success, error) = await _friendService.AcceptFriendRequestAsync(userId, friendUserId);

            if (!success)
            {
                await Clients.Caller.Error(error ?? "Failed to accept friend request");
                return false;
            }

            _logger.LogInformation("User {UserId} accepted friend request from {FriendUserId}", userId, friendUserId);

            // Notify the requester if they're online
            var requesterConnection = GetPlayerConnection(friendUserId);
            if (!string.IsNullOrEmpty(requesterConnection))
            {
                await Clients.Client(requesterConnection).FriendRequestAccepted();
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting friend request");
            await Clients.Caller.Error("Failed to accept friend request");
            return false;
        }
    }

    /// <summary>
    /// Decline a friend request
    /// </summary>
    public async Task<bool> DeclineFriendRequest(string friendUserId)
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            if (string.IsNullOrEmpty(userId))
            {
                await Clients.Caller.Error("Must be logged in to decline friend requests");
                return false;
            }

            var (success, error) = await _friendService.DeclineFriendRequestAsync(userId, friendUserId);

            if (!success)
            {
                await Clients.Caller.Error(error ?? "Failed to decline friend request");
                return false;
            }

            _logger.LogInformation("User {UserId} declined friend request from {FriendUserId}", userId, friendUserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error declining friend request");
            await Clients.Caller.Error("Failed to decline friend request");
            return false;
        }
    }

    /// <summary>
    /// Remove a friend
    /// </summary>
    public async Task<bool> RemoveFriend(string friendUserId)
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            if (string.IsNullOrEmpty(userId))
            {
                await Clients.Caller.Error("Must be logged in to remove friends");
                return false;
            }

            var (success, error) = await _friendService.RemoveFriendAsync(userId, friendUserId);

            if (!success)
            {
                await Clients.Caller.Error(error ?? "Failed to remove friend");
                return false;
            }

            _logger.LogInformation("User {UserId} removed friend {FriendUserId}", userId, friendUserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing friend");
            await Clients.Caller.Error("Failed to remove friend");
            return false;
        }
    }

    public async Task ContinueMatch(string matchId)
    {
        try
        {
            var match = await _matchService.GetMatchAsync(matchId);
            if (match == null)
            {
                await Clients.Caller.Error("Match not found");
                return;
            }

            if (match.Status != "InProgress")
            {
                await Clients.Caller.Error("Match is not in progress");
                return;
            }

            var playerId = GetEffectivePlayerId(Context.ConnectionId);
            if (playerId != match.Player1Id && playerId != match.Player2Id)
            {
                await Clients.Caller.Error("You are not a player in this match");
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

                    _logger.LogInformation(
                        "Added AI player {AiPlayerId} to next match game {GameId}",
                        aiPlayerId,
                        nextGame.GameId);
                }
            }

            // Send match status update
            await Clients.Caller.MatchContinued(new MatchContinuedDto
            {
                MatchId = match.MatchId,
                GameId = nextGame.GameId,
                Player1Score = match.Player1Score,
                Player2Score = match.Player2Score,
                TargetScore = match.TargetScore,
                IsCrawfordGame = match.IsCrawfordGame
            });

            // Join the new game
            await JoinGame(playerId, nextGame.GameId);

            _logger.LogInformation(
                "Continued match {MatchId} with game {GameId}",
                matchId,
                nextGame.GameId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error continuing match");
            await Clients.Caller.Error(ex.Message);
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
                await Clients.Caller.Error("Match not found");
                return;
            }

            // Authorization: only participants can view match status
            if (match.Player1Id != playerId && match.Player2Id != playerId)
            {
                await Clients.Caller.Error("Access denied");
                _logger.LogWarning(
                    "Player {PlayerId} attempted to access match {MatchId} without authorization",
                    playerId,
                    matchId);
                return;
            }

            await Clients.Caller.MatchStatus(new MatchStatusDto
            {
                MatchId = match.MatchId,
                TargetScore = match.TargetScore,
                Player1Name = match.Player1Name ?? string.Empty,
                Player2Name = match.Player2Name ?? string.Empty,
                Player1Score = match.Player1Score,
                Player2Score = match.Player2Score,
                IsCrawfordGame = match.IsCrawfordGame,
                HasCrawfordGameBeenPlayed = match.HasCrawfordGameBeenPlayed,
                Status = match.Status,
                WinnerId = match.WinnerId,
                TotalGames = match.GameIds.Count,
                CurrentGameId = match.CurrentGameId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting match status");
            await Clients.Caller.Error(ex.Message);
        }
    }

    /// <summary>
    /// Get authoritative match state from server.
    /// Used to sync client state on reconnection and detect stale data.
    /// Returns match scores with a timestamp for staleness detection.
    /// </summary>
    /// <param name="matchId">The match ID to fetch state for</param>
    /// <returns>MatchStateDto with current scores and timestamp</returns>
    public async Task<MatchStateDto?> GetMatchState(string matchId)
    {
        try
        {
            var playerId = GetEffectivePlayerId(Context.ConnectionId);
            var match = await _matchService.GetMatchAsync(matchId);

            if (match == null)
            {
                _logger.LogWarning("GetMatchState: Match {MatchId} not found", matchId);
                await Clients.Caller.Error("Match not found");
                return null;
            }

            // Authorization: only participants can view match state
            if (match.Player1Id != playerId && match.Player2Id != playerId)
            {
                _logger.LogWarning(
                    "GetMatchState: Player {PlayerId} attempted to access match {MatchId} without authorization",
                    playerId,
                    matchId);
                await Clients.Caller.Error("Access denied");
                return null;
            }

            var matchState = MatchStateDto.FromMatch(match);

            _logger.LogDebug(
                "GetMatchState: Returning state for match {MatchId} - P1: {P1Score}, P2: {P2Score}, Updated: {UpdatedAt}",
                matchId,
                matchState.Player1Score,
                matchState.Player2Score,
                matchState.LastUpdatedAt);

            return matchState;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting match state for {MatchId}", matchId);
            await Clients.Caller.Error("Failed to get match state");
            return null;
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

            var matchList = matches.Select(m => new MatchSummaryDto
            {
                MatchId = m.MatchId,
                TargetScore = m.TargetScore,
                OpponentId = m.Player1Id == playerId ? m.Player2Id : m.Player1Id,
                OpponentName = m.Player1Id == playerId ? m.Player2Name : m.Player1Name,
                MyScore = m.Player1Id == playerId ? m.Player1Score : m.Player2Score,
                OpponentScore = m.Player1Id == playerId ? m.Player2Score : m.Player1Score,
                Status = m.Status,
                CreatedAt = m.CreatedAt,
                TotalGames = m.GameIds.Count
            }).ToList();

            await Clients.Caller.MyMatches(matchList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting player matches");
            await Clients.Caller.Error(ex.Message);
        }
    }

    /// <summary>
    /// Get match lobbies, optionally filtered by type
    /// </summary>
    /// <param name="lobbyType">Filter by type: "regular", "correspondence", or null for all</param>
    public async Task<List<object>> GetMatchLobbies(string? lobbyType = null)
    {
        try
        {
            // Parse lobby type filter
            bool? isCorrespondence = lobbyType?.ToLower() switch
            {
                "correspondence" => true,
                "regular" => false,
                _ => null
            };

            var lobbies = await _matchService.GetOpenLobbiesAsync(isCorrespondence: isCorrespondence);

            var lobbyList = lobbies.Select(m => new
            {
                matchId = m.MatchId,
                creatorPlayerId = m.Player1Id,
                creatorUsername = m.Player1Name,
                opponentType = m.OpponentType,
                targetScore = m.TargetScore,
                status = m.Status,  // Will be "WaitingForPlayers"
                opponentPlayerId = m.Player2Id,
                opponentUsername = m.Player2Name,
                createdAt = m.CreatedAt.ToString("O"),
                isOpenLobby = m.IsOpenLobby,
                isCorrespondence = m.IsCorrespondence,
                timePerMoveDays = m.TimePerMoveDays
            }).ToList<object>();

            _logger.LogDebug(
                "Retrieved {Count} match lobbies (filter: {LobbyType})",
                lobbyList.Count,
                lobbyType ?? "all");

            return lobbyList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting match lobbies");
            throw;
        }
    }

    public async Task<List<object>> GetRecentGames(int limit = 10)
    {
        try
        {
            var playerId = GetEffectivePlayerId(Context.ConnectionId);
            var matches = await _matchService.GetPlayerMatchesAsync(playerId, "Completed");

            var recentGames = matches.Take(limit).Select(m =>
            {
                var isPlayer1 = m.Player1Id == playerId;
                var opponentId = isPlayer1 ? m.Player2Id : m.Player1Id;
                var opponentName = isPlayer1 ? m.Player2Name : m.Player1Name;
                var myScore = isPlayer1 ? m.Player1Score : m.Player2Score;
                var opponentScore = isPlayer1 ? m.Player2Score : m.Player1Score;
                var didWin = myScore > opponentScore;

                return new
                {
                    matchId = m.MatchId,
                    opponentId = opponentId,
                    opponentName = opponentName ?? "Unknown",
                    opponentRating = 0, // TODO: Fetch from user profile when available
                    result = didWin ? "win" : "loss",
                    myScore = myScore,
                    opponentScore = opponentScore,
                    matchScore = $"{myScore}-{opponentScore}",
                    targetScore = m.TargetScore,
                    matchLength = $"{m.TargetScore}-point",
                    timeControl = "Standard", // TODO: Add time control to Match model
                    ratingChange = 0, // TODO: Calculate rating change when rating system is implemented
                    completedAt = m.CompletedAt,
                    createdAt = m.CreatedAt
                };
            }).ToList<object>();

            return recentGames;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent games for player");
            throw;
        }
    }

    public async Task<List<object>> GetActiveGames(int limit = 10)
    {
        try
        {
            var playerId = GetEffectivePlayerId(Context.ConnectionId);
            var matches = await _matchService.GetPlayerMatchesAsync(playerId, "InProgress");

            var activeGames = matches.Take(limit).Select(m =>
            {
                var isPlayer1 = m.Player1Id == playerId;
                var opponentId = isPlayer1 ? m.Player2Id : m.Player1Id;
                var opponentName = isPlayer1 ? m.Player2Name : m.Player1Name;
                var myColor = isPlayer1 ? "White" : "Red";

                // Try to get current game state if there's an active game session
                var currentGameId = m.CurrentGameId;
                var gameSession = currentGameId != null ? _sessionManager.GetSession(currentGameId) : null;

                var currentPlayer = gameSession?.Engine?.CurrentPlayer.ToString() ?? "White";
                var isYourTurn = currentPlayer == myColor;

                // Get board state if session exists
                object[]? boardState = null;
                int whiteOnBar = 0;
                int redOnBar = 0;
                int whiteBornOff = 0;
                int redBornOff = 0;

                int[]? diceValues = null;
                int cubeValue = 1;
                string cubeOwner = "Center";

                if (gameSession?.Engine != null)
                {
                    var board = new List<object>();
                    for (int i = 1; i <= 24; i++)
                    {
                        var point = gameSession.Engine.Board.GetPoint(i);
                        board.Add(new
                        {
                            position = i,
                            color = point.Color?.ToString(),
                            count = point.Count
                        });
                    }

                    boardState = board.ToArray();
                    whiteOnBar = gameSession.Engine.WhitePlayer.CheckersOnBar;
                    redOnBar = gameSession.Engine.RedPlayer.CheckersOnBar;
                    whiteBornOff = gameSession.Engine.WhitePlayer.CheckersBornOff;
                    redBornOff = gameSession.Engine.RedPlayer.CheckersBornOff;

                    // Get dice values if rolled
                    if (gameSession.Engine.Dice?.Die1 > 0 && gameSession.Engine.Dice?.Die2 > 0)
                    {
                        diceValues = new[] { gameSession.Engine.Dice.Die1, gameSession.Engine.Dice.Die2 };
                    }

                    // Get cube info
                    cubeValue = gameSession.Engine.DoublingCube?.Value ?? 1;
                    cubeOwner = gameSession.Engine.DoublingCube?.Owner?.ToString() ?? "Center";
                }

                return new
                {
                    matchId = m.MatchId,
                    gameId = currentGameId,
                    player1Name = m.Player1Name ?? "Player 1",
                    player2Name = m.Player2Name ?? "Player 2",
                    player1Rating = 0, // TODO: Fetch from user profile
                    player2Rating = 0, // TODO: Fetch from user profile
                    currentPlayer,
                    myColor,
                    isYourTurn,
                    matchScore = $"{m.Player1Score}-{m.Player2Score}",
                    matchLength = m.TargetScore,
                    timeControl = "Standard", // TODO: Add time control to Match model
                    cubeValue,
                    cubeOwner,
                    isCrawford = m.IsCrawfordGame,
                    viewers = 0, // TODO: Add spectator tracking
                    board = boardState,
                    whiteCheckersOnBar = whiteOnBar,
                    redCheckersOnBar = redOnBar,
                    whiteBornOff,
                    redBornOff,
                    dice = diceValues
                };
            }).ToList<object>();

            return activeGames;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active games for player");
            throw;
        }
    }

    /// <summary>
    /// Get recent opponents for the current player with head-to-head records
    /// </summary>
    /// <param name="limit">Maximum number of opponents to return</param>
    /// <param name="includeAi">Whether to include AI opponents</param>
    /// <returns>List of recent opponents with their statistics</returns>
    public async Task<List<RecentOpponentDto>> GetRecentOpponents(int limit = 10, bool includeAi = false)
    {
        try
        {
            var playerId = GetEffectivePlayerId(Context.ConnectionId);

            // Get all completed matches for the player
            var matches = await _matchService.GetPlayerMatchesAsync(playerId, "Completed");

            // Group matches by opponent and aggregate statistics
            var opponentStats = new Dictionary<string, (string Name, int Wins, int Losses, DateTime LastPlayed, bool IsAi)>();

            foreach (var match in matches)
            {
                var isPlayer1 = match.Player1Id == playerId;
                var opponentId = isPlayer1 ? match.Player2Id : match.Player1Id;
                var opponentName = isPlayer1 ? match.Player2Name : match.Player1Name;
                var myScore = isPlayer1 ? match.Player1Score : match.Player2Score;
                var opponentScore = isPlayer1 ? match.Player2Score : match.Player1Score;
                var isAi = match.OpponentType == "AI";

                // Skip if opponent ID is missing
                if (string.IsNullOrEmpty(opponentId))
                {
                    continue;
                }

                // Skip AI opponents if not requested
                if (isAi && !includeAi)
                {
                    continue;
                }

                var matchTime = match.CompletedAt ?? match.CreatedAt;
                var didWin = myScore > opponentScore;

                if (opponentStats.TryGetValue(opponentId, out var existing))
                {
                    opponentStats[opponentId] = (
                        existing.Name,
                        existing.Wins + (didWin ? 1 : 0),
                        existing.Losses + (didWin ? 0 : 1),
                        matchTime > existing.LastPlayed ? matchTime : existing.LastPlayed,
                        isAi);
                }
                else
                {
                    opponentStats[opponentId] = (
                        opponentName ?? "Unknown",
                        didWin ? 1 : 0,
                        didWin ? 0 : 1,
                        matchTime,
                        isAi);
                }
            }

            // Sort by most recent and take the limit
            var recentOpponents = opponentStats
                .OrderByDescending(kvp => kvp.Value.LastPlayed)
                .Take(limit)
                .ToList();

            // Get opponent ratings from user profiles (non-AI only)
            var nonAiOpponentIds = recentOpponents
                .Where(kvp => !kvp.Value.IsAi)
                .Select(kvp => kvp.Key)
                .ToList();

            var opponentUsers = nonAiOpponentIds.Count > 0
                ? await _userRepository.GetUsersByIdsAsync(nonAiOpponentIds)
                : new List<User>();

            var userRatings = opponentUsers.ToDictionary(
                u => u.UserId,
                u => u?.Rating ?? 1500);

            // Build the result DTOs
            var result = recentOpponents.Select(kvp => new RecentOpponentDto
            {
                OpponentId = kvp.Key,
                OpponentName = kvp.Value.Name,
                OpponentRating = kvp.Value.IsAi ? 0 : (userRatings.TryGetValue(kvp.Key, out var rating) ? rating : 0),
                TotalMatches = kvp.Value.Wins + kvp.Value.Losses,
                Wins = kvp.Value.Wins,
                Losses = kvp.Value.Losses,
                LastPlayedAt = kvp.Value.LastPlayed,
                IsAi = kvp.Value.IsAi
            }).ToList();

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent opponents for player");
            throw;
        }
    }

    /// <summary>
    /// Create a new match with configuration (lobby-based)
    /// </summary>
    /// <summary>
    /// Create a new match and immediately create the first game
    /// </summary>
    public async Task CreateMatch(MatchConfig config)
    {
        try
        {
            var playerId = GetEffectivePlayerId(Context.ConnectionId);

            // Parse time control type
            Core.TimeControlConfig? timeControl = null;
            if (!string.IsNullOrEmpty(config.TimeControlType) && config.TimeControlType != "None")
            {
                if (Enum.TryParse<Core.TimeControlType>(config.TimeControlType, out var timeControlType))
                {
                    timeControl = new Core.TimeControlConfig
                    {
                        Type = timeControlType,
                        DelaySeconds = timeControlType == Core.TimeControlType.ChicagoPoint ? 12 : 0
                    };
                }
            }

            // Create match and first game immediately
            var (match, firstGame) = await _matchService.CreateMatchAsync(
                playerId,
                config.TargetScore,
                config.OpponentType,
                config.DisplayName,
                config.OpponentId,
                timeControl,
                config.IsRated,
                config.AiType);

            // Send MatchCreated event with game ID
            await Clients.Caller.MatchCreated(new MatchCreatedDto
            {
                MatchId = match.MatchId,
                GameId = firstGame.GameId,
                TargetScore = match.TargetScore,
                OpponentType = match.OpponentType ?? string.Empty,
                Player1Id = match.Player1Id,
                Player2Id = match.Player2Id,
                Player1Name = match.Player1Name ?? string.Empty,
                Player2Name = match.Player2Name
            });

            _logger.LogInformation(
                "Match {MatchId} created for player {PlayerId} (type: {OpponentType}), first game: {GameId}",
                match.MatchId,
                playerId,
                config.OpponentType,
                firstGame.GameId);

            // For OpenLobby, broadcast to all clients that a new lobby is available
            if (config.OpponentType == "OpenLobby")
            {
                await Clients.All.LobbyCreated(new LobbyCreatedDto
                {
                    MatchId = match.MatchId,
                    GameId = firstGame.GameId,
                    CreatorName = match.Player1Name ?? string.Empty,
                    TargetScore = match.TargetScore,
                    IsRated = match.IsRated
                });

                _logger.LogInformation(
                    "Broadcast LobbyCreated event for match {MatchId} (isRated: {IsRated})",
                    match.MatchId,
                    match.IsRated);
            }

            // For friend matches, notify the friend if they're online
            if (config.OpponentType == "Friend" && !string.IsNullOrEmpty(config.OpponentId))
            {
                if (_sessionManager.IsPlayerOnline(config.OpponentId))
                {
                    var opponentConnection = GetPlayerConnection(config.OpponentId);
                    if (!string.IsNullOrEmpty(opponentConnection))
                    {
                        await Clients.Client(opponentConnection).MatchInvite(new MatchInviteDto
                        {
                            MatchId = match.MatchId,
                            GameId = firstGame.GameId,
                            TargetScore = match.TargetScore,
                            ChallengerName = match.Player1Name ?? string.Empty,
                            ChallengerId = match.Player1Id
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating match");
            await Clients.Caller.Error(ex.Message);
        }
    }

    // Legacy method for backwards compatibility - redirects to CreateMatch
    public async Task CreateMatchWithConfig(MatchConfig config)
    {
        await CreateMatch(config);
    }

    /// <summary>
    /// Join an existing match as player 2
    /// </summary>
    public async Task JoinMatch(string matchId)
    {
        try
        {
            var playerId = GetEffectivePlayerId(Context.ConnectionId);
            var displayName = GetAuthenticatedDisplayName();

            // Track this player's connection
            _playerConnectionService.AddConnection(playerId, Context.ConnectionId);

            // Join the match
            var match = await _matchService.JoinMatchAsync(matchId, playerId, displayName);

            // Send MatchCreated to joiner (navigates to game page)
            await Clients.Caller.MatchCreated(new MatchCreatedDto
            {
                MatchId = match.MatchId,
                GameId = match.CurrentGameId ?? string.Empty,
                TargetScore = match.TargetScore,
                OpponentType = match.OpponentType ?? string.Empty,
                Player1Id = match.Player1Id,
                Player2Id = match.Player2Id,
                Player1Name = match.Player1Name ?? string.Empty,
                Player2Name = match.Player2Name
            });

            // Notify creator that opponent joined
            var creatorConnection = GetPlayerConnection(match.Player1Id);
            if (!string.IsNullOrEmpty(creatorConnection))
            {
                await Clients.Client(creatorConnection).OpponentJoinedMatch(new OpponentJoinedMatchDto
                {
                    MatchId = match.MatchId,
                    Player2Id = match.Player2Id ?? string.Empty,
                    Player2Name = match.Player2Name ?? string.Empty
                });
            }

            // For correspondence matches, notify Player1 it's their turn
            if (match.IsCorrespondence && _sessionManager.IsPlayerOnline(match.Player1Id))
            {
                var player1Connection = GetPlayerConnection(match.Player1Id);
                if (!string.IsNullOrEmpty(player1Connection))
                {
                    await Clients.Client(player1Connection).CorrespondenceTurnNotification(
                        new CorrespondenceTurnNotificationDto
                        {
                            MatchId = match.MatchId,
                            GameId = match.CurrentGameId,
                            Message = "Opponent joined! It's your turn."
                        });
                }
            }

            _logger.LogInformation("Player {PlayerId} joined match {MatchId}", playerId, matchId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining match {MatchId}", matchId);
            await Clients.Caller.Error(ex.Message);
        }
    }

    /// <summary>
    /// Analyze the current position and return evaluation
    /// </summary>
    /// <param name="gameId">The game ID to analyze</param>
    /// <param name="evaluatorType">Optional evaluator type ("Heuristic" or "Gnubg"). If null, uses default from settings.</param>
    public async Task<PositionEvaluationDto> AnalyzePosition(string gameId, string? evaluatorType)
    {
        var session = _sessionManager.GetSession(gameId);
        if (session == null)
        {
            throw new HubException("Game not found");
        }

        return await _analysisService.EvaluatePositionAsync(session.Engine, evaluatorType);
    }

    /// <summary>
    /// Find the best moves for the current position
    /// </summary>
    /// <param name="gameId">The game ID to analyze</param>
    /// <param name="evaluatorType">Optional evaluator type ("Heuristic" or "Gnubg"). If null, uses default from settings.</param>
    public async Task<BestMovesAnalysisDto> FindBestMoves(string gameId, string? evaluatorType)
    {
        var session = _sessionManager.GetSession(gameId);
        if (session == null)
        {
            throw new HubException("Game not found");
        }

        if (session.Engine.RemainingMoves.Count == 0)
        {
            throw new HubException("No dice rolled - cannot analyze moves");
        }

        return await _analysisService.FindBestMovesAsync(session.Engine, evaluatorType);
    }

    // ==================== Correspondence Game Methods ====================

    /// <summary>
    /// Get all correspondence games for the current user
    /// </summary>
    public async Task<CorrespondenceGamesResponse> GetCorrespondenceGames()
    {
        try
        {
            var playerId = GetEffectivePlayerId(Context.ConnectionId);
            var response = await _correspondenceGameService.GetAllCorrespondenceGamesAsync(playerId);

            _logger.LogInformation(
                "Retrieved correspondence games for player {PlayerId}: {YourTurn} your turn, {Waiting} waiting",
                playerId,
                response.TotalYourTurn,
                response.TotalWaiting);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting correspondence games");
            throw new HubException("Failed to retrieve correspondence games");
        }
    }

    /// <summary>
    /// Create a new correspondence match
    /// </summary>
    public async Task CreateCorrespondenceMatch(MatchConfig config)
    {
        try
        {
            var playerId = GetEffectivePlayerId(Context.ConnectionId);

            // Validate correspondence-specific settings
            if (!config.IsCorrespondence)
            {
                throw new ArgumentException("IsCorrespondence must be true for correspondence matches");
            }

            if (config.TimePerMoveDays <= 0 || config.TimePerMoveDays > 30)
            {
                throw new ArgumentException("TimePerMoveDays must be between 1 and 30");
            }

            // Create correspondence match
            var (match, firstGame) = await _correspondenceGameService.CreateCorrespondenceMatchAsync(
                playerId,
                config.TargetScore,
                config.TimePerMoveDays,
                config.OpponentType,
                config.DisplayName,
                config.OpponentId,
                config.IsRated);

            // Send MatchCreated event with game ID
            await Clients.Caller.MatchCreated(new MatchCreatedDto
            {
                MatchId = match.MatchId,
                GameId = firstGame.GameId,
                TargetScore = match.TargetScore,
                OpponentType = match.OpponentType ?? string.Empty,
                Player1Id = match.Player1Id,
                Player2Id = match.Player2Id,
                Player1Name = match.Player1Name ?? string.Empty,
                Player2Name = match.Player2Name,
                IsCorrespondence = true,
                TimePerMoveDays = match.TimePerMoveDays,
                TurnDeadline = match.TurnDeadline
            });

            _logger.LogInformation(
                "Correspondence match {MatchId} created for player {PlayerId}, time per move: {TimePerMove} days",
                match.MatchId,
                playerId,
                match.TimePerMoveDays);

            // For OpenLobby, broadcast to all clients that a new lobby is available
            if (config.OpponentType == "OpenLobby")
            {
                await Clients.All.CorrespondenceLobbyCreated(new CorrespondenceLobbyCreatedDto
                {
                    MatchId = match.MatchId,
                    GameId = firstGame.GameId,
                    CreatorPlayerId = match.Player1Id,
                    CreatorUsername = match.Player1Name ?? string.Empty,
                    TargetScore = match.TargetScore,
                    TimePerMoveDays = match.TimePerMoveDays,
                    IsRated = match.IsRated
                });

                _logger.LogInformation(
                    "Broadcast CorrespondenceLobbyCreated event for match {MatchId} (isRated: {IsRated})",
                    match.MatchId,
                    match.IsRated);
            }

            // For friend matches, notify the friend if they're online
            if (config.OpponentType == "Friend"
                && !string.IsNullOrEmpty(config.OpponentId)
                && _sessionManager.IsPlayerOnline(config.OpponentId))
            {
                var opponentConnection = GetPlayerConnection(config.OpponentId);
                if (!string.IsNullOrEmpty(opponentConnection))
                {
                    await Clients.Client(opponentConnection).CorrespondenceMatchInvite(
                        new CorrespondenceMatchInviteDto
                        {
                            MatchId = match.MatchId,
                            GameId = firstGame.GameId,
                            TargetScore = match.TargetScore,
                            ChallengerName = match.Player1Name ?? string.Empty,
                            ChallengerId = match.Player1Id,
                            TimePerMoveDays = match.TimePerMoveDays
                        });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating correspondence match");
            await Clients.Caller.Error(ex.Message);
        }
    }

    /// <summary>
    /// Notify that a turn has been completed in a correspondence game.
    /// This hub method should be called explicitly by the client after EndTurn in correspondence matches
    /// to inform the server that the turn has been completed and to trigger notification of the next player.
    /// </summary>
    public async Task NotifyCorrespondenceTurnComplete(string matchId, string nextPlayerId)
    {
        try
        {
            await _correspondenceGameService.HandleTurnCompletedAsync(matchId, nextPlayerId);

            // Notify next player if they're online
            if (_sessionManager.IsPlayerOnline(nextPlayerId))
            {
                var nextPlayerConnection = GetPlayerConnection(nextPlayerId);
                if (!string.IsNullOrEmpty(nextPlayerConnection))
                {
                    await Clients.Client(nextPlayerConnection).CorrespondenceTurnNotification(
                        new CorrespondenceTurnNotificationDto
                        {
                            MatchId = matchId,
                            Message = "It's your turn!"
                        });
                }
            }

            _logger.LogInformation(
                "Correspondence turn completed for match {MatchId}, next player: {NextPlayerId}",
                matchId,
                nextPlayerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying correspondence turn completion for match {MatchId}", matchId);
            await Clients.Caller.Error(ex.Message);
        }
    }

    // ==================== Daily Puzzle Methods ====================

    /// <summary>
    /// Get today's daily puzzle.
    /// </summary>
    /// <returns>The daily puzzle DTO, or null if no puzzle exists for today.</returns>
    public async Task<DailyPuzzleDto?> GetDailyPuzzle()
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            return await _dailyPuzzleService.GetTodaysPuzzleAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily puzzle");
            throw new HubException("Failed to get daily puzzle");
        }
    }

    /// <summary>
    /// Submit an answer to today's puzzle.
    /// </summary>
    /// <param name="moves">The moves the user played.</param>
    /// <returns>The result of the puzzle submission.</returns>
    public async Task<PuzzleResultDto> SubmitPuzzleAnswer(List<MoveDto> moves)
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            if (string.IsNullOrEmpty(userId))
            {
                throw new HubException("Authentication required");
            }

            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            return await _dailyPuzzleService.SubmitAnswerAsync(userId, today, moves);
        }
        catch (HubException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting puzzle answer");
            throw new HubException("Failed to submit puzzle answer");
        }
    }

    /// <summary>
    /// Give up on today's puzzle and reveal the answer.
    /// </summary>
    /// <returns>The result with the best moves revealed.</returns>
    public async Task<PuzzleResultDto> GiveUpPuzzle()
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            if (string.IsNullOrEmpty(userId))
            {
                throw new HubException("Authentication required");
            }

            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            return await _dailyPuzzleService.GiveUpPuzzleAsync(userId, today);
        }
        catch (HubException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error giving up on puzzle");
            throw new HubException("Failed to give up on puzzle");
        }
    }

    /// <summary>
    /// Get user's puzzle streak information.
    /// </summary>
    /// <returns>The user's puzzle streak info.</returns>
    public async Task<PuzzleStreakInfo> GetPuzzleStreak()
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return new PuzzleStreakInfo();
            }

            return await _dailyPuzzleService.GetStreakInfoAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting puzzle streak");
            throw new HubException("Failed to get puzzle streak");
        }
    }

    /// <summary>
    /// Get a historical puzzle by date.
    /// </summary>
    /// <param name="date">Date in yyyy-MM-dd format.</param>
    /// <returns>The puzzle for the specified date, or null if not found.</returns>
    public async Task<DailyPuzzleDto?> GetHistoricalPuzzle(string date)
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            return await _dailyPuzzleService.GetPuzzleByDateAsync(date, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting historical puzzle for date {Date}", date);
            throw new HubException("Failed to get historical puzzle");
        }
    }

    /// <summary>
    /// Get valid moves for a puzzle position with pending moves applied.
    /// Creates a temporary GameEngine, applies the position and pending moves,
    /// then returns valid moves using the engine's rules.
    /// </summary>
    /// <param name="request">The puzzle position with pending moves.</param>
    /// <returns>List of valid moves from the current position.</returns>
    public Task<List<MoveDto>> GetPuzzleValidMoves(PuzzleValidMovesRequest request)
    {
        try
        {
            var engine = new GameEngine();

            // Clear board first
            for (int i = 1; i <= 24; i++)
            {
                engine.Board.GetPoint(i).Checkers.Clear();
            }

            engine.WhitePlayer.CheckersOnBar = 0;
            engine.WhitePlayer.CheckersBornOff = 0;
            engine.RedPlayer.CheckersOnBar = 0;
            engine.RedPlayer.CheckersBornOff = 0;

            // Apply board state
            foreach (var pointState in request.BoardState)
            {
                if (pointState.Count > 0 && !string.IsNullOrEmpty(pointState.Color))
                {
                    var color = pointState.Color.Equals("White", StringComparison.OrdinalIgnoreCase)
                        ? CheckerColor.White
                        : CheckerColor.Red;
                    var point = engine.Board.GetPoint(pointState.Position);
                    for (int i = 0; i < pointState.Count; i++)
                    {
                        point.AddChecker(color);
                    }
                }
            }

            // Apply bar and bear-off counts
            engine.WhitePlayer.CheckersOnBar = request.WhiteCheckersOnBar;
            engine.RedPlayer.CheckersOnBar = request.RedCheckersOnBar;
            engine.WhitePlayer.CheckersBornOff = request.WhiteBornOff;
            engine.RedPlayer.CheckersBornOff = request.RedBornOff;

            // Set current player
            var currentPlayer = request.CurrentPlayer.Equals("White", StringComparison.OrdinalIgnoreCase)
                ? CheckerColor.White
                : CheckerColor.Red;
            engine.SetCurrentPlayer(currentPlayer);

            // Set dice and remaining moves
            if (request.Dice.Length >= 2)
            {
                engine.Dice.SetDice(request.Dice[0], request.Dice[1]);
            }

            // Build remaining moves: start with full dice values
            var remainingDice = new List<int>(engine.Dice.GetMoves());

            // Remove dice used by pending moves
            foreach (var pendingMove in request.PendingMoves)
            {
                // Remove the die value used
                var dieIndex = remainingDice.IndexOf(pendingMove.DieValue);
                if (dieIndex >= 0)
                {
                    remainingDice.RemoveAt(dieIndex);
                }
            }

            engine.RemainingMoves.Clear();
            engine.RemainingMoves.AddRange(remainingDice);

            // Mark game as started
            engine.SetGameStarted(true);

            // Apply pending moves to board state
            foreach (var pendingMove in request.PendingMoves)
            {
                ApplyPendingMoveToBoard(engine, pendingMove, currentPlayer);
            }

            // Get valid moves from the engine
            var validMoves = engine.GetValidMoves();

            // Convert to DTOs
            var moveDtos = validMoves.Select(m => new MoveDto
            {
                From = m.From,
                To = m.To,
                DieValue = m.DieValue,
                IsHit = m.IsHit
            }).ToList();

            return Task.FromResult(moveDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting puzzle valid moves");
            throw new HubException("Failed to get puzzle valid moves");
        }
    }

    // ==================== Players Page Methods ====================

    /// <summary>
    /// Get the leaderboard with top players by rating.
    /// </summary>
    /// <param name="limit">Maximum number of players to return (default 50).</param>
    /// <returns>List of leaderboard entries.</returns>
    public async Task<List<LeaderboardEntryDto>> GetLeaderboard(int limit = 50)
    {
        try
        {
            var topPlayers = await _userRepository.GetTopPlayersByRatingAsync(limit);
            var onlinePlayerIds = _playerConnectionService.GetAllConnectedPlayerIds().ToHashSet();

            var leaderboard = topPlayers.Select((user, index) => new LeaderboardEntryDto
            {
                Rank = index + 1,
                UserId = user.UserId,
                Username = user.Username,
                DisplayName = user.DisplayName,
                Rating = user.Rating,
                TotalGames = user.Stats.TotalGames,
                Wins = user.Stats.Wins,
                Losses = user.Stats.Losses,
                WinRate = user.Stats.TotalGames > 0
                    ? Math.Round((double)user.Stats.Wins / user.Stats.TotalGames * 100, 1)
                    : 0,
                IsOnline = onlinePlayerIds.Contains(user.UserId)
            }).ToList();

            _logger.LogDebug("Retrieved leaderboard with {Count} players", leaderboard.Count);
            return leaderboard;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting leaderboard");
            throw new HubException("Failed to get leaderboard");
        }
    }

    /// <summary>
    /// Get list of currently online players.
    /// </summary>
    /// <returns>List of online players.</returns>
    public async Task<List<OnlinePlayerDto>> GetOnlinePlayers()
    {
        try
        {
            var currentUserId = GetAuthenticatedUserId();
            var onlinePlayerIds = _playerConnectionService.GetAllConnectedPlayerIds().ToList();

            if (!onlinePlayerIds.Any())
            {
                return new List<OnlinePlayerDto>();
            }

            var users = await _userRepository.GetUsersByIdsAsync(onlinePlayerIds);

            // Get friends list for the current user to mark friends
            var friends = new HashSet<string>();
            if (!string.IsNullOrEmpty(currentUserId))
            {
                var friendsList = await _friendService.GetFriendsAsync(currentUserId);
                friends = friendsList.Select(f => f.UserId).ToHashSet();
            }

            var onlinePlayers = users
                .Where(u => !u.IsAnonymous && u.UserId != currentUserId)
                .Select(user =>
                {
                    // Check if user is in a game
                    var playerGames = _sessionManager.GetPlayerGames(user.UserId);
                    var activeGame = playerGames.FirstOrDefault(g => !g.Engine.GameOver);

                    return new OnlinePlayerDto
                    {
                        UserId = user.UserId,
                        Username = user.Username,
                        DisplayName = user.DisplayName,
                        Rating = user.Rating,
                        Status = activeGame != null ? OnlinePlayerStatus.InGame : OnlinePlayerStatus.Available,
                        CurrentGameId = activeGame?.Id,
                        IsFriend = friends.Contains(user.UserId)
                    };
                })
                .OrderByDescending(p => p.IsFriend)
                .ThenByDescending(p => p.Rating)
                .ToList();

            _logger.LogDebug("Retrieved {Count} online players", onlinePlayers.Count);
            return onlinePlayers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting online players");
            throw new HubException("Failed to get online players");
        }
    }

    /// <summary>
    /// Get rating distribution statistics.
    /// </summary>
    /// <returns>Rating distribution data.</returns>
    public async Task<RatingDistributionDto> GetRatingDistribution()
    {
        try
        {
            var currentUserId = GetAuthenticatedUserId();
            var allRatings = await _userRepository.GetAllRatingsAsync();

            if (!allRatings.Any())
            {
                return new RatingDistributionDto
                {
                    Buckets = new List<RatingBucketDto>(),
                    TotalPlayers = 0,
                    AverageRating = 0,
                    MedianRating = 0
                };
            }

            var sortedRatings = allRatings.OrderBy(r => r).ToList();
            var totalPlayers = sortedRatings.Count;
            var averageRating = sortedRatings.Average();
            var medianRating = sortedRatings[totalPlayers / 2];

            // Get current user's rating if authenticated
            int? userRating = null;
            double? userPercentile = null;
            if (!string.IsNullOrEmpty(currentUserId))
            {
                var user = await _userRepository.GetByUserIdAsync(currentUserId);
                if (user != null && user.RatedGamesCount > 0)
                {
                    userRating = user.Rating;
                    var playersBelow = sortedRatings.Count(r => r < user.Rating);
                    userPercentile = Math.Round((double)playersBelow / totalPlayers * 100, 1);
                }
            }

            // Create buckets (100-point ranges)
            var minRating = (sortedRatings.Min() / 100) * 100;
            var maxRating = ((sortedRatings.Max() / 100) + 1) * 100;
            var buckets = new List<RatingBucketDto>();

            for (int bucketStart = minRating; bucketStart < maxRating; bucketStart += 100)
            {
                var bucketEnd = bucketStart + 100;
                var count = sortedRatings.Count(r => r >= bucketStart && r < bucketEnd);
                var isUserBucket = userRating.HasValue && userRating >= bucketStart && userRating < bucketEnd;

                buckets.Add(new RatingBucketDto
                {
                    MinRating = bucketStart,
                    MaxRating = bucketEnd,
                    Label = $"{bucketStart}-{bucketEnd - 1}",
                    Count = count,
                    Percentage = Math.Round((double)count / totalPlayers * 100, 1),
                    IsUserBucket = isUserBucket
                });
            }

            return new RatingDistributionDto
            {
                Buckets = buckets,
                UserRating = userRating,
                UserPercentile = userPercentile,
                TotalPlayers = totalPlayers,
                AverageRating = Math.Round(averageRating, 1),
                MedianRating = medianRating
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rating distribution");
            throw new HubException("Failed to get rating distribution");
        }
    }

    /// <summary>
    /// Get list of available AI bots.
    /// </summary>
    /// <returns>List of available bots.</returns>
    public Task<List<BotInfoDto>> GetAvailableBots()
    {
        var bots = new List<BotInfoDto>
        {
            new BotInfoDto
            {
                Id = "random",
                Name = "Random Bot",
                Description = "Makes completely random moves. Great for beginners learning the game.",
                Difficulty = 1,
                IsAvailable = true,
                Icon = "dice"
            },
            new BotInfoDto
            {
                Id = "greedy",
                Name = "Greedy Bot",
                Description = "Prioritizes hitting blots and bearing off. A solid intermediate challenge.",
                Difficulty = 3,
                IsAvailable = true,
                Icon = "target"
            }
        };

        _logger.LogDebug("Retrieved {Count} available bots", bots.Count);
        return Task.FromResult(bots);
    }

    /// <summary>
    /// Generate a consistent anonymous display name from a player ID.
    /// </summary>
    /// <param name="playerId">The player ID to generate a name from.</param>
    /// <returns>The generated anonymous display name.</returns>
    private static string GenerateAnonymousDisplayName(string playerId)
    {
        // Extract the random suffix (last part after final underscore)
        var parts = playerId.Split('_');
        var suffix = parts.Length > 0 ? parts[^1] : "unknown";
        return $"Anonymous-{suffix[..Math.Min(6, suffix.Length)]}";
    }

    /// <summary>
    /// Apply a pending puzzle move to the board state.
    /// </summary>
    private static void ApplyPendingMoveToBoard(GameEngine engine, MoveDto move, CheckerColor movingColor)
    {
        var opponent = movingColor == CheckerColor.White ? CheckerColor.Red : CheckerColor.White;

        // Remove from source
        if (move.From == 0)
        {
            // From bar
            if (movingColor == CheckerColor.White)
            {
                engine.WhitePlayer.CheckersOnBar--;
            }
            else
            {
                engine.RedPlayer.CheckersOnBar--;
            }
        }
        else
        {
            var sourcePoint = engine.Board.GetPoint(move.From);
            if (sourcePoint.Count > 0)
            {
                sourcePoint.RemoveChecker();
            }
        }

        // Handle destination
        if (move.To == 0 || move.To == 25)
        {
            // Bear off
            if (movingColor == CheckerColor.White)
            {
                engine.WhitePlayer.CheckersBornOff++;
            }
            else
            {
                engine.RedPlayer.CheckersBornOff++;
            }
        }
        else
        {
            var destPoint = engine.Board.GetPoint(move.To);

            // Handle hit
            if (move.IsHit && destPoint.Count == 1 && destPoint.Color == opponent)
            {
                destPoint.RemoveChecker();
                if (opponent == CheckerColor.White)
                {
                    engine.WhitePlayer.CheckersOnBar++;
                }
                else
                {
                    engine.RedPlayer.CheckersOnBar++;
                }
            }

            // Add checker to destination
            destPoint.AddChecker(movingColor);
        }
    }

    private string? GetAuthenticatedUserId()
    {
        return Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    private string? GetAuthenticatedDisplayName()
    {
        return Context.User?.FindFirst("displayName")?.Value;
    }

    /// <summary>
    /// Gets the effective display name for a player from JWT claims.
    /// Since OnConnectedAsync validates user exists, JWT will always have displayName claim.
    /// </summary>
    private string? GetEffectiveDisplayNameAsync(string playerId)
    {
        // JWT claims should always have displayName after HTTP registration
        var claimDisplayName = GetAuthenticatedDisplayName();

        if (string.IsNullOrEmpty(claimDisplayName))
        {
            _logger.LogWarning(
                "No displayName in JWT claims for player {PlayerId} - this should not happen after proper registration",
                playerId);
        }

        return claimDisplayName;
    }

    /// <summary>
    /// Gets the authenticated player ID from JWT claims.
    /// Never falls back to client-provided IDs for security reasons.
    /// </summary>
    /// <param name="clientProvidedId">Legacy parameter - ignored for security. Use only authenticated ID.</param>
    /// <returns>The authenticated user ID from JWT</returns>
    /// <exception cref="HubException">Thrown if no authenticated user found</exception>
    private string GetEffectivePlayerId(string clientProvidedId)
    {
        var authenticatedUserId = GetAuthenticatedUserId();

        // Security: Never use client-provided IDs - always require server-validated JWT ID
        if (string.IsNullOrEmpty(authenticatedUserId))
        {
            _logger.LogWarning(
                "GetEffectivePlayerId called without authenticated user. Client attempted to use ID: {ClientId}",
                clientProvidedId);
            throw new HubException("Authentication required. Player ID must come from valid JWT.");
        }

        // Log if client tried to spoof a different ID
        if (!string.IsNullOrEmpty(clientProvidedId) && clientProvidedId != authenticatedUserId)
        {
            _logger.LogWarning(
                "Client attempted to use different player ID. JWT ID: {JwtId}, Attempted ID: {AttemptedId}",
                authenticatedUserId,
                clientProvidedId);
        }

        return authenticatedUserId;
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
                    await Clients.Group(session.Id).OpponentLeft();

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
    /// Get the connection ID for a specific player
    /// </summary>
    private string? GetPlayerConnection(string playerId)
    {
        // First check our connection tracking service (for lobby players)
        var connectionId = _playerConnectionService.GetConnectionId(playerId);
        if (connectionId != null)
        {
            _logger.LogDebug(
                "GetPlayerConnection: Found playerId={PlayerId} via connection service with connectionId={ConnectionId}",
                playerId,
                connectionId);
            return connectionId;
        }

        _logger.LogDebug(
            "GetPlayerConnection: playerId={PlayerId} not found in connection service, checking game sessions",
            playerId);

        // Fall back to checking game sessions (for players in active games)
        var sessions = _sessionManager.GetPlayerGames(playerId);
        var session = sessions.FirstOrDefault();
        if (session == null)
        {
            _logger.LogDebug(
                "GetPlayerConnection: No game session found for playerId={PlayerId}",
                playerId);
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

    // ==================== Analysis Mode Helper Methods ====================

    /// <summary>
    /// Validates if a direct move is valid (ignores game rules, only basic constraints)
    /// </summary>
    /// <summary>
    /// Validates if a direct move is valid (ignores game rules, only basic constraints)
    /// </summary>
    private bool IsValidDirectMove(GameEngine engine, int from, int to)
    {
        // Point ranges: 0=bar, 1-24=board, 25=bearoff
        if (from < 0 || from > 25 || to < 0 || to > 25)
        {
            return false;
        }

        if (from == to)
        {
            return false;
        }

        // Must have checker at source
        CheckerColor? sourceColor = GetCheckerColorAtPoint(engine, from);
        if (sourceColor == null)
        {
            return false;
        }

        // Destination validation (board points only)
        if (to >= 1 && to <= 24)
        {
            var destPoint = engine.Board.GetPoint(to);

            // Can't place opposing colors on same point
            if (destPoint.Color != null && destPoint.Color != sourceColor)
            {
                return false;
            }
        }

        // Prevent exceeding 15 checkers per player
        return CountCheckers(engine, sourceColor.Value) <= 15;
    }

    /// <summary>
    /// Executes a direct move in analysis mode (bypasses game rules)
    /// </summary>
    private void ExecuteDirectMove(GameEngine engine, int from, int to)
    {
        // Remove from source
        CheckerColor color = RemoveCheckerFrom(engine, from);

        // Add to destination
        AddCheckerTo(engine, to, color);

        // Clear remaining moves (reset turn state)
        engine.RemainingMoves.Clear();
    }

    /// <summary>
    /// Gets the checker color at a specific point
    /// </summary>
    /// <summary>
    /// Gets the checker color at a specific point
    /// </summary>
    private CheckerColor? GetCheckerColorAtPoint(GameEngine engine, int point)
    {
        // Bar
        if (point == 0)
        {
            if (engine.WhitePlayer.CheckersOnBar > 0)
            {
                return CheckerColor.White;
            }

            if (engine.RedPlayer.CheckersOnBar > 0)
            {
                return CheckerColor.Red;
            }
        }
        else if (point >= 1 && point <= 24)
        {
            var boardPoint = engine.Board.GetPoint(point);
            return boardPoint.Color;
        }

        // Bear-off
        else if (point == 25)
        {
            if (engine.WhitePlayer.CheckersBornOff > 0)
            {
                return CheckerColor.White;
            }

            if (engine.RedPlayer.CheckersBornOff > 0)
            {
                return CheckerColor.Red;
            }
        }

        return null;
    }

    /// <summary>
    /// Removes a checker from a point and returns its color
    /// </summary>
    private CheckerColor RemoveCheckerFrom(GameEngine engine, int point)
    {
        // Bar
        if (point == 0)
        {
            if (engine.WhitePlayer.CheckersOnBar > 0)
            {
                engine.WhitePlayer.CheckersOnBar--;
                return CheckerColor.White;
            }
            else
            {
                engine.RedPlayer.CheckersOnBar--;
                return CheckerColor.Red;
            }
        }
        else if (point >= 1 && point <= 24)
        {
            var boardPoint = engine.Board.GetPoint(point);
            CheckerColor color = boardPoint.Color!.Value;
            boardPoint.Checkers.RemoveAt(boardPoint.Checkers.Count - 1);
            return color;
        }

        // Bear-off
        else
        {
            if (engine.WhitePlayer.CheckersBornOff > 0)
            {
                engine.WhitePlayer.CheckersBornOff--;
                return CheckerColor.White;
            }
            else
            {
                engine.RedPlayer.CheckersBornOff--;
                return CheckerColor.Red;
            }
        }
    }

    /// <summary>
    /// Adds a checker to a point
    /// </summary>
    /// <summary>
    /// Adds a checker to a point
    /// </summary>
    private void AddCheckerTo(GameEngine engine, int point, CheckerColor color)
    {
        // Bar
        if (point == 0)
        {
            var player = color == CheckerColor.White ? engine.WhitePlayer : engine.RedPlayer;
            player.CheckersOnBar++;
        }
        else if (point >= 1 && point <= 24)
        {
            engine.Board.GetPoint(point).AddChecker(color);
        }

        // Bear-off
        else if (point == 25)
        {
            var player = color == CheckerColor.White ? engine.WhitePlayer : engine.RedPlayer;
            player.CheckersBornOff++;
        }
    }

    /// <summary>
    /// Counts total checkers for a player (board + bar + borne off)
    /// </summary>
    /// <summary>
    /// Counts total checkers for a player (board + bar + borne off)
    /// </summary>
    private int CountCheckers(GameEngine engine, CheckerColor color)
    {
        int count = 0;
        for (int i = 1; i <= 24; i++)
        {
            var point = engine.Board.GetPoint(i);
            if (point.Color == color)
            {
                count += point.Count;
            }
        }

        var player = color == CheckerColor.White ? engine.WhitePlayer : engine.RedPlayer;
        return count + player.CheckersOnBar + player.CheckersBornOff;
    }
}
