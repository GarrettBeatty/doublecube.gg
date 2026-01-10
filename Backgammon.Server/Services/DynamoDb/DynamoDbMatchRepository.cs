using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Backgammon.Server.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Backgammon.Server.Services.DynamoDb;

public class DynamoDbMatchRepository : IMatchRepository
{
    private readonly IAmazonDynamoDB _dynamoDbClient;
    private readonly string _tableName;
    private readonly ILogger<DynamoDbMatchRepository> _logger;

    public DynamoDbMatchRepository(
        IAmazonDynamoDB dynamoDbClient,
        IConfiguration configuration,
        ILogger<DynamoDbMatchRepository> logger)
    {
        _dynamoDbClient = dynamoDbClient;
        _tableName = configuration["DynamoDb:TableName"] ?? "backgammon-local";
        _logger = logger;
    }

    public async Task SaveMatchAsync(Match match)
    {
        try
        {
            var matchItem = DynamoDbHelpers.MarshalMatch(match);

            // Create player-match index items
            var transactItems = new List<TransactWriteItem>
            {
                // Save the match itself
                new TransactWriteItem
                {
                    Put = new Put
                    {
                        TableName = _tableName,
                        Item = matchItem
                    }
                }
            };

            // Add player-match index items
            var player1Match = DynamoDbHelpers.CreatePlayerMatchIndexItem(
                match.Player1Id,
                match.MatchId,
                match.Player2Id ?? string.Empty, // Empty string for null opponent (OpenLobby)
                match.Status,
                match.CreatedAt);

            transactItems.Add(new TransactWriteItem
            {
                Put = new Put
                {
                    TableName = _tableName,
                    Item = player1Match
                }
            });

            // Only create Player2 index item if Player2Id is set
            if (!string.IsNullOrEmpty(match.Player2Id))
            {
                var player2Match = DynamoDbHelpers.CreatePlayerMatchIndexItem(
                    match.Player2Id,
                    match.MatchId,
                    match.Player1Id,
                    match.Status,
                    match.CreatedAt);

                transactItems.Add(new TransactWriteItem
                {
                    Put = new Put
                    {
                        TableName = _tableName,
                        Item = player2Match
                    }
                });
            }

            await _dynamoDbClient.TransactWriteItemsAsync(new TransactWriteItemsRequest
            {
                TransactItems = transactItems
            });

            _logger.LogDebug("Saved match {MatchId} with status {Status}", match.MatchId, match.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save match {MatchId}", match.MatchId);
            throw;
        }
    }

    public async Task<Match?> GetMatchByIdAsync(string matchId)
    {
        try
        {
            var response = await _dynamoDbClient.GetItemAsync(new GetItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["PK"] = new AttributeValue { S = $"MATCH#{matchId}" },
                    ["SK"] = new AttributeValue { S = "METADATA" }
                }
            });

            if (!response.IsItemSet)
            {
                return null;
            }

            return DynamoDbHelpers.UnmarshalMatch(response.Item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve match {MatchId}", matchId);
            return null;
        }
    }

    public async Task UpdateMatchAsync(Match match)
    {
        try
        {
            match.LastUpdatedAt = DateTime.UtcNow;

            var updateExpression = @"
                SET player1Score = :p1Score, 
                    player2Score = :p2Score,
                    isCrawfordGame = :crawford,
                    hasCrawfordGameBeenPlayed = :hadCrawford,
                    currentGameId = :currentGame,
                    lastUpdatedAt = :now,
                    #status = :status,
                    GSI3PK = :gsi3pk,
                    GSI3SK = :gsi3sk";

            var expressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":p1Score"] = new AttributeValue { N = match.Player1Score.ToString() },
                [":p2Score"] = new AttributeValue { N = match.Player2Score.ToString() },
                [":crawford"] = new AttributeValue { BOOL = match.IsCrawfordGame },
                [":hadCrawford"] = new AttributeValue { BOOL = match.HasCrawfordGameBeenPlayed },
                [":currentGame"] = string.IsNullOrEmpty(match.CurrentGameId)
                    ? new AttributeValue { NULL = true }
                    : new AttributeValue { S = match.CurrentGameId },
                [":now"] = new AttributeValue { S = match.LastUpdatedAt.ToString("O") },
                [":status"] = new AttributeValue { S = match.Status },
                [":gsi3pk"] = new AttributeValue { S = $"MATCH_STATUS#{match.Status}" },
                [":gsi3sk"] = new AttributeValue { S = match.LastUpdatedAt.Ticks.ToString("D19") }
            };

