using Backgammon.Server.Models;
using Backgammon.Server.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Backgammon.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IGameRepository> _mockGameRepository;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<AuthService>> _mockLogger;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockGameRepository = new Mock<IGameRepository>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<AuthService>>();

        // Setup JWT configuration
        _mockConfiguration.Setup(c => c["Jwt:Secret"]).Returns("test-secret-key-that-is-at-least-32-characters-long");
        _mockConfiguration.Setup(c => c["Jwt:Issuer"]).Returns("test-issuer");
        _mockConfiguration.Setup(c => c["Jwt:Audience"]).Returns("test-audience");
        _mockConfiguration.Setup(c => c["Jwt:ExpirationDays"]).Returns("7");

        _authService = new AuthService(
            _mockUserRepository.Object,
            _mockGameRepository.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);
    }

    // ==================== ValidateUsername Tests ====================

    [Fact]
    public void ValidateUsername_NullOrEmpty_ReturnsInvalid()
    {
        // Act
        var result = _authService.ValidateUsername(string.Empty);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Username is required", result.Error);
    }

    [Fact]
    public void ValidateUsername_TooShort_ReturnsInvalid()
    {
        // Act
        var result = _authService.ValidateUsername("ab");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Username must be at least 3 characters", result.Error);
    }

    [Fact]
    public void ValidateUsername_TooLong_ReturnsInvalid()
    {
        // Act
        var result = _authService.ValidateUsername("a_very_long_username_that_exceeds_20_chars");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Username must be 20 characters or less", result.Error);
    }

    [Fact]
    public void ValidateUsername_InvalidCharacters_ReturnsInvalid()
    {
        // Act
        var result = _authService.ValidateUsername("user@name!");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Username can only contain letters, numbers, and underscores", result.Error);
    }

    [Fact]
    public void ValidateUsername_Valid_ReturnsValid()
    {
        // Act
        var result = _authService.ValidateUsername("valid_user123");

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.Error);
    }

    // ==================== ValidatePassword Tests ====================

    [Fact]
    public void ValidatePassword_NullOrEmpty_ReturnsInvalid()
    {
        // Act
        var result = _authService.ValidatePassword(string.Empty);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Password is required", result.Error);
    }

    [Fact]
    public void ValidatePassword_TooShort_ReturnsInvalid()
    {
        // Act
        var result = _authService.ValidatePassword("short");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("Password must be at least 8 characters", result.Error);
    }

    [Fact]
    public void ValidatePassword_Valid_ReturnsValid()
    {
        // Act
        var result = _authService.ValidatePassword("validpassword123");

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.Error);
    }

    // ==================== HashPassword / VerifyPassword Tests ====================

    [Fact]
    public void HashPassword_CreatesValidHash()
    {
        // Arrange
        var password = "mySecurePassword123";

        // Act
        var hash = _authService.HashPassword(password);

        // Assert
        Assert.NotNull(hash);
        Assert.NotEqual(password, hash);
        Assert.StartsWith("$2", hash); // BCrypt hash prefix
    }

    [Fact]
    public void VerifyPassword_CorrectPassword_ReturnsTrue()
    {
        // Arrange
        var password = "mySecurePassword123";
        var hash = _authService.HashPassword(password);

        // Act
        var result = _authService.VerifyPassword(password, hash);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_WrongPassword_ReturnsFalse()
    {
        // Arrange
        var password = "mySecurePassword123";
        var hash = _authService.HashPassword(password);

        // Act
        var result = _authService.VerifyPassword("wrongPassword", hash);

        // Assert
        Assert.False(result);
    }

    // ==================== GenerateToken Tests ====================

    [Fact]
    public void GenerateToken_CreatesValidJwtToken()
    {
        // Arrange
        var user = new User
        {
            UserId = "user-123",
            Username = "testuser",
            DisplayName = "Test User"
        };

        // Act
        var token = _authService.GenerateToken(user);

        // Assert
        Assert.NotNull(token);
        Assert.Contains(".", token); // JWT has 3 parts separated by dots
        var parts = token.Split('.');
        Assert.Equal(3, parts.Length);
    }

    // ==================== RegisterAsync Tests ====================

    [Fact]
    public async Task RegisterAsync_InvalidUsername_ReturnsFailure()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "ab", // Too short
            Password = "validpassword123"
        };

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("3 characters", result.Error);
    }

    [Fact]
    public async Task RegisterAsync_InvalidPassword_ReturnsFailure()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "validuser",
            Password = "short" // Too short
        };

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("8 characters", result.Error);
    }

    [Fact]
    public async Task RegisterAsync_UsernameExists_ReturnsFailure()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "existinguser",
            Password = "validpassword123"
        };

        _mockUserRepository.Setup(r => r.UsernameExistsAsync("existinguser"))
            .ReturnsAsync(true);

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Username already taken", result.Error);
    }

    [Fact]
    public async Task RegisterAsync_EmailExists_ReturnsFailure()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "newuser",
            Password = "validpassword123",
            Email = "existing@example.com"
        };

        _mockUserRepository.Setup(r => r.UsernameExistsAsync("newuser"))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(r => r.EmailExistsAsync("existing@example.com"))
            .ReturnsAsync(true);

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Email already registered", result.Error);
    }

    [Fact]
    public async Task RegisterAsync_Success_ReturnsTokenAndUser()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "newuser",
            Password = "validpassword123",
            DisplayName = "New User"
        };

        _mockUserRepository.Setup(r => r.UsernameExistsAsync("newuser"))
            .ReturnsAsync(false);

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Token);
        Assert.NotNull(result.User);
        Assert.Equal("newuser", result.User.Username);
        Assert.Equal("New User", result.User.DisplayName);
        _mockUserRepository.Verify(r => r.CreateUserAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithAnonymousPlayerId_LinksStats()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "newuser",
            Password = "validpassword123",
            AnonymousPlayerId = "anon-123"
        };

        var playerStats = new PlayerStats
        {
            TotalGames = 10,
            Wins = 5,
            Losses = 5
        };

        _mockUserRepository.Setup(r => r.UsernameExistsAsync("newuser"))
            .ReturnsAsync(false);
        _mockGameRepository.Setup(r => r.GetPlayerStatsAsync("anon-123"))
            .ReturnsAsync(playerStats);

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.True(result.Success);
        _mockUserRepository.Verify(
            r => r.CreateUserAsync(It.Is<User>(u => u.LinkedAnonymousIds.Contains("anon-123"))),
            Times.Once);
    }

    // ==================== RegisterAnonymousUserAsync Tests ====================

    [Fact]
    public async Task RegisterAnonymousUserAsync_UserExists_ReturnsExistingToken()
    {
        // Arrange
        var existingUser = new User
        {
            UserId = "player-123",
            Username = "ExistingPlayer",
            DisplayName = "Existing Player"
        };

        _mockUserRepository.Setup(r => r.GetByUserIdAsync("player-123"))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _authService.RegisterAnonymousUserAsync("player-123", "New Name");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Token);
        Assert.Equal("Existing Player", result.User!.DisplayName);
        _mockUserRepository.Verify(r => r.CreateUserAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAnonymousUserAsync_NewUser_CreatesUser()
    {
        // Arrange
        _mockUserRepository.Setup(r => r.GetByUserIdAsync("new-player"))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _authService.RegisterAnonymousUserAsync("new-player", "New Player");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Token);
        Assert.Equal("New Player", result.User!.DisplayName);
        _mockUserRepository.Verify(
            r => r.CreateUserAsync(It.Is<User>(u => u.UserId == "new-player" && u.IsAnonymous)),
            Times.Once);
    }

    // ==================== LoginAsync Tests ====================

    [Fact]
    public async Task LoginAsync_UserNotFound_ReturnsFailure()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "nonexistent",
            Password = "password123"
        };

        _mockUserRepository.Setup(r => r.GetByUsernameAsync("nonexistent"))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Invalid username or password", result.Error);
    }

    [Fact]
    public async Task LoginAsync_UserInactive_ReturnsFailure()
    {
        // Arrange
        var user = new User
        {
            UserId = "user-123",
            Username = "testuser",
            PasswordHash = _authService.HashPassword("password123"),
            IsActive = false
        };

        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "password123"
        };

        _mockUserRepository.Setup(r => r.GetByUsernameAsync("testuser"))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Account is disabled", result.Error);
    }

    [Fact]
    public async Task LoginAsync_UserBanned_ReturnsFailure()
    {
        // Arrange
        var user = new User
        {
            UserId = "user-123",
            Username = "testuser",
            PasswordHash = _authService.HashPassword("password123"),
            IsActive = true,
            IsBanned = true,
            BannedUntil = DateTime.UtcNow.AddDays(7),
            BannedReason = "Cheating"
        };

        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "password123"
        };

        _mockUserRepository.Setup(r => r.GetByUsernameAsync("testuser"))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("banned", result.Error);
        Assert.Contains("Cheating", result.Error);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsFailure()
    {
        // Arrange
        var user = new User
        {
            UserId = "user-123",
            Username = "testuser",
            PasswordHash = _authService.HashPassword("correctpassword"),
            IsActive = true
        };

        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "wrongpassword"
        };

        _mockUserRepository.Setup(r => r.GetByUsernameAsync("testuser"))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Invalid username or password", result.Error);
    }

    [Fact]
    public async Task LoginAsync_Success_ReturnsTokenAndUpdatesLastLogin()
    {
        // Arrange
        var user = new User
        {
            UserId = "user-123",
            Username = "testuser",
            DisplayName = "Test User",
            PasswordHash = _authService.HashPassword("correctpassword"),
            IsActive = true
        };

        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "correctpassword"
        };

        _mockUserRepository.Setup(r => r.GetByUsernameAsync("testuser"))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Token);
        Assert.NotNull(result.User);
        Assert.Equal("testuser", result.User.Username);
        _mockUserRepository.Verify(r => r.UpdateLastLoginAsync("user-123"), Times.Once);
    }

    // ==================== GetUserFromTokenAsync Tests ====================

    [Fact]
    public async Task GetUserFromTokenAsync_ValidToken_ReturnsUser()
    {
        // Arrange
        var user = new User
        {
            UserId = "user-123",
            Username = "testuser",
            DisplayName = "Test User"
        };

        var token = _authService.GenerateToken(user);

        _mockUserRepository.Setup(r => r.GetByUserIdAsync("user-123"))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.GetUserFromTokenAsync(token);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("testuser", result.Username);
    }

    [Fact]
    public async Task GetUserFromTokenAsync_InvalidToken_ReturnsNull()
    {
        // Act
        var result = await _authService.GetUserFromTokenAsync("invalid-token");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserFromTokenAsync_UserNotFound_ReturnsNull()
    {
        // Arrange
        var user = new User
        {
            UserId = "user-123",
            Username = "testuser",
            DisplayName = "Test User"
        };

        var token = _authService.GenerateToken(user);

        _mockUserRepository.Setup(r => r.GetByUserIdAsync("user-123"))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _authService.GetUserFromTokenAsync(token);

        // Assert
        Assert.Null(result);
    }
}
