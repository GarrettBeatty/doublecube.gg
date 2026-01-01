using Backgammon.Server.Hubs;
using Backgammon.Server.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace Backgammon.Server.Services;

/// <summary>
/// Friend management service with SignalR notifications and HybridCache.
/// </summary>
public class FriendService : IFriendService
{
    private readonly IFriendshipRepository _friendshipRepository;
    private readonly IUserRepository _userRepository;
    private readonly IGameSessionManager _sessionManager;
    private readonly IHubContext<GameHub> _hubContext;
    private readonly HybridCache _cache;
    private readonly ILogger<FriendService> _logger;

    public FriendService(
        IFriendshipRepository friendshipRepository,
        IUserRepository userRepository,
        IGameSessionManager sessionManager,
        IHubContext<GameHub> hubContext,
        HybridCache cache,
        ILogger<FriendService> logger)
    {
        _friendshipRepository = friendshipRepository;
        _userRepository = userRepository;
        _sessionManager = sessionManager;
        _hubContext = hubContext;
        _cache = cache;
        _logger = logger;
    }

    public async Task<(bool Success, string? Error)> SendFriendRequestAsync(string fromUserId, string toUserId)
    {
        try
        {
            // Can't friend yourself
            if (fromUserId == toUserId)
            {
                return (false, "Cannot send friend request to yourself");
            }

            // Check if target user exists
            var toUser = await _userRepository.GetByUserIdAsync(toUserId);
            if (toUser == null)
            {
                return (false, "User not found");
            }

            // Check if blocked
            if (await _friendshipRepository.IsBlockedAsync(fromUserId, toUserId))
            {
                return (false, "Cannot send friend request to this user");
            }

            // Check if already friends or pending
            var existing = await _friendshipRepository.GetFriendshipAsync(fromUserId, toUserId);
            if (existing != null)
            {
                if (existing.Status == FriendshipStatus.Accepted)
                {
                    return (false, "Already friends with this user");
                }

                if (existing.Status == FriendshipStatus.Pending)
                {
                    return (false, "Friend request already pending");
                }
            }

            // Get from user info
            var fromUser = await _userRepository.GetByUserIdAsync(fromUserId);
            if (fromUser == null)
            {
                return (false, "User not found");
            }

            // Send the request
            await _friendshipRepository.SendFriendRequestAsync(
                fromUserId, toUserId,
                fromUser.Username, fromUser.DisplayName,
                toUser.Username, toUser.DisplayName);

            // Send real-time notification to recipient if online
            // Note: This requires tracking user connections by userId
            // For now, we'll skip the real-time notification
            // await NotifyUserAsync(toUserId, "FriendRequestReceived", new { fromUserId, fromDisplayName = fromUser.DisplayName });

            _logger.LogInformation("Friend request sent from {FromUserId} to {ToUserId}", fromUserId, toUserId);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send friend request from {FromUserId} to {ToUserId}", fromUserId, toUserId);
            return (false, "Failed to send friend request");
        }
    }

    public async Task<(bool Success, string? Error)> AcceptFriendRequestAsync(string userId, string friendUserId)
    {
        try
        {
            var friendship = await _friendshipRepository.GetFriendshipAsync(userId, friendUserId);

            if (friendship == null)
            {
                return (false, "Friend request not found");
            }

            if (friendship.Status != FriendshipStatus.Pending)
            {
                return (false, "No pending friend request");
            }

            // Only the recipient can accept
            if (friendship.InitiatedBy == userId)
            {
                return (false, "Cannot accept your own friend request");
            }

            await _friendshipRepository.AcceptFriendRequestAsync(userId, friendUserId);

            // Invalidate friend list caches for both users
            await _cache.RemoveByTagAsync($"friends:{userId}");
            await _cache.RemoveByTagAsync($"friends:{friendUserId}");

            _logger.LogInformation("Friend request accepted between {UserId} and {FriendUserId}", userId, friendUserId);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to accept friend request between {UserId} and {FriendUserId}", userId, friendUserId);
            return (false, "Failed to accept friend request");
        }
    }

    public async Task<(bool Success, string? Error)> DeclineFriendRequestAsync(string userId, string friendUserId)
    {
        try
        {
            var friendship = await _friendshipRepository.GetFriendshipAsync(userId, friendUserId);

            if (friendship == null)
            {
                return (false, "Friend request not found");
            }

            if (friendship.Status != FriendshipStatus.Pending)
            {
                return (false, "No pending friend request");
            }

            await _friendshipRepository.DeclineFriendRequestAsync(userId, friendUserId);

            _logger.LogInformation("Friend request declined between {UserId} and {FriendUserId}", userId, friendUserId);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decline friend request between {UserId} and {FriendUserId}", userId, friendUserId);
            return (false, "Failed to decline friend request");
        }
    }

