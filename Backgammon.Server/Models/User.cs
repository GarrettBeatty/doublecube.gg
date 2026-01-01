using System.Text.Json.Serialization;

namespace Backgammon.Server.Models;

/// <summary>
/// Cosmos DB document for storing user accounts.
/// </summary>
public class User
{
    /// <summary>
    /// Cosmos DB document id - uses userId as the unique identifier
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Unique user identifier (GUID) - same as id
    /// </summary>
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Username for login (unique, 3-20 chars)
    /// </summary>
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Lowercase username for case-insensitive lookups
    /// </summary>
    [JsonPropertyName("usernameNormalized")]
    public string UsernameNormalized { get; set; } = string.Empty;

    /// <summary>
    /// Display name shown to other players
    /// </summary>
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Optional email for password recovery
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// Lowercase email for case-insensitive lookups
    /// </summary>
    [JsonPropertyName("emailNormalized")]
    public string? EmailNormalized { get; set; }

    /// <summary>
    /// BCrypt password hash
    /// </summary>
    [JsonPropertyName("passwordHash")]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// When the account was created
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last successful login time
    /// </summary>
    [JsonPropertyName("lastLoginAt")]
    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last time the user was active (game activity)
    /// </summary>
    [JsonPropertyName("lastSeenAt")]
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User's game statistics (denormalized for fast reads)
    /// </summary>
    [JsonPropertyName("stats")]
    public UserStats Stats { get; set; } = new();

    /// <summary>
    /// Anonymous player IDs that have been linked to this account
    /// Used to claim game history when signing up
    /// </summary>
    [JsonPropertyName("linkedAnonymousIds")]
    public List<string> LinkedAnonymousIds { get; set; } = new();

    /// <summary>
    /// Whether the account is active
    /// </summary>
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether the account is banned
    /// </summary>
    [JsonPropertyName("isBanned")]
    public bool IsBanned { get; set; } = false;

    /// <summary>
    /// Reason for ban if banned
    /// </summary>
    [JsonPropertyName("bannedReason")]
    public string? BannedReason { get; set; }

    /// <summary>
    /// When the ban expires (null = permanent)
    /// </summary>
    [JsonPropertyName("bannedUntil")]
    public DateTime? BannedUntil { get; set; }

    /// <summary>
    /// Profile privacy settings
    /// </summary>
    [JsonPropertyName("profilePrivacy")]
    public ProfilePrivacyLevel ProfilePrivacy { get; set; } = ProfilePrivacyLevel.Public;

    /// <summary>
    /// Game history privacy settings
    /// </summary>
    [JsonPropertyName("gameHistoryPrivacy")]
    public ProfilePrivacyLevel GameHistoryPrivacy { get; set; } = ProfilePrivacyLevel.Public;

    /// <summary>
    /// Friends list privacy settings
    /// </summary>
    [JsonPropertyName("friendsListPrivacy")]
    public ProfilePrivacyLevel FriendsListPrivacy { get; set; } = ProfilePrivacyLevel.Public;
}

/// <summary>
/// User statistics - denormalized on User document for fast reads
/// </summary>
public class UserStats
{
    [JsonPropertyName("totalGames")]
    public int TotalGames { get; set; }

    [JsonPropertyName("wins")]
    public int Wins { get; set; }

    [JsonPropertyName("losses")]
    public int Losses { get; set; }

    [JsonPropertyName("totalStakes")]
    public int TotalStakes { get; set; }

    [JsonPropertyName("normalWins")]
    public int NormalWins { get; set; }

    [JsonPropertyName("gammonWins")]
    public int GammonWins { get; set; }

    [JsonPropertyName("backgammonWins")]
    public int BackgammonWins { get; set; }

    [JsonPropertyName("winStreak")]
    public int WinStreak { get; set; }

    [JsonPropertyName("bestWinStreak")]
    public int BestWinStreak { get; set; }
}

/// <summary>
/// Privacy levels for profile visibility
/// </summary>
public enum ProfilePrivacyLevel
{
    /// <summary>
    /// Visible to everyone
    /// </summary>
    Public = 0,

    /// <summary>
    /// Visible only to friends
    /// </summary>
    FriendsOnly = 1,

    /// <summary>
    /// Hidden from everyone
    /// </summary>
    Private = 2
}
