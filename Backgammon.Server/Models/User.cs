using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Backgammon.Server.Models;

/// <summary>
/// MongoDB document for storing user accounts.
/// </summary>
public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    /// <summary>
    /// Unique user identifier (GUID)
    /// </summary>
    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Username for login (unique, 3-20 chars)
    /// </summary>
    [BsonElement("username")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Lowercase username for case-insensitive lookups
    /// </summary>
    [BsonElement("usernameNormalized")]
    public string UsernameNormalized { get; set; } = string.Empty;

    /// <summary>
    /// Display name shown to other players
    /// </summary>
    [BsonElement("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Optional email for password recovery
    /// </summary>
    [BsonElement("email")]
    public string? Email { get; set; }

    /// <summary>
    /// Lowercase email for case-insensitive lookups
    /// </summary>
    [BsonElement("emailNormalized")]
    public string? EmailNormalized { get; set; }

    /// <summary>
    /// BCrypt password hash
    /// </summary>
    [BsonElement("passwordHash")]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// When the account was created
    /// </summary>
    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last successful login time
    /// </summary>
    [BsonElement("lastLoginAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last time the user was active (game activity)
    /// </summary>
    [BsonElement("lastSeenAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User's game statistics (denormalized for fast reads)
    /// </summary>
    [BsonElement("stats")]
    public UserStats Stats { get; set; } = new();

    /// <summary>
    /// Anonymous player IDs that have been linked to this account
    /// Used to claim game history when signing up
    /// </summary>
    [BsonElement("linkedAnonymousIds")]
    public List<string> LinkedAnonymousIds { get; set; } = new();

    /// <summary>
    /// Whether the account is active
    /// </summary>
    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether the account is banned
    /// </summary>
    [BsonElement("isBanned")]
    public bool IsBanned { get; set; } = false;

    /// <summary>
    /// Reason for ban if banned
    /// </summary>
    [BsonElement("bannedReason")]
    public string? BannedReason { get; set; }

    /// <summary>
    /// When the ban expires (null = permanent)
    /// </summary>
    [BsonElement("bannedUntil")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? BannedUntil { get; set; }
}

/// <summary>
/// User statistics - denormalized on User document for fast reads
/// </summary>
public class UserStats
{
    [BsonElement("totalGames")]
    public int TotalGames { get; set; }

    [BsonElement("wins")]
    public int Wins { get; set; }

    [BsonElement("losses")]
    public int Losses { get; set; }

    [BsonElement("totalStakes")]
    public int TotalStakes { get; set; }

    [BsonElement("normalWins")]
    public int NormalWins { get; set; }

    [BsonElement("gammonWins")]
    public int GammonWins { get; set; }

    [BsonElement("backgammonWins")]
    public int BackgammonWins { get; set; }

    [BsonElement("winStreak")]
    public int WinStreak { get; set; }

    [BsonElement("bestWinStreak")]
    public int BestWinStreak { get; set; }
}
