namespace Backgammon.Server.Models;

/// <summary>
/// Request model for updating user profile
/// </summary>
public class UpdateProfileRequest
{
    /// <summary>
    /// New display name (optional)
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// New email (optional)
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Profile privacy level (optional)
    /// </summary>
    public ProfilePrivacyLevel? ProfilePrivacy { get; set; }

    /// <summary>
    /// Game history privacy level (optional)
    /// </summary>
    public ProfilePrivacyLevel? GameHistoryPrivacy { get; set; }

    /// <summary>
    /// Friends list privacy level (optional)
    /// </summary>
    public ProfilePrivacyLevel? FriendsListPrivacy { get; set; }
}
