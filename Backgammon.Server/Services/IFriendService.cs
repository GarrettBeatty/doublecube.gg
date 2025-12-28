using Backgammon.Server.Models;

namespace Backgammon.Server.Services;

/// <summary>
/// Interface for friend management operations.
/// </summary>
public interface IFriendService
{
    /// <summary>
    /// Send a friend request to another user
    /// </summary>
    Task<(bool Success, string? Error)> SendFriendRequestAsync(string fromUserId, string toUserId);

    /// <summary>
    /// Accept a pending friend request
    /// </summary>
    Task<(bool Success, string? Error)> AcceptFriendRequestAsync(string userId, string friendUserId);

    /// <summary>
    /// Decline a pending friend request
    /// </summary>
    Task<(bool Success, string? Error)> DeclineFriendRequestAsync(string userId, string friendUserId);

    /// <summary>
    /// Remove a friend
    /// </summary>
    Task<(bool Success, string? Error)> RemoveFriendAsync(string userId, string friendUserId);

    /// <summary>
    /// Block a user
    /// </summary>
    Task<(bool Success, string? Error)> BlockUserAsync(string userId, string blockedUserId);

    /// <summary>
    /// Get a user's friends list with online status
    /// </summary>
    Task<List<FriendDto>> GetFriendsAsync(string userId);

    /// <summary>
    /// Get pending friend requests for a user
    /// </summary>
    Task<List<FriendDto>> GetPendingRequestsAsync(string userId);

    /// <summary>
    /// Invite a friend to a game
    /// </summary>
    Task<(bool Success, string? Error)> InviteFriendToGameAsync(string userId, string friendUserId, string gameId);
}
