using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Backgammon.Server.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Backgammon.Server.Services.DynamoDb;

/// <summary>
/// DynamoDB implementation of the puzzle repository.
/// Uses single-table design with:
/// - Puzzles: PK=PUZZLE#{date}, SK=METADATA
/// - Attempts: PK=USER#{userId}, SK=PUZZLE_ATTEMPT#{date}
/// - Streaks: PK=USER#{userId}, SK=PUZZLE_STREAK
/// </summary>
public class DynamoDbPuzzleRepository : IPuzzleRepository
{
    private readonly IAmazonDynamoDB _dynamoDbClient;
    private readonly string _tableName;
    private readonly ILogger<DynamoDbPuzzleRepository> _logger;

    public DynamoDbPuzzleRepository(
        IAmazonDynamoDB dynamoDbClient,
        IConfiguration configuration,
        ILogger<DynamoDbPuzzleRepository> logger)
    {
        _dynamoDbClient = dynamoDbClient;
        _tableName = configuration["DynamoDb:TableName"] ?? "backgammon-local";
        _logger = logger;
    }

    public async Task SavePuzzleAsync(DailyPuzzle puzzle)
    {
        try
        {
            var item = MarshalPuzzle(puzzle);

            await _dynamoDbClient.PutItemAsync(new PutItemRequest
            {
                TableName = _tableName,
                Item = item,
                ConditionExpression = "attribute_not_exists(PK)"
            });

            _logger.LogInformation("Created puzzle for date {PuzzleDate}", puzzle.PuzzleDate);
        }
        catch (ConditionalCheckFailedException)
        {
            _logger.LogWarning("Puzzle for date {PuzzleDate} already exists", puzzle.PuzzleDate);
            throw new InvalidOperationException($"Puzzle for {puzzle.PuzzleDate} already exists");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save puzzle for date {PuzzleDate}", puzzle.PuzzleDate);
            throw;
        }
    }

    public async Task<DailyPuzzle?> GetPuzzleByDateAsync(string date)
    {
        try
        {
            var response = await _dynamoDbClient.GetItemAsync(new GetItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["PK"] = new AttributeValue { S = $"PUZZLE#{date}" },
                    ["SK"] = new AttributeValue { S = "METADATA" }
                }
            });

            if (!response.IsItemSet)
            {
                return null;
            }

