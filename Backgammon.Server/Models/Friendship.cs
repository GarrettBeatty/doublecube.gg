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
