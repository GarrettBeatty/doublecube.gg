using Backgammon.Server.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Backgammon.Server.Services;

/// <summary>
/// Implementation of player profile retrieval
/// </summary>
public class PlayerProfileService : IPlayerProfileService
{
    private readonly IUserRepository _userRepository;
    private readonly IGameRepository _gameRepository;
    private readonly IFriendshipRepository _friendshipRepository;
    private readonly IGameSessionManager _sessionManager;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PlayerProfileService> _logger;

    public PlayerProfileService(
        IUserRepository userRepository,
        IGameRepository gameRepository,
        IFriendshipRepository friendshipRepository,
        IGameSessionManager sessionManager,
        IMemoryCache cache,
        ILogger<PlayerProfileService> logger)
    {
        _userRepository = userRepository;
        _gameRepository = gameRepository;
        _friendshipRepository = friendshipRepository;
        _sessionManager = sessionManager;
        _cache = cache;
        _logger = logger;
    }

    public async Task<(PlayerProfileDto? Profile, string? Error)> GetPlayerProfileAsync(
        string username,
        string? viewingUserId)
    {
        // Validate username
        if (string.IsNullOrWhiteSpace(username))
        {
            return (null, "Username is required");
        }

        // Create cache key based on username and viewer
        var cacheKey = $"profile:{username}:viewer:{viewingUserId ?? "anonymous"}";

        // Try to get from cache first
        if (_cache.TryGetValue<PlayerProfileDto>(cacheKey, out var cachedProfile))
        {
            _logger.LogDebug("Returning cached profile for {Username}", username);
            return (cachedProfile, null);
        }

        // Get the target user
        var targetUser = await _userRepository.GetByUsernameAsync(username);
        if (targetUser == null)
        {
            return (null, "User not found");
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
            targetUser.Username,
            viewingUserId ?? "anonymous");

        // Cache the profile for 2 minutes (shorter for own profile to reflect updates faster)
        var cacheExpiration = isOwnProfile ? TimeSpan.FromMinutes(1) : TimeSpan.FromMinutes(2);
        _cache.Set(cacheKey, profile, cacheExpiration);

        return (profile, null);
    }

    private string GetOpponentUsername(Game game, string userId)
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

    private bool DetermineIfPlayerWon(Game game, string userId)
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

    private string? DetermineWinType(Game game, string userId)
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
}
