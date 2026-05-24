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
    private readonly ILogger<GameHub> _logger;
    private readonly IAnalysisService _analysisService;
    private readonly IUserRepository _userRepository;
    private readonly IFriendService _friendService;
    private readonly ICorrespondenceGameService _correspondenceGameService;
    private readonly IDailyPuzzleService _dailyPuzzleService;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameHub"/> class.
    /// </summary>
    public GameHub(
        IGrainFactory grainFactory,
        IGameRepository gameRepository,
        IMatchService matchService,
        IPlayerProfileService playerProfileService,
        ILogger<GameHub> logger,
        IAnalysisService analysisService,
        IUserRepository userRepository,
        IFriendService friendService,
        ICorrespondenceGameService correspondenceGameService,
        IDailyPuzzleService dailyPuzzleService)
    {
        _grainFactory = grainFactory;
        _gameRepository = gameRepository;
        _matchService = matchService;
        _playerProfileService = playerProfileService;
        _logger = logger;
        _analysisService = analysisService;
        _userRepository = userRepository;
        _friendService = friendService;
        _correspondenceGameService = correspondenceGameService;
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
}
