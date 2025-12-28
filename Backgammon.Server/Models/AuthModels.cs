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

/// <summary>
/// Request model for user login
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Username
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Password
    /// </summary>
    public string Password { get; set; } = string.Empty;
}

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

/// <summary>
/// User data transfer object for API responses
/// </summary>
public class UserDto
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
    /// User's game statistics
    /// </summary>
    public UserStats Stats { get; set; } = new();

    /// <summary>
    /// When the account was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Create UserDto from User entity
    /// </summary>
    public static UserDto FromUser(User user)
    {
        return new UserDto
        {
            UserId = user.UserId,
            Username = user.Username,
            DisplayName = user.DisplayName,
            Stats = user.Stats,
            CreatedAt = user.CreatedAt
        };
    }
}

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
}

/// <summary>
/// Friend data transfer object for API responses
/// </summary>
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
