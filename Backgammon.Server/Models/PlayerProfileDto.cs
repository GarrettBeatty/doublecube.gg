using Tapper;

namespace Backgammon.Server.Models;

/// <summary>
/// Player profile data transfer object for public profile pages
/// </summary>
[TranspilationSource]
public class PlayerProfileDto
{
    /// <summary>
    /// User's unique ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Username
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Display name
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// User's game statistics (null if private)
    /// </summary>
    public UserStats? Stats { get; set; }

    /// <summary>
    /// User's current ELO rating
    /// </summary>
    public int Rating { get; set; }

    /// <summary>
    /// User's peak ELO rating
    /// </summary>
    public int PeakRating { get; set; }

    /// <summary>
    /// When the account was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Whether the profile is private
    /// </summary>
    public bool IsPrivate { get; set; }

    /// <summary>
    /// Recent games (null if private or friends-only)
    /// </summary>
    public List<GameSummaryDto>? RecentGames { get; set; }

    /// <summary>
    /// Friends list (null if private or friends-only)
    /// </summary>
    public List<FriendDto>? Friends { get; set; }

    /// <summary>
    /// Whether the viewing user is friends with this player
    /// </summary>
    public bool IsFriend { get; set; }

    /// <summary>
    /// Profile privacy level
    /// </summary>
    public ProfilePrivacyLevel ProfilePrivacy { get; set; }

    /// <summary>
    /// Game history privacy level
    /// </summary>
    public ProfilePrivacyLevel GameHistoryPrivacy { get; set; }

    /// <summary>
    /// Friends list privacy level
    /// </summary>
    public ProfilePrivacyLevel FriendsListPrivacy { get; set; }

    /// <summary>
    /// Create PlayerProfileDto from User entity, respecting privacy settings
    /// </summary>
    public static PlayerProfileDto FromUser(User user, bool isFriend = false, bool isOwnProfile = false)
    {
        var profile = new PlayerProfileDto
        {
            UserId = user.UserId,
            Username = user.Username,
            DisplayName = user.DisplayName,
            CreatedAt = user.CreatedAt,
            Rating = user.Rating,
            PeakRating = user.PeakRating,
            ProfilePrivacy = user.ProfilePrivacy,
            GameHistoryPrivacy = user.GameHistoryPrivacy,
            FriendsListPrivacy = user.FriendsListPrivacy,
            IsFriend = isFriend
        };

        // Always show stats to own profile or if public
        if (isOwnProfile || user.ProfilePrivacy == ProfilePrivacyLevel.Public ||
            (user.ProfilePrivacy == ProfilePrivacyLevel.FriendsOnly && isFriend))
        {
            profile.Stats = user.Stats;
        }
        else
        {
            profile.IsPrivate = true;
        }

        // Note: RecentGames and Friends will be populated by the service layer
        // based on privacy settings
        return profile;
    }
}
