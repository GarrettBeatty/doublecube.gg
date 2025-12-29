using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Backgammon.Server.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Backgammon.Server.Services.DynamoDb;

public class DynamoDbFriendshipRepository : IFriendshipRepository
{
    private readonly IAmazonDynamoDB _dynamoDbClient;
    private readonly string _tableName;
    private readonly ILogger<DynamoDbFriendshipRepository> _logger;

    public DynamoDbFriendshipRepository(
        IAmazonDynamoDB dynamoDbClient,
        IConfiguration configuration,
        ILogger<DynamoDbFriendshipRepository> logger)
    {
        _dynamoDbClient = dynamoDbClient;
        _tableName = configuration["DynamoDb:TableName"] ?? "backgammon-local";
        _logger = logger;
    }

    public async Task<List<Friendship>> GetFriendsAsync(string userId)
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
                    [":skPrefix"] = new AttributeValue { S = $"FRIEND#{FriendshipStatus.Accepted}#" }
                }
            });

            return response.Items.Select(DynamoDbHelpers.UnmarshalFriendship).ToList();
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
            var response = await _dynamoDbClient.QueryAsync(new QueryRequest
            {
                TableName = _tableName,
                KeyConditionExpression = "PK = :pk AND begins_with(SK, :skPrefix)",
                FilterExpression = "initiatedBy <> :userId",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":pk"] = new AttributeValue { S = $"USER#{userId}" },
                    [":skPrefix"] = new AttributeValue { S = $"FRIEND#{FriendshipStatus.Pending}#" },
                    [":userId"] = new AttributeValue { S = userId }
                }
            });

            return response.Items.Select(DynamoDbHelpers.UnmarshalFriendship).ToList();
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
            var response = await _dynamoDbClient.QueryAsync(new QueryRequest
            {
                TableName = _tableName,
                KeyConditionExpression = "PK = :pk AND begins_with(SK, :skPrefix)",
                FilterExpression = "initiatedBy = :userId",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":pk"] = new AttributeValue { S = $"USER#{userId}" },
                    [":skPrefix"] = new AttributeValue { S = $"FRIEND#{FriendshipStatus.Pending}#" },
                    [":userId"] = new AttributeValue { S = userId }
                }
            });

            return response.Items.Select(DynamoDbHelpers.UnmarshalFriendship).ToList();
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
            // Query all friendship statuses for this user pair
            var response = await _dynamoDbClient.QueryAsync(new QueryRequest
            {
                TableName = _tableName,
                KeyConditionExpression = "PK = :pk AND begins_with(SK, :skPrefix)",
                FilterExpression = "friendUserId = :friendUserId",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":pk"] = new AttributeValue { S = $"USER#{userId}" },
                    [":skPrefix"] = new AttributeValue { S = $"FRIEND#" },
                    [":friendUserId"] = new AttributeValue { S = friendUserId }
                }
            });

            if (response.Items.Count == 0)
                return null;

            return DynamoDbHelpers.UnmarshalFriendship(response.Items[0]);
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
            var timestamp = DateTime.UtcNow;

            var transactItems = new List<TransactWriteItem>
            {
                // Create friendship record for fromUser
                new TransactWriteItem
                {
                    Put = new Put
                    {
                        TableName = _tableName,
                        Item = new Dictionary<string, AttributeValue>
                        {
                            ["PK"] = new AttributeValue { S = $"USER#{fromUserId}" },
                            ["SK"] = new AttributeValue { S = $"FRIEND#{FriendshipStatus.Pending}#{toUserId}" },
                            ["friendUserId"] = new AttributeValue { S = toUserId },
                            ["friendUsername"] = new AttributeValue { S = toUsername },
                            ["friendDisplayName"] = new AttributeValue { S = toDisplayName },
                            ["status"] = new AttributeValue { S = FriendshipStatus.Pending.ToString() },
                            ["initiatedBy"] = new AttributeValue { S = fromUserId },
                            ["createdAt"] = new AttributeValue { S = timestamp.ToString("O") },
                            ["entityType"] = new AttributeValue { S = "FRIENDSHIP" }
                        }
                    }
                },
                // Create bidirectional friendship record for toUser
                new TransactWriteItem
                {
                    Put = new Put
                    {
                        TableName = _tableName,
                        Item = new Dictionary<string, AttributeValue>
                        {
                            ["PK"] = new AttributeValue { S = $"USER#{toUserId}" },
                            ["SK"] = new AttributeValue { S = $"FRIEND#{FriendshipStatus.Pending}#{fromUserId}" },
                            ["friendUserId"] = new AttributeValue { S = fromUserId },
                            ["friendUsername"] = new AttributeValue { S = fromUsername },
                            ["friendDisplayName"] = new AttributeValue { S = fromDisplayName },
                            ["status"] = new AttributeValue { S = FriendshipStatus.Pending.ToString() },
                            ["initiatedBy"] = new AttributeValue { S = fromUserId },
                            ["createdAt"] = new AttributeValue { S = timestamp.ToString("O") },
                            ["entityType"] = new AttributeValue { S = "FRIENDSHIP" }
                        }
                    }
                }
            };

            await _dynamoDbClient.TransactWriteItemsAsync(new TransactWriteItemsRequest
            {
                TransactItems = transactItems
            });

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
            // Get existing friendship to preserve metadata
            var existingFriendship = await GetFriendshipAsync(userId, friendUserId);
            if (existingFriendship == null)
            {
                throw new InvalidOperationException("Friendship not found");
            }

            var now = DateTime.UtcNow;

            // Delete old Pending items and create new Accepted items
            var transactItems = new List<TransactWriteItem>
            {
                // Delete old Pending record for userId
                new TransactWriteItem
                {
                    Delete = new Delete
                    {
                        TableName = _tableName,
                        Key = new Dictionary<string, AttributeValue>
                        {
                            ["PK"] = new AttributeValue { S = $"USER#{userId}" },
                            ["SK"] = new AttributeValue { S = $"FRIEND#{FriendshipStatus.Pending}#{friendUserId}" }
                        }
                    }
                },
                // Delete old Pending record for friendUserId
                new TransactWriteItem
                {
                    Delete = new Delete
                    {
                        TableName = _tableName,
                        Key = new Dictionary<string, AttributeValue>
                        {
                            ["PK"] = new AttributeValue { S = $"USER#{friendUserId}" },
                            ["SK"] = new AttributeValue { S = $"FRIEND#{FriendshipStatus.Pending}#{userId}" }
                        }
                    }
                },
                // Create new Accepted record for userId
                new TransactWriteItem
                {
                    Put = new Put
                    {
                        TableName = _tableName,
                        Item = new Dictionary<string, AttributeValue>
                        {
                            ["PK"] = new AttributeValue { S = $"USER#{userId}" },
                            ["SK"] = new AttributeValue { S = $"FRIEND#{FriendshipStatus.Accepted}#{friendUserId}" },
                            ["friendUserId"] = new AttributeValue { S = existingFriendship.FriendUserId },
                            ["friendUsername"] = new AttributeValue { S = existingFriendship.FriendUsername },
                            ["friendDisplayName"] = new AttributeValue { S = existingFriendship.FriendDisplayName },
                            ["status"] = new AttributeValue { S = FriendshipStatus.Accepted.ToString() },
                            ["initiatedBy"] = new AttributeValue { S = existingFriendship.InitiatedBy },
                            ["createdAt"] = new AttributeValue { S = existingFriendship.CreatedAt.ToString("O") },
                            ["acceptedAt"] = new AttributeValue { S = now.ToString("O") },
                            ["entityType"] = new AttributeValue { S = "FRIENDSHIP" }
                        }
                    }
                },
                // Create new Accepted record for friendUserId
                new TransactWriteItem
                {
                    Put = new Put
                    {
                        TableName = _tableName,
                        Item = new Dictionary<string, AttributeValue>
                        {
                            ["PK"] = new AttributeValue { S = $"USER#{friendUserId}" },
                            ["SK"] = new AttributeValue { S = $"FRIEND#{FriendshipStatus.Accepted}#{userId}" },
                            ["friendUserId"] = new AttributeValue { S = userId },
                            ["friendUsername"] = new AttributeValue { S = "" },  // Would need to pass this in
                            ["friendDisplayName"] = new AttributeValue { S = "" },  // Would need to pass this in
                            ["status"] = new AttributeValue { S = FriendshipStatus.Accepted.ToString() },
                            ["initiatedBy"] = new AttributeValue { S = existingFriendship.InitiatedBy },
                            ["createdAt"] = new AttributeValue { S = existingFriendship.CreatedAt.ToString("O") },
                            ["acceptedAt"] = new AttributeValue { S = now.ToString("O") },
                            ["entityType"] = new AttributeValue { S = "FRIENDSHIP" }
                        }
                    }
                }
            };

            await _dynamoDbClient.TransactWriteItemsAsync(new TransactWriteItemsRequest
            {
                TransactItems = transactItems
            });

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
            var transactItems = new List<TransactWriteItem>
            {
                new TransactWriteItem
                {
                    Delete = new Delete
                    {
                        TableName = _tableName,
                        Key = new Dictionary<string, AttributeValue>
                        {
                            ["PK"] = new AttributeValue { S = $"USER#{userId}" },
                            ["SK"] = new AttributeValue { S = $"FRIEND#{FriendshipStatus.Pending}#{friendUserId}" }
                        }
                    }
                },
                new TransactWriteItem
                {
                    Delete = new Delete
                    {
                        TableName = _tableName,
                        Key = new Dictionary<string, AttributeValue>
                        {
                            ["PK"] = new AttributeValue { S = $"USER#{friendUserId}" },
                            ["SK"] = new AttributeValue { S = $"FRIEND#{FriendshipStatus.Pending}#{userId}" }
                        }
                    }
                }
            };

            await _dynamoDbClient.TransactWriteItemsAsync(new TransactWriteItemsRequest
            {
                TransactItems = transactItems
            });

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
            // First, remove any existing friendship
            var existingFriendship = await GetFriendshipAsync(userId, blockedUserId);

            var transactItems = new List<TransactWriteItem>();

            // If there's an existing friendship, delete it first
            if (existingFriendship != null)
            {
                transactItems.Add(new TransactWriteItem
                {
                    Delete = new Delete
                    {
                        TableName = _tableName,
                        Key = new Dictionary<string, AttributeValue>
                        {
                            ["PK"] = new AttributeValue { S = $"USER#{userId}" },
                            ["SK"] = new AttributeValue { S = $"FRIEND#{existingFriendship.Status}#{blockedUserId}" }
                        }
                    }
                });
            }

            // Create blocked relationship
            transactItems.Add(new TransactWriteItem
            {
                Put = new Put
                {
                    TableName = _tableName,
                    Item = new Dictionary<string, AttributeValue>
                    {
                        ["PK"] = new AttributeValue { S = $"USER#{userId}" },
                        ["SK"] = new AttributeValue { S = $"FRIEND#{FriendshipStatus.Blocked}#{blockedUserId}" },
                        ["friendUserId"] = new AttributeValue { S = blockedUserId },
                        ["friendUsername"] = new AttributeValue { S = "" },
                        ["friendDisplayName"] = new AttributeValue { S = "" },
                        ["status"] = new AttributeValue { S = FriendshipStatus.Blocked.ToString() },
                        ["initiatedBy"] = new AttributeValue { S = userId },
                        ["createdAt"] = new AttributeValue { S = DateTime.UtcNow.ToString("O") },
                        ["entityType"] = new AttributeValue { S = "FRIENDSHIP" }
                    }
                }
            });

            await _dynamoDbClient.TransactWriteItemsAsync(new TransactWriteItemsRequest
            {
                TransactItems = transactItems
            });

            _logger.LogInformation("User {UserId} blocked {BlockedUserId}", userId, blockedUserId);
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
            var transactItems = new List<TransactWriteItem>
            {
                new TransactWriteItem
                {
                    Delete = new Delete
                    {
                        TableName = _tableName,
                        Key = new Dictionary<string, AttributeValue>
                        {
                            ["PK"] = new AttributeValue { S = $"USER#{userId}" },
                            ["SK"] = new AttributeValue { S = $"FRIEND#{FriendshipStatus.Accepted}#{friendUserId}" }
                        }
                    }
                },
                new TransactWriteItem
                {
                    Delete = new Delete
                    {
                        TableName = _tableName,
                        Key = new Dictionary<string, AttributeValue>
                        {
                            ["PK"] = new AttributeValue { S = $"USER#{friendUserId}" },
                            ["SK"] = new AttributeValue { S = $"FRIEND#{FriendshipStatus.Accepted}#{userId}" }
                        }
                    }
                }
            };

            await _dynamoDbClient.TransactWriteItemsAsync(new TransactWriteItemsRequest
            {
                TransactItems = transactItems
            });

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
            var response = await _dynamoDbClient.QueryAsync(new QueryRequest
            {
                TableName = _tableName,
                KeyConditionExpression = "PK = :pk AND SK = :sk",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":pk"] = new AttributeValue { S = $"USER#{userId}" },
                    [":sk"] = new AttributeValue { S = $"FRIEND#{FriendshipStatus.Accepted}#{otherUserId}" }
                },
                Select = Select.COUNT
            });

            return response.Count > 0;
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
            var response = await _dynamoDbClient.QueryAsync(new QueryRequest
            {
                TableName = _tableName,
                KeyConditionExpression = "PK = :pk AND SK = :sk",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":pk"] = new AttributeValue { S = $"USER#{byUserId}" },
                    [":sk"] = new AttributeValue { S = $"FRIEND#{FriendshipStatus.Blocked}#{userId}" }
                },
                Select = Select.COUNT
            });

            return response.Count > 0;
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
            var response = await _dynamoDbClient.QueryAsync(new QueryRequest
            {
                TableName = _tableName,
                KeyConditionExpression = "PK = :pk AND begins_with(SK, :skPrefix)",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":pk"] = new AttributeValue { S = $"USER#{userId}" },
                    [":skPrefix"] = new AttributeValue { S = $"FRIEND#{FriendshipStatus.Accepted}#" }
                },
                Select = Select.COUNT
            });

            return response.Count ?? 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get friend count for user {UserId}", userId);
            return 0;
        }
    }
}
