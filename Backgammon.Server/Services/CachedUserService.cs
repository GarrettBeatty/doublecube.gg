using System.Security.Cryptography;
using System.Text;
using Backgammon.Server.Configuration;
using Backgammon.Server.Models;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace Backgammon.Server.Services;

/// <summary>
/// User service with HybridCache for improved performance.
/// </summary>
public class CachedUserService : IUserRepository
{
    private readonly IUserRepository _userRepository;
    private readonly HybridCache _cache;
    private readonly CacheSettings _cacheSettings;
    private readonly ILogger<CachedUserService> _logger;

    public CachedUserService(
        IUserRepository userRepository,
        HybridCache cache,
        CacheSettings cacheSettings,
        ILogger<CachedUserService> logger)
    {
        _userRepository = userRepository;
        _cache = cache;
        _cacheSettings = cacheSettings;
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
                Expiration = _cacheSettings.UserProfile.Expiration,
                LocalCacheExpiration = _cacheSettings.UserProfile.LocalCacheExpiration
            },
            tags: [$"user:{userId}"]);
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
                Expiration = _cacheSettings.UserProfile.Expiration,
                LocalCacheExpiration = _cacheSettings.UserProfile.LocalCacheExpiration
            });
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
                Expiration = _cacheSettings.UserProfile.Expiration,
                LocalCacheExpiration = _cacheSettings.UserProfile.LocalCacheExpiration
            });
    }

    public async Task CreateUserAsync(User user)
    {
        await _userRepository.CreateUserAsync(user);

        // CRITICAL: Cache immediately after creation to prevent race conditions
        // When anonymous users register via HTTP and immediately connect to SignalR,
        // the SignalR OnConnectedAsync validation happens milliseconds later.
        // Even with DynamoDB ConsistentRead=true, there can be propagation delays.
        // Caching here ensures the user is available instantly for subsequent reads.
        var cacheKey = $"user:id:{user.UserId}";
        await _cache.SetAsync(
            cacheKey,
            user,
            new HybridCacheEntryOptions
            {
                Expiration = _cacheSettings.UserProfile.Expiration,
                LocalCacheExpiration = _cacheSettings.UserProfile.LocalCacheExpiration
            },
            tags: [$"user:{user.UserId}"]);

        _logger.LogDebug("Cached newly created user {UserId} to prevent read-after-write race conditions", user.UserId);
    }

    /// <summary>
    /// Updates a user and invalidates all related caches.
    ///
    /// KNOWN LIMITATION: In rare concurrent update scenarios where username/email changes happen
    /// simultaneously from multiple threads, some cache entries may not be invalidated immediately.
    /// These will expire naturally within 5 minutes. To fully prevent this, implement optimistic
    /// concurrency control with a Version field in the User model and conditional writes in DynamoDB.
    /// </summary>
    public async Task UpdateUserAsync(User user)
    {
        // Fetch current user to get old username/email values for cache invalidation
        User? currentUser = null;
        try
        {
            // Use repository directly to bypass cache and get current values from DB
            currentUser = await _userRepository.GetByUserIdAsync(user.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch current user for cache invalidation, will invalidate new values only");
        }

        await _userRepository.UpdateUserAsync(user);

        // Invalidate all user-related caches
        await InvalidateUserCacheAsync(user.UserId);

        // Invalidate both old and new username/email caches
        if (currentUser != null)
        {
            // Invalidate old username cache if it changed
            if (!string.IsNullOrEmpty(currentUser.UsernameNormalized) &&
                currentUser.UsernameNormalized != user.UsernameNormalized)
            {
                try
                {
                    var oldUsernameKey = $"user:username:{currentUser.UsernameNormalized}";
                    await _cache.RemoveAsync(oldUsernameKey);
                    _logger.LogDebug("Invalidated old username cache (hash: {CacheKeyHash})", HashCacheKey(oldUsernameKey));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to invalidate old username cache");
                }
            }

            // Invalidate old email cache if it changed
            if (!string.IsNullOrEmpty(currentUser.EmailNormalized) &&
                currentUser.EmailNormalized != user.EmailNormalized)
            {
                try
                {
                    var oldEmailKey = $"user:email:{currentUser.EmailNormalized}";
                    await _cache.RemoveAsync(oldEmailKey);
                    _logger.LogDebug("Invalidated old email cache (hash: {CacheKeyHash})", HashCacheKey(oldEmailKey));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to invalidate old email cache");
                }
            }
        }

        // Invalidate new username/email caches
        try
        {
            var usernameKey = $"user:username:{user.UsernameNormalized}";
            await _cache.RemoveAsync(usernameKey);
            _logger.LogDebug("Invalidated new username cache (hash: {CacheKeyHash})", HashCacheKey(usernameKey));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate new username cache");
        }

        try
        {
            var emailKey = $"user:email:{user.EmailNormalized}";
            await _cache.RemoveAsync(emailKey);
            _logger.LogDebug("Invalidated new email cache (hash: {CacheKeyHash})", HashCacheKey(emailKey));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate new email cache");
        }

        // Invalidate player profile cache (privacy settings or display name may have changed)
        try
        {
            await _cache.RemoveByTagAsync($"profile:{user.UserId}");
            _logger.LogDebug("Invalidated player profile cache for user {UserId}", user.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate player profile cache");
        }
    }

    public async Task UpdateLastLoginAsync(string userId)
    {
        await _userRepository.UpdateLastLoginAsync(userId);

        // Invalidate cache since last login timestamp changed
        await InvalidateUserCacheAsync(userId);
    }

    /// <summary>
    /// Updates user statistics and invalidates all related caches.
    ///
    /// This invalidates:
    /// - user:{userId} tag: user profile cache
    /// - player:{userId} tag: player stats AND all player game history caches
    ///   (including all pagination variants with different limit/skip values)
    /// </summary>
    public async Task UpdateStatsAsync(string userId, UserStats stats)
    {
        await _userRepository.UpdateStatsAsync(userId, stats);

        // Invalidate user cache (user:id tag)
        await InvalidateUserCacheAsync(userId);

        // Also invalidate player stats and game history caches (uses player:id tag in Program.cs endpoints)
        // This invalidates ALL cache entries tagged with player:{userId}, including:
        // - player:stats:{userId}
        // - player:games:{userId}:completed:limit=*:skip=* (all pagination variants)
        try
        {
            await _cache.RemoveByTagAsync($"player:{userId}");
            _logger.LogDebug("Invalidated player stats and game history caches for {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate player caches for {UserId}", userId);
        }

        // Invalidate player profile cache (stats changed)
        try
        {
            await _cache.RemoveByTagAsync($"profile:{userId}");
            _logger.LogDebug("Invalidated player profile cache for user {UserId} (stats changed)", userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate player profile cache after stats update");
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

    public Task<List<User>> GetTopPlayersByRatingAsync(int limit = 100)
    {
        // Leaderboard queries don't need caching - they're dynamic and updated frequently
        return _userRepository.GetTopPlayersByRatingAsync(limit);
    }

    public Task<List<int>> GetAllRatingsAsync()
    {
        // Rating distribution queries don't need caching - they're aggregate stats
        return _userRepository.GetAllRatingsAsync();
    }

    public Task SaveRatingHistoryAsync(RatingHistoryEntry entry)
    {
        // Write operation - no caching needed
        return _userRepository.SaveRatingHistoryAsync(entry);
    }

    public Task<List<RatingHistoryEntry>> GetRatingHistoryAsync(string userId, int limit = 30)
    {
        // Rating history is user-specific and frequently changing - no caching needed
        return _userRepository.GetRatingHistoryAsync(userId, limit);
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
}
