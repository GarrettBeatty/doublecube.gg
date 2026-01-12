using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Backgammon.Server.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Backgammon.Server.Services.DynamoDb;

/// <summary>
/// DynamoDB implementation of the theme repository.
/// </summary>
public class DynamoDbThemeRepository : IThemeRepository
{
    private readonly IAmazonDynamoDB _dynamoDbClient;
    private readonly string _tableName;
    private readonly ILogger<DynamoDbThemeRepository> _logger;

    public DynamoDbThemeRepository(
        IAmazonDynamoDB dynamoDbClient,
        IConfiguration configuration,
        ILogger<DynamoDbThemeRepository> logger)
    {
        _dynamoDbClient = dynamoDbClient;
        _tableName = configuration["DynamoDb:TableName"] ?? "backgammon-local";
        _logger = logger;
    }

    public async Task<BoardTheme?> GetByIdAsync(string themeId)
    {
        try
        {
            var response = await _dynamoDbClient.GetItemAsync(new GetItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["PK"] = new AttributeValue { S = $"THEME#{themeId}" },
                    ["SK"] = new AttributeValue { S = "METADATA" }
                }
            });

            if (!response.IsItemSet)
            {
                _logger.LogDebug("Theme {ThemeId} not found", themeId);
                return null;
            }

