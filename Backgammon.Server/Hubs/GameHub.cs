using System.Security.Claims;
using Backgammon.Core;
using Backgammon.Server.Extensions;
using Backgammon.Server.Models;
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
public class GameHub : Hub
{
    private readonly IGameSessionManager _sessionManager;
    private readonly IGameRepository _gameRepository;
    private readonly IAiMoveService _aiMoveService;
    private readonly IHubContext<GameHub> _hubContext;
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

    public GameHub(
        IGameSessionManager sessionManager,
        IGameRepository gameRepository,
        IAiMoveService aiMoveService,
        IHubContext<GameHub> hubContext,
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
        AnalysisService analysisService)
    {
        _sessionManager = sessionManager;
        _gameRepository = gameRepository;
        _aiMoveService = aiMoveService;
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
            var displayName = GetAuthenticatedDisplayName();

            if (string.IsNullOrEmpty(gameId))
            {
                await Clients.Caller.SendAsync("Error", "Game ID is required");
                return;
            }

            await _gameService.JoinGameAsync(connectionId, effectivePlayerId, displayName, gameId);
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

            await _gameService.CreateAnalysisGameAsync(connectionId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating analysis game");
            await Clients.Caller.SendAsync("Error", ex.Message);
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
                await Clients.Caller.SendAsync("Error", "Not in a game");
                return;
            }

            // Only allow in analysis mode
            if (!session.IsAnalysisMode)
            {
                await Clients.Caller.SendAsync("Error", "Dice can only be set in analysis mode");
                return;
            }

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
                await Clients.Caller.SendAsync("Error", "End your turn or undo moves before setting new dice");
                return;
            }

            // Validate dice values
            if (die1 < 1 || die1 > 6 || die2 < 1 || die2 > 6)
            {
                await Clients.Caller.SendAsync("Error", "Dice values must be between 1 and 6");
                return;
            }

            // Set the dice
            session.Engine.Dice.SetDice(die1, die2);
            session.Engine.RemainingMoves.Clear();
            session.Engine.RemainingMoves.AddRange(session.Engine.Dice.GetMoves());

            _logger.LogInformation(
                "Set dice to [{Die1}, {Die2}] in analysis game {GameId}",
                die1,
                die2,
                session.Id);

            // Broadcast update
            await _gameService.BroadcastGameUpdateAsync(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting dice");
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
            var effectivePlayerId = GetEffectivePlayerId(playerId);
            var displayName = GetAuthenticatedDisplayName();

            await _gameService.CreateAiGameAsync(connectionId, effectivePlayerId, displayName);
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
                await Clients.Caller.SendAsync("Error", "Not in a game");
                return;
            }

            var result = await _gameActionOrchestrator.RollDiceAsync(session, Context.ConnectionId);
            if (!result.Success)
            {
                await Clients.Caller.SendAsync("Error", result.ErrorMessage);
            }
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
            var session = _sessionManager.GetGameByPlayer(Context.ConnectionId);
            if (session == null)
            {
                await Clients.Caller.SendAsync("Error", "Not in a game");
                return;
            }

            var result = await _gameActionOrchestrator.MakeMoveAsync(session, Context.ConnectionId, from, to);
            if (!result.Success)
            {
                await Clients.Caller.SendAsync("Error", result.ErrorMessage);
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

            var result = await _gameActionOrchestrator.EndTurnAsync(session, Context.ConnectionId);
            if (!result.Success)
            {
                await Clients.Caller.SendAsync("Error", result.ErrorMessage);
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

            var result = await _gameActionOrchestrator.UndoLastMoveAsync(session, Context.ConnectionId);
            if (!result.Success)
            {
                await Clients.Caller.SendAsync("Error", result.ErrorMessage);
            }
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

            var (success, currentValue, newValue, error) = await _doubleOfferService.OfferDoubleAsync(session, Context.ConnectionId);
            if (!success)
            {
                await Clients.Caller.SendAsync("Error", error);
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
                            await Clients.Caller.SendAsync("DoubleAccepted", state);
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
                        await Clients.Caller.SendAsync("Info", "Computer declined the double. You win!");

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
                            await Clients.Caller.SendAsync("GameOver", finalState);
                        }

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

            var (success, winner, stakes, error) = await _doubleOfferService.DeclineDoubleAsync(session, Context.ConnectionId);
            if (!success)
            {
                await Clients.Caller.SendAsync("Error", error);
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
            await Clients.Group(session.Id).SendAsync("GameOver", finalState);

            // Remove from memory to prevent memory leak
            _sessionManager.RemoveGame(session.Id);
            _logger.LogInformation("Removed abandoned game {GameId} from memory", session.Id);
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
            await _chatService.SendChatMessageAsync(Context.ConnectionId, message);
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
            await Clients.Caller.SendAsync("Error", "You are not in a game");
            return;
        }

        // Only allow in analysis mode
        if (!session.IsAnalysisMode)
        {
            await Clients.Caller.SendAsync("Error", "Direct moves only allowed in analysis mode");
            return;
        }

        // Validate basic constraints
        if (!IsValidDirectMove(session.Engine, from, to))
        {
            await Clients.Caller.SendAsync("Error", "Invalid move: check piece placement rules");
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
            await Clients.Caller.SendAsync("Error", "Failed to move checker");
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
            await Clients.Caller.SendAsync("Error", "You are not in a game");
            return;
        }

        if (!session.IsAnalysisMode)
        {
            await Clients.Caller.SendAsync("Error", "Can only set player in analysis mode");
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
            await Clients.Caller.SendAsync("Error", "Failed to set current player");
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
                await Clients.Caller.SendAsync("Error", error);
                return null;
            }

            return profile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting player profile");
            await Clients.Caller.SendAsync("Error", "Failed to load profile");
            return null;
        }
    }

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

