using Amazon.DynamoDBv2.Model;
using Backgammon.Server.Models;

namespace Backgammon.Server.Services.DynamoDb;

public static class DynamoDbHelpers
{
    // Helper to get string attribute or null
    public static string? GetStringOrNull(Dictionary<string, AttributeValue> item, string key)
    {
        return item.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value.S) ? value.S : null;
    }

    // Helper to get DateTime
    public static DateTime GetDateTime(Dictionary<string, AttributeValue> item, string key)
    {
        if (item.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value.S))
        {
            return DateTime.Parse(value.S).ToUniversalTime();
        }
        return DateTime.UtcNow;
    }

    // Helper to get nullable DateTime
    public static DateTime? GetNullableDateTime(Dictionary<string, AttributeValue> item, string key)
    {
        if (item.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value.S))
        {
            return DateTime.Parse(value.S).ToUniversalTime();
        }
        return null;
    }

    // Helper to get int
    public static int GetInt(Dictionary<string, AttributeValue> item, string key, int defaultValue = 0)
    {
        if (item.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value.N))
        {
            return int.Parse(value.N);
        }
        return defaultValue;
    }

    // Helper to get bool
    public static bool GetBool(Dictionary<string, AttributeValue> item, string key, bool defaultValue = false)
    {
        if (item.TryGetValue(key, out var value) && value.BOOL.HasValue)
        {
            return value.BOOL.Value;
        }
        return defaultValue;
    }

    // Helper to get string list
    public static List<string> GetStringList(Dictionary<string, AttributeValue> item, string key)
    {
        if (item.TryGetValue(key, out var value) && value.L != null)
        {
            return value.L.Select(v => v.S).ToList();
        }
        return new List<string>();
    }

    // Marshal UserStats to AttributeValue
    public static AttributeValue MarshalUserStats(UserStats stats)
    {
        return new AttributeValue
        {
            M = new Dictionary<string, AttributeValue>
            {
                ["totalGames"] = new AttributeValue { N = stats.TotalGames.ToString() },
                ["wins"] = new AttributeValue { N = stats.Wins.ToString() },
                ["losses"] = new AttributeValue { N = stats.Losses.ToString() },
                ["totalStakes"] = new AttributeValue { N = stats.TotalStakes.ToString() },
                ["normalWins"] = new AttributeValue { N = stats.NormalWins.ToString() },
                ["gammonWins"] = new AttributeValue { N = stats.GammonWins.ToString() },
                ["backgammonWins"] = new AttributeValue { N = stats.BackgammonWins.ToString() },
                ["winStreak"] = new AttributeValue { N = stats.WinStreak.ToString() },
                ["bestWinStreak"] = new AttributeValue { N = stats.BestWinStreak.ToString() }
            }
        };
    }

    // Unmarshal UserStats from AttributeValue
    public static UserStats UnmarshalUserStats(Dictionary<string, AttributeValue> statsMap)
    {
        return new UserStats
        {
            TotalGames = GetInt(statsMap, "totalGames"),
            Wins = GetInt(statsMap, "wins"),
            Losses = GetInt(statsMap, "losses"),
            TotalStakes = GetInt(statsMap, "totalStakes"),
            NormalWins = GetInt(statsMap, "normalWins"),
            GammonWins = GetInt(statsMap, "gammonWins"),
            BackgammonWins = GetInt(statsMap, "backgammonWins"),
            WinStreak = GetInt(statsMap, "winStreak"),
            BestWinStreak = GetInt(statsMap, "bestWinStreak")
        };
    }

    // Marshal User to DynamoDB item
    public static Dictionary<string, AttributeValue> MarshalUser(User user)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new AttributeValue { S = $"USER#{user.UserId}" },
            ["SK"] = new AttributeValue { S = "PROFILE" },
            ["userId"] = new AttributeValue { S = user.UserId },
            ["username"] = new AttributeValue { S = user.Username },
            ["usernameNormalized"] = new AttributeValue { S = user.UsernameNormalized },
            ["displayName"] = new AttributeValue { S = user.DisplayName },
            ["passwordHash"] = new AttributeValue { S = user.PasswordHash },
            ["createdAt"] = new AttributeValue { S = user.CreatedAt.ToString("O") },
            ["lastLoginAt"] = new AttributeValue { S = user.LastLoginAt.ToString("O") },
            ["lastSeenAt"] = new AttributeValue { S = user.LastSeenAt.ToString("O") },
            ["stats"] = MarshalUserStats(user.Stats),
            ["isActive"] = new AttributeValue { BOOL = user.IsActive },
            ["isBanned"] = new AttributeValue { BOOL = user.IsBanned },
            ["GSI1PK"] = new AttributeValue { S = $"USERNAME#{user.UsernameNormalized}" },
            ["GSI1SK"] = new AttributeValue { S = "PROFILE" },
            ["entityType"] = new AttributeValue { S = "USER" }
        };

        // Optional email
        if (!string.IsNullOrEmpty(user.Email))
        {
            item["email"] = new AttributeValue { S = user.Email };
            item["emailNormalized"] = new AttributeValue { S = user.EmailNormalized ?? user.Email.ToLowerInvariant() };
            item["GSI2PK"] = new AttributeValue { S = $"EMAIL#{user.EmailNormalized ?? user.Email.ToLowerInvariant()}" };
            item["GSI2SK"] = new AttributeValue { S = "PROFILE" };
        }

        // Linked anonymous IDs
        if (user.LinkedAnonymousIds.Any())
        {
            item["linkedAnonymousIds"] = new AttributeValue { L = user.LinkedAnonymousIds.Select(id => new AttributeValue { S = id }).ToList() };
        }

        // Ban information
        if (!string.IsNullOrEmpty(user.BannedReason))
        {
            item["bannedReason"] = new AttributeValue { S = user.BannedReason };
        }

        if (user.BannedUntil.HasValue)
        {
            item["bannedUntil"] = new AttributeValue { S = user.BannedUntil.Value.ToString("O") };
        }

        return item;
    }

    // Unmarshal User from DynamoDB item
    public static User UnmarshalUser(Dictionary<string, AttributeValue> item)
    {
        var user = new User
        {
            UserId = item["userId"].S,
            Id = item["userId"].S,  // DynamoDB uses userId as id
            Username = item["username"].S,
            UsernameNormalized = item["usernameNormalized"].S,
            DisplayName = item["displayName"].S,
            PasswordHash = item["passwordHash"].S,
            CreatedAt = GetDateTime(item, "createdAt"),
            LastLoginAt = GetDateTime(item, "lastLoginAt"),
            LastSeenAt = GetDateTime(item, "lastSeenAt"),
            IsActive = GetBool(item, "isActive", true),
            IsBanned = GetBool(item, "isBanned", false),
            Email = GetStringOrNull(item, "email"),
            EmailNormalized = GetStringOrNull(item, "emailNormalized"),
            BannedReason = GetStringOrNull(item, "bannedReason"),
            BannedUntil = GetNullableDateTime(item, "bannedUntil"),
            LinkedAnonymousIds = GetStringList(item, "linkedAnonymousIds")
        };

        // Unmarshal stats
        if (item.TryGetValue("stats", out var statsValue) && statsValue.M != null)
        {
            user.Stats = UnmarshalUserStats(statsValue.M);
        }

        return user;
    }

    // Marshal PointStateDto list to AttributeValue
    public static AttributeValue MarshalBoardState(List<PointStateDto> boardState)
    {
        return new AttributeValue
        {
            L = boardState.Select(point => new AttributeValue
            {
                M = new Dictionary<string, AttributeValue>
                {
                    ["position"] = new AttributeValue { N = point.Position.ToString() },
                    ["color"] = string.IsNullOrEmpty(point.Color) ? new AttributeValue { NULL = true } : new AttributeValue { S = point.Color },
                    ["count"] = new AttributeValue { N = point.Count.ToString() }
                }
            }).ToList()
        };
    }

    // Unmarshal PointStateDto list from AttributeValue
    public static List<PointStateDto> UnmarshalBoardState(List<AttributeValue> boardStateList)
    {
        return boardStateList.Select(pointValue =>
        {
            var pointMap = pointValue.M;
            return new PointStateDto
            {
                Position = GetInt(pointMap, "position"),
                Color = GetStringOrNull(pointMap, "color"),
                Count = GetInt(pointMap, "count")
            };
        }).ToList();
    }

    // Marshal Game to DynamoDB item
    public static Dictionary<string, AttributeValue> MarshalGame(Game game)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new AttributeValue { S = $"GAME#{game.GameId}" },
            ["SK"] = new AttributeValue { S = "METADATA" },
            ["gameId"] = new AttributeValue { S = game.GameId },
            ["status"] = new AttributeValue { S = game.Status },
            ["gameStarted"] = new AttributeValue { BOOL = game.GameStarted },
            ["boardState"] = MarshalBoardState(game.BoardState),
            ["whiteCheckersOnBar"] = new AttributeValue { N = game.WhiteCheckersOnBar.ToString() },
            ["redCheckersOnBar"] = new AttributeValue { N = game.RedCheckersOnBar.ToString() },
            ["whiteBornOff"] = new AttributeValue { N = game.WhiteBornOff.ToString() },
            ["redBornOff"] = new AttributeValue { N = game.RedBornOff.ToString() },
            ["currentPlayer"] = new AttributeValue { S = game.CurrentPlayer },
            ["die1"] = new AttributeValue { N = game.Die1.ToString() },
            ["die2"] = new AttributeValue { N = game.Die2.ToString() },
            ["doublingCubeValue"] = new AttributeValue { N = game.DoublingCubeValue.ToString() },
            ["stakes"] = new AttributeValue { N = game.Stakes.ToString() },
            ["moveCount"] = new AttributeValue { N = game.MoveCount.ToString() },
            ["createdAt"] = new AttributeValue { S = game.CreatedAt.ToString("O") },
            ["lastUpdatedAt"] = new AttributeValue { S = game.LastUpdatedAt.ToString("O") },
            ["entityType"] = new AttributeValue { S = "GAME" }
        };

        // GSI3 for game status queries
        item["GSI3PK"] = new AttributeValue { S = $"GAME_STATUS#{game.Status}" };
        if (game.Status == "Completed" && game.CompletedAt.HasValue)
        {
            item["GSI3SK"] = new AttributeValue { S = game.CompletedAt.Value.Ticks.ToString("D19") };
        }
        else
        {
            item["GSI3SK"] = new AttributeValue { S = game.LastUpdatedAt.Ticks.ToString("D19") };
        }

        // Player IDs
        if (!string.IsNullOrEmpty(game.WhitePlayerId))
            item["whitePlayerId"] = new AttributeValue { S = game.WhitePlayerId };
        if (!string.IsNullOrEmpty(game.RedPlayerId))
            item["redPlayerId"] = new AttributeValue { S = game.RedPlayerId };
        if (!string.IsNullOrEmpty(game.WhiteUserId))
            item["whiteUserId"] = new AttributeValue { S = game.WhiteUserId };
        if (!string.IsNullOrEmpty(game.RedUserId))
            item["redUserId"] = new AttributeValue { S = game.RedUserId };
        if (!string.IsNullOrEmpty(game.WhitePlayerName))
            item["whitePlayerName"] = new AttributeValue { S = game.WhitePlayerName };
        if (!string.IsNullOrEmpty(game.RedPlayerName))
            item["redPlayerName"] = new AttributeValue { S = game.RedPlayerName };

        // Remaining moves
        if (game.RemainingMoves.Any())
        {
            item["remainingMoves"] = new AttributeValue { L = game.RemainingMoves.Select(m => new AttributeValue { N = m.ToString() }).ToList() };
        }

        // Doubling cube owner
        if (!string.IsNullOrEmpty(game.DoublingCubeOwner))
        {
            item["doublingCubeOwner"] = new AttributeValue { S = game.DoublingCubeOwner };
        }

        // Move history
        if (game.Moves.Any())
        {
            item["moves"] = new AttributeValue { L = game.Moves.Select(m => new AttributeValue { S = m }).ToList() };
        }

        // Winner
        if (!string.IsNullOrEmpty(game.Winner))
        {
            item["winner"] = new AttributeValue { S = game.Winner };
        }

        // Completed at
        if (game.CompletedAt.HasValue)
        {
            item["completedAt"] = new AttributeValue { S = game.CompletedAt.Value.ToString("O") };
        }

        // Duration
        if (game.DurationSeconds > 0)
        {
            item["durationSeconds"] = new AttributeValue { N = game.DurationSeconds.ToString() };
        }

        return item;
    }

    // Unmarshal Game from DynamoDB item
    public static Game UnmarshalGame(Dictionary<string, AttributeValue> item)
    {
        var game = new Game
        {
            GameId = item["gameId"].S,
            Id = item["gameId"].S,
            Status = item["status"].S,
            GameStarted = GetBool(item, "gameStarted"),
            WhiteCheckersOnBar = GetInt(item, "whiteCheckersOnBar"),
            RedCheckersOnBar = GetInt(item, "redCheckersOnBar"),
            WhiteBornOff = GetInt(item, "whiteBornOff"),
            RedBornOff = GetInt(item, "redBornOff"),
            CurrentPlayer = item["currentPlayer"].S,
            Die1 = GetInt(item, "die1"),
            Die2 = GetInt(item, "die2"),
            DoublingCubeValue = GetInt(item, "doublingCubeValue", 1),
            Stakes = GetInt(item, "stakes"),
            MoveCount = GetInt(item, "moveCount"),
            CreatedAt = GetDateTime(item, "createdAt"),
            LastUpdatedAt = GetDateTime(item, "lastUpdatedAt"),
            WhitePlayerId = GetStringOrNull(item, "whitePlayerId"),
            RedPlayerId = GetStringOrNull(item, "redPlayerId"),
            WhiteUserId = GetStringOrNull(item, "whiteUserId"),
            RedUserId = GetStringOrNull(item, "redUserId"),
            WhitePlayerName = GetStringOrNull(item, "whitePlayerName"),
            RedPlayerName = GetStringOrNull(item, "redPlayerName"),
            DoublingCubeOwner = GetStringOrNull(item, "doublingCubeOwner"),
            Winner = GetStringOrNull(item, "winner"),
            CompletedAt = GetNullableDateTime(item, "completedAt"),
            DurationSeconds = GetInt(item, "durationSeconds")
        };

        // Board state
        if (item.TryGetValue("boardState", out var boardStateValue) && boardStateValue.L != null)
        {
            game.BoardState = UnmarshalBoardState(boardStateValue.L);
        }

        // Remaining moves
        if (item.TryGetValue("remainingMoves", out var remainingMovesValue) && remainingMovesValue.L != null)
        {
            game.RemainingMoves = remainingMovesValue.L.Select(v => int.Parse(v.N)).ToList();
        }

        // Move history
        if (item.TryGetValue("moves", out var movesValue) && movesValue.L != null)
        {
            game.Moves = movesValue.L.Select(v => v.S).ToList();
        }

        return game;
    }

    // Marshal Friendship to DynamoDB item
    public static Dictionary<string, AttributeValue> MarshalFriendship(Friendship friendship)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["PK"] = new AttributeValue { S = $"USER#{friendship.UserId}" },
            ["SK"] = new AttributeValue { S = $"FRIEND#{friendship.Status}#{friendship.FriendUserId}" },
            ["friendUserId"] = new AttributeValue { S = friendship.FriendUserId },
            ["friendUsername"] = new AttributeValue { S = friendship.FriendUsername },
            ["friendDisplayName"] = new AttributeValue { S = friendship.FriendDisplayName },
            ["status"] = new AttributeValue { S = friendship.Status.ToString() },
            ["createdAt"] = new AttributeValue { S = friendship.CreatedAt.ToString("O") },
            ["initiatedBy"] = new AttributeValue { S = friendship.InitiatedBy },
            ["entityType"] = new AttributeValue { S = "FRIENDSHIP" }
        };

        if (friendship.AcceptedAt.HasValue)
        {
            item["acceptedAt"] = new AttributeValue { S = friendship.AcceptedAt.Value.ToString("O") };
        }

        return item;
    }

    // Unmarshal Friendship from DynamoDB item
    public static Friendship UnmarshalFriendship(Dictionary<string, AttributeValue> item)
    {
        var statusString = item["status"].S;
        var status = Enum.Parse<FriendshipStatus>(statusString);

        return new Friendship
        {
            UserId = item["PK"].S.Replace("USER#", ""),
            FriendUserId = item["friendUserId"].S,
            FriendUsername = item["friendUsername"].S,
            FriendDisplayName = item["friendDisplayName"].S,
            Status = status,
            CreatedAt = GetDateTime(item, "createdAt"),
            AcceptedAt = GetNullableDateTime(item, "acceptedAt"),
            InitiatedBy = item["initiatedBy"].S
        };
    }

    // Create player-game index item
    public static Dictionary<string, AttributeValue> CreatePlayerGameIndexItem(
        string playerId,
        string gameId,
        string playerColor,
        string opponentId,
        string status,
        DateTime lastUpdatedAt)
    {
        // Use reversed timestamp for latest-first sorting
        var reversedTimestamp = (DateTime.MaxValue.Ticks - lastUpdatedAt.Ticks).ToString("D19");

        return new Dictionary<string, AttributeValue>
        {
            ["PK"] = new AttributeValue { S = $"USER#{playerId}" },
            ["SK"] = new AttributeValue { S = $"GAME#{reversedTimestamp}#{gameId}" },
            ["gameId"] = new AttributeValue { S = gameId },
            ["playerColor"] = new AttributeValue { S = playerColor },
            ["opponentId"] = new AttributeValue { S = opponentId },
            ["status"] = new AttributeValue { S = status },
            ["lastUpdatedAt"] = new AttributeValue { S = lastUpdatedAt.ToString("O") },
            ["entityType"] = new AttributeValue { S = "PLAYER_GAME" }
        };
    }
}