            return DynamoDbHelpers.UnmarshalBoardTheme(response.Item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve theme {ThemeId}", themeId);
            return null;
        }
    }

    public async Task<(List<BoardTheme> Themes, string? NextCursor)> GetPublicThemesAsync(int limit = 50, string? cursor = null)
    {
        try
        {
            var request = new QueryRequest
            {
                TableName = _tableName,
                IndexName = "GSI3",
                KeyConditionExpression = "GSI3PK = :pk",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":pk"] = new AttributeValue { S = "PUBLIC_THEMES" }
                },
                ScanIndexForward = false, // Most popular first (higher usage count)
                Limit = limit
            };

            // Handle pagination cursor
            if (!string.IsNullOrEmpty(cursor))
            {
                try
                {
                    var cursorParts = cursor.Split('|');
                    if (cursorParts.Length == 2)
                    {
                        request.ExclusiveStartKey = new Dictionary<string, AttributeValue>
                        {
                            ["GSI3PK"] = new AttributeValue { S = "PUBLIC_THEMES" },
                            ["GSI3SK"] = new AttributeValue { S = cursorParts[0] },
                            ["PK"] = new AttributeValue { S = cursorParts[1] },
                            ["SK"] = new AttributeValue { S = "METADATA" }
                        };
                    }
                }
                catch
                {
                    _logger.LogWarning("Invalid cursor format: {Cursor}", cursor);
                }
            }

            var response = await _dynamoDbClient.QueryAsync(request);

            var themes = response.Items.Select(DynamoDbHelpers.UnmarshalBoardTheme).ToList();

            string? nextCursor = null;
            if (response.LastEvaluatedKey != null && response.LastEvaluatedKey.Count > 0)
            {
                var gsi3sk = response.LastEvaluatedKey["GSI3SK"].S;
                var pk = response.LastEvaluatedKey["PK"].S;
                nextCursor = $"{gsi3sk}|{pk}";
            }

            return (themes, nextCursor);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve public themes");
            return (new List<BoardTheme>(), null);
        }
    }

    public async Task<List<BoardTheme>> GetThemesByAuthorAsync(string authorId)
    {
        try
        {
            var response = await _dynamoDbClient.QueryAsync(new QueryRequest
            {
                TableName = _tableName,
                IndexName = "GSI1",
                KeyConditionExpression = "GSI1PK = :pk AND begins_with(GSI1SK, :skPrefix)",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":pk"] = new AttributeValue { S = $"USER#{authorId}" },
                    [":skPrefix"] = new AttributeValue { S = "THEME#" }
                },
                ScanIndexForward = false // Newest first
            });

            return response.Items.Select(DynamoDbHelpers.UnmarshalBoardTheme).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve themes for author {AuthorId}", authorId);
            return new List<BoardTheme>();
        }
    }

    public async Task<List<BoardTheme>> GetDefaultThemesAsync()
    {
        try
        {
            // Scan for default themes (isDefault = true)
            var response = await _dynamoDbClient.ScanAsync(new ScanRequest
            {
                TableName = _tableName,
                FilterExpression = "entityType = :entityType AND isDefault = :isDefault",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":entityType"] = new AttributeValue { S = "THEME" },
                    [":isDefault"] = new AttributeValue { BOOL = true }
                }
            });

            return response.Items.Select(DynamoDbHelpers.UnmarshalBoardTheme).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve default themes");
            return new List<BoardTheme>();
        }
    }

    public async Task CreateThemeAsync(BoardTheme theme)
    {
        try
        {
            var item = DynamoDbHelpers.MarshalBoardTheme(theme);

            await _dynamoDbClient.PutItemAsync(new PutItemRequest
            {
                TableName = _tableName,
                Item = item,
                ConditionExpression = "attribute_not_exists(PK)" // Prevent overwriting existing theme
            });

            _logger.LogInformation("Created theme {ThemeId} by author {AuthorId}", theme.ThemeId, theme.AuthorId);
        }
        catch (ConditionalCheckFailedException)
        {
            _logger.LogWarning("Theme {ThemeId} already exists", theme.ThemeId);
            throw new InvalidOperationException($"Theme with ID {theme.ThemeId} already exists");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create theme {ThemeId}", theme.ThemeId);
            throw;
        }
    }

    public async Task UpdateThemeAsync(BoardTheme theme)
    {
        try
        {
            theme.UpdatedAt = DateTime.UtcNow;
            var item = DynamoDbHelpers.MarshalBoardTheme(theme);

            await _dynamoDbClient.PutItemAsync(new PutItemRequest
            {
                TableName = _tableName,
                Item = item
            });

            _logger.LogInformation("Updated theme {ThemeId}", theme.ThemeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update theme {ThemeId}", theme.ThemeId);
            throw;
        }
    }

    public async Task DeleteThemeAsync(string themeId)
    {
        try
        {
            await _dynamoDbClient.DeleteItemAsync(new DeleteItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["PK"] = new AttributeValue { S = $"THEME#{themeId}" },
                    ["SK"] = new AttributeValue { S = "METADATA" }
                }
            });

            // Also delete all likes for this theme
            await DeleteAllLikesForThemeAsync(themeId);

            _logger.LogInformation("Deleted theme {ThemeId}", themeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete theme {ThemeId}", themeId);
            throw;
        }
    }

    public async Task IncrementUsageCountAsync(string themeId)
    {
        try
        {
            await _dynamoDbClient.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["PK"] = new AttributeValue { S = $"THEME#{themeId}" },
                    ["SK"] = new AttributeValue { S = "METADATA" }
                },
                UpdateExpression = "SET usageCount = usageCount + :inc, updatedAt = :updatedAt",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":inc"] = new AttributeValue { N = "1" },
                    [":updatedAt"] = new AttributeValue { S = DateTime.UtcNow.ToString("O") }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to increment usage count for theme {ThemeId}", themeId);
        }
    }

    public async Task DecrementUsageCountAsync(string themeId)
    {
        try
        {
            await _dynamoDbClient.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["PK"] = new AttributeValue { S = $"THEME#{themeId}" },
                    ["SK"] = new AttributeValue { S = "METADATA" }
                },
                UpdateExpression = "SET usageCount = usageCount - :dec, updatedAt = :updatedAt",
                ConditionExpression = "usageCount > :zero",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":dec"] = new AttributeValue { N = "1" },
                    [":zero"] = new AttributeValue { N = "0" },
                    [":updatedAt"] = new AttributeValue { S = DateTime.UtcNow.ToString("O") }
                }
            });
        }
        catch (ConditionalCheckFailedException)
        {
            // Usage count is already 0, ignore
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrement usage count for theme {ThemeId}", themeId);
        }
    }

    public async Task<bool> HasUserLikedThemeAsync(string themeId, string userId)
    {
        try
        {
            var response = await _dynamoDbClient.GetItemAsync(new GetItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["PK"] = new AttributeValue { S = $"THEME#{themeId}" },
                    ["SK"] = new AttributeValue { S = $"LIKE#{userId}" }
                }
            });

            return response.IsItemSet;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if user {UserId} liked theme {ThemeId}", userId, themeId);
            return false;
        }
    }

    public async Task LikeThemeAsync(string themeId, string userId)
    {
        try
        {
            // Create like record
            var likeItem = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new AttributeValue { S = $"THEME#{themeId}" },
                ["SK"] = new AttributeValue { S = $"LIKE#{userId}" },
                ["userId"] = new AttributeValue { S = userId },
                ["themeId"] = new AttributeValue { S = themeId },
                ["createdAt"] = new AttributeValue { S = DateTime.UtcNow.ToString("O") },
                ["entityType"] = new AttributeValue { S = "THEME_LIKE" },
                // GSI1 for user's liked themes
                ["GSI1PK"] = new AttributeValue { S = $"USER#{userId}" },
                ["GSI1SK"] = new AttributeValue { S = $"LIKED_THEME#{themeId}" }
            };

            await _dynamoDbClient.PutItemAsync(new PutItemRequest
            {
                TableName = _tableName,
                Item = likeItem,
                ConditionExpression = "attribute_not_exists(PK)" // Prevent duplicate likes
            });

            // Increment like count on theme
            await _dynamoDbClient.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["PK"] = new AttributeValue { S = $"THEME#{themeId}" },
                    ["SK"] = new AttributeValue { S = "METADATA" }
                },
                UpdateExpression = "SET likeCount = likeCount + :inc",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":inc"] = new AttributeValue { N = "1" }
                }
            });

            _logger.LogInformation("User {UserId} liked theme {ThemeId}", userId, themeId);
        }
        catch (ConditionalCheckFailedException)
        {
            _logger.LogDebug("User {UserId} already liked theme {ThemeId}", userId, themeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to like theme {ThemeId} by user {UserId}", themeId, userId);
            throw;
        }
    }

    public async Task UnlikeThemeAsync(string themeId, string userId)
    {
        try
        {
            // Delete like record
            var response = await _dynamoDbClient.DeleteItemAsync(new DeleteItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["PK"] = new AttributeValue { S = $"THEME#{themeId}" },
                    ["SK"] = new AttributeValue { S = $"LIKE#{userId}" }
                },
                ReturnValues = ReturnValue.ALL_OLD
            });

            // Only decrement if the like actually existed
            if (response.Attributes != null && response.Attributes.Count > 0)
            {
                await _dynamoDbClient.UpdateItemAsync(new UpdateItemRequest
                {
                    TableName = _tableName,
                    Key = new Dictionary<string, AttributeValue>
                    {
                        ["PK"] = new AttributeValue { S = $"THEME#{themeId}" },
                        ["SK"] = new AttributeValue { S = "METADATA" }
                    },
                    UpdateExpression = "SET likeCount = likeCount - :dec",
                    ConditionExpression = "likeCount > :zero",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        [":dec"] = new AttributeValue { N = "1" },
                        [":zero"] = new AttributeValue { N = "0" }
                    }
                });

                _logger.LogInformation("User {UserId} unliked theme {ThemeId}", userId, themeId);
            }
        }
        catch (ConditionalCheckFailedException)
        {
            // Like count already at 0, ignore
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unlike theme {ThemeId} by user {UserId}", themeId, userId);
            throw;
        }
    }

    public async Task<List<string>> GetUserLikedThemeIdsAsync(string userId)
    {
        try
        {
            var response = await _dynamoDbClient.QueryAsync(new QueryRequest
            {
                TableName = _tableName,
                IndexName = "GSI1",
                KeyConditionExpression = "GSI1PK = :pk AND begins_with(GSI1SK, :skPrefix)",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":pk"] = new AttributeValue { S = $"USER#{userId}" },
                    [":skPrefix"] = new AttributeValue { S = "LIKED_THEME#" }
                }
            });

            return response.Items
                .Select(item => item.TryGetValue("themeId", out var themeId) ? themeId.S : null)
                .Where(id => id != null)
                .Cast<string>()
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get liked themes for user {UserId}", userId);
            return new List<string>();
        }
    }

    public async Task<List<BoardTheme>> SearchThemesAsync(string query, int limit = 20)
    {
        try
        {
            // For now, do a scan with filter (consider adding a GSI for name search in production)
            var normalizedQuery = query.ToLowerInvariant();

            var response = await _dynamoDbClient.ScanAsync(new ScanRequest
            {
                TableName = _tableName,
                FilterExpression = "entityType = :entityType AND visibility = :visibility AND contains(#name, :query)",
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    ["#name"] = "name"
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":entityType"] = new AttributeValue { S = "THEME" },
                    [":visibility"] = new AttributeValue { S = "Public" },
                    [":query"] = new AttributeValue { S = query }
                },
                Limit = limit
            });

            return response.Items.Select(DynamoDbHelpers.UnmarshalBoardTheme).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search themes with query {Query}", query);
            return new List<BoardTheme>();
        }
    }

    private async Task DeleteAllLikesForThemeAsync(string themeId)
    {
        try
        {
            // Query all likes for this theme
            var response = await _dynamoDbClient.QueryAsync(new QueryRequest
            {
                TableName = _tableName,
                KeyConditionExpression = "PK = :pk AND begins_with(SK, :skPrefix)",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":pk"] = new AttributeValue { S = $"THEME#{themeId}" },
                    [":skPrefix"] = new AttributeValue { S = "LIKE#" }
                }
            });

            // Delete each like
            foreach (var item in response.Items)
            {
                await _dynamoDbClient.DeleteItemAsync(new DeleteItemRequest
                {
                    TableName = _tableName,
                    Key = new Dictionary<string, AttributeValue>
                    {
                        ["PK"] = item["PK"],
                        ["SK"] = item["SK"]
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete likes for theme {ThemeId}", themeId);
        }
    }
}
