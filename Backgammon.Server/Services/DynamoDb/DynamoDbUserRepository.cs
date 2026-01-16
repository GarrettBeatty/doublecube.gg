using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Backgammon.Server.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Backgammon.Server.Services.DynamoDb;

public class DynamoDbUserRepository : IUserRepository
{
    private readonly IAmazonDynamoDB _dynamoDbClient;
    private readonly string _tableName;
    private readonly ILogger<DynamoDbUserRepository> _logger;

    public DynamoDbUserRepository(
        IAmazonDynamoDB dynamoDbClient,
        IConfiguration configuration,
        ILogger<DynamoDbUserRepository> logger)
    {
        _dynamoDbClient = dynamoDbClient;
        _tableName = configuration["DynamoDb:TableName"] ?? "backgammon-local";
        _logger = logger;
    }

    public async Task<User?> GetByUserIdAsync(string userId)
    {
        try
        {
            var response = await _dynamoDbClient.GetItemAsync(new GetItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["PK"] = new AttributeValue { S = $"USER#{userId}" },
                    ["SK"] = new AttributeValue { S = "PROFILE" }
                }

                // Using eventually consistent reads (default) since CachedUserService
                // caches users immediately after creation, eliminating race conditions
            });

            if (!response.IsItemSet)
            {
                _logger.LogWarning("User {UserId} not found", userId);
                return null;
            }

