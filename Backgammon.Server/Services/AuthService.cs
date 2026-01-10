using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Backgammon.Server.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Backgammon.Server.Services;

/// <summary>
/// Authentication service handling registration, login, and JWT token management.
/// </summary>
public class AuthService : IAuthService
{
    private static readonly Regex UsernameRegex = new(@"^[a-zA-Z0-9_]{3,20}$", RegexOptions.Compiled);

    private readonly IUserRepository _userRepository;
    private readonly IGameRepository _gameRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IGameRepository gameRepository,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _gameRepository = gameRepository;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        try
        {
            // Validate username
            var usernameValidation = ValidateUsername(request.Username);
            if (!usernameValidation.IsValid)
            {
                return new AuthResponse { Success = false, Error = usernameValidation.Error };
            }

            // Validate password
            var passwordValidation = ValidatePassword(request.Password);
            if (!passwordValidation.IsValid)
            {
                return new AuthResponse { Success = false, Error = passwordValidation.Error };
            }

            // Check if username exists
            if (await _userRepository.UsernameExistsAsync(request.Username))
            {
                return new AuthResponse { Success = false, Error = "Username already taken" };
            }

            // Check if email exists (if provided)
            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                if (await _userRepository.EmailExistsAsync(request.Email))
                {
                    return new AuthResponse { Success = false, Error = "Email already registered" };
                }
            }

            // Create user
            var user = new User
            {
                UserId = Guid.NewGuid().ToString(),
                Username = request.Username,
                UsernameNormalized = request.Username.ToLowerInvariant(),
                DisplayName = !string.IsNullOrWhiteSpace(request.DisplayName) ? request.DisplayName : request.Username,
                Email = !string.IsNullOrWhiteSpace(request.Email) ? request.Email : null,
                EmailNormalized = !string.IsNullOrWhiteSpace(request.Email) ? request.Email.ToLowerInvariant() : null,
                PasswordHash = HashPassword(request.Password),
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow,
                LastSeenAt = DateTime.UtcNow,
                Stats = new UserStats(),
                IsActive = true
            };

            // Link anonymous player ID if provided
            if (!string.IsNullOrWhiteSpace(request.AnonymousPlayerId))
            {
                user.LinkedAnonymousIds.Add(request.AnonymousPlayerId);

                // Calculate stats from anonymous game history
                var stats = await _gameRepository.GetPlayerStatsAsync(request.AnonymousPlayerId);
                user.Stats = new UserStats
                {
                    TotalGames = stats.TotalGames,
                    Wins = stats.Wins,
                    Losses = stats.Losses,
                    TotalStakes = stats.TotalStakes,
                    NormalWins = stats.NormalWins,
                    GammonWins = stats.GammonWins,
                    BackgammonWins = stats.BackgammonWins
                };
            }

            await _userRepository.CreateUserAsync(user);

            _logger.LogInformation("User {Username} registered successfully with ID {UserId}", user.Username, user.UserId);

            var token = GenerateToken(user);

            return new AuthResponse
            {
                Success = true,
                Token = token,
                User = UserDto.FromUser(user)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register user {Username}", request.Username);
            return new AuthResponse { Success = false, Error = "Registration failed. Please try again." };
        }
    }

    public async Task<AuthResponse> RegisterAnonymousUserAsync(string playerId, string displayName)
    {
        try
        {
            // Check if user already exists with this player ID
            var existingUser = await _userRepository.GetByUserIdAsync(playerId);
            if (existingUser != null)
            {
                // User already registered, return existing token
                var existingToken = GenerateToken(existingUser);
                return new AuthResponse
                {
                    Success = true,
                    Token = existingToken,
                    User = UserDto.FromUser(existingUser)
                };
            }

            // Create anonymous user
            var user = new User
            {
                UserId = playerId,
                Username = displayName, // Use display name as username for anonymous users
                UsernameNormalized = displayName.ToLowerInvariant(),
                DisplayName = displayName,
                Email = null,
                EmailNormalized = null,
                PasswordHash = string.Empty, // No password for anonymous users
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow,
                LastSeenAt = DateTime.UtcNow,
                Stats = new UserStats(),
                IsActive = true,
                IsAnonymous = true
            };

            try
            {
                await _userRepository.CreateUserAsync(user);
                _logger.LogInformation("Anonymous user registered with ID {UserId} and display name {DisplayName}", user.UserId, displayName);
            }
            catch (InvalidOperationException)
            {
                // Race condition: user was created between our check and now (e.g., by OnConnectedAsync)
                // Fetch the existing user and return its token
                _logger.LogInformation("Anonymous user {UserId} already exists (race condition), returning existing token", playerId);
                var raceConditionUser = await _userRepository.GetByUserIdAsync(playerId);
                if (raceConditionUser != null)
                {
                    var raceToken = GenerateToken(raceConditionUser);
                    return new AuthResponse
                    {
                        Success = true,
                        Token = raceToken,
                        User = UserDto.FromUser(raceConditionUser)
                    };
                }

                // If user still doesn't exist somehow, fall through to generic error
                throw;
            }

            var token = GenerateToken(user);

            return new AuthResponse
            {
                Success = true,
                Token = token,
                User = UserDto.FromUser(user)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register anonymous user {PlayerId}", playerId);
            return new AuthResponse { Success = false, Error = "Anonymous registration failed. Please try again." };
        }
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            var user = await _userRepository.GetByUsernameAsync(request.Username);

            if (user == null)
            {
                return new AuthResponse { Success = false, Error = "Invalid username or password" };
            }

            if (!user.IsActive)
            {
                return new AuthResponse { Success = false, Error = "Account is disabled" };
            }

            if (user.IsBanned)
            {
                if (user.BannedUntil == null || user.BannedUntil > DateTime.UtcNow)
                {
                    return new AuthResponse { Success = false, Error = $"Account is banned: {user.BannedReason}" };
                }
            }

            if (!VerifyPassword(request.Password, user.PasswordHash))
            {
                return new AuthResponse { Success = false, Error = "Invalid username or password" };
            }

            // Update last login
            await _userRepository.UpdateLastLoginAsync(user.UserId);

            var token = GenerateToken(user);

            _logger.LogInformation("User {Username} logged in successfully", user.Username);

            return new AuthResponse
            {
                Success = true,
                Token = token,
                User = UserDto.FromUser(user)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to login user {Username}", request.Username);
            return new AuthResponse { Success = false, Error = "Login failed. Please try again." };
        }
    }

    public async Task<UserDto?> GetUserFromTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"]!);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return null;
            }

            var user = await _userRepository.GetByUserIdAsync(userId);
            return user != null ? UserDto.FromUser(user) : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return null;
        }
    }

    public string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("displayName", user.DisplayName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var expirationDays = int.Parse(_configuration["Jwt:ExpirationDays"] ?? "7");

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(expirationDays),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }

    public (bool IsValid, string? Error) ValidateUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return (false, "Username is required");
        }

        if (username.Length < 3)
        {
            return (false, "Username must be at least 3 characters");
        }

        if (username.Length > 20)
        {
            return (false, "Username must be 20 characters or less");
        }

        if (!UsernameRegex.IsMatch(username))
        {
            return (false, "Username can only contain letters, numbers, and underscores");
        }

        return (true, null);
    }

    public (bool IsValid, string? Error) ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return (false, "Password is required");
        }

        if (password.Length < 8)
        {
            return (false, "Password must be at least 8 characters");
        }

        return (true, null);
    }
}
