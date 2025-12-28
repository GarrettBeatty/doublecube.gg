using MongoDB.Driver;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Backgammon.Server.Models;

namespace Backgammon.Server.Services;

/// <summary>
/// MongoDB implementation of friendship data access.
/// </summary>
public class MongoFriendshipRepository : IFriendshipRepository
{
    private readonly IMongoCollection<Friendship> _friendships;
    private readonly ILogger<MongoFriendshipRepository> _logger;

    public MongoFriendshipRepository(IMongoClient mongoClient, IConfiguration configuration, ILogger<MongoFriendshipRepository> logger)
    {
        _logger = logger;

        var databaseName = configuration["MongoDB:DatabaseName"] ?? "backgammon";
        var database = mongoClient.GetDatabase(databaseName);
        _friendships = database.GetCollection<Friendship>("friendships");

        CreateIndexes().Wait();
    }

    private async Task CreateIndexes()
    {
        try
        {
            // Compound unique index on userId + friendUserId
            await _friendships.Indexes.CreateOneAsync(
                new CreateIndexModel<Friendship>(
                    Builders<Friendship>.IndexKeys
                        .Ascending(f => f.UserId)
                        .Ascending(f => f.FriendUserId),
                    new CreateIndexOptions { Unique = true }
                )
            );

            // Index for listing friends by status
            await _friendships.Indexes.CreateOneAsync(
                new CreateIndexModel<Friendship>(
                    Builders<Friendship>.IndexKeys
                        .Ascending(f => f.UserId)
                        .Ascending(f => f.Status)
                )
            );

            _logger.LogInformation("Friendship collection indexes created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create friendship indexes (may already exist)");
        }
    }

    public async Task<List<Friendship>> GetFriendsAsync(string userId)
    {
        try
        {
            var filter = Builders<Friendship>.Filter.And(
                Builders<Friendship>.Filter.Eq(f => f.UserId, userId),
                Builders<Friendship>.Filter.Eq(f => f.Status, FriendshipStatus.Accepted)
            );

            return await _friendships.Find(filter).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get friends for user {UserId}", userId);
            return new List<Friendship>();
        }
    }

