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
    private readonly IMatchLobbyService _matchLobbyService;
    private readonly IDoubleOfferService _doubleOfferService;
    private readonly IGameStateService _gameStateService;
    private readonly IPlayerProfileService _playerProfileService;
    private readonly IGameActionOrchestrator _gameActionOrchestrator;
    private readonly IPlayerStatsService _playerStatsService;
    private readonly IGameCreationService _gameCreationService;
    private readonly IMoveQueryService _moveQueryService;
    private readonly IGameImportExportService _gameImportExportService;
    private readonly IChatService _chatService;
    private readonly ILogger<GameHub> _logger;

    public GameHub(
        IGameSessionManager sessionManager,
        IGameRepository gameRepository,
        IAiMoveService aiMoveService,
        IHubContext<GameHub> hubContext,
        IMatchService matchService,
        IPlayerConnectionService playerConnectionService,
        IMatchLobbyService matchLobbyService,
        IDoubleOfferService doubleOfferService,
        IGameStateService gameStateService,
        IPlayerProfileService playerProfileService,
        IGameActionOrchestrator gameActionOrchestrator,
        IPlayerStatsService playerStatsService,
        IGameCreationService gameCreationService,
        IMoveQueryService moveQueryService,
        IGameImportExportService gameImportExportService,
        IChatService chatService,
        ILogger<GameHub> logger)
    {
        _sessionManager = sessionManager;
        _gameRepository = gameRepository;
        _aiMoveService = aiMoveService;
        _hubContext = hubContext;
        _matchService = matchService;
        _playerConnectionService = playerConnectionService;
        _matchLobbyService = matchLobbyService;
        _doubleOfferService = doubleOfferService;
        _gameStateService = gameStateService;
        _playerProfileService = playerProfileService;
        _gameActionOrchestrator = gameActionOrchestrator;
        _playerStatsService = playerStatsService;
        _gameCreationService = gameCreationService;
        _moveQueryService = moveQueryService;
        _gameImportExportService = gameImportExportService;
        _chatService = chatService;
        _logger = logger;
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

            await _gameCreationService.JoinGameAsync(connectionId, effectivePlayerId, displayName, gameId);
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

            await _gameCreationService.CreateAnalysisGameAsync(connectionId, userId);
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
            var effectivePlayerId = GetEffectivePlayerId(playerId);
            var displayName = GetAuthenticatedDisplayName();

            await _gameCreationService.CreateAiGameAsync(connectionId, effectivePlayerId, displayName);
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
                await _gameStateService.BroadcastDoubleOfferAsync(session, Context.ConnectionId, currentValue, newValue);
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
            await _gameStateService.BroadcastDoubleAcceptedAsync(session);

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
            await _gameStateService.BroadcastGameOverAsync(session);

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
    /// Export the current game position in SGF format
    /// </summary>
    public async Task<string> ExportPosition()
    {
        return await _gameImportExportService.ExportPositionAsync(Context.ConnectionId);
    }

    /// <summary>
    /// Import a position from SGF format
    /// </summary>
    public async Task ImportPosition(string sgf)
    {
        await _gameImportExportService.ImportPositionAsync(Context.ConnectionId, sgf);
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
                match.MatchId,
                match.Player1Name,
                match.Player2Name);
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

    /// <summary>
    /// Create a new match with configuration (lobby-based)
    /// </summary>
    public async Task CreateMatchWithConfig(MatchConfig config)
    {
        try
        {
            var playerId = GetEffectivePlayerId(Context.ConnectionId);

            // Create match lobby
            var match = await _matchLobbyService.CreateMatchLobbyAsync(playerId, config, config.DisplayName);

            // For AI matches, skip lobby and start immediately
            if (config.OpponentType == "AI")
            {
                var result = await _matchLobbyService.StartMatchWithAiAsync(match);
                if (result == null)
                {
                    await Clients.Caller.SendAsync("Error", "Failed to refresh match data");
                    return;
                }

                var (game, updatedMatch) = result.Value;

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
                    match.MatchId,
                    playerId);
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
            if (config.OpponentType == "Friend" && !string.IsNullOrEmpty(config.OpponentId))
            {
                if (_sessionManager.IsPlayerOnline(config.OpponentId))
                {
                    var opponentConnection = GetPlayerConnection(config.OpponentId);
                    if (!string.IsNullOrEmpty(opponentConnection))
                    {
                        await Clients.Client(opponentConnection).SendAsync(
                            "MatchLobbyInvite",
                            new
                            {
                                matchId = match.MatchId,
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
                playerId,
                matchId,
                existingMatch.IsOpenLobby,
                existingMatch.OpponentType,
                existingMatch.Player1Id,
                existingMatch.Player2Id ?? "null",
                existingMatch.LobbyStatus);

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
                    match.Player1Id,
                    creatorConnection ?? "NULL");

                if (!string.IsNullOrEmpty(creatorConnection))
                {
                    _logger.LogInformation(
                        "Sending MatchLobbyPlayerJoined to creator at connection {ConnectionId}",
                        creatorConnection);
                    await Clients.Client(creatorConnection).SendAsync(
                        "MatchLobbyPlayerJoined",
                        new
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
                    matchId,
                    match.Player1Id,
                    match.Player2Id);
                try
                {
                    // Use the match data we just got from JoinOpenLobbyAsync (it's fresh)
                    if (string.IsNullOrEmpty(match.Player2Id))
                    {
                        _logger.LogWarning(
                            "Cannot auto-start match {MatchId} - Player2Id not set in returned match data. Player1Id={Player1Id}, Player2Id={Player2Id}",
                            matchId,
                            match.Player1Id,
                            match.Player2Id);
                        return;
                    }

                    // Start the first game (pass the match object to avoid DB reload)
                    var firstGame = await _matchService.StartMatchFirstGameAsync(match);

                    // Get updated match state
                    var updatedMatch = await _matchService.GetMatchAsync(matchId);
                    if (updatedMatch == null)
                    {
                        _logger.LogError("Failed to get match {MatchId} after starting first game", matchId);
                        await Clients.Caller.SendAsync("Error", "Failed to load match data");
                        return;
                    }

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
                        matchId,
                        firstGame.GameId);
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
                    playerId,
                    matchId,
                    reason,
                    existingMatch.IsOpenLobby,
                    existingMatch.Player2Id);

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
            if (updatedMatch == null)
            {
                _logger.LogError("Failed to get match {MatchId} after starting first game", matchId);
                await Clients.Caller.SendAsync("Error", "Failed to load match data");
                return;
            }

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
                matchId,
                game.GameId,
                playerId);
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
}
