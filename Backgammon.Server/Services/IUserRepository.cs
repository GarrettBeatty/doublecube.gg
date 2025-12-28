using Backgammon.Server.Models;

namespace Backgammon.Server.Services;

/// <summary>
/// Interface for user data access operations.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Get a user by their unique user ID
    /// </summary>
    Task<User?> GetByUserIdAsync(string userId);

    /// <summary>
    /// Get a user by their username (case-insensitive)
    /// </summary>
    Task<User?> GetByUsernameAsync(string username);

    /// <summary>
    /// Get a user by their email (case-insensitive)
    /// </summary>
    Task<User?> GetByEmailAsync(string email);

    /// <summary>
    /// Check if a username already exists
    /// </summary>
    Task<bool> UsernameExistsAsync(string username);

    /// <summary>
    /// Check if an email already exists
    /// </summary>
    Task<bool> EmailExistsAsync(string email);

    /// <summary>
    /// Create a new user
    /// </summary>
    Task CreateUserAsync(User user);

    /// <summary>
    /// Update an existing user
    /// </summary>
    Task UpdateUserAsync(User user);

    /// <summary>
    /// Update a user's stats
    /// </summary>
    Task UpdateStatsAsync(string userId, UserStats stats);

    /// <summary>
    /// Update a user's last login time
    /// </summary>
    Task UpdateLastLoginAsync(string userId);

    /// <summary>
    /// Link an anonymous player ID to a user account
    /// </summary>
    Task LinkAnonymousIdAsync(string userId, string anonymousId);

    /// <summary>
    /// Search for users by username or display name
    /// </summary>
    Task<List<User>> SearchUsersAsync(string query, int limit = 10);

    /// <summary>
    /// Get multiple users by their user IDs
    /// </summary>
    Task<List<User>> GetUsersByIdsAsync(IEnumerable<string> userIds);
}
