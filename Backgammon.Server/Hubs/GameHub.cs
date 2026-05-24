using System.Security.Claims;
using Backgammon.Core;
using Backgammon.Server.Extensions;
using Backgammon.Server.Grains.Interfaces;
using Backgammon.Server.Hubs.Interfaces;
using Backgammon.Server.Models;
using Backgammon.Server.Models.SignalR;
using Backgammon.Server.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Orleans;

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
    private readonly IGrainFactory _grainFactory;
    private readonly IGameRepository _gameRepository;
    private readonly IMatchService _matchService;
    private readonly IPlayerProfileService _playerProfileService;
    private readonly IChatService _chatService;
    private readonly ILogger<GameHub> _logger;
    private readonly IAnalysisService _analysisService;
    private readonly IUserRepository _userRepository;
    private readonly IFriendService _friendService;
    private readonly ICorrespondenceGameService _correspondenceGameService;
    private readonly IDailyPuzzleService _dailyPuzzleService;
    private readonly IAnalysisSessionManager _analysisSessionManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameHub"/> class.
    /// </summary>
    public GameHub(
        IGrainFactory grainFactory,
        IGameRepository gameRepository,
        IMatchService matchService,
        IPlayerProfileService playerProfileService,
        IChatService chatService,
        ILogger<GameHub> logger,
        IAnalysisService analysisService,
        IUserRepository userRepository,
        IFriendService friendService,
        ICorrespondenceGameService correspondenceGameService,
        IDailyPuzzleService dailyPuzzleService,
        IAnalysisSessionManager analysisSessionManager)
    {
        _grainFactory = grainFactory;
        _gameRepository = gameRepository;
        _matchService = matchService;
        _playerProfileService = playerProfileService;
        _chatService = chatService;
        _logger = logger;
        _analysisService = analysisService;
        _userRepository = userRepository;
        _friendService = friendService;
        _correspondenceGameService = correspondenceGameService;
        _dailyPuzzleService = dailyPuzzleService;
        _analysisSessionManager = analysisSessionManager;
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

            await _grainFactory.GetGrain<IPresenceGrain>(IPresenceGrain.Key)
                .SetOnlineAsync(jwtUserId, connectionId);
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
    /// Gets the game grain for the calling connection via the presence registry.
    /// Returns null if the caller is not in an active game.
    /// </summary>
    private async Task<IGameGrain?> GetGameGrainForCallerAsync()
    {
        if (GetAuthenticatedUserId() == null) return null;
        var presence = _grainFactory.GetGrain<IPresenceGrain>(IPresenceGrain.Key);
        var gameId = await presence.GetGameIdForConnectionAsync(Context.ConnectionId);
        if (gameId == null) return null;
        return _grainFactory.GetGrain<IGameGrain>(gameId);
    }

    /// <summary>
    /// Gets any one connection ID for a player from the presence registry.
    /// </summary>
    private Task<string?> GetPlayerConnectionAsync(string playerId)
    {
        return _grainFactory.GetGrain<IPresenceGrain>(IPresenceGrain.Key).GetConnectionIdAsync(playerId);
    }

    // ==================== Analysis Mode Helper Methods ====================

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
