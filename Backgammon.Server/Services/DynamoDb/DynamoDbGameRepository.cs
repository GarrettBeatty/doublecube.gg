using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Backgammon.Server.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Backgammon.Server.Services.DynamoDb;

public class DynamoDbGameRepository : IGameRepository
{
    private readonly IAmazonDynamoDB _dynamoDbClient;
    private readonly string _tableName;
    private readonly ILogger<DynamoDbGameRepository> _logger;

    public DynamoDbGameRepository(
        IAmazonDynamoDB dynamoDbClient,
        IConfiguration configuration,
        ILogger<DynamoDbGameRepository> logger)
    {
        _dynamoDbClient = dynamoDbClient;
        _tableName = configuration["DynamoDb:TableName"] ?? "backgammon-local";
        _logger = logger;
    }

    public async Task SaveGameAsync(Game game)
    {
        try
        {
            var gameItem = DynamoDbHelpers.MarshalGame(game);

            // Create player-game index items
            var transactItems = new List<TransactWriteItem>
            {
                // Save the game itself
                new TransactWriteItem
                {
                    Put = new Put
                    {
                        TableName = _tableName,
                        Item = gameItem
                    }
                }
            };

            // Add player-game index items if players are assigned
            // In analysis mode, both players have the same ID, so only create one index item
            if (!string.IsNullOrEmpty(game.WhitePlayerId))
            {
                var whitePlayerGame = DynamoDbHelpers.CreatePlayerGameIndexItem(
                    game.WhitePlayerId,
                    game.GameId,
                    "White",
                    game.RedPlayerId ?? "",
                    game.Status,
                    game.CreatedAt,
                    game.LastUpdatedAt
                );

                transactItems.Add(new TransactWriteItem
                {
                    Put = new Put
                    {
                        TableName = _tableName,
                        Item = whitePlayerGame
                    }
                });
            }

            // Only add Red player index if it's a different player (not analysis mode)
            if (!string.IsNullOrEmpty(game.RedPlayerId) && game.RedPlayerId != game.WhitePlayerId)
            {
                var redPlayerGame = DynamoDbHelpers.CreatePlayerGameIndexItem(
                    game.RedPlayerId,
                    game.GameId,
                    "Red",
                    game.WhitePlayerId ?? "",
                    game.Status,
                    game.CreatedAt,
                    game.LastUpdatedAt
                );

                transactItems.Add(new TransactWriteItem
                {
                    Put = new Put
                    {
                        TableName = _tableName,
                        Item = redPlayerGame
                    }
                });
            }

            await _dynamoDbClient.TransactWriteItemsAsync(new TransactWriteItemsRequest
            {
                TransactItems = transactItems
            });

            _logger.LogDebug("Saved game {GameId} with status {Status}", game.GameId, game.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save game {GameId}", game.GameId);
            throw;
        }
    }

    public async Task<Game?> GetGameByGameIdAsync(string gameId)
    {
        try
        {
            var response = await _dynamoDbClient.GetItemAsync(new GetItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["PK"] = new AttributeValue { S = $"GAME#{gameId}" },
                    ["SK"] = new AttributeValue { S = "METADATA" }
                }
            });

            if (!response.IsItemSet)
                return null;

            return DynamoDbHelpers.UnmarshalGame(response.Item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve game {GameId}", gameId);
            return null;
        }
    }

    public async Task<List<Game>> GetActiveGamesAsync()
    {
        try
        {
            var response = await _dynamoDbClient.QueryAsync(new QueryRequest
            {
                TableName = _tableName,
                IndexName = "GSI3",
                KeyConditionExpression = "GSI3PK = :pk",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":pk"] = new AttributeValue { S = "GAME_STATUS#InProgress" }
                }
            });