                    _logger.LogInformation(
                        "Added AI player {AiPlayerId} to next match game {GameId}",
                        aiPlayerId,
                        nextGame.GameId);
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
                matchId,
                nextGame.GameId);
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
                _logger.LogWarning(
                    "Player {PlayerId} attempted to access match {MatchId} without authorization",
                    playerId,
                    matchId);
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

    public async Task<List<object>> GetMatchLobbies()
    {
        try
        {
            var lobbies = await _matchService.GetOpenLobbiesAsync();

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
                isOpenLobby = m.IsOpenLobby
            }).ToList<object>();

            return lobbyList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting match lobbies");
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
                timeControl);

            // Send MatchCreated event with game ID
            await Clients.Caller.SendAsync("MatchCreated", new
            {
                matchId = match.MatchId,
                gameId = firstGame.GameId,
                targetScore = match.TargetScore,
                opponentType = match.OpponentType,
                player1Id = match.Player1Id,
                player2Id = match.Player2Id,
                player1Name = match.Player1Name,
                player2Name = match.Player2Name
            });

            _logger.LogInformation(
                "Match {MatchId} created for player {PlayerId} (type: {OpponentType}), first game: {GameId}",
                match.MatchId,
                playerId,
                config.OpponentType,
                firstGame.GameId);

            // For friend matches, notify the friend if they're online
            if (config.OpponentType == "Friend" && !string.IsNullOrEmpty(config.OpponentId))
            {
                if (_sessionManager.IsPlayerOnline(config.OpponentId))
                {
                    var opponentConnection = GetPlayerConnection(config.OpponentId);
                    if (!string.IsNullOrEmpty(opponentConnection))
                    {
                        await Clients.Client(opponentConnection).SendAsync(
                            "MatchInvite",
                            new
                            {
                                matchId = match.MatchId,
                                gameId = firstGame.GameId,
                                targetScore = match.TargetScore,
                                challengerName = match.Player1Name,
                                challengerId = match.Player1Id
                            });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating match");
            await Clients.Caller.SendAsync("Error", ex.Message);
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
            await Clients.Caller.SendAsync("MatchCreated", new
            {
                matchId = match.MatchId,
                gameId = match.CurrentGameId,
                targetScore = match.TargetScore,
                opponentType = match.OpponentType,
                player1Id = match.Player1Id,
                player2Id = match.Player2Id,
                player1Name = match.Player1Name,
                player2Name = match.Player2Name
            });

            // Notify creator that opponent joined
            var creatorConnection = GetPlayerConnection(match.Player1Id);
            if (!string.IsNullOrEmpty(creatorConnection))
            {
                await Clients.Client(creatorConnection).SendAsync("OpponentJoinedMatch", new
                {
                    matchId = match.MatchId,
                    player2Id = match.Player2Id,
                    player2Name = match.Player2Name
                });
            }

            _logger.LogInformation("Player {PlayerId} joined match {MatchId}", playerId, matchId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining match {MatchId}", matchId);
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    /// <summary>
    /// Analyze the current position and return evaluation
    /// </summary>
    public async Task<PositionEvaluationDto> AnalyzePosition(string gameId)
    {
        var session = _sessionManager.GetSession(gameId);
        if (session == null)
        {
            throw new HubException("Game not found");
        }

        return await Task.Run(() => _analysisService.EvaluatePosition(session.Engine));
    }

    /// <summary>
    /// Find the best moves for the current position
    /// </summary>
    public async Task<BestMovesAnalysisDto> FindBestMoves(string gameId)
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

        return await Task.Run(() => _analysisService.FindBestMoves(session.Engine));
    }

    private string? GetAuthenticatedUserId()
    {
        return Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    private string? GetAuthenticatedDisplayName()
    {
        return Context.User?.FindFirst("displayName")?.Value;
    }

    private string GetEffectivePlayerId(string anonymousPlayerId)
    {
        return GetAuthenticatedUserId() ?? anonymousPlayerId;
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
