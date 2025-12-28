using MongoDB.Driver;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Backgammon.Server.Models;

namespace Backgammon.Server.Services;

/// <summary>
/// MongoDB implementation of user data access.
/// </summary>
public class MongoUserRepository : IUserRepository
{
    private readonly IMongoCollection<User> _users;
    private readonly ILogger<MongoUserRepository> _logger;

    public MongoUserRepository(IMongoClient mongoClient, IConfiguration configuration, ILogger<MongoUserRepository> logger)
    {
        _logger = logger;

        var databaseName = configuration["MongoDB:DatabaseName"] ?? "backgammon";
        var database = mongoClient.GetDatabase(databaseName);
        _users = database.GetCollection<User>("users");

        CreateIndexes().Wait();
    }

    private async Task CreateIndexes()
    {
        try
        {
            // Unique index on userId
            await _users.Indexes.CreateOneAsync(
                new CreateIndexModel<User>(
                    Builders<User>.IndexKeys.Ascending(u => u.UserId),
                    new CreateIndexOptions { Unique = true }
                )
            );

            // Unique index on normalized username
            await _users.Indexes.CreateOneAsync(
                new CreateIndexModel<User>(
                    Builders<User>.IndexKeys.Ascending(u => u.UsernameNormalized),
                    new CreateIndexOptions { Unique = true }
                )
            );

            // Unique sparse index on normalized email
            await _users.Indexes.CreateOneAsync(
                new CreateIndexModel<User>(
                    Builders<User>.IndexKeys.Ascending(u => u.EmailNormalized),
                    new CreateIndexOptions { Unique = true, Sparse = true }
                )
            );

            // Index on linked anonymous IDs for game history claims
            await _users.Indexes.CreateOneAsync(
                new CreateIndexModel<User>(
                    Builders<User>.IndexKeys.Ascending(u => u.LinkedAnonymousIds)
                )
            );

            // Text index for search
            await _users.Indexes.CreateOneAsync(
                new CreateIndexModel<User>(
                    Builders<User>.IndexKeys
                        .Text(u => u.Username)
                        .Text(u => u.DisplayName)
                )
            );

            _logger.LogInformation("User collection indexes created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create user indexes (may already exist)");
        }
    }

    public async Task<User?> GetByUserIdAsync(string userId)
    {
        try
        {
            var filter = Builders<User>.Filter.Eq(u => u.UserId, userId);
            return await _users.Find(filter).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user by ID {UserId}", userId);
            return null;
        }
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        try
        {
            var normalized = username.ToLowerInvariant();
            var filter = Builders<User>.Filter.Eq(u => u.UsernameNormalized, normalized);
            return await _users.Find(filter).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user by username {Username}", username);
            return null;
        }
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        try
        {
            var normalized = email.ToLowerInvariant();
            var filter = Builders<User>.Filter.Eq(u => u.EmailNormalized, normalized);
            return await _users.Find(filter).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user by email");
            return null;
        }
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        try
        {
            var normalized = username.ToLowerInvariant();
            var filter = Builders<User>.Filter.Eq(u => u.UsernameNormalized, normalized);
            return await _users.Find(filter).AnyAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check username existence");
            return true; // Fail safe - assume exists
        }
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        try
        {
            var normalized = email.ToLowerInvariant();
            var filter = Builders<User>.Filter.Eq(u => u.EmailNormalized, normalized);
            return await _users.Find(filter).AnyAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check email existence");
            return true; // Fail safe - assume exists
        }
    }

    public async Task CreateUserAsync(User user)
    {
        try
        {
            await _users.InsertOneAsync(user);
            _logger.LogInformation("Created user {Username} with ID {UserId}", user.Username, user.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user {Username}", user.Username);
            throw;
        }
    }

    public async Task UpdateUserAsync(User user)
    {
        try
        {
            var filter = Builders<User>.Filter.Eq(u => u.UserId, user.UserId);
            await _users.ReplaceOneAsync(filter, user);
            _logger.LogInformation("Updated user {UserId}", user.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user {UserId}", user.UserId);
            throw;
        }
    }

    public async Task UpdateStatsAsync(string userId, UserStats stats)
    {
        try
        {
            var filter = Builders<User>.Filter.Eq(u => u.UserId, userId);
            var update = Builders<User>.Update.Set(u => u.Stats, stats);
            await _users.UpdateOneAsync(filter, update);
            _logger.LogInformation("Updated stats for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update stats for user {UserId}", userId);
            throw;
        }
    }

    public async Task UpdateLastLoginAsync(string userId)
    {
        try
        {
            var filter = Builders<User>.Filter.Eq(u => u.UserId, userId);
            var update = Builders<User>.Update
                .Set(u => u.LastLoginAt, DateTime.UtcNow)
                .Set(u => u.LastSeenAt, DateTime.UtcNow);
            await _users.UpdateOneAsync(filter, update);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update last login for user {UserId}", userId);
        }
    }

    public async Task LinkAnonymousIdAsync(string userId, string anonymousId)
    {
        try
        {
            var filter = Builders<User>.Filter.Eq(u => u.UserId, userId);
            var update = Builders<User>.Update.AddToSet(u => u.LinkedAnonymousIds, anonymousId);
            await _users.UpdateOneAsync(filter, update);
            _logger.LogInformation("Linked anonymous ID {AnonymousId} to user {UserId}", anonymousId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to link anonymous ID to user {UserId}", userId);
            throw;
        }
    }

    public async Task<List<User>> SearchUsersAsync(string query, int limit = 10)
    {
        try
        {
            var normalizedQuery = query.ToLowerInvariant();

            // Search by username prefix or display name prefix
            var filter = Builders<User>.Filter.Or(
                Builders<User>.Filter.Regex(u => u.UsernameNormalized, new MongoDB.Bson.BsonRegularExpression($"^{normalizedQuery}", "i")),
                Builders<User>.Filter.Regex(u => u.DisplayName, new MongoDB.Bson.BsonRegularExpression($"^{query}", "i"))
            );

            return await _users.Find(filter)
                .Limit(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search users with query {Query}", query);
            return new List<User>();
        }
    }

    public async Task<List<User>> GetUsersByIdsAsync(IEnumerable<string> userIds)
    {
        try
        {
            var filter = Builders<User>.Filter.In(u => u.UserId, userIds);
            return await _users.Find(filter).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get users by IDs");
            return new List<User>();
        }
    }
}