            var expressionAttributeNames = new Dictionary<string, string>
            {
                ["#status"] = "status"
            };

            // Add Player2 info if set (for when player 2 joins)
            if (!string.IsNullOrEmpty(match.Player2Id))
            {
                updateExpression += ", player2Id = :p2Id, player2Name = :p2Name";
                expressionAttributeValues[":p2Id"] = new AttributeValue { S = match.Player2Id };
                expressionAttributeValues[":p2Name"] = new AttributeValue { S = match.Player2Name ?? "Unknown" };
            }

            // Add game IDs if they've changed
            if (match.GameIds.Any())
            {
                updateExpression += ", gameIds = :gameIds";
                expressionAttributeValues[":gameIds"] = new AttributeValue
                {
                    L = match.GameIds.Select(id => new AttributeValue { S = id }).ToList()
                };
            }

            // Add completion data if match is complete
            if (match.Status == "Completed" && match.CompletedAt.HasValue)
            {
                updateExpression += ", completedAt = :completedAt, winnerId = :winnerId, durationSeconds = :duration";
                expressionAttributeValues[":completedAt"] = new AttributeValue { S = match.CompletedAt.Value.ToString("O") };
                expressionAttributeValues[":winnerId"] = string.IsNullOrEmpty(match.WinnerId)
                    ? new AttributeValue { NULL = true }
                    : new AttributeValue { S = match.WinnerId };
                expressionAttributeValues[":duration"] = new AttributeValue { N = match.DurationSeconds.ToString() };
            }

