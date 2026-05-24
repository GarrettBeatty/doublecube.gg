using Backgammon.Server.Data;
using Backgammon.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace Backgammon.Server.Services.Postgres;

/// <summary>
/// PostgreSQL implementation of <see cref="IFriendshipRepository"/> using EF Core.
/// </summary>
public class PostgresFriendshipRepository : IFriendshipRepository
{
    private readonly IDbContextFactory<BackgammonDbContext> _dbFactory;

    /// <summary>
    /// Initializes a new instance of <see cref="PostgresFriendshipRepository"/>.
    /// </summary>
    public PostgresFriendshipRepository(IDbContextFactory<BackgammonDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    /// <inheritdoc />
    public async Task<List<Friendship>> GetFriendsAsync(string userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Friendships
            .AsNoTracking()
            .Where(f => f.UserId == userId && f.Status == FriendshipStatus.Accepted)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<Friendship>> GetPendingRequestsAsync(string userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Friendships
            .AsNoTracking()
            .Where(f => f.UserId == userId
                        && f.Status == FriendshipStatus.Pending
                        && f.InitiatedBy != userId)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<Friendship>> GetSentRequestsAsync(string userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Friendships
            .AsNoTracking()
            .Where(f => f.UserId == userId
                        && f.Status == FriendshipStatus.Pending
                        && f.InitiatedBy == userId)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Friendship?> GetFriendshipAsync(string userId, string friendUserId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Friendships
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.UserId == userId && f.FriendUserId == friendUserId);
    }

    /// <inheritdoc />
    public async Task SendFriendRequestAsync(
        string fromUserId,
        string toUserId,
        string fromUsername,
        string fromDisplayName,
        string toUsername,
        string toDisplayName)
    {
        var now = DateTime.UtcNow;
        await using var db = await _dbFactory.CreateDbContextAsync();

        // Two rows — one per direction, so each user can query their own view
        var fromRow = new Friendship
        {
            UserId = fromUserId,
            FriendUserId = toUserId,
            FriendUsername = toUsername,
            FriendDisplayName = toDisplayName,
            Status = FriendshipStatus.Pending,
            CreatedAt = now,
            InitiatedBy = fromUserId,
        };

        var toRow = new Friendship
        {
            UserId = toUserId,
            FriendUserId = fromUserId,
            FriendUsername = fromUsername,
            FriendDisplayName = fromDisplayName,
            Status = FriendshipStatus.Pending,
            CreatedAt = now,
            InitiatedBy = fromUserId,
        };

        db.Friendships.AddRange(fromRow, toRow);
        await db.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task AcceptFriendRequestAsync(string userId, string friendUserId)
    {
        var now = DateTime.UtcNow;
        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.Friendships
            .Where(f => (f.UserId == userId && f.FriendUserId == friendUserId) ||
                        (f.UserId == friendUserId && f.FriendUserId == userId))
            .ExecuteUpdateAsync(s => s
                .SetProperty(f => f.Status, FriendshipStatus.Accepted)
                .SetProperty(f => f.AcceptedAt, now));
    }

    /// <inheritdoc />
    public async Task DeclineFriendRequestAsync(string userId, string friendUserId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.Friendships
            .Where(f => (f.UserId == userId && f.FriendUserId == friendUserId) ||
                        (f.UserId == friendUserId && f.FriendUserId == userId))
            .ExecuteDeleteAsync();
    }

    /// <inheritdoc />
    public async Task BlockUserAsync(string userId, string blockedUserId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        // Remove existing friendship rows in both directions, then insert a block row
        await db.Friendships
            .Where(f => (f.UserId == userId && f.FriendUserId == blockedUserId) ||
                        (f.UserId == blockedUserId && f.FriendUserId == userId))
            .ExecuteDeleteAsync();

        var blockRow = new Friendship
        {
            UserId = userId,
            FriendUserId = blockedUserId,
            Status = FriendshipStatus.Blocked,
            CreatedAt = DateTime.UtcNow,
            InitiatedBy = userId,
        };

        db.Friendships.Add(blockRow);
        await db.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task RemoveFriendAsync(string userId, string friendUserId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.Friendships
            .Where(f => (f.UserId == userId && f.FriendUserId == friendUserId) ||
                        (f.UserId == friendUserId && f.FriendUserId == userId))
            .ExecuteDeleteAsync();
    }

    /// <inheritdoc />
    public async Task<bool> AreFriendsAsync(string userId, string otherUserId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Friendships.AnyAsync(f =>
            f.UserId == userId &&
            f.FriendUserId == otherUserId &&
            f.Status == FriendshipStatus.Accepted);
    }

    /// <inheritdoc />
    public async Task<bool> IsBlockedAsync(string userId, string byUserId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Friendships.AnyAsync(f =>
            f.UserId == byUserId &&
            f.FriendUserId == userId &&
            f.Status == FriendshipStatus.Blocked);
    }

    /// <inheritdoc />
    public async Task<int> GetFriendCountAsync(string userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Friendships.CountAsync(f =>
            f.UserId == userId && f.Status == FriendshipStatus.Accepted);
    }
}