            return response.Items.Select(DynamoDbHelpers.UnmarshalGame).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve active games");
            return new List<Game>();
        }
    }

    public async Task UpdateGameStatusAsync(string gameId, string status)
    {
        try
        {
            // First get the game to know player IDs
            var game = await GetGameByGameIdAsync(gameId);
            if (game == null)
            {
                _logger.LogWarning("Cannot update status for non-existent game {GameId}", gameId);
                return;
            }

            var now = DateTime.UtcNow;
            var transactItems = new List<TransactWriteItem>();

            // Update the game itself
            var updateExpression = "SET #status = :status, lastUpdatedAt = :now, GSI3PK = :gsi3pk, GSI3SK = :gsi3sk";
            var expressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":status"] = new AttributeValue { S = status },
                [":now"] = new AttributeValue { S = now.ToString("O") },
                [":gsi3pk"] = new AttributeValue { S = $"GAME_STATUS#{status}" },
                [":gsi3sk"] = new AttributeValue { S = now.Ticks.ToString("D19") }
            };

            if (status == "Completed")
            {
                updateExpression += ", completedAt = :completedAt";
                expressionAttributeValues[":completedAt"] = new AttributeValue { S = now.ToString("O") };
            }

            transactItems.Add(new TransactWriteItem
            {
                Update = new Update
                {
                    TableName = _tableName,
                    Key = new Dictionary<string, AttributeValue>
                    {
                        ["PK"] = new AttributeValue { S = $"GAME#{gameId}" },
                        ["SK"] = new AttributeValue { S = "METADATA" }
                    },
                    UpdateExpression = updateExpression,
                    ExpressionAttributeNames = new Dictionary<string, string>
                    {
                        ["#status"] = "status"
                    },
                    ExpressionAttributeValues = expressionAttributeValues
                }
            });

            // Update player-game index items
            if (!string.IsNullOrEmpty(game.WhitePlayerId))
            {
                var reversedTimestamp = (DateTime.MaxValue.Ticks - game.LastUpdatedAt.Ticks).ToString("D19");
                transactItems.Add(new TransactWriteItem
                {
                    Update = new Update
                    {
                        TableName = _tableName,
                        Key = new Dictionary<string, AttributeValue>
                        {
                            ["PK"] = new AttributeValue { S = $"USER#{game.WhitePlayerId}" },
                            ["SK"] = new AttributeValue { S = $"GAME#{reversedTimestamp}#{gameId}" }
                        },
                        UpdateExpression = "SET #status = :status, lastUpdatedAt = :now",
                        ExpressionAttributeNames = new Dictionary<string, string>
                        {
                            ["#status"] = "status"
                        },
                        ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                        {
                            [":status"] = new AttributeValue { S = status },
                            [":now"] = new AttributeValue { S = now.ToString("O") }
                        }
                    }
                });
            }

            if (!string.IsNullOrEmpty(game.RedPlayerId))
            {
                var reversedTimestamp = (DateTime.MaxValue.Ticks - game.LastUpdatedAt.Ticks).ToString("D19");
                transactItems.Add(new TransactWriteItem
                {
                    Update = new Update
                    {
                        TableName = _tableName,
                        Key = new Dictionary<string, AttributeValue>
                        {
                            ["PK"] = new AttributeValue { S = $"USER#{game.RedPlayerId}" },
                            ["SK"] = new AttributeValue { S = $"GAME#{reversedTimestamp}#{gameId}" }
                        },
                        UpdateExpression = "SET #status = :status, lastUpdatedAt = :now",
                        ExpressionAttributeNames = new Dictionary<string, string>
                        {
                            ["#status"] = "status"
                        },
                        ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                        {
                            [":status"] = new AttributeValue { S = status },
                            [":now"] = new AttributeValue { S = now.ToString("O") }
                        }
                    }
                });
            }

            await _dynamoDbClient.TransactWriteItemsAsync(new TransactWriteItemsRequest
            {
                TransactItems = transactItems
            });

            _logger.LogInformation("Updated game {GameId} status to {Status}", gameId, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update game status for {GameId}", gameId);
            throw;
        }
    }

    public async Task<List<Game>> GetPlayerGamesAsync(string playerId, string? status = null, int limit = 50, int skip = 0)
    {
        try
        {
            var request = new QueryRequest
            {
                TableName = _tableName,
                KeyConditionExpression = "PK = :pk AND begins_with(SK, :skPrefix)",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":pk"] = new AttributeValue { S = $"USER#{playerId}" },
                    [":skPrefix"] = new AttributeValue { S = "GAME#" }
                },
                ScanIndexForward = false // Descending order (latest first)
            };

            if (!string.IsNullOrEmpty(status))
            {
                request.FilterExpression = "#status = :status";
                request.ExpressionAttributeNames = new Dictionary<string, string>
                {
                    ["#status"] = "status"
                };
                request.ExpressionAttributeValues[":status"] = new AttributeValue { S = status };
            }

            var playerGameItems = new List<Dictionary<string, AttributeValue>>();
            Dictionary<string, AttributeValue>? lastKey = null;
            var itemsProcessed = 0;

            // Query player-game index items with pagination
            while (playerGameItems.Count < limit)
            {
                if (lastKey != null)
                    request.ExclusiveStartKey = lastKey;

                var response = await _dynamoDbClient.QueryAsync(request);

                foreach (var item in response.Items)
                {
                    if (itemsProcessed < skip)
                    {
                        itemsProcessed++;
                        continue;
                    }

                    playerGameItems.Add(item);
                    if (playerGameItems.Count >= limit)
                        break;
                }

                if (response.LastEvaluatedKey == null || response.LastEvaluatedKey.Count == 0)
                    break;

                lastKey = response.LastEvaluatedKey;
            }

            // Get full game details (deduplicate game IDs - same game may have multiple index items from different saves)
            var gameIds = playerGameItems.Select(item => item["gameId"].S).Distinct().ToList();
            var games = new List<Game>();

            foreach (var gameId in gameIds)
            {
                var game = await GetGameByGameIdAsync(gameId);
                if (game != null)
                {
                    games.Add(game);
                }
            }

            return games;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve games for player {PlayerId}", playerId);
            return new List<Game>();
        }
    }

    public async Task<PlayerStats> GetPlayerStatsAsync(string playerId)
    {
        try
        {
            // Get all completed games for the player
            var games = await GetPlayerGamesAsync(playerId, "Completed", limit: 1000);

            var stats = new PlayerStats
            {
                PlayerId = playerId,
                TotalGames = games.Count
            };

            foreach (var game in games)
            {
                // Determine if player won
                var playerColor = game.WhitePlayerId == playerId ? "White" : "Red";
                var isWin = game.Winner == playerColor;

                if (isWin)
                {
                    stats.Wins++;
                    stats.TotalStakes += game.Stakes;

                    // Categorize win type based on stakes and doubling cube
                    var baseStakes = game.Stakes / game.DoublingCubeValue;
                    if (baseStakes == 3)
                    {
                        stats.BackgammonWins++;
                    }
                    else if (baseStakes == 2)
                    {
                        stats.GammonWins++;
                    }
                    else
                    {
                        stats.NormalWins++;
                    }
                }
                else
                {
                    stats.Losses++;
                }
            }

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate stats for player {PlayerId}", playerId);
            return new PlayerStats { PlayerId = playerId };
        }
    }

    public async Task<List<Game>> GetRecentGamesAsync(string? status = "Completed", int limit = 20)
    {
        try
        {
            if (string.IsNullOrEmpty(status))
            {
                // If no status filter, we'd need to scan - not efficient
                // For now, default to completed games
                status = "Completed";
            }

            var response = await _dynamoDbClient.QueryAsync(new QueryRequest
            {
                TableName = _tableName,
                IndexName = "GSI3",
                KeyConditionExpression = "GSI3PK = :pk",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":pk"] = new AttributeValue { S = $"GAME_STATUS#{status}" }
                },
                ScanIndexForward = false, // Descending order (most recent first)
                Limit = limit
            });

            return response.Items.Select(DynamoDbHelpers.UnmarshalGame).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve recent games");
            return new List<Game>();
        }
    }

    public async Task<long> GetTotalGameCountAsync(string? status = null)
    {
        try
        {
            if (!string.IsNullOrEmpty(status))
            {
                var response = await _dynamoDbClient.QueryAsync(new QueryRequest
                {
                    TableName = _tableName,
                    IndexName = "GSI3",
                    KeyConditionExpression = "GSI3PK = :pk",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        [":pk"] = new AttributeValue { S = $"GAME_STATUS#{status}" }
                    },
                    Select = Select.COUNT
                });

                return response.Count ?? 0;
            }
            else
            {
                // Count all games across all statuses
                var statuses = new[] { "InProgress", "Completed", "Abandoned" };
                long totalCount = 0;

                foreach (var s in statuses)
                {
                    var response = await _dynamoDbClient.QueryAsync(new QueryRequest
                    {
                        TableName = _tableName,
                        IndexName = "GSI3",
                        KeyConditionExpression = "GSI3PK = :pk",
                        ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                        {
                            [":pk"] = new AttributeValue { S = $"GAME_STATUS#{s}" }
                        },
                        Select = Select.COUNT
                    });

                    totalCount += response.Count ?? 0;
                }

                return totalCount;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to count games");
            return 0;
        }
    }

    public async Task DeleteGameAsync(string gameId)
    {
        try
        {
            // First get the game to know player IDs
            var game = await GetGameByGameIdAsync(gameId);
            if (game == null)
            {
                _logger.LogWarning("Cannot delete non-existent game {GameId}", gameId);
                return;
            }

            var transactItems = new List<TransactWriteItem>
            {
                // Delete the game itself
                new TransactWriteItem
                {
                    Delete = new Delete
                    {
                        TableName = _tableName,
                        Key = new Dictionary<string, AttributeValue>
                        {
                            ["PK"] = new AttributeValue { S = $"GAME#{gameId}" },
                            ["SK"] = new AttributeValue { S = "METADATA" }
                        }
                    }
                }
            };

            // Delete player-game index items
            if (!string.IsNullOrEmpty(game.WhitePlayerId))
            {
                var reversedTimestamp = (DateTime.MaxValue.Ticks - game.LastUpdatedAt.Ticks).ToString("D19");
                transactItems.Add(new TransactWriteItem
                {
                    Delete = new Delete
                    {
                        TableName = _tableName,
                        Key = new Dictionary<string, AttributeValue>
                        {
                            ["PK"] = new AttributeValue { S = $"USER#{game.WhitePlayerId}" },
                            ["SK"] = new AttributeValue { S = $"GAME#{reversedTimestamp}#{gameId}" }
                        }
                    }
                });
            }

            if (!string.IsNullOrEmpty(game.RedPlayerId))
            {
                var reversedTimestamp = (DateTime.MaxValue.Ticks - game.LastUpdatedAt.Ticks).ToString("D19");
                transactItems.Add(new TransactWriteItem
                {
                    Delete = new Delete
                    {
                        TableName = _tableName,
                        Key = new Dictionary<string, AttributeValue>
                        {
                            ["PK"] = new AttributeValue { S = $"USER#{game.RedPlayerId}" },
                            ["SK"] = new AttributeValue { S = $"GAME#{reversedTimestamp}#{gameId}" }
                        }
                    }
                });
            }

            await _dynamoDbClient.TransactWriteItemsAsync(new TransactWriteItemsRequest
            {
                TransactItems = transactItems
            });

            _logger.LogInformation("Deleted game {GameId}", gameId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete game {GameId}", gameId);
            throw;
        }
    }

    public async Task<List<Game>> GetGamesLastUpdatedBeforeAsync(DateTime timestamp, string status)
    {
        try
        {
            var response = await _dynamoDbClient.QueryAsync(new QueryRequest
            {
                TableName = _tableName,
                IndexName = "GSI3",
                KeyConditionExpression = "GSI3PK = :pk AND GSI3SK < :timestamp",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":pk"] = new AttributeValue { S = $"GAME_STATUS#{status}" },
                    [":timestamp"] = new AttributeValue { S = timestamp.Ticks.ToString("D19") }
                }
            });

            return response.Items.Select(DynamoDbHelpers.UnmarshalGame).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query games before {Timestamp} with status {Status}", timestamp, status);
            return new List<Game>();
        }
    }
}