            return UnmarshalPuzzle(response.Item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get puzzle for date {Date}", date);
            return null;
        }
    }

    public async Task<bool> PuzzleExistsAsync(string date)
    {
        try
        {
            var response = await _dynamoDbClient.GetItemAsync(new GetItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["PK"] = new AttributeValue { S = $"PUZZLE#{date}" },
                    ["SK"] = new AttributeValue { S = "METADATA" }
                },
                ProjectionExpression = "PK"
            });

            return response.IsItemSet;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if puzzle exists for date {Date}", date);
            return false;
        }
    }

    public async Task IncrementSolvedCountAsync(string puzzleDate)
    {
        try
        {
            await _dynamoDbClient.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["PK"] = new AttributeValue { S = $"PUZZLE#{puzzleDate}" },
                    ["SK"] = new AttributeValue { S = "METADATA" }
                },
                UpdateExpression = "SET solvedCount = if_not_exists(solvedCount, :zero) + :one",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":zero"] = new AttributeValue { N = "0" },
                    [":one"] = new AttributeValue { N = "1" }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to increment solved count for puzzle {PuzzleDate}", puzzleDate);
            throw;
        }
    }

    public async Task IncrementAttemptCountAsync(string puzzleDate)
    {
        try
        {
            await _dynamoDbClient.UpdateItemAsync(new UpdateItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["PK"] = new AttributeValue { S = $"PUZZLE#{puzzleDate}" },
                    ["SK"] = new AttributeValue { S = "METADATA" }
                },
                UpdateExpression = "SET attemptCount = if_not_exists(attemptCount, :zero) + :one",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":zero"] = new AttributeValue { N = "0" },
                    [":one"] = new AttributeValue { N = "1" }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to increment attempt count for puzzle {PuzzleDate}", puzzleDate);
            throw;
        }
    }

    public async Task SaveAttemptAsync(PuzzleAttempt attempt)
    {
        try
        {
            var item = MarshalAttempt(attempt);

            await _dynamoDbClient.PutItemAsync(new PutItemRequest
            {
                TableName = _tableName,
                Item = item
            });

            _logger.LogDebug(
                "Saved attempt for user {UserId} on puzzle {PuzzleDate}",
                attempt.UserId,
                attempt.PuzzleDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to save attempt for user {UserId} on puzzle {PuzzleDate}",
                attempt.UserId,
                attempt.PuzzleDate);
            throw;
        }
    }

    public async Task<PuzzleAttempt?> GetAttemptAsync(string userId, string puzzleDate)
    {
        try
        {
            var response = await _dynamoDbClient.GetItemAsync(new GetItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["PK"] = new AttributeValue { S = $"USER#{userId}" },
                    ["SK"] = new AttributeValue { S = $"PUZZLE_ATTEMPT#{puzzleDate}" }
                }
            });

            if (!response.IsItemSet)
            {
                return null;
            }

            return UnmarshalAttempt(response.Item);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to get attempt for user {UserId} on puzzle {PuzzleDate}",
                userId,
                puzzleDate);
            return null;
        }
    }

    public async Task UpdateAttemptAsync(PuzzleAttempt attempt)
    {
        // For simplicity, just overwrite the entire item
        await SaveAttemptAsync(attempt);
    }

    public async Task<PuzzleStreakInfo?> GetStreakInfoAsync(string userId)
    {
        try
        {
            var response = await _dynamoDbClient.GetItemAsync(new GetItemRequest
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    ["PK"] = new AttributeValue { S = $"USER#{userId}" },
                    ["SK"] = new AttributeValue { S = "PUZZLE_STREAK" }
                }
            });

            if (!response.IsItemSet)
            {
                return null;
            }

            return UnmarshalStreakInfo(response.Item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get streak info for user {UserId}", userId);
            return null;
        }
    }

    public async Task SaveStreakInfoAsync(PuzzleStreakInfo streakInfo)
    {
        try
        {
            var item = MarshalStreakInfo(streakInfo);

            await _dynamoDbClient.PutItemAsync(new PutItemRequest
            {
                TableName = _tableName,
                Item = item
            });

            _logger.LogDebug(
                "Saved streak info for user {UserId}: streak={Streak}",
                streakInfo.UserId,
                streakInfo.CurrentStreak);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save streak info for user {UserId}", streakInfo.UserId);
            throw;
        }
    }

    private static Dictionary<string, AttributeValue> MarshalPuzzle(DailyPuzzle puzzle)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new AttributeValue { S = $"PUZZLE#{puzzle.PuzzleDate}" },
            ["SK"] = new AttributeValue { S = "METADATA" },
            ["puzzleId"] = new AttributeValue { S = puzzle.PuzzleId },
            ["puzzleDate"] = new AttributeValue { S = puzzle.PuzzleDate },
            ["positionSgf"] = new AttributeValue { S = puzzle.PositionSgf },
            ["currentPlayer"] = new AttributeValue { S = puzzle.CurrentPlayer },
            ["dice"] = new AttributeValue
            {
                L = puzzle.Dice.Select(d => new AttributeValue { N = d.ToString() }).ToList()
            },
            ["boardState"] = MarshalBoardState(puzzle.BoardState),
            ["whiteCheckersOnBar"] = new AttributeValue { N = puzzle.WhiteCheckersOnBar.ToString() },
            ["redCheckersOnBar"] = new AttributeValue { N = puzzle.RedCheckersOnBar.ToString() },
            ["whiteBornOff"] = new AttributeValue { N = puzzle.WhiteBornOff.ToString() },
            ["redBornOff"] = new AttributeValue { N = puzzle.RedBornOff.ToString() },
            ["bestMoves"] = MarshalMoves(puzzle.BestMoves),
            ["bestMovesNotation"] = new AttributeValue { S = puzzle.BestMovesNotation },
            ["bestMoveEquity"] = new AttributeValue { N = puzzle.BestMoveEquity.ToString() },
            ["alternativeMoves"] = MarshalAlternativeMoves(puzzle.AlternativeMoves),
            ["evaluatorType"] = new AttributeValue { S = puzzle.EvaluatorType },
            ["createdAt"] = new AttributeValue { S = puzzle.CreatedAt.ToString("O") },
            ["solvedCount"] = new AttributeValue { N = puzzle.SolvedCount.ToString() },
            ["attemptCount"] = new AttributeValue { N = puzzle.AttemptCount.ToString() },
            ["entityType"] = new AttributeValue { S = "PUZZLE" }
        };

        return item;
    }

    private static DailyPuzzle UnmarshalPuzzle(Dictionary<string, AttributeValue> item)
    {
        return new DailyPuzzle
        {
            PuzzleId = DynamoDbHelpers.GetStringOrNull(item, "puzzleId") ?? string.Empty,
            PuzzleDate = DynamoDbHelpers.GetStringOrNull(item, "puzzleDate") ?? string.Empty,
            PositionSgf = DynamoDbHelpers.GetStringOrNull(item, "positionSgf") ?? string.Empty,
            CurrentPlayer = DynamoDbHelpers.GetStringOrNull(item, "currentPlayer") ?? "White",
            Dice = UnmarshalDice(item),
            BoardState = UnmarshalBoardState(item),
            WhiteCheckersOnBar = DynamoDbHelpers.GetInt(item, "whiteCheckersOnBar"),
            RedCheckersOnBar = DynamoDbHelpers.GetInt(item, "redCheckersOnBar"),
            WhiteBornOff = DynamoDbHelpers.GetInt(item, "whiteBornOff"),
            RedBornOff = DynamoDbHelpers.GetInt(item, "redBornOff"),
            BestMoves = UnmarshalMoves(item, "bestMoves"),
            BestMovesNotation = DynamoDbHelpers.GetStringOrNull(item, "bestMovesNotation") ?? string.Empty,
            BestMoveEquity = GetDouble(item, "bestMoveEquity"),
            AlternativeMoves = UnmarshalAlternativeMoves(item),
            EvaluatorType = DynamoDbHelpers.GetStringOrNull(item, "evaluatorType") ?? string.Empty,
            CreatedAt = DynamoDbHelpers.GetDateTime(item, "createdAt"),
            SolvedCount = DynamoDbHelpers.GetInt(item, "solvedCount"),
            AttemptCount = DynamoDbHelpers.GetInt(item, "attemptCount")
        };
    }

    private static Dictionary<string, AttributeValue> MarshalAttempt(PuzzleAttempt attempt)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new AttributeValue { S = $"USER#{attempt.UserId}" },
            ["SK"] = new AttributeValue { S = $"PUZZLE_ATTEMPT#{attempt.PuzzleDate}" },
            ["userId"] = new AttributeValue { S = attempt.UserId },
            ["puzzleId"] = new AttributeValue { S = attempt.PuzzleId },
            ["puzzleDate"] = new AttributeValue { S = attempt.PuzzleDate },
            ["submittedMoves"] = MarshalMoves(attempt.SubmittedMoves),
            ["submittedNotation"] = new AttributeValue { S = attempt.SubmittedNotation },
            ["isCorrect"] = new AttributeValue { BOOL = attempt.IsCorrect },
            ["equityLoss"] = new AttributeValue { N = attempt.EquityLoss.ToString() },
            ["attemptCount"] = new AttributeValue { N = attempt.AttemptCount.ToString() },
            ["createdAt"] = new AttributeValue { S = attempt.CreatedAt.ToString("O") },
            ["entityType"] = new AttributeValue { S = "PUZZLE_ATTEMPT" }
        };

        if (attempt.SolvedAt.HasValue)
        {
            item["solvedAt"] = new AttributeValue { S = attempt.SolvedAt.Value.ToString("O") };
        }

        return item;
    }

    private static PuzzleAttempt UnmarshalAttempt(Dictionary<string, AttributeValue> item)
    {
        return new PuzzleAttempt
        {
            UserId = DynamoDbHelpers.GetStringOrNull(item, "userId") ?? string.Empty,
            PuzzleId = DynamoDbHelpers.GetStringOrNull(item, "puzzleId") ?? string.Empty,
            PuzzleDate = DynamoDbHelpers.GetStringOrNull(item, "puzzleDate") ?? string.Empty,
            SubmittedMoves = UnmarshalMoves(item, "submittedMoves"),
            SubmittedNotation = DynamoDbHelpers.GetStringOrNull(item, "submittedNotation") ?? string.Empty,
            IsCorrect = DynamoDbHelpers.GetBool(item, "isCorrect"),
            EquityLoss = GetDouble(item, "equityLoss"),
            AttemptCount = DynamoDbHelpers.GetInt(item, "attemptCount"),
            CreatedAt = DynamoDbHelpers.GetDateTime(item, "createdAt"),
            SolvedAt = DynamoDbHelpers.GetNullableDateTime(item, "solvedAt")
        };
    }

    private static Dictionary<string, AttributeValue> MarshalStreakInfo(PuzzleStreakInfo info)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new AttributeValue { S = $"USER#{info.UserId}" },
            ["SK"] = new AttributeValue { S = "PUZZLE_STREAK" },
            ["userId"] = new AttributeValue { S = info.UserId },
            ["currentStreak"] = new AttributeValue { N = info.CurrentStreak.ToString() },
            ["bestStreak"] = new AttributeValue { N = info.BestStreak.ToString() },
            ["totalSolved"] = new AttributeValue { N = info.TotalSolved.ToString() },
            ["totalAttempts"] = new AttributeValue { N = info.TotalAttempts.ToString() },
            ["entityType"] = new AttributeValue { S = "PUZZLE_STREAK" }
        };

        if (!string.IsNullOrEmpty(info.LastSolvedDate))
        {
            item["lastSolvedDate"] = new AttributeValue { S = info.LastSolvedDate };
        }

        return item;
    }

    private static PuzzleStreakInfo UnmarshalStreakInfo(Dictionary<string, AttributeValue> item)
    {
        return new PuzzleStreakInfo
        {
            UserId = DynamoDbHelpers.GetStringOrNull(item, "userId") ?? string.Empty,
            CurrentStreak = DynamoDbHelpers.GetInt(item, "currentStreak"),
            BestStreak = DynamoDbHelpers.GetInt(item, "bestStreak"),
            LastSolvedDate = DynamoDbHelpers.GetStringOrNull(item, "lastSolvedDate"),
            TotalSolved = DynamoDbHelpers.GetInt(item, "totalSolved"),
            TotalAttempts = DynamoDbHelpers.GetInt(item, "totalAttempts")
        };
    }

    private static AttributeValue MarshalBoardState(List<PointStateDto> boardState)
    {
        return new AttributeValue
        {
            L = boardState.Select(p => new AttributeValue
            {
                M = new Dictionary<string, AttributeValue>
                {
                    ["position"] = new AttributeValue { N = p.Position.ToString() },
                    ["color"] = p.Color != null ? new AttributeValue { S = p.Color } : new AttributeValue { NULL = true },
                    ["count"] = new AttributeValue { N = p.Count.ToString() }
                }
            }).ToList()
        };
    }

    private static List<PointStateDto> UnmarshalBoardState(Dictionary<string, AttributeValue> item)
    {
        if (!item.TryGetValue("boardState", out var value) || value.L == null)
        {
            return new List<PointStateDto>();
        }

        return value.L.Select(p => new PointStateDto
        {
            Position = int.Parse(p.M["position"].N),
            Color = p.M.TryGetValue("color", out var c) && c.NULL != true ? c.S : null,
            Count = int.Parse(p.M["count"].N)
        }).ToList();
    }

    private static AttributeValue MarshalMoves(List<MoveDto> moves)
    {
        return new AttributeValue
        {
            L = moves.Select(m => new AttributeValue
            {
                M = new Dictionary<string, AttributeValue>
                {
                    ["from"] = new AttributeValue { N = m.From.ToString() },
                    ["to"] = new AttributeValue { N = m.To.ToString() },
                    ["dieValue"] = new AttributeValue { N = m.DieValue.ToString() },
                    ["isHit"] = new AttributeValue { BOOL = m.IsHit }
                }
            }).ToList()
        };
    }

    private static List<MoveDto> UnmarshalMoves(Dictionary<string, AttributeValue> item, string key)
    {
        if (!item.TryGetValue(key, out var value) || value.L == null)
        {
            return new List<MoveDto>();
        }

        return value.L.Select(m => new MoveDto
        {
            From = int.Parse(m.M["from"].N),
            To = int.Parse(m.M["to"].N),
            DieValue = int.Parse(m.M["dieValue"].N),
            IsHit = m.M.TryGetValue("isHit", out var h) && h.BOOL == true
        }).ToList();
    }

    private static AttributeValue MarshalAlternativeMoves(List<AlternativeMove> alternatives)
    {
        return new AttributeValue
        {
            L = alternatives.Select(a => new AttributeValue
            {
                M = new Dictionary<string, AttributeValue>
                {
                    ["moves"] = MarshalMoves(a.Moves),
                    ["notation"] = new AttributeValue { S = a.Notation },
                    ["equity"] = new AttributeValue { N = a.Equity.ToString() },
                    ["equityLoss"] = new AttributeValue { N = a.EquityLoss.ToString() }
                }
            }).ToList()
        };
    }

    private static List<AlternativeMove> UnmarshalAlternativeMoves(Dictionary<string, AttributeValue> item)
    {
        if (!item.TryGetValue("alternativeMoves", out var value) || value.L == null)
        {
            return new List<AlternativeMove>();
        }

        return value.L.Select(a => new AlternativeMove
        {
            Moves = UnmarshalMoves(a.M, "moves"),
            Notation = a.M.TryGetValue("notation", out var n) ? n.S : string.Empty,
            Equity = a.M.TryGetValue("equity", out var e) && double.TryParse(e.N, out var eq) ? eq : 0,
            EquityLoss = a.M.TryGetValue("equityLoss", out var el) && double.TryParse(el.N, out var loss) ? loss : 0
        }).ToList();
    }

    private static int[] UnmarshalDice(Dictionary<string, AttributeValue> item)
    {
        if (!item.TryGetValue("dice", out var value) || value.L == null || value.L.Count < 2)
        {
            return new[] { 0, 0 };
        }

        return value.L.Select(d => int.Parse(d.N)).ToArray();
    }

    private static double GetDouble(Dictionary<string, AttributeValue> item, string key)
    {
        if (item.TryGetValue(key, out var value) && double.TryParse(value.N, out var result))
        {
            return result;
        }

        return 0;
    }
}