    public async Task<List<Friendship>> GetPendingRequestsAsync(string userId)
    {
        try
        {
            // Get pending requests where this user is NOT the initiator
            var filter = Builders<Friendship>.Filter.And(
                Builders<Friendship>.Filter.Eq(f => f.UserId, userId),
                Builders<Friendship>.Filter.Eq(f => f.Status, FriendshipStatus.Pending),
                Builders<Friendship>.Filter.Ne(f => f.InitiatedBy, userId)
            );

            return await _friendships.Find(filter)
                .SortByDescending(f => f.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pending requests for user {UserId}", userId);
            return new List<Friendship>();
        }
    }

    public async Task<List<Friendship>> GetSentRequestsAsync(string userId)
    {
        try
        {
            // Get pending requests where this user IS the initiator
            var filter = Builders<Friendship>.Filter.And(
                Builders<Friendship>.Filter.Eq(f => f.UserId, userId),
                Builders<Friendship>.Filter.Eq(f => f.Status, FriendshipStatus.Pending),
                Builders<Friendship>.Filter.Eq(f => f.InitiatedBy, userId)
            );

            return await _friendships.Find(filter)
                .SortByDescending(f => f.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get sent requests for user {UserId}", userId);
            return new List<Friendship>();
        }
    }

    public async Task<Friendship?> GetFriendshipAsync(string userId, string friendUserId)
    {
        try
        {
            var filter = Builders<Friendship>.Filter.And(
                Builders<Friendship>.Filter.Eq(f => f.UserId, userId),
                Builders<Friendship>.Filter.Eq(f => f.FriendUserId, friendUserId)
            );

            return await _friendships.Find(filter).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get friendship between {UserId} and {FriendUserId}", userId, friendUserId);
            return null;
        }
    }

    public async Task SendFriendRequestAsync(string fromUserId, string toUserId, string fromUsername, string fromDisplayName, string toUsername, string toDisplayName)
    {
        try
        {
            var now = DateTime.UtcNow;

            // Create bidirectional friendship records
            var fromRecord = new Friendship
            {
                UserId = fromUserId,
                FriendUserId = toUserId,
                FriendUsername = toUsername,
                FriendDisplayName = toDisplayName,
                Status = FriendshipStatus.Pending,
                CreatedAt = now,
                InitiatedBy = fromUserId
            };

            var toRecord = new Friendship
            {
                UserId = toUserId,
                FriendUserId = fromUserId,
                FriendUsername = fromUsername,
                FriendDisplayName = fromDisplayName,
                Status = FriendshipStatus.Pending,
                CreatedAt = now,
                InitiatedBy = fromUserId
            };

            await _friendships.InsertManyAsync(new[] { fromRecord, toRecord });
            _logger.LogInformation("Friend request sent from {FromUserId} to {ToUserId}", fromUserId, toUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send friend request from {FromUserId} to {ToUserId}", fromUserId, toUserId);
            throw;
        }
    }

    public async Task AcceptFriendRequestAsync(string userId, string friendUserId)
    {
        try
        {
            var now = DateTime.UtcNow;

            // Update both directional records
            var filter1 = Builders<Friendship>.Filter.And(
                Builders<Friendship>.Filter.Eq(f => f.UserId, userId),
                Builders<Friendship>.Filter.Eq(f => f.FriendUserId, friendUserId)
            );

            var filter2 = Builders<Friendship>.Filter.And(
                Builders<Friendship>.Filter.Eq(f => f.UserId, friendUserId),
                Builders<Friendship>.Filter.Eq(f => f.FriendUserId, userId)
            );

            var update = Builders<Friendship>.Update
                .Set(f => f.Status, FriendshipStatus.Accepted)
                .Set(f => f.AcceptedAt, now);

            await _friendships.UpdateOneAsync(filter1, update);
            await _friendships.UpdateOneAsync(filter2, update);

            _logger.LogInformation("Friend request accepted between {UserId} and {FriendUserId}", userId, friendUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to accept friend request between {UserId} and {FriendUserId}", userId, friendUserId);
            throw;
        }
    }

    public async Task DeclineFriendRequestAsync(string userId, string friendUserId)
    {
        try
        {
            // Delete both directional records
            var filter1 = Builders<Friendship>.Filter.And(
                Builders<Friendship>.Filter.Eq(f => f.UserId, userId),
                Builders<Friendship>.Filter.Eq(f => f.FriendUserId, friendUserId)
            );

            var filter2 = Builders<Friendship>.Filter.And(
                Builders<Friendship>.Filter.Eq(f => f.UserId, friendUserId),
                Builders<Friendship>.Filter.Eq(f => f.FriendUserId, userId)
            );

            await _friendships.DeleteOneAsync(filter1);
            await _friendships.DeleteOneAsync(filter2);

            _logger.LogInformation("Friend request declined between {UserId} and {FriendUserId}", userId, friendUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decline friend request between {UserId} and {FriendUserId}", userId, friendUserId);
            throw;
        }
    }

    public async Task BlockUserAsync(string userId, string blockedUserId)
    {
        try
        {
            // First check if there's an existing record
            var existing = await GetFriendshipAsync(userId, blockedUserId);

            if (existing != null)
            {
                // Update existing to blocked
                var filter = Builders<Friendship>.Filter.And(
                    Builders<Friendship>.Filter.Eq(f => f.UserId, userId),
                    Builders<Friendship>.Filter.Eq(f => f.FriendUserId, blockedUserId)
                );

                var update = Builders<Friendship>.Update.Set(f => f.Status, FriendshipStatus.Blocked);
                await _friendships.UpdateOneAsync(filter, update);

                // Delete the reverse record
                var reverseFilter = Builders<Friendship>.Filter.And(
                    Builders<Friendship>.Filter.Eq(f => f.UserId, blockedUserId),
                    Builders<Friendship>.Filter.Eq(f => f.FriendUserId, userId)
                );
                await _friendships.DeleteOneAsync(reverseFilter);
            }
            else
            {
                // Create new blocked record
                var blockedRecord = new Friendship
                {
                    UserId = userId,
                    FriendUserId = blockedUserId,
                    Status = FriendshipStatus.Blocked,
                    CreatedAt = DateTime.UtcNow,
                    InitiatedBy = userId
                };
                await _friendships.InsertOneAsync(blockedRecord);
            }

            _logger.LogInformation("User {UserId} blocked user {BlockedUserId}", userId, blockedUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to block user {BlockedUserId} by {UserId}", blockedUserId, userId);
            throw;
        }
    }

    public async Task RemoveFriendAsync(string userId, string friendUserId)
    {
        try
        {
            // Delete both directional records
            var filter1 = Builders<Friendship>.Filter.And(
                Builders<Friendship>.Filter.Eq(f => f.UserId, userId),
                Builders<Friendship>.Filter.Eq(f => f.FriendUserId, friendUserId)
            );

            var filter2 = Builders<Friendship>.Filter.And(
                Builders<Friendship>.Filter.Eq(f => f.UserId, friendUserId),
                Builders<Friendship>.Filter.Eq(f => f.FriendUserId, userId)
            );

            await _friendships.DeleteOneAsync(filter1);
            await _friendships.DeleteOneAsync(filter2);

            _logger.LogInformation("Friendship removed between {UserId} and {FriendUserId}", userId, friendUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove friendship between {UserId} and {FriendUserId}", userId, friendUserId);
            throw;
        }
    }

    public async Task<bool> AreFriendsAsync(string userId, string otherUserId)
    {
        try
        {
            var filter = Builders<Friendship>.Filter.And(
                Builders<Friendship>.Filter.Eq(f => f.UserId, userId),
                Builders<Friendship>.Filter.Eq(f => f.FriendUserId, otherUserId),
                Builders<Friendship>.Filter.Eq(f => f.Status, FriendshipStatus.Accepted)
            );

            return await _friendships.Find(filter).AnyAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check friendship between {UserId} and {OtherUserId}", userId, otherUserId);
            return false;
        }
    }

    public async Task<bool> IsBlockedAsync(string userId, string byUserId)
    {
        try
        {
            var filter = Builders<Friendship>.Filter.And(
                Builders<Friendship>.Filter.Eq(f => f.UserId, byUserId),
                Builders<Friendship>.Filter.Eq(f => f.FriendUserId, userId),
                Builders<Friendship>.Filter.Eq(f => f.Status, FriendshipStatus.Blocked)
            );

            return await _friendships.Find(filter).AnyAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if {UserId} is blocked by {ByUserId}", userId, byUserId);
            return false;
        }
    }

    public async Task<int> GetFriendCountAsync(string userId)
    {
        try
        {
            var filter = Builders<Friendship>.Filter.And(
                Builders<Friendship>.Filter.Eq(f => f.UserId, userId),
                Builders<Friendship>.Filter.Eq(f => f.Status, FriendshipStatus.Accepted)
            );

            return (int)await _friendships.CountDocumentsAsync(filter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get friend count for user {UserId}", userId);
            return 0;
        }
    }
}
