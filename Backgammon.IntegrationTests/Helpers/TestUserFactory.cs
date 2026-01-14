using Backgammon.IntegrationTests.Fixtures;
using Backgammon.Server.Models;
using Backgammon.Server.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Backgammon.IntegrationTests.Helpers;

/// <summary>
/// Factory for creating test users in the database.
/// </summary>
public class TestUserFactory
{
    private readonly WebApplicationFixture _fixture;

    public TestUserFactory(WebApplicationFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Creates a test user in the database and returns the user with a valid JWT token.
    /// </summary>
    /// <param name="displayName">Display name for the user (default: auto-generated).</param>
    /// <param name="username">Username for the user (default: auto-generated).</param>
    /// <param name="isAnonymous">Whether this is an anonymous user.</param>
    /// <param name="rating">Initial ELO rating (default: 1500).</param>
    /// <returns>A tuple containing the User object and a valid JWT token.</returns>
    public async Task<(User User, string JwtToken)> CreateUserAsync(
        string? displayName = null,
        string? username = null,
        bool isAnonymous = true,
        int rating = 1500)
    {
        var userId = Guid.NewGuid().ToString();
        var generatedName = $"TestPlayer_{userId[..6]}";

        var user = new User
        {
            Id = userId,
            UserId = userId,
            Username = username ?? generatedName,
            UsernameNormalized = (username ?? generatedName).ToLowerInvariant(),
            DisplayName = displayName ?? generatedName,
            IsAnonymous = isAnonymous,
            Rating = rating,
            PeakRating = rating,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow,
            IsActive = true,
            IsBanned = false
        };

        // Get the user repository and save the user
        var userRepository = _fixture.Services.GetRequiredService<IUserRepository>();
        await userRepository.CreateUserAsync(user);

        // Generate JWT token
        var token = _fixture.GenerateJwtToken(userId, user.DisplayName);

        return (user, token);
    }

    /// <summary>
    /// Creates a registered (non-anonymous) test user with an email.
    /// </summary>
    public async Task<(User User, string JwtToken)> CreateRegisteredUserAsync(
        string? displayName = null,
        string? username = null,
        string? email = null,
        int rating = 1500)
    {
        var userId = Guid.NewGuid().ToString();
        var generatedName = $"TestPlayer_{userId[..6]}";
        var generatedEmail = $"test_{userId[..6]}@example.com";

        var user = new User
        {
            Id = userId,
            UserId = userId,
            Username = username ?? generatedName,
            UsernameNormalized = (username ?? generatedName).ToLowerInvariant(),
            DisplayName = displayName ?? generatedName,
            Email = email ?? generatedEmail,
            EmailNormalized = (email ?? generatedEmail).ToLowerInvariant(),
            PasswordHash = "test-password-hash", // Not used in tests
            IsAnonymous = false,
            Rating = rating,
            PeakRating = rating,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow,
            IsActive = true,
            IsBanned = false
        };

        var userRepository = _fixture.Services.GetRequiredService<IUserRepository>();
        await userRepository.CreateUserAsync(user);

        var token = _fixture.GenerateJwtToken(userId, user.DisplayName);

        return (user, token);
    }

    /// <summary>
    /// Creates two test users for a game scenario.
    /// </summary>
    public async Task<(User White, string WhiteToken, User Red, string RedToken)> CreateTwoPlayersAsync()
    {
        var (white, whiteToken) = await CreateUserAsync(displayName: "WhitePlayer");
        var (red, redToken) = await CreateUserAsync(displayName: "RedPlayer");
        return (white, whiteToken, red, redToken);
    }
}
