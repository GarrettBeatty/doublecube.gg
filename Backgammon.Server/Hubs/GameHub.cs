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
public partial class GameHub : Hub<IGameHubClient>
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

    // ============================================
    // Shared Utility Methods
    // ============================================

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
