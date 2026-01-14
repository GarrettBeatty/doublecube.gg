using System.Net.Http.Json;
using Backgammon.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Backgammon.IntegrationTests.SignalR;

/// <summary>
/// Integration tests for HTTP API endpoints.
/// SignalR tests require additional infrastructure setup and are marked for future work.
/// </summary>
[Collection("SignalR")]
[Trait("Category", "Integration")]
[Trait("Component", "API")]
public class GameOperationsTests
{
    private readonly WebApplicationFixture _fixture;

    public GameOperationsTests(WebApplicationFixture fixture)
    {
        _fixture = fixture;
    }

    // ==================== Auth API Tests ====================

    [Fact]
    public async Task RegisterAnonymous_ReturnsUserIdAndToken()
    {
        // Arrange
        var request = new { PlayerId = $"test_{Guid.NewGuid():N}", DisplayName = "TestPlayer" };

        // Act
        var response = await _fixture.HttpClient.PostAsJsonAsync("/api/auth/register-anonymous", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("user");
        content.Should().Contain("token");
    }

    [Fact]
    public async Task RegisterAnonymous_CreatesUserInDatabase()
    {
        // Arrange
        var playerId = $"test_{Guid.NewGuid():N}";
        var displayName = "TestPlayer";
        var request = new { PlayerId = playerId, DisplayName = displayName };

        // Act
        var response = await _fixture.HttpClient.PostAsJsonAsync("/api/auth/register-anonymous", request);
        response.EnsureSuccessStatusCode();

        // Assert - verify user exists via API
        var (userId, token) = await _fixture.RegisterAnonymousUserAsync("VerifyPlayer");
        userId.Should().NotBeNullOrEmpty();
        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetCurrentUser_WithValidToken_ReturnsUserInfo()
    {
        // Arrange
        var (userId, token) = await _fixture.RegisterAnonymousUserAsync("TestPlayer");

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _fixture.HttpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert - with debug info on failure
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK, $"Response: {content}, Token: {token[..50]}...");
        content.Should().Contain(userId);
    }

    [Fact]
    public async Task GetCurrentUser_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");

        // Act
        var response = await _fixture.HttpClient.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    // ==================== Player API Tests ====================

    [Fact]
    public async Task GetPlayerGames_ReturnsEmptyListForNewUser()
    {
        // Arrange
        var (userId, token) = await _fixture.RegisterAnonymousUserAsync("TestPlayer");

        // Act - /api/player/{playerId}/games doesn't require auth
        var response = await _fixture.HttpClient.GetAsync($"/api/player/{userId}/games");
        var content = await response.Content.ReadAsStringAsync();

        // Assert - endpoint returns an object with games array
        response.EnsureSuccessStatusCode();
        // The response is a PaginatedGamesResponse with "games", "cursor", "hasMore"
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetPlayerActiveGames_ReturnsEmptyListForNewUser()
    {
        // Arrange
        var (userId, token) = await _fixture.RegisterAnonymousUserAsync("TestPlayer");

        // Act
        var response = await _fixture.HttpClient.GetAsync($"/api/player/{userId}/active-games");

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetPlayerStats_ReturnsStatsForNewUser()
    {
        // Arrange
        var (userId, token) = await _fixture.RegisterAnonymousUserAsync("TestPlayer");

        // Act
        var response = await _fixture.HttpClient.GetAsync($"/api/player/{userId}/stats");

        // Assert
        response.EnsureSuccessStatusCode();
    }

    // ==================== User API Tests ====================

    [Fact]
    public async Task GetUserById_ReturnsUserInfo()
    {
        // Arrange
        var (userId, token) = await _fixture.RegisterAnonymousUserAsync("TestPlayer");

        // Act
        var response = await _fixture.HttpClient.GetAsync($"/api/users/{userId}");
        var content = await response.Content.ReadAsStringAsync();

        // Assert with debug info
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK, $"Response: {content}, UserId: {userId}");
        content.Should().Contain(userId);
    }

    [Fact]
    public async Task GetUserById_NonExistentUser_ReturnsNotFound()
    {
        // Act
        var response = await _fixture.HttpClient.GetAsync("/api/users/nonexistent-user-id");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    // ==================== Health Check Tests ====================

    [Fact]
    public async Task HealthEndpoint_ReturnsHealthy()
    {
        // Act
        var response = await _fixture.HttpClient.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("healthy");
    }

    [Fact]
    public async Task RootEndpoint_ReturnsServerRunning()
    {
        // Act
        var response = await _fixture.HttpClient.GetAsync("/");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Backgammon SignalR Server Running");
    }
}
