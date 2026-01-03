using Backgammon.Server.Configuration;
using Backgammon.Server.Models;
using Microsoft.Extensions.Caching.Hybrid;
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
    private readonly HybridCache _cache;
    private readonly CacheSettings _cacheSettings;
    private readonly ILogger<PlayerProfileService> _logger;

    public PlayerProfileService(
        IUserRepository userRepository,
        IGameRepository gameRepository,
        IFriendshipRepository friendshipRepository,
        IGameSessionManager sessionManager,
        HybridCache cache,
        CacheSettings cacheSettings,
        ILogger<PlayerProfileService> logger)
    {
        _userRepository = userRepository;
        _gameRepository = gameRepository;
        _friendshipRepository = friendshipRepository;
        _sessionManager = sessionManager;
        _cache = cache;
        _cacheSettings = cacheSettings;
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

        // Get target user first (needed for tags and profile building)
        var targetUser = await _userRepository.GetByUsernameAsync(username);
        if (targetUser == null)
        {
            return (null, "User not found");
        }

        // Simplified cache key - all viewers see the same public data
        var cacheKey = $"profile:{username}";

        // Use HybridCache with GetOrCreateAsync pattern
        var profile = await _cache.GetOrCreateAsync(
            cacheKey,
            async cancel => await BuildProfileAsync(targetUser, viewingUserId),
            new HybridCacheEntryOptions
            {
                Expiration = _cacheSettings.PlayerProfile.Expiration,
                LocalCacheExpiration = _cacheSettings.PlayerProfile.LocalCacheExpiration
            },
            tags: [$"profile:{targetUser.UserId}"]);

        return (profile, null);
    }

    private async Task<PlayerProfileDto> BuildProfileAsync(User targetUser, string? viewingUserId)
    {
        var isOwnProfile = viewingUserId == targetUser.UserId;
        var isFriend = false;

        // Check if viewer is friends with target
        if (!string.IsNullOrEmpty(viewingUserId) && !isOwnProfile)
        {
            var viewerFriendships = await _friendshipRepository.GetFriendsAsync(viewingUserId);
            isFriend = viewerFriendships.Any(f => f.FriendUserId == targetUser.UserId && f.Status == FriendshipStatus.Accepted);
        }

        // Create profile DTO (privacy settings no longer enforced - all profiles are public)
        var profile = PlayerProfileDto.FromUser(targetUser, isFriend, isOwnProfile);

        // Always show recent games (privacy removed - all game history is public)
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

        // Always show friends list (privacy removed - all friends lists are public)
        var targetFriendships = await _friendshipRepository.GetFriendsAsync(targetUser.UserId);
        var friendUsers = new List<FriendDto>();

        foreach (var friendship in targetFriendships.Where(f => f.Status == FriendshipStatus.Accepted))
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

        _logger.LogInformation(
            "Profile viewed for {TargetUser} by {ViewingUser}",
            targetUser.Username,
            viewingUserId ?? "anonymous");

        return profile;
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
