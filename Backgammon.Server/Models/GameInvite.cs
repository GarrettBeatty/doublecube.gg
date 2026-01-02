using System.Text.Json.Serialization;

namespace Backgammon.Server.Models;

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
