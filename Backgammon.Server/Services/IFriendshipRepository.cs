using Backgammon.Server.Models;

namespace Backgammon.Server.Services;

/// <summary>
/// Interface for friendship data access operations.
/// </summary>
public interface IFriendshipRepository
{
    /// <summary>
    /// Get all accepted friends for a user
    /// </summary>
    Task<List<Friendship>> GetFriendsAsync(string userId);

    /// <summary>
    /// Get pending friend requests for a user (where they are the recipient)
    /// </summary>
    Task<List<Friendship>> GetPendingRequestsAsync(string userId);

    /// <summary>
    /// Get pending friend requests sent by a user
    /// </summary>
    Task<List<Friendship>> GetSentRequestsAsync(string userId);

    /// <summary>
    /// Get a specific friendship between two users
    /// </summary>
    Task<Friendship?> GetFriendshipAsync(string userId, string friendUserId);

    /// <summary>
    /// Create a friend request (creates both directional records)
    /// </summary>
    Task SendFriendRequestAsync(string fromUserId, string toUserId, string fromUsername, string fromDisplayName, string toUsername, string toDisplayName);

    /// <summary>
    /// Accept a pending friend request
    /// </summary>
    Task AcceptFriendRequestAsync(string userId, string friendUserId);

    /// <summary>
    /// Decline a pending friend request
    /// </summary>
    Task DeclineFriendRequestAsync(string userId, string friendUserId);

    /// <summary>
    /// Block a user
    /// </summary>
    Task BlockUserAsync(string userId, string blockedUserId);

    /// <summary>
    /// Remove a friend (removes both directional records)
    /// </summary>
    Task RemoveFriendAsync(string userId, string friendUserId);

    /// <summary>
    /// Check if two users are friends
    /// </summary>
    Task<bool> AreFriendsAsync(string userId, string otherUserId);

    /// <summary>
    /// Check if a user is blocked by another user
    /// </summary>
    Task<bool> IsBlockedAsync(string userId, string byUserId);

    /// <summary>
    /// Get friend count for a user
    /// </summary>
    Task<int> GetFriendCountAsync(string userId);
}
