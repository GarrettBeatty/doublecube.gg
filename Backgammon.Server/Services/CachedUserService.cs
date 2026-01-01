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
        return await _cache.GetOrCreateAsync(
            $"user:id:{userId}",
            async cancel => await _userRepository.GetByUserIdAsync(userId),
            tags: [$"user:{userId}"]
        );
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _cache.GetOrCreateAsync(
            $"user:username:{username.ToLowerInvariant()}",
            async cancel => await _userRepository.GetByUsernameAsync(username),
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5),
                LocalCacheExpiration = TimeSpan.FromMinutes(1)
            }
        );
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _cache.GetOrCreateAsync(
            $"user:email:{email.ToLowerInvariant()}",
            async cancel => await _userRepository.GetByEmailAsync(email),
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

        // Invalidate all user caches
        await InvalidateUserCacheAsync(user.UserId);
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

        // Invalidate cache since stats changed
        await InvalidateUserCacheAsync(userId);
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
}
