using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Backgammon.Server.Models;

/// <summary>
/// MongoDB document for storing friendships between users.
/// Each friendship creates two documents (one for each user) for efficient per-user queries.
/// </summary>
public class Friendship
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    /// <summary>
    /// User ID who owns this relationship entry
    /// </summary>
    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// The friend's user ID
    /// </summary>
    [BsonElement("friendUserId")]
    public string FriendUserId { get; set; } = string.Empty;

    /// <summary>
    /// Friend's username (denormalized for display)
    /// </summary>
    [BsonElement("friendUsername")]
    public string FriendUsername { get; set; } = string.Empty;

    /// <summary>
    /// Friend's display name (denormalized for display)
    /// </summary>
    [BsonElement("friendDisplayName")]
    public string FriendDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Friendship status
    /// </summary>
    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public FriendshipStatus Status { get; set; }

    /// <summary>
    /// When the friend request was created
    /// </summary>
    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the request was accepted (if applicable)
    /// </summary>
    [BsonElement("acceptedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? AcceptedAt { get; set; }

    /// <summary>
    /// User ID of who initiated the friend request
    /// </summary>
    [BsonElement("initiatedBy")]
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
/// MongoDB document for game invites between friends.
/// </summary>
public class GameInvite
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    /// <summary>
    /// Unique invite ID/code
    /// </summary>
    [BsonElement("inviteId")]
    public string InviteId { get; set; } = string.Empty;

    /// <summary>
    /// User ID who sent the invite
    /// </summary>
    [BsonElement("fromUserId")]
    public string FromUserId { get; set; } = string.Empty;

    /// <summary>
    /// Display name of sender
    /// </summary>
    [BsonElement("fromDisplayName")]
    public string FromDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// User ID of recipient (null for invite links)
    /// </summary>
    [BsonElement("toUserId")]
    public string? ToUserId { get; set; }

    /// <summary>
    /// Display name of recipient
    /// </summary>
    [BsonElement("toDisplayName")]
    public string? ToDisplayName { get; set; }

    /// <summary>
    /// Game session ID once created
    /// </summary>
    [BsonElement("gameId")]
    public string? GameId { get; set; }

    /// <summary>
    /// Invite status
    /// </summary>
    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public InviteStatus Status { get; set; }

    /// <summary>
    /// When the invite was created
    /// </summary>
    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the invite expires
    /// </summary>
    [BsonElement("expiresAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// When the invite was responded to
    /// </summary>
    [BsonElement("respondedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
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
