using Backgammon.Server.Models;
using Backgammon.Server.Services;
using Microsoft.Extensions.Caching.Memory;
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
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<PlayerProfileService>> _loggerMock;
    private readonly PlayerProfileService _service;

    public PlayerProfileServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _gameRepositoryMock = new Mock<IGameRepository>();
        _friendshipRepositoryMock = new Mock<IFriendshipRepository>();
        _sessionManagerMock = new Mock<IGameSessionManager>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _loggerMock = new Mock<ILogger<PlayerProfileService>>();

        _service = new PlayerProfileService(
            _userRepositoryMock.Object,
            _gameRepositoryMock.Object,
            _friendshipRepositoryMock.Object,
            _sessionManagerMock.Object,
            _cache,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetPlayerProfileAsync_EmptyUsername_ReturnsError()
    {
        // Act
        var (profile, error) = await _service.GetPlayerProfileAsync("", null);

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
    public async Task GetPlayerProfileAsync_OwnProfile_IncludesPrivateData()
    {
        // Arrange
        var userId = "user123";
        var user = new User
        {
            UserId = userId,
            Username = "testuser",
            DisplayName = "Test User",
            Stats = new UserStats(),
            GameHistoryPrivacy = ProfilePrivacyLevel.Private,
            FriendsListPrivacy = ProfilePrivacyLevel.Private
        };

        _userRepositoryMock.Setup(r => r.GetByUsernameAsync("testuser"))
            .ReturnsAsync(user);
        _friendshipRepositoryMock.Setup(r => r.GetFriendsAsync(userId))
            .ReturnsAsync(new List<Friendship>());
        _gameRepositoryMock.Setup(r => r.GetPlayerGamesAsync(userId, "Completed", 10, 0))
            .ReturnsAsync(new List<Game>());

        // Act
        var (profile, error) = await _service.GetPlayerProfileAsync("testuser", userId);

        // Assert
        Assert.Null(error);
        Assert.NotNull(profile);
        Assert.NotNull(profile.RecentGames); // Should include games even though privacy is Private
        Assert.NotNull(profile.Friends); // Should include friends even though privacy is Private
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

        // Act - First call
        await _service.GetPlayerProfileAsync("testuser", null);

        // Act - Second call (should use cache)
        await _service.GetPlayerProfileAsync("testuser", null);

        // Assert - Repository should only be called once
        _userRepositoryMock.Verify(r => r.GetByUsernameAsync("testuser"), Times.Once);
    }
}
