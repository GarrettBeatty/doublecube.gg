namespace Backgammon.Server.Models;

/// <summary>
/// Represents a user's like/favorite of a board theme.
/// </summary>
public class ThemeLike
{
    /// <summary>
    /// Theme ID that was liked.
    /// </summary>
    public string ThemeId { get; set; } = string.Empty;

    /// <summary>
    /// User ID who liked the theme.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// When the like was recorded.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
