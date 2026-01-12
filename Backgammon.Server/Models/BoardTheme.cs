using System.Text.Json.Serialization;

namespace Backgammon.Server.Models;

/// <summary>
/// A board theme with customizable colors and metadata.
/// </summary>
public class BoardTheme
{
    /// <summary>
    /// Unique identifier for the theme.
    /// </summary>
    [JsonPropertyName("themeId")]
    public string ThemeId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Display name for the theme.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the theme.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// User ID of the theme creator.
    /// </summary>
    [JsonPropertyName("authorId")]
    public string AuthorId { get; set; } = string.Empty;

    /// <summary>
    /// Username of the theme creator (denormalized for display).
    /// </summary>
    [JsonPropertyName("authorUsername")]
    public string AuthorUsername { get; set; } = string.Empty;

    /// <summary>
    /// Visibility of the theme (public, private, or unlisted).
    /// </summary>
    [JsonPropertyName("visibility")]
    public ThemeVisibility Visibility { get; set; } = ThemeVisibility.Public;

    /// <summary>
    /// Whether this is a system-provided default theme.
    /// </summary>
    [JsonPropertyName("isDefault")]
    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// When the theme was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the theme was last updated.
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Number of users currently using this theme.
    /// </summary>
    [JsonPropertyName("usageCount")]
    public int UsageCount { get; set; } = 0;

    /// <summary>
    /// Number of likes this theme has received.
    /// </summary>
    [JsonPropertyName("likeCount")]
    public int LikeCount { get; set; } = 0;

    /// <summary>
    /// The theme's color configuration.
    /// </summary>
    [JsonPropertyName("colors")]
    public ThemeColors Colors { get; set; } = new();

    /// <summary>
    /// Optional URL to a thumbnail preview image.
    /// </summary>
    [JsonPropertyName("thumbnailUrl")]
    public string? ThumbnailUrl { get; set; }
}
