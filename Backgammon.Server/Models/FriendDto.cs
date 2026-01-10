using Tapper;

namespace Backgammon.Server.Models;

/// <summary>
/// Friend data transfer object for API responses
/// </summary>
[TranspilationSource]
public class FriendDto
{
    /// <summary>
    /// Friend's user ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Friend's username
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Friend's display name
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Whether the friend is currently online
    /// </summary>
    public bool IsOnline { get; set; }

    /// <summary>
    /// Friendship status
    /// </summary>
    public FriendshipStatus Status { get; set; }

    /// <summary>
    /// Who initiated the friend request
    /// </summary>
    public string InitiatedBy { get; set; } = string.Empty;
}
