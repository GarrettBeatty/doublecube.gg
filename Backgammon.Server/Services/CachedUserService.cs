using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Backgammon.Server.Models;

namespace Backgammon.Server.Services;

/// <summary>
/// User service with HybridCache for improved performance.
/// </summary>
public class CachedUserService : IUserRepository
{
    private readonly IUserRepository _userRepository;
    private readonly HybridCache _cache;
    private readonly ILogger<CachedUserService> _logger;

    public CachedUserService(
        IUserRepository userRepository,
        HybridCache cache,
        ILogger<CachedUserService> logger)
    {
        _userRepository = userRepository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<User?> GetByUserIdAsync(string userId)
    {
        var cacheKey = $"user:id:{userId}";
        return await GetOrCreateWithLoggingAsync(
            cacheKey,
            async () => await _userRepository.GetByUserIdAsync(userId),
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5),
                LocalCacheExpiration = TimeSpan.FromMinutes(1)
            },
            tags: [$"user:{userId}"]
        );
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        // Note: Cannot tag with user:id because we don't know the userId until after cache miss
        // Must invalidate manually when user profile changes (see UpdateUserAsync)
        var cacheKey = $"user:username:{username.ToLowerInvariant()}";
        return await GetOrCreateWithLoggingAsync(
            cacheKey,
            async () => await _userRepository.GetByUsernameAsync(username),
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5),
                LocalCacheExpiration = TimeSpan.FromMinutes(1)
            }
        );
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        // Note: Cannot tag with user:id because we don't know the userId until after cache miss
        // Must invalidate manually when user profile changes (see UpdateUserAsync)
        var cacheKey = $"user:email:{email.ToLowerInvariant()}";
        return await GetOrCreateWithLoggingAsync(
            cacheKey,
            async () => await _userRepository.GetByEmailAsync(email),
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5),
                LocalCacheExpiration = TimeSpan.FromMinutes(1)
            }
        );
    }

    public async Task CreateUserAsync(User user)
    {
        await _userRepository.CreateUserAsync(user);
        // No need to cache on create - user will be cached on first read
    }

    public async Task UpdateUserAsync(User user)
    {
        await _userRepository.UpdateUserAsync(user);

        // Invalidate all user-related caches
        // Note: We invalidate broadly to avoid race conditions from fetching old user data through the cache
        await InvalidateUserCacheAsync(user.UserId);

        // Invalidate current username/email caches
        // Note: We don't know the old values without risking stale cache reads,
        // so we only invalidate the current values. Old username/email caches will expire naturally.
        try
        {
            var usernameKey = $"user:username:{user.UsernameNormalized}";
            await _cache.RemoveAsync(usernameKey);
            _logger.LogDebug("Invalidated username cache (hash: {CacheKeyHash})", HashCacheKey(usernameKey));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate username cache");
        }

        try
        {
            var emailKey = $"user:email:{user.EmailNormalized}";
            await _cache.RemoveAsync(emailKey);
            _logger.LogDebug("Invalidated email cache (hash: {CacheKeyHash})", HashCacheKey(emailKey));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate email cache");
        }
    }

    public async Task UpdateLastLoginAsync(string userId)
    {
        await _userRepository.UpdateLastLoginAsync(userId);

        // Invalidate cache since last login timestamp changed
        await InvalidateUserCacheAsync(userId);
    }

    public async Task UpdateStatsAsync(string userId, UserStats stats)
    {
        await _userRepository.UpdateStatsAsync(userId, stats);

        // Invalidate user cache (user:id tag)
        await InvalidateUserCacheAsync(userId);

        // Also invalidate player stats cache (uses player:id tag in Program.cs endpoints)
        try
        {
            await _cache.RemoveByTagAsync($"player:{userId}");
            _logger.LogDebug("Invalidated player stats cache for {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate player stats cache for {UserId}", userId);
        }
    }

    public async Task LinkAnonymousIdAsync(string userId, string anonymousId)
    {
        await _userRepository.LinkAnonymousIdAsync(userId, anonymousId);

        // Invalidate cache since linked IDs changed
        await InvalidateUserCacheAsync(userId);
    }

    public Task<bool> UsernameExistsAsync(string username)
    {
        // Existence checks don't need caching - they're fast enough
        return _userRepository.UsernameExistsAsync(username);
    }

    public Task<bool> EmailExistsAsync(string email)
    {
        // Existence checks don't need caching - they're fast enough
        return _userRepository.EmailExistsAsync(email);
    }

    public Task<List<User>> SearchUsersAsync(string query, int limit)
    {
        // Search results don't need caching - they're dynamic and user-specific
        return _userRepository.SearchUsersAsync(query, limit);
    }

    public Task<List<User>> GetUsersByIdsAsync(IEnumerable<string> userIds)
    {
        // Batch gets don't need caching - they're already optimized
        return _userRepository.GetUsersByIdsAsync(userIds);
    }

    /// <summary>
    /// Invalidate all caches for a user
    /// </summary>
    private async Task InvalidateUserCacheAsync(string userId)
    {
        try
        {
            // Remove by tag to invalidate all user-related cache entries
            await _cache.RemoveByTagAsync($"user:{userId}");

            _logger.LogDebug("Invalidated cache for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate cache for user {UserId}", userId);
        }
    }

    /// <summary>
    /// Get or create cache entry with logging
    /// </summary>
    private async Task<User?> GetOrCreateWithLoggingAsync(
        string cacheKey,
        Func<Task<User?>> factory,
        HybridCacheEntryOptions? options = null,
        string[]? tags = null)
    {
        // Hash the cache key to avoid logging sensitive data (email addresses, usernames)
        var hashedKey = HashCacheKey(cacheKey);
        _logger.LogTrace("Cache lookup for key hash {CacheKeyHash}", hashedKey);

        User? result;
        if (options != null && tags != null)
        {
            result = await _cache.GetOrCreateAsync(cacheKey, async cancel => await factory(), options, tags: tags);
        }
        else if (options != null)
        {
            result = await _cache.GetOrCreateAsync(cacheKey, async cancel => await factory(), options);
        }
        else if (tags != null)
        {
            result = await _cache.GetOrCreateAsync(cacheKey, async cancel => await factory(), tags: tags);
        }
        else
        {
            result = await _cache.GetOrCreateAsync(cacheKey, async cancel => await factory());
        }

        return result;
    }

    /// <summary>
    /// Hash a cache key to avoid logging sensitive data.
    /// Returns a short hash that preserves diagnostic usefulness while protecting privacy.
    /// </summary>
    private static string HashCacheKey(string cacheKey)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(cacheKey));
        // Return first 8 bytes as hex (16 characters) - enough to correlate operations
        return Convert.ToHexString(hashBytes[..8]);
    }
}
