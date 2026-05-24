using Backgammon.Server.Data;
using Backgammon.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace Backgammon.Server.Services.Postgres;

/// <summary>
/// PostgreSQL implementation of <see cref="IUserRepository"/> using EF Core.
/// </summary>
public class PostgresUserRepository : IUserRepository
{
    private readonly IDbContextFactory<BackgammonDbContext> _dbFactory;

    /// <summary>
    /// Initializes a new instance of <see cref="PostgresUserRepository"/>.
    /// </summary>
    public PostgresUserRepository(IDbContextFactory<BackgammonDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    /// <inheritdoc />
    public async Task<User?> GetByUserIdAsync(string userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
    }

    /// <inheritdoc />
    public async Task<User?> GetByUsernameAsync(string username)
    {
        var normalized = username.ToLowerInvariant();
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UsernameNormalized == normalized);
    }

    /// <inheritdoc />
    public async Task<User?> GetByEmailAsync(string email)
    {
        var normalized = email.ToLowerInvariant();
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.EmailNormalized == normalized);
    }

    /// <inheritdoc />
    public async Task<bool> UsernameExistsAsync(string username)
    {
        var normalized = username.ToLowerInvariant();
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Users.AnyAsync(u => u.UsernameNormalized == normalized);
    }

    /// <inheritdoc />
    public async Task<bool> EmailExistsAsync(string email)
    {
        var normalized = email.ToLowerInvariant();
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Users.AnyAsync(u => u.EmailNormalized == normalized);
    }

    /// <inheritdoc />
    public async Task CreateUserAsync(User user)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateUserAsync(User user)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.Users.Update(user);
        await db.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateStatsAsync(string userId, UserStats stats)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.Users
            .Where(u => u.UserId == userId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.Stats, stats));
    }

    /// <inheritdoc />
    public async Task UpdateLastLoginAsync(string userId)
    {
        var now = DateTime.UtcNow;
        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.Users
            .Where(u => u.UserId == userId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.LastLoginAt, now)
                .SetProperty(u => u.LastSeenAt, now));
    }

    /// <inheritdoc />
    public async Task LinkAnonymousIdAsync(string userId, string anonymousId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var user = await db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user is null)
        {
            return;
        }

        if (!user.LinkedAnonymousIds.Contains(anonymousId))
        {
            user.LinkedAnonymousIds.Add(anonymousId);
            await db.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task<List<User>> SearchUsersAsync(string query, int limit = 10)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        // Use pg_trgm similarity for fuzzy search — requires the gin_trgm_ops index
        var normalized = query.ToLowerInvariant();
        return await db.Users
            .AsNoTracking()
            .Where(u => !u.IsAnonymous && u.IsActive &&
                        (EF.Functions.ILike(u.Username, $"%{normalized}%") ||
                         EF.Functions.ILike(u.DisplayName, $"%{normalized}%")))
            .OrderBy(u => u.Username)
            .Take(limit)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<User>> GetAllPlayersAsync(int limit = 50)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Users
            .AsNoTracking()
            .Where(u => !u.IsAnonymous && u.IsActive)
            .OrderByDescending(u => u.Rating)
            .Take(limit)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<User>> GetUsersByIdsAsync(IEnumerable<string> userIds)
    {
        var ids = userIds.ToList();
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Users
            .AsNoTracking()
            .Where(u => ids.Contains(u.UserId))
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<User>> GetTopPlayersByRatingAsync(int limit = 100)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Users
            .AsNoTracking()
            .Where(u => !u.IsAnonymous && u.IsActive && u.RatedGamesCount > 0)
            .OrderByDescending(u => u.Rating)
            .Take(limit)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<int>> GetAllRatingsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Users
            .AsNoTracking()
            .Where(u => !u.IsAnonymous && u.IsActive && u.RatedGamesCount > 0)
            .Select(u => u.Rating)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task SaveRatingHistoryAsync(RatingHistoryEntry entry)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.RatingHistory.Add(entry);
        await db.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<List<RatingHistoryEntry>> GetRatingHistoryAsync(string userId, int limit = 30)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.RatingHistory
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.Timestamp)
            .Take(limit)
            .ToListAsync();
    }
}
