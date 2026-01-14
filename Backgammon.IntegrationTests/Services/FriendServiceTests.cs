using Backgammon.IntegrationTests.Fixtures;
using Backgammon.Server.Models;
using Backgammon.Server.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Backgammon.IntegrationTests.Services;

/// <summary>
/// Integration tests for FriendService.
/// Tests friend request workflows, blocking, and friend list management.
/// </summary>
[Collection("SignalR")]
[Trait("Category", "Integration")]
[Trait("Component", "FriendService")]
public class FriendServiceTests : IClassFixture<WebApplicationFixture>
{
    private readonly WebApplicationFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="FriendServiceTests"/> class.
    /// </summary>
    /// <param name="fixture">The web application fixture.</param>
    public FriendServiceTests(WebApplicationFixture fixture)
    {
        _fixture = fixture;
    }

    // ==================== Send Friend Request Tests ====================

    [Fact]
    public async Task SendFriendRequest_ToValidUser_Succeeds()
    {
        // Arrange
        var friendService = GetFriendService();
        var fromUserId = await CreateTestUserAsync("FriendRequester");
        var toUserId = await CreateTestUserAsync("FriendTarget");

        // Act
        var (success, error) = await friendService.SendFriendRequestAsync(fromUserId, toUserId);

        // Assert
        success.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public async Task SendFriendRequest_ToSelf_Fails()
    {
        // Arrange
        var friendService = GetFriendService();
        var userId = await CreateTestUserAsync("SelfFriend");

        // Act
        var (success, error) = await friendService.SendFriendRequestAsync(userId, userId);

        // Assert
        success.Should().BeFalse();
        error.Should().Be("Cannot send friend request to yourself");
    }

    [Fact]
    public async Task SendFriendRequest_ToNonexistentUser_Fails()
    {
        // Arrange
        var friendService = GetFriendService();
        var fromUserId = await CreateTestUserAsync("FriendRequester2");

        // Act
        var (success, error) = await friendService.SendFriendRequestAsync(fromUserId, "nonexistent-user-id");

        // Assert
        success.Should().BeFalse();
        error.Should().Be("User not found");
    }

    [Fact]
    public async Task SendFriendRequest_WhenAlreadyPending_Fails()
    {
        // Arrange
        var friendService = GetFriendService();
        var fromUserId = await CreateTestUserAsync("DuplicateRequester");
        var toUserId = await CreateTestUserAsync("DuplicateTarget");

        // First request
        await friendService.SendFriendRequestAsync(fromUserId, toUserId);

        // Act - second request
        var (success, error) = await friendService.SendFriendRequestAsync(fromUserId, toUserId);

        // Assert
        success.Should().BeFalse();
        error.Should().Be("Friend request already pending");
    }

    // ==================== Accept Friend Request Tests ====================

    [Fact]
    public async Task AcceptFriendRequest_ValidRequest_Succeeds()
    {
        // Arrange
        var friendService = GetFriendService();
        var fromUserId = await CreateTestUserAsync("AcceptRequester");
        var toUserId = await CreateTestUserAsync("AcceptTarget");

        await friendService.SendFriendRequestAsync(fromUserId, toUserId);

        // Act - recipient accepts
        var (success, error) = await friendService.AcceptFriendRequestAsync(toUserId, fromUserId);

        // Assert
        success.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public async Task AcceptFriendRequest_NoRequest_Fails()
    {
        // Arrange
        var friendService = GetFriendService();
        var userId = await CreateTestUserAsync("NoRequestUser");
        var otherUserId = await CreateTestUserAsync("NoRequestOther");

        // Act - try to accept without pending request
        var (success, error) = await friendService.AcceptFriendRequestAsync(userId, otherUserId);

        // Assert
        success.Should().BeFalse();
        error.Should().Be("Friend request not found");
    }

    [Fact]
    public async Task AcceptFriendRequest_ByInitiator_Fails()
    {
        // Arrange
        var friendService = GetFriendService();
        var fromUserId = await CreateTestUserAsync("SelfAccepter");
        var toUserId = await CreateTestUserAsync("SelfAcceptTarget");

        await friendService.SendFriendRequestAsync(fromUserId, toUserId);

        // Act - initiator tries to accept their own request
        var (success, error) = await friendService.AcceptFriendRequestAsync(fromUserId, toUserId);

        // Assert
        success.Should().BeFalse();
        error.Should().Be("Cannot accept your own friend request");
    }

    // ==================== Decline Friend Request Tests ====================

    [Fact]
    public async Task DeclineFriendRequest_ValidRequest_Succeeds()
    {
        // Arrange
        var friendService = GetFriendService();
        var fromUserId = await CreateTestUserAsync("DeclineRequester");
        var toUserId = await CreateTestUserAsync("DeclineTarget");

        await friendService.SendFriendRequestAsync(fromUserId, toUserId);

        // Act
        var (success, error) = await friendService.DeclineFriendRequestAsync(toUserId, fromUserId);

        // Assert
        success.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public async Task DeclineFriendRequest_NoRequest_Fails()
    {
        // Arrange
        var friendService = GetFriendService();
        var userId = await CreateTestUserAsync("NoDeclineUser");
        var otherUserId = await CreateTestUserAsync("NoDeclineOther");

        // Act
        var (success, error) = await friendService.DeclineFriendRequestAsync(userId, otherUserId);

        // Assert
        success.Should().BeFalse();
        error.Should().Be("Friend request not found");
    }

    // ==================== Remove Friend Tests ====================

    [Fact]
    public async Task RemoveFriend_ExistingFriend_Succeeds()
    {
        // Arrange
        var friendService = GetFriendService();
        var userId1 = await CreateTestUserAsync("RemoveFriend1");
        var userId2 = await CreateTestUserAsync("RemoveFriend2");

        // Create friendship
        await friendService.SendFriendRequestAsync(userId1, userId2);
        await friendService.AcceptFriendRequestAsync(userId2, userId1);

        // Act
        var (success, error) = await friendService.RemoveFriendAsync(userId1, userId2);

        // Assert
        success.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public async Task RemoveFriend_NotFriends_Fails()
    {
        // Arrange
        var friendService = GetFriendService();
        var userId1 = await CreateTestUserAsync("NotFriend1");
        var userId2 = await CreateTestUserAsync("NotFriend2");

        // Act
        var (success, error) = await friendService.RemoveFriendAsync(userId1, userId2);

        // Assert
        success.Should().BeFalse();
        error.Should().Be("Not friends with this user");
    }

    // ==================== Block User Tests ====================

    [Fact]
    public async Task BlockUser_ValidUser_Succeeds()
    {
        // Arrange
        var friendService = GetFriendService();
        var userId = await CreateTestUserAsync("Blocker");
        var blockedUserId = await CreateTestUserAsync("Blocked");

        // Act
        var (success, error) = await friendService.BlockUserAsync(userId, blockedUserId);

        // Assert
        success.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public async Task BlockUser_Self_Fails()
    {
        // Arrange
        var friendService = GetFriendService();
        var userId = await CreateTestUserAsync("SelfBlocker");

        // Act
        var (success, error) = await friendService.BlockUserAsync(userId, userId);

        // Assert
        success.Should().BeFalse();
        error.Should().Be("Cannot block yourself");
    }

    [Fact]
    public async Task BlockUser_NonexistentUser_Fails()
    {
        // Arrange
        var friendService = GetFriendService();
        var userId = await CreateTestUserAsync("BlockerOfNone");

        // Act
        var (success, error) = await friendService.BlockUserAsync(userId, "nonexistent-user");

        // Assert
        success.Should().BeFalse();
        error.Should().Be("User not found");
    }

    [Fact]
    public async Task SendFriendRequest_ToBlockedUser_Fails()
    {
        // Arrange
        var friendService = GetFriendService();
        var userId = await CreateTestUserAsync("BlockedRequester");
        var blockerUserId = await CreateTestUserAsync("BlockingUser");

        // Block the requester
        await friendService.BlockUserAsync(blockerUserId, userId);

        // Act - blocked user tries to send friend request
        var (success, error) = await friendService.SendFriendRequestAsync(userId, blockerUserId);

        // Assert
        success.Should().BeFalse();
        error.Should().Be("Cannot send friend request to this user");
    }

    // ==================== Get Friends Tests ====================

    [Fact]
    public async Task GetFriends_WithFriends_ReturnsFriendsList()
    {
        // Arrange
        var friendService = GetFriendService();
        var userId = await CreateTestUserAsync("GetFriendsUser");
        var friendId = await CreateTestUserAsync("GetFriendsFriend");

        // Create friendship
        await friendService.SendFriendRequestAsync(userId, friendId);
        await friendService.AcceptFriendRequestAsync(friendId, userId);

        // Act
        var friends = await friendService.GetFriendsAsync(userId);

        // Assert
        friends.Should().NotBeEmpty();
        friends.Should().Contain(f => f.UserId == friendId);
    }

    [Fact]
    public async Task GetFriends_NoFriends_ReturnsEmptyList()
    {
        // Arrange
        var friendService = GetFriendService();
        var userId = await CreateTestUserAsync("LonelyUser");

        // Act
        var friends = await friendService.GetFriendsAsync(userId);

        // Assert
        friends.Should().BeEmpty();
    }

    // ==================== Get Pending Requests Tests ====================

    [Fact]
    public async Task GetPendingRequests_WithPendingRequests_ReturnsRequests()
    {
        // Arrange
        var friendService = GetFriendService();
        var fromUserId = await CreateTestUserAsync("PendingFrom");
        var toUserId = await CreateTestUserAsync("PendingTo");

        await friendService.SendFriendRequestAsync(fromUserId, toUserId);

        // Act
        var requests = await friendService.GetPendingRequestsAsync(toUserId);

        // Assert
        requests.Should().NotBeEmpty();
        requests.Should().Contain(r => r.UserId == fromUserId);
    }

    [Fact]
    public async Task GetPendingRequests_NoPendingRequests_ReturnsEmptyList()
    {
        // Arrange
        var friendService = GetFriendService();
        var userId = await CreateTestUserAsync("NoPendingUser");

        // Act
        var requests = await friendService.GetPendingRequestsAsync(userId);

        // Assert
        requests.Should().BeEmpty();
    }

    // ==================== Already Friends Tests ====================

    [Fact]
    public async Task SendFriendRequest_WhenAlreadyFriends_Fails()
    {
        // Arrange
        var friendService = GetFriendService();
        var userId1 = await CreateTestUserAsync("AlreadyFriend1");
        var userId2 = await CreateTestUserAsync("AlreadyFriend2");

        // Create friendship
        await friendService.SendFriendRequestAsync(userId1, userId2);
        await friendService.AcceptFriendRequestAsync(userId2, userId1);

        // Act - try to send another request
        var (success, error) = await friendService.SendFriendRequestAsync(userId1, userId2);

        // Assert
        success.Should().BeFalse();
        error.Should().Be("Already friends with this user");
    }

    // ==================== Private Helpers ====================

    private IFriendService GetFriendService() =>
        _fixture.Services.GetRequiredService<IFriendService>();

    private IUserRepository GetUserRepository() =>
        _fixture.Services.GetRequiredService<IUserRepository>();

    private async Task<string> CreateTestUserAsync(string displayName)
    {
        var userId = $"test_{Guid.NewGuid():N}";
        var user = new User
        {
            UserId = userId,
            Username = displayName,
            UsernameNormalized = displayName.ToLowerInvariant(),
            DisplayName = displayName,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow,
            Stats = new UserStats(),
            IsActive = true
        };
        await GetUserRepository().CreateUserAsync(user);
        return userId;
    }
}
