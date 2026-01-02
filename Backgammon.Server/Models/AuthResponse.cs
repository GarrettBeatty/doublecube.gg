namespace Backgammon.Server.Models;

/// <summary>
/// Response model for authentication operations
/// </summary>
public class AuthResponse
{
    /// <summary>
    /// Whether the operation succeeded
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// JWT token for authenticated requests
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// Error message if operation failed
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// User info on success
    /// </summary>
    public UserDto? User { get; set; }
}