            _logger.LogDebug("Successfully retrieved user {UserId}", userId);
            return DynamoDbHelpers.UnmarshalUser(response.Item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve user {UserId}", userId);
            return null;
        }
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        try
        {
            var normalizedUsername = username.ToLowerInvariant();

            var response = await _dynamoDbClient.QueryAsync(new QueryRequest
            {
                TableName = _tableName,
                IndexName = "GSI1",
                KeyConditionExpression = "GSI1PK = :pk AND GSI1SK = :sk",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":pk"] = new AttributeValue { S = $"USERNAME#{normalizedUsername}" },
                    [":sk"] = new AttributeValue { S = "PROFILE" }
                },
                Limit = 1
            });

            if (response.Items.Count == 0)
            {
                return null;
            }

            return DynamoDbHelpers.UnmarshalUser(response.Items[0]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve user by username {Username}", username);
            return null;
        }
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        try
        {
            var normalizedEmail = email.ToLowerInvariant();

            var response = await _dynamoDbClient.QueryAsync(new QueryRequest
            {
                TableName = _tableName,
                IndexName = "GSI2",
                KeyConditionExpression = "GSI2PK = :pk AND GSI2SK = :sk",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":pk"] = new AttributeValue { S = $"EMAIL#{normalizedEmail}" },
                    [":sk"] = new AttributeValue { S = "PROFILE" }
                },
                Limit = 1
            });

            if (response.Items.Count == 0)
            {
                return null;
            }

            return DynamoDbHelpers.UnmarshalUser(response.Items[0]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve user by email {Email}", email);
            return null;
        }
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        try
        {
            var normalizedUsername = username.ToLowerInvariant();

            var response = await _dynamoDbClient.QueryAsync(new QueryRequest
            {
                TableName = _tableName,
                IndexName = "GSI1",
                KeyConditionExpression = "GSI1PK = :pk AND GSI1SK = :sk",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":pk"] = new AttributeValue { S = $"USERNAME#{normalizedUsername}" },
                    [":sk"] = new AttributeValue { S = "PROFILE" }
                },
                Select = Select.COUNT,
                Limit = 1
            });

            return response.Count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if username exists {Username}", username);
            return false;
        }
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        try
        {
            var normalizedEmail = email.ToLowerInvariant();

            var response = await _dynamoDbClient.QueryAsync(new QueryRequest
            {
                TableName = _tableName,
                IndexName = "GSI2",
                KeyConditionExpression = "GSI2PK = :pk AND GSI2SK = :sk",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":pk"] = new AttributeValue { S = $"EMAIL#{normalizedEmail}" },
                    [":sk"] = new AttributeValue { S = "PROFILE" }
                },
                Select = Select.COUNT,
                Limit = 1
            });

            return response.Count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if email exists {Email}", email);
            return false;
        }
    }

    public async Task CreateUserAsync(User user)
    {
        try
        {
            var item = DynamoDbHelpers.MarshalUser(user);

            await _dynamoDbClient.PutItemAsync(new PutItemRequest
            {
                TableName = _tableName,
                Item = item,
                ConditionExpression = "attribute_not_exists(PK)"
            });

            _logger.LogInformation("Created user {UserId} ({Username})", user.UserId, user.Username);
        }
        catch (ConditionalCheckFailedException)
        {
            _logger.LogWarning("User {UserId} already exists", user.UserId);
            throw new InvalidOperationException($"User {user.UserId} already exists");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user {UserId}", user.UserId);
            throw;
        }
    }

    public async Task UpdateUserAsync(User user)
    {
        try
        {
            var item = DynamoDbHelpers.MarshalUser(user);

            await _dynamoDbClient.PutItemAsync(new PutItemRequest
            {
                TableName = _tableName,
                Item = item
            });

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
            await _dynamoDbClient.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["PK"] = new AttributeValue { S = $"USER#{userId}" },
                    ["SK"] = new AttributeValue { S = "PROFILE" }
                },
                UpdateExpression = "SET stats = :stats",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":stats"] = DynamoDbHelpers.MarshalUserStats(stats)
                }
            });

            _logger.LogDebug("Updated stats for user {UserId}", userId);
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
            var now = DateTime.UtcNow;

            await _dynamoDbClient.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["PK"] = new AttributeValue { S = $"USER#{userId}" },
                    ["SK"] = new AttributeValue { S = "PROFILE" }
                },
                UpdateExpression = "SET lastLoginAt = :now, lastSeenAt = :now",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":now"] = new AttributeValue { S = now.ToString("O") }
                }
            });

            _logger.LogDebug("Updated last login for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update last login for user {UserId}", userId);
            throw;
        }
    }

    public async Task LinkAnonymousIdAsync(string userId, string anonymousId)
    {
        try
        {
            await _dynamoDbClient.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["PK"] = new AttributeValue { S = $"USER#{userId}" },
                    ["SK"] = new AttributeValue { S = "PROFILE" }
                },
                UpdateExpression = "SET linkedAnonymousIds = list_append(if_not_exists(linkedAnonymousIds, :empty_list), :anonymousId)",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":anonymousId"] = new AttributeValue { L = new List<AttributeValue> { new AttributeValue { S = anonymousId } } },
                    [":empty_list"] = new AttributeValue { L = new List<AttributeValue>() }
                }
            });

            _logger.LogInformation("Linked anonymous ID {AnonymousId} to user {UserId}", anonymousId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to link anonymous ID {AnonymousId} to user {UserId}", anonymousId, userId);
            throw;
        }
    }

    public async Task<List<User>> SearchUsersAsync(string query, int limit = 10)
    {
        try
        {
            var normalizedQuery = query.ToLowerInvariant();

            var response = await _dynamoDbClient.QueryAsync(new QueryRequest
            {
                TableName = _tableName,
                IndexName = "GSI1",
                KeyConditionExpression = "begins_with(GSI1PK, :prefix)",
                FilterExpression = "GSI1SK = :sk",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":prefix"] = new AttributeValue { S = $"USERNAME#{normalizedQuery}" },
                    [":sk"] = new AttributeValue { S = "PROFILE" }
                },
                Limit = limit
            });

            return response.Items.Select(DynamoDbHelpers.UnmarshalUser).ToList();
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
            var userIdList = userIds.ToList();
            if (!userIdList.Any())
            {
                return new List<User>();
            }

            // DynamoDB BatchGetItem has a limit of 100 items
            var users = new List<User>();
            const int batchSize = 100;

            for (int i = 0; i < userIdList.Count; i += batchSize)
            {
                var batchUserIds = userIdList.Skip(i).Take(batchSize).ToList();
                var keys = batchUserIds.Select(userId => new Dictionary<string, AttributeValue>
                {
                    ["PK"] = new AttributeValue { S = $"USER#{userId}" },
                    ["SK"] = new AttributeValue { S = "PROFILE" }
                }).ToList();

                var response = await _dynamoDbClient.BatchGetItemAsync(new BatchGetItemRequest
                {
                    RequestItems = new Dictionary<string, KeysAndAttributes>
                    {
                        [_tableName] = new KeysAndAttributes { Keys = keys }
                    }
                });

                if (response.Responses.TryGetValue(_tableName, out var items))
                {
                    users.AddRange(items.Select(DynamoDbHelpers.UnmarshalUser));
                }
            }

            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve users by IDs");
            return new List<User>();
        }
    }

    public async Task<List<User>> GetTopPlayersByRatingAsync(int limit = 100)
    {
        try
        {
            var users = new List<User>();
            string? lastEvaluatedKey = null;

            do
            {
                var request = new ScanRequest
                {
                    TableName = _tableName,
                    FilterExpression = "SK = :sk AND isAnonymous = :notAnon AND ratedGamesCount > :minGames",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        [":sk"] = new AttributeValue { S = "PROFILE" },
                        [":notAnon"] = new AttributeValue { BOOL = false },
                        [":minGames"] = new AttributeValue { N = "0" }
                    },
                    ProjectionExpression = "userId, username, displayName, rating, ratedGamesCount, stats"
                };

                if (lastEvaluatedKey != null)
                {
                    request.ExclusiveStartKey = new Dictionary<string, AttributeValue>
                    {
                        ["PK"] = new AttributeValue { S = lastEvaluatedKey },
                        ["SK"] = new AttributeValue { S = "PROFILE" }
                    };
                }

                var response = await _dynamoDbClient.ScanAsync(request);
                users.AddRange(response.Items.Select(DynamoDbHelpers.UnmarshalUser));

                lastEvaluatedKey = response.LastEvaluatedKey?.GetValueOrDefault("PK")?.S;
            }
            while (lastEvaluatedKey != null);

            // Sort by rating descending and take top N
            return users
                .OrderByDescending(u => u.Rating)
                .ThenByDescending(u => u.Stats.TotalGames)
                .Take(limit)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve top players by rating");
            return new List<User>();
        }
    }

    public async Task<List<int>> GetAllRatingsAsync()
    {
        try
        {
            var ratings = new List<int>();
            string? lastEvaluatedKey = null;

            do
            {
                var request = new ScanRequest
                {
                    TableName = _tableName,
                    FilterExpression = "SK = :sk AND isAnonymous = :notAnon AND ratedGamesCount > :minGames",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        [":sk"] = new AttributeValue { S = "PROFILE" },
                        [":notAnon"] = new AttributeValue { BOOL = false },
                        [":minGames"] = new AttributeValue { N = "0" }
                    },
                    ProjectionExpression = "rating"
                };

                if (lastEvaluatedKey != null)
                {
                    request.ExclusiveStartKey = new Dictionary<string, AttributeValue>
                    {
                        ["PK"] = new AttributeValue { S = lastEvaluatedKey },
                        ["SK"] = new AttributeValue { S = "PROFILE" }
                    };
                }

                var response = await _dynamoDbClient.ScanAsync(request);

                foreach (var item in response.Items)
                {
                    if (item.TryGetValue("rating", out var ratingAttr) && int.TryParse(ratingAttr.N, out var rating))
                    {
                        ratings.Add(rating);
                    }
                }

                lastEvaluatedKey = response.LastEvaluatedKey?.GetValueOrDefault("PK")?.S;
            }
            while (lastEvaluatedKey != null);

            return ratings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve all ratings");
            return new List<int>();
        }
    }

    public async Task SaveRatingHistoryAsync(RatingHistoryEntry entry)
    {
        try
        {
            // Use reversed timestamp for natural descending order in DynamoDB
            var reversedTimestamp = DateTime.MaxValue.Ticks - entry.Timestamp.Ticks;

            var item = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new AttributeValue { S = $"USER#{entry.UserId}" },
                ["SK"] = new AttributeValue { S = $"RATING#{reversedTimestamp:D19}" },
                ["userId"] = new AttributeValue { S = entry.UserId },
                ["timestamp"] = new AttributeValue { S = entry.Timestamp.ToString("O") },
                ["rating"] = new AttributeValue { N = entry.Rating.ToString() },
                ["ratingChange"] = new AttributeValue { N = entry.RatingChange.ToString() },
                ["gameId"] = new AttributeValue { S = entry.GameId },
                ["won"] = new AttributeValue { BOOL = entry.Won }
            };

            if (!string.IsNullOrEmpty(entry.OpponentUserId))
            {
                item["opponentUserId"] = new AttributeValue { S = entry.OpponentUserId };
            }

            if (!string.IsNullOrEmpty(entry.OpponentUsername))
            {
                item["opponentUsername"] = new AttributeValue { S = entry.OpponentUsername };
            }

            await _dynamoDbClient.PutItemAsync(new PutItemRequest
            {
                TableName = _tableName,
                Item = item
            });

            _logger.LogDebug(
                "Saved rating history entry for user {UserId}: {Rating} ({RatingChange:+#;-#;0})",
                entry.UserId,
                entry.Rating,
                entry.RatingChange);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save rating history for user {UserId}", entry.UserId);
            throw;
        }
    }

    public async Task<List<RatingHistoryEntry>> GetRatingHistoryAsync(string userId, int limit = 30)
    {
        try
        {
            var response = await _dynamoDbClient.QueryAsync(new QueryRequest
            {
                TableName = _tableName,
                KeyConditionExpression = "PK = :pk AND begins_with(SK, :skPrefix)",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":pk"] = new AttributeValue { S = $"USER#{userId}" },
                    [":skPrefix"] = new AttributeValue { S = "RATING#" }
                },
                Limit = limit,
                ScanIndexForward = true // Ascending by reversed timestamp = descending by actual timestamp (most recent first)
            });

            var entries = response.Items.Select(item => new RatingHistoryEntry
            {
                UserId = item.GetValueOrDefault("userId")?.S ?? userId,
                Timestamp = item.TryGetValue("timestamp", out var ts) && DateTime.TryParse(ts.S, out var parsedTs)
                    ? parsedTs
                    : DateTime.UtcNow,
                Rating = item.TryGetValue("rating", out var r) && int.TryParse(r.N, out var rating) ? rating : 1500,
                RatingChange = item.TryGetValue("ratingChange", out var rc) && int.TryParse(rc.N, out var ratingChange) ? ratingChange : 0,
                GameId = item.GetValueOrDefault("gameId")?.S ?? string.Empty,
                OpponentUserId = item.GetValueOrDefault("opponentUserId")?.S,
                OpponentUsername = item.GetValueOrDefault("opponentUsername")?.S,
                Won = item.TryGetValue("won", out var w) && w.BOOL == true
            }).ToList();

            _logger.LogDebug("Retrieved {Count} rating history entries for user {UserId}", entries.Count, userId);
            return entries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve rating history for user {UserId}", userId);
            return new List<RatingHistoryEntry>();
        }
    }
}