            // Update match
            await _dynamoDbClient.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["PK"] = new AttributeValue { S = $"MATCH#{match.MatchId}" },
                    ["SK"] = new AttributeValue { S = "METADATA" }
                },
                UpdateExpression = updateExpression,
                ExpressionAttributeNames = expressionAttributeNames,
                ExpressionAttributeValues = expressionAttributeValues
            });

            // Update player-match index items
            var reversedTimestamp = (DateTime.MaxValue.Ticks - match.CreatedAt.Ticks).ToString("D19");

            var playerIndexUpdateTasks = new List<Task>
            {
                UpdatePlayerMatchIndex(match.Player1Id, match.MatchId, reversedTimestamp, match.Status)
            };

            // Only update Player2 index if Player2Id exists
            if (!string.IsNullOrEmpty(match.Player2Id))
            {
                playerIndexUpdateTasks.Add(
                    UpdatePlayerMatchIndex(match.Player2Id, match.MatchId, reversedTimestamp, match.Status));
            }

            await Task.WhenAll(playerIndexUpdateTasks);

            _logger.LogInformation(
                "Updated match {MatchId} - P1: {P1Score}, P2: {P2Score}, Status: {Status}",
                match.MatchId,
                match.Player1Score,
                match.Player2Score,
                match.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update match {MatchId}", match.MatchId);
            throw;
        }
    }

    public async Task<List<Match>> GetPlayerMatchesAsync(string playerId, string? status = null, int limit = 50, int skip = 0)
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
                    [":skPrefix"] = new AttributeValue { S = "MATCH#" }
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

            var playerMatchItems = new List<Dictionary<string, AttributeValue>>();
            Dictionary<string, AttributeValue>? lastKey = null;
            var itemsProcessed = 0;

            // Query player-match index items with pagination
            while (playerMatchItems.Count < limit)
            {
                if (lastKey != null)
                {
                    request.ExclusiveStartKey = lastKey;
                }

                var response = await _dynamoDbClient.QueryAsync(request);

                foreach (var item in response.Items)
                {
                    if (itemsProcessed < skip)
                    {
                        itemsProcessed++;
                        continue;
                    }

                    playerMatchItems.Add(item);
                    if (playerMatchItems.Count >= limit)
                    {
                        break;
                    }
                }

                if (response.LastEvaluatedKey == null || response.LastEvaluatedKey.Count == 0)
                {
                    break;
                }

                lastKey = response.LastEvaluatedKey;
            }

            // Get full match details
            var matchIds = playerMatchItems.Select(item => item["matchId"].S).ToList();
            var matches = new List<Match>();

            foreach (var matchId in matchIds)
            {
                var match = await GetMatchByIdAsync(matchId);
                if (match != null)
                {
                    matches.Add(match);
                }
            }

            return matches;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve matches for player {PlayerId}", playerId);
            return new List<Match>();
        }
    }

    public async Task<List<Match>> GetActiveMatchesAsync()
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
                    [":pk"] = new AttributeValue { S = "MATCH_STATUS#InProgress" }
                }
            });

            return response.Items.Select(DynamoDbHelpers.UnmarshalMatch).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve active matches");
            return new List<Match>();
        }
    }

    public async Task<List<Match>> GetRecentMatchesAsync(string? status = "Completed", int limit = 20)
    {
        try
        {
            if (string.IsNullOrEmpty(status))
            {
                status = "Completed";
            }

            var response = await _dynamoDbClient.QueryAsync(new QueryRequest
            {
                TableName = _tableName,
                IndexName = "GSI3",
                KeyConditionExpression = "GSI3PK = :pk",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":pk"] = new AttributeValue { S = $"MATCH_STATUS#{status}" }
                },
                ScanIndexForward = false, // Descending order (most recent first)
                Limit = limit
            });

            return response.Items.Select(DynamoDbHelpers.UnmarshalMatch).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve recent matches");
            return new List<Match>();
        }
    }

    public async Task<MatchStats> GetPlayerMatchStatsAsync(string playerId)
    {
        try
        {
            // Get all completed matches for the player
            var matches = await GetPlayerMatchesAsync(playerId, "Completed", limit: 1000);

            var stats = new MatchStats
            {
                PlayerId = playerId,
                TotalMatches = matches.Count
            };

            foreach (var match in matches)
            {
                var isPlayer1 = match.Player1Id == playerId;
                var playerScore = isPlayer1 ? match.Player1Score : match.Player2Score;
                var opponentScore = isPlayer1 ? match.Player2Score : match.Player1Score;

                if (match.WinnerId == playerId)
                {
                    stats.MatchesWon++;
                }
                else
                {
                    stats.MatchesLost++;
                }

                stats.TotalPointsScored += playerScore;
                stats.TotalPointsConceded += opponentScore;
            }

            // Get abandoned matches
            var abandonedMatches = await GetPlayerMatchesAsync(playerId, "Abandoned", limit: 1000);
            stats.MatchesAbandoned = abandonedMatches.Count;

            // Calculate average match length
            if (matches.Any())
            {
                var totalDuration = matches.Sum(m => m.DurationSeconds);
                stats.AverageMatchLength = (double)totalDuration / matches.Count;
            }

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate match stats for player {PlayerId}", playerId);
            return new MatchStats { PlayerId = playerId };
        }
    }

    public async Task<List<Match>> GetOpenLobbiesAsync(int limit = 50, bool? isCorrespondence = null)
    {
        try
        {
            // Build filter expression
            var filterExpression = "isOpenLobby = :isOpen";
            var expressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"] = new AttributeValue { S = "MATCH_STATUS#WaitingForPlayers" },
                [":isOpen"] = new AttributeValue { BOOL = true }
            };

            // Add correspondence filter if specified
            if (isCorrespondence.HasValue)
            {
                filterExpression += " AND isCorrespondence = :isCorrespondence";
                expressionAttributeValues[":isCorrespondence"] = new AttributeValue { BOOL = isCorrespondence.Value };
            }

            // Query GSI3 for WaitingForPlayers status - FAST!
            var response = await _dynamoDbClient.QueryAsync(new QueryRequest
            {
                TableName = _tableName,
                IndexName = "GSI3",
                KeyConditionExpression = "GSI3PK = :pk",
                FilterExpression = filterExpression,
                ExpressionAttributeValues = expressionAttributeValues,
                Limit = limit,
                ScanIndexForward = false // Most recent first
            });

            var lobbies = response.Items.Select(DynamoDbHelpers.UnmarshalMatch).ToList();
            _logger.LogDebug(
                "Retrieved {Count} open lobbies via GSI3 (isCorrespondence: {IsCorrespondence})",
                lobbies.Count,
                isCorrespondence?.ToString() ?? "all");
            return lobbies;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve open lobbies");
            return new List<Match>();
        }
    }

    public async Task DeleteMatchAsync(string matchId)
    {
        try
        {
            // First get the match to know player IDs
            var match = await GetMatchByIdAsync(matchId);
            if (match == null)
            {
                _logger.LogWarning("Cannot delete non-existent match {MatchId}", matchId);
                return;
            }

            var transactItems = new List<TransactWriteItem>
            {
                // Delete the match itself
                new TransactWriteItem
                {
                    Delete = new Delete
                    {
                        TableName = _tableName,
                        Key = new Dictionary<string, AttributeValue>
                        {
                            ["PK"] = new AttributeValue { S = $"MATCH#{matchId}" },
                            ["SK"] = new AttributeValue { S = "METADATA" }
                        }
                    }
                }
            };

            // Delete player-match index items
            var reversedTimestamp = (DateTime.MaxValue.Ticks - match.CreatedAt.Ticks).ToString("D19");

            transactItems.Add(new TransactWriteItem
            {
                Delete = new Delete
                {
                    TableName = _tableName,
                    Key = new Dictionary<string, AttributeValue>
                    {
                        ["PK"] = new AttributeValue { S = $"USER#{match.Player1Id}" },
                        ["SK"] = new AttributeValue { S = $"MATCH#{reversedTimestamp}#{matchId}" }
                    }
                }
            });

            transactItems.Add(new TransactWriteItem
            {
                Delete = new Delete
                {
                    TableName = _tableName,
                    Key = new Dictionary<string, AttributeValue>
                    {
                        ["PK"] = new AttributeValue { S = $"USER#{match.Player2Id}" },
                        ["SK"] = new AttributeValue { S = $"MATCH#{reversedTimestamp}#{matchId}" }
                    }
                }
            });

            await _dynamoDbClient.TransactWriteItemsAsync(new TransactWriteItemsRequest
            {
                TransactItems = transactItems
            });

            _logger.LogInformation("Deleted match {MatchId}", matchId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete match {MatchId}", matchId);
            throw;
        }
    }

    public async Task AddGameToMatchAsync(string matchId, string gameId)
    {
        try
        {
            await _dynamoDbClient.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["PK"] = new AttributeValue { S = $"MATCH#{matchId}" },
                    ["SK"] = new AttributeValue { S = "METADATA" }
                },
                UpdateExpression = "SET currentGameId = :gameId, lastUpdatedAt = :now ADD gameIds :newGameId",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":gameId"] = new AttributeValue { S = gameId },
                    [":now"] = new AttributeValue { S = DateTime.UtcNow.ToString("O") },
                    [":newGameId"] = new AttributeValue { SS = new List<string> { gameId } }
                }
            });

            _logger.LogInformation("Added game {GameId} to match {MatchId}", gameId, matchId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add game {GameId} to match {MatchId}", gameId, matchId);
            throw;
        }
    }

    public async Task UpdateMatchStatusAsync(string matchId, string status)
    {
        try
        {
            var now = DateTime.UtcNow;
            var updateExpression = "SET #status = :status, lastUpdatedAt = :now, GSI3PK = :gsi3pk, GSI3SK = :gsi3sk";
            var expressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":status"] = new AttributeValue { S = status },
                [":now"] = new AttributeValue { S = now.ToString("O") },
                [":gsi3pk"] = new AttributeValue { S = $"MATCH_STATUS#{status}" },
                [":gsi3sk"] = new AttributeValue { S = now.Ticks.ToString("D19") }
            };

            if (status == "Completed")
            {
                updateExpression += ", completedAt = :completedAt";
                expressionAttributeValues[":completedAt"] = new AttributeValue { S = now.ToString("O") };
            }

            await _dynamoDbClient.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["PK"] = new AttributeValue { S = $"MATCH#{matchId}" },
                    ["SK"] = new AttributeValue { S = "METADATA" }
                },
                UpdateExpression = updateExpression,
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    ["#status"] = "status"
                },
                ExpressionAttributeValues = expressionAttributeValues
            });

            _logger.LogInformation("Updated match {MatchId} status to {Status}", matchId, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update match status for {MatchId}", matchId);
            throw;
        }
    }

    public async Task<List<Match>> GetCorrespondenceMatchesForTurnAsync(string playerId)
    {
        try
        {
            // Query GSI4 for matches where it's this player's turn
            var response = await _dynamoDbClient.QueryAsync(new QueryRequest
            {
                TableName = _tableName,
                IndexName = "GSI4",
                KeyConditionExpression = "GSI4PK = :pk",
                FilterExpression = "#status = :inProgress",
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    ["#status"] = "status"
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":pk"] = new AttributeValue { S = $"CORRESPONDENCE_TURN#{playerId}" },
                    [":inProgress"] = new AttributeValue { S = "InProgress" }
                },
                ScanIndexForward = true // Sort by deadline (earliest first)
            });

            var matches = response.Items.Select(DynamoDbHelpers.UnmarshalMatch).ToList();

            // Also get matches where CurrentTurnPlayerId is null (opening roll phase)
            // These won't be in GSI4, so we need to fetch them separately
            var allPlayerMatches = await GetPlayerMatchesAsync(playerId, "InProgress", limit: 100);

            _logger.LogInformation(
                "GetCorrespondenceMatchesForTurnAsync: GetPlayerMatchesAsync returned {Count} InProgress matches for player {PlayerId}",
                allPlayerMatches.Count,
                playerId);

            var openingRollMatches = allPlayerMatches
                .Where(m => m.IsCorrespondence && m.CurrentTurnPlayerId == null)
                .ToList();

            _logger.LogInformation(
                "GetCorrespondenceMatchesForTurnAsync: Found {Count} opening roll matches (CurrentTurnPlayerId=null) for player {PlayerId}",
                openingRollMatches.Count,
                playerId);

            // Log each opening roll match
            foreach (var match in openingRollMatches)
            {
                _logger.LogInformation(
                    "Opening roll match: {MatchId}, Player1={P1}, Player2={P2}, Status={Status}",
                    match.MatchId,
                    match.Player1Id,
                    match.Player2Id,
                    match.Status);
            }

            // Combine both lists
            matches.AddRange(openingRollMatches);

            // Sort by deadline
            matches = matches.OrderBy(m => m.TurnDeadline).ToList();

            _logger.LogInformation(
                "Retrieved {Count} correspondence matches for player {PlayerId}'s turn ({YourTurn} specific turn, {OpeningRoll} opening roll)",
                matches.Count,
                playerId,
                matches.Count - openingRollMatches.Count,
                openingRollMatches.Count);

            return matches;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve correspondence matches for player {PlayerId}'s turn", playerId);
            return new List<Match>();
        }
    }

    public async Task<List<Match>> GetCorrespondenceMatchesWaitingAsync(string playerId)
    {
        try
        {
            // Get all correspondence matches for the player where it's NOT their turn
            var allMatches = await GetPlayerMatchesAsync(playerId, "InProgress", limit: 100);

            _logger.LogInformation(
                "GetCorrespondenceMatchesWaitingAsync: Found {TotalMatches} InProgress matches for player {PlayerId}",
                allMatches.Count,
                playerId);

            // Log each match's correspondence status and current turn
            foreach (var match in allMatches)
            {
                _logger.LogInformation(
                    "Match {MatchId}: IsCorrespondence={IsCorr}, CurrentTurnPlayerId={CurrentTurn}, Player1={P1}, Player2={P2}",
                    match.MatchId,
                    match.IsCorrespondence,
                    match.CurrentTurnPlayerId ?? "null",
                    match.Player1Id,
                    match.Player2Id);
            }

            // Filter to correspondence matches where it's NOT the player's turn
            // Note: CurrentTurnPlayerId == null means opening roll phase (both players need to roll)
            // In this case, we DON'T show it in waiting games - it will be shown in "your turn" games
            var waitingMatches = allMatches
                .Where(m => m.IsCorrespondence
                    && m.CurrentTurnPlayerId != null
                    && m.CurrentTurnPlayerId != playerId)
                .OrderBy(m => m.TurnDeadline)
                .ToList();

            _logger.LogInformation("Retrieved {Count} correspondence matches where player {PlayerId} is waiting", waitingMatches.Count, playerId);
            return waitingMatches;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve correspondence matches where player {PlayerId} is waiting", playerId);
            return new List<Match>();
        }
    }

    public async Task<List<Match>> GetExpiredCorrespondenceMatchesAsync()
    {
        try
        {
            // Query GSI3 for in-progress matches, then filter for correspondence with expired deadlines
            // Note: ISO 8601 datetime strings (format "O") are lexicographically sortable,
            // so string comparison in DynamoDB filter expressions works correctly for UTC times
            var response = await _dynamoDbClient.QueryAsync(new QueryRequest
            {
                TableName = _tableName,
                IndexName = "GSI3",
                KeyConditionExpression = "GSI3PK = :pk",
                FilterExpression = "isCorrespondence = :isCorr AND turnDeadline < :now",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":pk"] = new AttributeValue { S = "MATCH_STATUS#InProgress" },
                    [":isCorr"] = new AttributeValue { BOOL = true },
                    [":now"] = new AttributeValue { S = DateTime.UtcNow.ToString("O") }
                }
            });

            var matches = response.Items.Select(DynamoDbHelpers.UnmarshalMatch).ToList();
            _logger.LogDebug("Retrieved {Count} expired correspondence matches", matches.Count);
            return matches;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve expired correspondence matches");
            return new List<Match>();
        }
    }

    public async Task UpdateCorrespondenceTurnAsync(string matchId, string currentTurnPlayerId, DateTime turnDeadline)
    {
        try
        {
            var now = DateTime.UtcNow;

            await _dynamoDbClient.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["PK"] = new AttributeValue { S = $"MATCH#{matchId}" },
                    ["SK"] = new AttributeValue { S = "METADATA" }
                },
                UpdateExpression = @"
                    SET currentTurnPlayerId = :turnPlayer,
                        turnDeadline = :deadline,
                        lastUpdatedAt = :now,
                        GSI4PK = :gsi4pk,
                        GSI4SK = :gsi4sk",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":turnPlayer"] = new AttributeValue { S = currentTurnPlayerId },
                    [":deadline"] = new AttributeValue { S = turnDeadline.ToString("O") },
                    [":now"] = new AttributeValue { S = now.ToString("O") },
                    [":gsi4pk"] = new AttributeValue { S = $"CORRESPONDENCE_TURN#{currentTurnPlayerId}" },
                    [":gsi4sk"] = new AttributeValue { S = turnDeadline.Ticks.ToString("D19") }
                }
            });

            _logger.LogInformation(
                "Updated correspondence turn for match {MatchId} - Turn: {PlayerId}, Deadline: {Deadline}",
                matchId,
                currentTurnPlayerId,
                turnDeadline);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update correspondence turn for match {MatchId}", matchId);
            throw;
        }
    }

    public async Task CreatePlayerMatchIndexAsync(string playerId, string matchId, string opponentId, string status, DateTime createdAt)
    {
        var playerMatchItem = DynamoDbHelpers.CreatePlayerMatchIndexItem(
            playerId,
            matchId,
            opponentId,
            status,
            createdAt);

        await _dynamoDbClient.PutItemAsync(new PutItemRequest
        {
            TableName = _tableName,
            Item = playerMatchItem
        });

        _logger.LogInformation(
            "Created player-match index for player {PlayerId}, match {MatchId}",
            playerId,
            matchId);
    }

    private async Task UpdatePlayerMatchIndex(string playerId, string matchId, string reversedTimestamp, string status)
    {
        await _dynamoDbClient.UpdateItemAsync(new UpdateItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["PK"] = new AttributeValue { S = $"USER#{playerId}" },
                ["SK"] = new AttributeValue { S = $"MATCH#{reversedTimestamp}#{matchId}" }
            },
            UpdateExpression = "SET #status = :status",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                ["#status"] = "status"
            },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":status"] = new AttributeValue { S = status }
            }
        });
    }
}
