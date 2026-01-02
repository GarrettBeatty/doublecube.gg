namespace Backgammon.Server.Models;

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