    public async Task<(bool Success, string? Error)> RemoveFriendAsync(string userId, string friendUserId)
    {
        try
        {
            var friendship = await _friendshipRepository.GetFriendshipAsync(userId, friendUserId);

            if (friendship == null || friendship.Status != FriendshipStatus.Accepted)
            {
                return (false, "Not friends with this user");
            }

            await _friendshipRepository.RemoveFriendAsync(userId, friendUserId);

            // Invalidate friend list caches for both users
            await _cache.RemoveByTagAsync($"friends:{userId}");
            await _cache.RemoveByTagAsync($"friends:{friendUserId}");

            _logger.LogInformation("Friendship removed between {UserId} and {FriendUserId}", userId, friendUserId);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove friend {FriendUserId} for user {UserId}", friendUserId, userId);
            return (false, "Failed to remove friend");
        }
    }

    public async Task<(bool Success, string? Error)> BlockUserAsync(string userId, string blockedUserId)
    {
        try
        {
            if (userId == blockedUserId)
            {
                return (false, "Cannot block yourself");
            }

            var blockedUser = await _userRepository.GetByUserIdAsync(blockedUserId);
            if (blockedUser == null)
            {
                return (false, "User not found");
            }

            await _friendshipRepository.BlockUserAsync(userId, blockedUserId);

            _logger.LogInformation("User {UserId} blocked user {BlockedUserId}", userId, blockedUserId);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to block user {BlockedUserId} for user {UserId}", blockedUserId, userId);
            return (false, "Failed to block user");
        }
    }

    public async Task<List<FriendDto>> GetFriendsAsync(string userId)
    {
        try
        {
            var friendships = await _cache.GetOrCreateAsync(
                $"friends:{userId}",
                async cancel => await _friendshipRepository.GetFriendsAsync(userId),
                new HybridCacheEntryOptions
                {
                    Expiration = TimeSpan.FromMinutes(5),
                    LocalCacheExpiration = TimeSpan.FromMinutes(1)
                },
                tags: [$"friends:{userId}"]
            );

            var result = new List<FriendDto>();

            foreach (var friendship in friendships)
            {
                // Check if friend is online by looking for active game sessions
                // Note: Online status is not cached as it changes frequently
                var isOnline = _sessionManager.IsPlayerOnline(friendship.FriendUserId);

                result.Add(new FriendDto
                {
                    UserId = friendship.FriendUserId,
                    Username = friendship.FriendUsername,
                    DisplayName = friendship.FriendDisplayName,
                    IsOnline = isOnline,
                    Status = friendship.Status,
                    InitiatedBy = friendship.InitiatedBy
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get friends for user {UserId}", userId);
            return new List<FriendDto>();
        }
    }

    public async Task<List<FriendDto>> GetPendingRequestsAsync(string userId)
    {
        try
        {
            var requests = await _friendshipRepository.GetPendingRequestsAsync(userId);
            var result = new List<FriendDto>();

            foreach (var request in requests)
            {
                result.Add(new FriendDto
                {
                    UserId = request.FriendUserId,
                    Username = request.FriendUsername,
                    DisplayName = request.FriendDisplayName,
                    IsOnline = _sessionManager.IsPlayerOnline(request.FriendUserId),
                    Status = request.Status,
                    InitiatedBy = request.InitiatedBy
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pending requests for user {UserId}", userId);
            return new List<FriendDto>();
        }
    }

    public async Task<(bool Success, string? Error)> InviteFriendToGameAsync(string userId, string friendUserId, string gameId)
    {
        try
        {
            // Verify they are friends
            if (!await _friendshipRepository.AreFriendsAsync(userId, friendUserId))
            {
                return (false, "Not friends with this user");
            }

            // Verify the game exists and has room
            var session = _sessionManager.GetSession(gameId);
            if (session == null)
            {
                return (false, "Game not found");
            }

            if (session.IsFull)
            {
                return (false, "Game is full");
            }

            // Get user info for the notification
            var fromUser = await _userRepository.GetByUserIdAsync(userId);
            if (fromUser == null)
            {
                return (false, "User not found");
            }

            // Send notification to friend if online
            // This would require tracking user connections by userId
            // For now, we just return success and the friend can see the game in their list
            _logger.LogInformation("Game invite sent from {UserId} to {FriendUserId} for game {GameId}", userId, friendUserId, gameId);

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invite friend {FriendUserId} to game {GameId}", friendUserId, gameId);
            return (false, "Failed to send game invite");
        }
    }
}
