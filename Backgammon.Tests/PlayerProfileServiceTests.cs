using Backgammon.Server.Configuration;
using Backgammon.Server.Models;
using Backgammon.Server.Services;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Backgammon.Tests;

public class PlayerProfileServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IGameRepository> _gameRepositoryMock;
    private readonly Mock<IFriendshipRepository> _friendshipRepositoryMock;
    private readonly Mock<IGameSessionManager> _sessionManagerMock;
    private readonly HybridCache _cache;
    private readonly CacheSettings _cacheSettings;
    private readonly Mock<ILogger<PlayerProfileService>> _loggerMock;
    private readonly PlayerProfileService _service;

    public PlayerProfileServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _gameRepositoryMock = new Mock<IGameRepository>();
        _friendshipRepositoryMock = new Mock<IFriendshipRepository>();
        _sessionManagerMock = new Mock<IGameSessionManager>();

        // Set up HybridCache with in-memory backend for testing
        var services = new ServiceCollection();
        services.AddMemoryCache();
        services.AddHybridCache();
        var serviceProvider = services.BuildServiceProvider();
        _cache = serviceProvider.GetRequiredService<HybridCache>();

        _cacheSettings = new CacheSettings();
        _loggerMock = new Mock<ILogger<PlayerProfileService>>();

        _service = new PlayerProfileService(
            _userRepositoryMock.Object,
            _gameRepositoryMock.Object,
            _friendshipRepositoryMock.Object,
            _sessionManagerMock.Object,
            _cache,
            _cacheSettings,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetPlayerProfileAsync_EmptyUsername_ReturnsError()
    {
        // Act
        var (profile, error) = await _service.GetPlayerProfileAsync(string.Empty, null);

        // Assert
        Assert.Null(profile);
        Assert.Equal("Username is required", error);
    }

    [Fact]
    public async Task GetPlayerProfileAsync_UserNotFound_ReturnsError()
    {
        // Arrange
        _userRepositoryMock.Setup(r => r.GetByUsernameAsync("nonexistent"))
            .ReturnsAsync((User?)null);

        // Act
        var (profile, error) = await _service.GetPlayerProfileAsync("nonexistent", null);

        // Assert
        Assert.Null(profile);
        Assert.Equal("User not found", error);
    }

    [Fact]
    public async Task GetPlayerProfileAsync_ValidUser_ReturnsProfile()
    {
        // Arrange
        var user = new User
        {
            UserId = "user123",
            Username = "testuser",
            DisplayName = "Test User",
            Stats = new UserStats()
        };

        _userRepositoryMock.Setup(r => r.GetByUsernameAsync("testuser"))
            .ReturnsAsync(user);
        _friendshipRepositoryMock.Setup(r => r.GetFriendsAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<Friendship>());
        _gameRepositoryMock.Setup(r => r.GetPlayerGamesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new List<Game>());

        // Act
        var (profile, error) = await _service.GetPlayerProfileAsync("testuser", null);

        // Assert
        Assert.Null(error);
        Assert.NotNull(profile);
        Assert.Equal("testuser", profile.Username);
        Assert.Equal("Test User", profile.DisplayName);
    }

    [Fact]
    public async Task GetPlayerProfileAsync_AllDataIsPublic()
    {
        // Arrange
        var userId = "user123";
        var user = new User
        {
            UserId = userId,
            Username = "testuser",
            DisplayName = "Test User",
            Stats = new UserStats(),
            GameHistoryPrivacy = ProfilePrivacyLevel.Private, // Privacy settings no longer enforced
            FriendsListPrivacy = ProfilePrivacyLevel.Private // Privacy settings no longer enforced
        };

        _userRepositoryMock.Setup(r => r.GetByUsernameAsync("testuser"))
            .ReturnsAsync(user);
        _friendshipRepositoryMock.Setup(r => r.GetFriendsAsync(userId))
            .ReturnsAsync(new List<Friendship>());
        _gameRepositoryMock.Setup(r => r.GetPlayerGamesAsync(userId, "Completed", 10, 0))
            .ReturnsAsync(new List<Game>());

        // Act - Even as anonymous viewer, all data is visible
        var (profile, error) = await _service.GetPlayerProfileAsync("testuser", null);

        // Assert
        Assert.Null(error);
        Assert.NotNull(profile);
        Assert.NotNull(profile.RecentGames); // Always included (privacy removed)
        Assert.NotNull(profile.Friends); // Always included (privacy removed)
    }

    [Fact]
    public async Task GetPlayerProfileAsync_CachesResult()
    {
        // Arrange
        var user = new User
        {
            UserId = "user123",
            Username = "testuser",
            DisplayName = "Test User",
            Stats = new UserStats()
        };

        _userRepositoryMock.Setup(r => r.GetByUsernameAsync("testuser"))
            .ReturnsAsync(user);
        _friendshipRepositoryMock.Setup(r => r.GetFriendsAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<Friendship>());
        _gameRepositoryMock.Setup(r => r.GetPlayerGamesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new List<Game>());

        // Act - First call as anonymous
        await _service.GetPlayerProfileAsync("testuser", null);

        // Act - Second call as different viewer (should use same cache - no viewer-specific caching)
        await _service.GetPlayerProfileAsync("testuser", "viewer123");

        // Assert - User repository is called twice (needed for cache tag generation)
        // but expensive operations (games, friendships) should only be called once due to caching
        _userRepositoryMock.Verify(r => r.GetByUsernameAsync("testuser"), Times.Exactly(2));
        _gameRepositoryMock.Verify(r => r.GetPlayerGamesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
        // FriendshipRepository called once: only when building the cached profile
        _friendshipRepositoryMock.Verify(r => r.GetFriendsAsync(It.IsAny<string>()), Times.Once);
    }
}
