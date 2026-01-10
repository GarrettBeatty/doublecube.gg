namespace Backgammon.Server.Models;

/// <summary>
/// Request model for anonymous user registration
/// </summary>
public class AnonymousRegisterRequest
{
    /// <summary>
    /// Unique player ID from frontend localStorage
    /// </summary>
    public string PlayerId { get; set; } = string.Empty;

    /// <summary>
    /// Display name (e.g., "Anonymous-k5j2n9")
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
}
