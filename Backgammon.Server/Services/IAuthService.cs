using Backgammon.Server.Models;

namespace Backgammon.Server.Services;

/// <summary>
/// Interface for authentication operations.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Register a new user
    /// </summary>
    Task<AuthResponse> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// Register an anonymous user
    /// </summary>
    Task<AuthResponse> RegisterAnonymousUserAsync(string playerId, string displayName);

    /// <summary>
    /// Authenticate a user and return a JWT token
    /// </summary>
    Task<AuthResponse> LoginAsync(LoginRequest request);

    /// <summary>
    /// Validate a JWT token and return the user
    /// </summary>
    Task<UserDto?> GetUserFromTokenAsync(string token);

    /// <summary>
    /// Generate a JWT token for a user
    /// </summary>
    string GenerateToken(User user);

    /// <summary>
    /// Hash a password using BCrypt
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Verify a password against a BCrypt hash
    /// </summary>
    bool VerifyPassword(string password, string hash);

    /// <summary>
    /// Validate username format
    /// </summary>
    (bool IsValid, string? Error) ValidateUsername(string username);

    /// <summary>
    /// Validate password strength
    /// </summary>
    (bool IsValid, string? Error) ValidatePassword(string password);
}
