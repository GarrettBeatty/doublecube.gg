namespace Backgammon.Server.Models;

/// <summary>
/// Request model for user registration
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// Username for login (3-20 chars, alphanumeric + underscores)
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Password (8+ chars)
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Display name shown to other players (optional, defaults to username)
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Email for password recovery (optional)
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Anonymous player ID to claim game history (optional)
    /// </summary>
    public string? AnonymousPlayerId { get; set; }
}
