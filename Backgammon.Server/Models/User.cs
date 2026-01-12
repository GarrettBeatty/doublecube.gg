using System.Text.Json.Serialization;

namespace Backgammon.Server.Models;

/// <summary>
/// Cosmos DB document for storing user accounts.
/// </summary>
public class User
{
    /// <summary>
    /// Default starting ELO rating for new users
    /// </summary>
    public const int DefaultStartingRating = 1500;

    /// <summary>
    /// Minimum allowed ELO rating (floor)
    /// </summary>
    public const int MinimumRating = 100;

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
    /// Whether this is an anonymous user (no email/password)
    /// </summary>
    [JsonPropertyName("isAnonymous")]
    public bool IsAnonymous { get; set; } = false;

    /// <summary>
    /// User's game statistics (denormalized for fast reads)
    /// </summary>
    [JsonPropertyName("stats")]
    public UserStats Stats { get; set; } = new();

    /// <summary>
    /// Current ELO rating (1500 = starting rating)
    /// </summary>
    [JsonPropertyName("rating")]
    public int Rating { get; set; } = DefaultStartingRating;

    /// <summary>
    /// Highest rating ever achieved
    /// </summary>
    [JsonPropertyName("peakRating")]
    public int PeakRating { get; set; } = DefaultStartingRating;

    /// <summary>
    /// When the rating was last updated
    /// </summary>
    [JsonPropertyName("ratingLastUpdatedAt")]
    public DateTime? RatingLastUpdatedAt { get; set; }

    /// <summary>
    /// Number of rated games played (used for K-factor calculation)
    /// </summary>
    [JsonPropertyName("ratedGamesCount")]
    public int RatedGamesCount { get; set; } = 0;

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

    /// <summary>
    /// Selected board theme ID (null = default theme)
    /// </summary>
    [JsonPropertyName("selectedThemeId")]
    public string? SelectedThemeId { get; set; }
}
