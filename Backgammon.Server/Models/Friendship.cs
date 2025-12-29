using System.Text.Json.Serialization;

namespace Backgammon.Server.Models;

/// <summary>
/// Cosmos DB document for storing friendships between users.
/// Each friendship creates two documents (one for each user) for efficient per-user queries.
/// </summary>
public class Friendship
{
    /// <summary>
    /// Cosmos DB document id - auto-generated unique identifier
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// User ID who owns this relationship entry
    /// </summary>
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// The friend's user ID
    /// </summary>
    [JsonPropertyName("friendUserId")]
    public string FriendUserId { get; set; } = string.Empty;

    /// <summary>
    /// Friend's username (denormalized for display)
    /// </summary>
    [JsonPropertyName("friendUsername")]
    public string FriendUsername { get; set; } = string.Empty;

    /// <summary>
    /// Friend's display name (denormalized for display)
    /// </summary>
    [JsonPropertyName("friendDisplayName")]
    public string FriendDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Friendship status
    /// </summary>
    [JsonPropertyName("status")]
    public FriendshipStatus Status { get; set; }

    /// <summary>
    /// When the friend request was created
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the request was accepted (if applicable)
    /// </summary>
    [JsonPropertyName("acceptedAt")]
    public DateTime? AcceptedAt { get; set; }

    /// <summary>
    /// User ID of who initiated the friend request
    /// </summary>
    [JsonPropertyName("initiatedBy")]
    public string InitiatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Friendship status values
/// </summary>
public enum FriendshipStatus
{
    Pending,
    Accepted,
    Blocked
}

/// <summary>
/// Cosmos DB document for game invites between friends.
/// </summary>
public class GameInvite
{
    /// <summary>
    /// Cosmos DB document id - auto-generated unique identifier
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Unique invite ID/code
    /// </summary>
    [JsonPropertyName("inviteId")]
    public string InviteId { get; set; } = string.Empty;

    /// <summary>
    /// User ID who sent the invite
    /// </summary>
    [JsonPropertyName("fromUserId")]
    public string FromUserId { get; set; } = string.Empty;

    /// <summary>
    /// Display name of sender
    /// </summary>
    [JsonPropertyName("fromDisplayName")]
    public string FromDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// User ID of recipient (null for invite links)
    /// </summary>
    [JsonPropertyName("toUserId")]
    public string? ToUserId { get; set; }

    /// <summary>
    /// Display name of recipient
    /// </summary>
    [JsonPropertyName("toDisplayName")]
    public string? ToDisplayName { get; set; }

    /// <summary>
    /// Game session ID once created
    /// </summary>
    [JsonPropertyName("gameId")]
    public string? GameId { get; set; }

    /// <summary>
    /// Invite status
    /// </summary>
    [JsonPropertyName("status")]
    public InviteStatus Status { get; set; }

    /// <summary>
    /// When the invite was created
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the invite expires
    /// </summary>
    [JsonPropertyName("expiresAt")]
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// When the invite was responded to
    /// </summary>
    [JsonPropertyName("respondedAt")]
    public DateTime? RespondedAt { get; set; }
}

/// <summary>
/// Game invite status values
/// </summary>
public enum InviteStatus
{
    Pending,
    Accepted,
    Declined,
    Expired
}
