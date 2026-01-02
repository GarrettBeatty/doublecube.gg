using Backgammon.Server.Models;
using Backgammon.Server.Services;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Backgammon.Tests;

public class CachedUserServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<HybridCache> _cacheMock;
    private readonly Mock<ILogger<CachedUserService>> _loggerMock;
    private readonly CachedUserService _service;

    // Note: These tests focus on verifying cache invalidation logic.
    // Full integration testing of HybridCache would require a real cache instance.
    public CachedUserServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _cacheMock = new Mock<HybridCache>();
        _loggerMock = new Mock<ILogger<CachedUserService>>();
        _service = new CachedUserService(
            _userRepositoryMock.Object,
            _cacheMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task UpdateUserAsync_InvalidatesUserIdCache()
    {
        // Arrange
        var user = new User
        {
            UserId = "user123",
            Username = "testuser",
            UsernameNormalized = "testuser",
            DisplayName = "Test User",
            Email = "test@example.com",
            EmailNormalized = "test@example.com"
        };

        _userRepositoryMock
            .Setup(x => x.UpdateUserAsync(user))
            .Returns(Task.CompletedTask);

        _cacheMock
            .Setup(x => x.RemoveByTagAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        _cacheMock
            .Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _service.UpdateUserAsync(user);

        // Assert
        _userRepositoryMock.Verify(x => x.UpdateUserAsync(user), Times.Once);

        // Verify user:id tag was invalidated
        var expectedTag = "user:" + user.UserId;
        _cacheMock.Verify(
            x => x.RemoveByTagAsync(It.Is<string>(tag => tag == expectedTag), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateUserAsync_InvalidatesUsernameCache()
    {
        // Arrange
        var user = new User
        {
            UserId = "user123",
            Username = "newuser",
            UsernameNormalized = "newuser",
            DisplayName = "New User",
            Email = "test@example.com",
            EmailNormalized = "test@example.com"
        };

        _userRepositoryMock
            .Setup(x => x.UpdateUserAsync(user))
            .Returns(Task.CompletedTask);

        _cacheMock
            .Setup(x => x.RemoveByTagAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        _cacheMock
            .Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _service.UpdateUserAsync(user);

        // Assert
        // Verify current username cache was invalidated
        var expectedKey = "user:username:" + user.UsernameNormalized;
        _cacheMock.Verify(
            x => x.RemoveAsync(It.Is<string>(key => key == expectedKey), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateUserAsync_InvalidatesEmailCache()
    {
        // Arrange
        var user = new User
        {
            UserId = "user123",
            Username = "testuser",
            UsernameNormalized = "testuser",
            DisplayName = "Test User",
            Email = "newemail@example.com",
            EmailNormalized = "newemail@example.com"
        };

        _userRepositoryMock
            .Setup(x => x.UpdateUserAsync(user))
            .Returns(Task.CompletedTask);

        _cacheMock
            .Setup(x => x.RemoveByTagAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        _cacheMock
            .Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _service.UpdateUserAsync(user);

        // Assert
        // Verify current email cache was invalidated
        var expectedKey = "user:email:" + user.EmailNormalized;
        _cacheMock.Verify(
            x => x.RemoveAsync(It.Is<string>(key => key == expectedKey), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateStatsAsync_InvalidatesUserAndPlayerCaches()
    {
        // Arrange
        var userId = "user123";
        var stats = new UserStats
        {
            TotalGames = 10,
            Wins = 7,
            Losses = 3,
            TotalStakes = 15
        };

        _userRepositoryMock
            .Setup(x => x.UpdateStatsAsync(userId, stats))
            .Returns(Task.CompletedTask);

        _cacheMock
            .Setup(x => x.RemoveByTagAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _service.UpdateStatsAsync(userId, stats);

        // Assert
        _userRepositoryMock.Verify(x => x.UpdateStatsAsync(userId, stats), Times.Once);

        // Verify both user:id and player:id tags were invalidated
        var userTag = "user:" + userId;
        var playerTag = "player:" + userId;

        _cacheMock.Verify(
            x => x.RemoveByTagAsync(It.Is<string>(tag => tag == userTag), It.IsAny<CancellationToken>()),
            Times.Once);

        _cacheMock.Verify(
            x => x.RemoveByTagAsync(It.Is<string>(tag => tag == playerTag), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UsernameExistsAsync_BypassesCache()
    {
        // Arrange
        var username = "testuser";
        _userRepositoryMock
            .Setup(x => x.UsernameExistsAsync(username))
            .ReturnsAsync(true);

        // Act
        var result = await _service.UsernameExistsAsync(username);

        // Assert
        Assert.True(result);

        // Verify repository was called directly (bypasses cache)
        _userRepositoryMock.Verify(x => x.UsernameExistsAsync(username), Times.Once);
    }

    [Fact]
    public async Task EmailExistsAsync_BypassesCache()
    {
        // Arrange
        var email = "test@example.com";
        _userRepositoryMock
            .Setup(x => x.EmailExistsAsync(email))
            .ReturnsAsync(true);

        // Act
        var result = await _service.EmailExistsAsync(email);

        // Assert
        Assert.True(result);

        // Verify repository was called directly (bypasses cache)
        _userRepositoryMock.Verify(x => x.EmailExistsAsync(email), Times.Once);
    }

    [Fact]
    public async Task SearchUsersAsync_BypassesCache()
    {
        // Arrange
        var query = "test";
        var limit = 10;
        var users = new List<User>
        {
            new User { UserId = "user1", Username = "testuser1" },
            new User { UserId = "user2", Username = "testuser2" }
        };

        _userRepositoryMock
            .Setup(x => x.SearchUsersAsync(query, limit))
            .ReturnsAsync(users);

        // Act
        var result = await _service.SearchUsersAsync(query, limit);

        // Assert
        Assert.Equal(2, result.Count);

        // Verify repository was called directly (bypasses cache)
        _userRepositoryMock.Verify(x => x.SearchUsersAsync(query, limit), Times.Once);
    }

    [Fact]
    public async Task UpdateLastLoginAsync_InvalidatesUserCache()
    {
        // Arrange
        var userId = "user123";

        _userRepositoryMock
            .Setup(x => x.UpdateLastLoginAsync(userId))
            .Returns(Task.CompletedTask);

        _cacheMock
            .Setup(x => x.RemoveByTagAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _service.UpdateLastLoginAsync(userId);

        // Assert
        _userRepositoryMock.Verify(x => x.UpdateLastLoginAsync(userId), Times.Once);

        // Verify user cache was invalidated
        var expectedTag = "user:" + userId;
        _cacheMock.Verify(
            x => x.RemoveByTagAsync(It.Is<string>(tag => tag == expectedTag), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
