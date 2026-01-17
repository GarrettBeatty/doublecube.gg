using Amazon.DynamoDBv2.Model;
using Backgammon.Server.Models;
using Backgammon.Server.Services.DynamoDb;
using Xunit;

namespace Backgammon.Tests.Services.DynamoDb;

/// <summary>
/// Tests for DynamoDbHelpers - marshal/unmarshal operations.
/// </summary>
public class DynamoDbHelpersTests
{
    // ==================== UnmarshalTurnSnapshot Tests ====================

    [Fact]
    public void UnmarshalTurnSnapshot_BasicTurn_MapsAllProperties()
    {
        // Arrange
        var turnMap = new Dictionary<string, AttributeValue>
        {
            ["turnNumber"] = new AttributeValue { N = "5" },
            ["player"] = new AttributeValue { S = "White" },
            ["diceRolled"] = new AttributeValue
            {
                L = new List<AttributeValue>
                {
                    new AttributeValue { N = "3" },
                    new AttributeValue { N = "5" }
                }
            },
            ["positionSgf"] = new AttributeValue { S = "(;GM[6])" },
            ["moves"] = new AttributeValue
            {
                L = new List<AttributeValue>
                {
                    new AttributeValue { S = "24/21" },
                    new AttributeValue { S = "13/8" }
                }
            },
            ["cubeValue"] = new AttributeValue { N = "2" },
            ["cubeOwner"] = new AttributeValue { S = "White" },
            ["doublingAction"] = new AttributeValue { S = "Offered" }
        };

        // Act
        var turn = DynamoDbHelpers.UnmarshalTurnSnapshot(turnMap);

        // Assert
        Assert.Equal(5, turn.TurnNumber);
        Assert.Equal("White", turn.Player);
        Assert.Equal(new[] { 3, 5 }, turn.DiceRolled);
        Assert.Equal("(;GM[6])", turn.PositionSgf);
        Assert.Equal(2, turn.Moves.Count);
        Assert.Equal("24/21", turn.Moves[0]);
        Assert.Equal("13/8", turn.Moves[1]);
        Assert.Equal(2, turn.CubeValue);
        Assert.Equal("White", turn.CubeOwner);
        Assert.Equal("Offered", turn.DoublingAction);
    }

    [Fact]
    public void UnmarshalTurnSnapshot_NullCubeOwner_ReturnsNull()
    {
        // Arrange
        var turnMap = new Dictionary<string, AttributeValue>
        {
            ["turnNumber"] = new AttributeValue { N = "1" },
            ["player"] = new AttributeValue { S = "White" },
            ["diceRolled"] = new AttributeValue { L = new List<AttributeValue>() },
            ["moves"] = new AttributeValue { L = new List<AttributeValue>() },
            ["cubeValue"] = new AttributeValue { N = "1" },
            ["cubeOwner"] = new AttributeValue { NULL = true }
        };

        // Act
        var turn = DynamoDbHelpers.UnmarshalTurnSnapshot(turnMap);

        // Assert
        Assert.Null(turn.CubeOwner);
    }

    [Fact]
    public void UnmarshalTurnSnapshot_NullDoublingAction_ReturnsNull()
    {
        // Arrange
        var turnMap = new Dictionary<string, AttributeValue>
        {
            ["turnNumber"] = new AttributeValue { N = "1" },
            ["player"] = new AttributeValue { S = "Red" },
            ["diceRolled"] = new AttributeValue { L = new List<AttributeValue>() },
            ["moves"] = new AttributeValue { L = new List<AttributeValue>() },
            ["cubeValue"] = new AttributeValue { N = "1" },
            ["doublingAction"] = new AttributeValue { NULL = true }
        };

        // Act
        var turn = DynamoDbHelpers.UnmarshalTurnSnapshot(turnMap);

        // Assert
        Assert.Null(turn.DoublingAction);
    }

    [Fact]
    public void UnmarshalTurnSnapshot_DoublesDice_PreservesFourValues()
    {
        // Arrange
        var turnMap = new Dictionary<string, AttributeValue>
        {
            ["turnNumber"] = new AttributeValue { N = "2" },
            ["player"] = new AttributeValue { S = "White" },
            ["diceRolled"] = new AttributeValue
            {
                L = new List<AttributeValue>
                {
                    new AttributeValue { N = "6" },
                    new AttributeValue { N = "6" },
                    new AttributeValue { N = "6" },
                    new AttributeValue { N = "6" }
                }
            },
            ["moves"] = new AttributeValue { L = new List<AttributeValue>() },
            ["cubeValue"] = new AttributeValue { N = "1" }
        };

        // Act
        var turn = DynamoDbHelpers.UnmarshalTurnSnapshot(turnMap);

        // Assert
        Assert.Equal(4, turn.DiceRolled.Length);
        Assert.All(turn.DiceRolled, d => Assert.Equal(6, d));
    }

    [Fact]
    public void UnmarshalTurnSnapshot_EmptyDice_ReturnsEmptyArray()
    {
        // Arrange
        var turnMap = new Dictionary<string, AttributeValue>
        {
            ["turnNumber"] = new AttributeValue { N = "1" },
            ["player"] = new AttributeValue { S = "White" },
            ["moves"] = new AttributeValue { L = new List<AttributeValue>() },
            ["cubeValue"] = new AttributeValue { N = "1" }
        };

        // Act
        var turn = DynamoDbHelpers.UnmarshalTurnSnapshot(turnMap);

        // Assert
        Assert.Empty(turn.DiceRolled);
    }

    [Fact]
    public void UnmarshalTurnSnapshot_EmptyMoves_ReturnsEmptyList()
    {
        // Arrange
        var turnMap = new Dictionary<string, AttributeValue>
        {
            ["turnNumber"] = new AttributeValue { N = "1" },
            ["player"] = new AttributeValue { S = "White" },
            ["diceRolled"] = new AttributeValue { L = new List<AttributeValue>() },
            ["cubeValue"] = new AttributeValue { N = "1" }
        };

        // Act
        var turn = DynamoDbHelpers.UnmarshalTurnSnapshot(turnMap);

        // Assert
        Assert.Empty(turn.Moves);
    }

    [Fact]
    public void UnmarshalTurnSnapshot_NullPositionSgf_ReturnsEmpty()
    {
        // Arrange
        var turnMap = new Dictionary<string, AttributeValue>
        {
            ["turnNumber"] = new AttributeValue { N = "1" },
            ["player"] = new AttributeValue { S = "White" },
            ["diceRolled"] = new AttributeValue { L = new List<AttributeValue>() },
            ["positionSgf"] = new AttributeValue { NULL = true },
            ["moves"] = new AttributeValue { L = new List<AttributeValue>() },
            ["cubeValue"] = new AttributeValue { N = "1" }
        };

        // Act
        var turn = DynamoDbHelpers.UnmarshalTurnSnapshot(turnMap);

        // Assert
        Assert.Equal(string.Empty, turn.PositionSgf);
    }

    // ==================== MarshalGame/UnmarshalGame with Turns Tests ====================

    [Fact]
    public void MarshalGame_WithTurns_IncludesTurnsInItem()
    {
        // Arrange
        var game = new Game
        {
            GameId = "test-game-1",
            Status = "InProgress",
            CurrentPlayer = "White",
            Turns = new List<TurnSnapshotDto>
            {
                new TurnSnapshotDto
                {
                    TurnNumber = 1,
                    Player = "White",
                    DiceRolled = new[] { 3, 5 },
                    Moves = new List<string> { "24/21", "13/8" },
                    CubeValue = 1
                },
                new TurnSnapshotDto
                {
                    TurnNumber = 2,
                    Player = "Red",
                    DiceRolled = new[] { 6, 4 },
                    Moves = new List<string> { "1/7", "12/16" },
                    CubeValue = 1
                }
            }
        };

        // Act
        var item = DynamoDbHelpers.MarshalGame(game);

        // Assert
        Assert.True(item.ContainsKey("turns"));
        Assert.Equal(2, item["turns"].L.Count);

        var firstTurn = item["turns"].L[0].M;
        Assert.Equal("1", firstTurn["turnNumber"].N);
        Assert.Equal("White", firstTurn["player"].S);
        Assert.Equal(2, firstTurn["diceRolled"].L.Count);
        Assert.Equal("3", firstTurn["diceRolled"].L[0].N);
        Assert.Equal("5", firstTurn["diceRolled"].L[1].N);
        Assert.Equal(2, firstTurn["moves"].L.Count);
        Assert.Equal("24/21", firstTurn["moves"].L[0].S);
    }

    [Fact]
    public void MarshalGame_EmptyTurns_DoesNotIncludeTurns()
    {
        // Arrange
        var game = new Game
        {
            GameId = "test-game-2",
            Status = "InProgress",
            CurrentPlayer = "White",
            Turns = new List<TurnSnapshotDto>()
        };

        // Act
        var item = DynamoDbHelpers.MarshalGame(game);

        // Assert
        Assert.False(item.ContainsKey("turns"));
    }

    [Fact]
    public void MarshalGame_TurnWithDoublingAction_IncludesDoublingAction()
    {
        // Arrange
        var game = new Game
        {
            GameId = "test-game-3",
            Status = "InProgress",
            CurrentPlayer = "Red",
            Turns = new List<TurnSnapshotDto>
            {
                new TurnSnapshotDto
                {
                    TurnNumber = 4,
                    Player = "White",
                    DoublingAction = "Offered",
                    CubeValue = 2
                }
            }
        };

        // Act
        var item = DynamoDbHelpers.MarshalGame(game);

        // Assert
        var turn = item["turns"].L[0].M;
        Assert.Equal("Offered", turn["doublingAction"].S);
    }

    [Fact]
    public void MarshalGame_TurnWithNullCubeOwner_SetsNullAttribute()
    {
        // Arrange
        var game = new Game
        {
            GameId = "test-game-4",
            Status = "InProgress",
            CurrentPlayer = "White",
            Turns = new List<TurnSnapshotDto>
            {
                new TurnSnapshotDto
                {
                    TurnNumber = 1,
                    Player = "White",
                    CubeValue = 1,
                    CubeOwner = null
                }
            }
        };

        // Act
        var item = DynamoDbHelpers.MarshalGame(game);

        // Assert
        var turn = item["turns"].L[0].M;
        Assert.True(turn["cubeOwner"].NULL);
    }

    [Fact]
    public void UnmarshalGame_WithTurns_RestoresTurns()
    {
        // Arrange
        var item = new Dictionary<string, AttributeValue>
        {
            ["gameId"] = new AttributeValue { S = "test-game-5" },
            ["status"] = new AttributeValue { S = "InProgress" },
            ["currentPlayer"] = new AttributeValue { S = "White" },
            ["doublingCubeValue"] = new AttributeValue { N = "1" },
            ["stakes"] = new AttributeValue { N = "0" },
            ["moveCount"] = new AttributeValue { N = "0" },
            ["createdAt"] = new AttributeValue { S = DateTime.UtcNow.ToString("O") },
            ["lastUpdatedAt"] = new AttributeValue { S = DateTime.UtcNow.ToString("O") },
            ["turns"] = new AttributeValue
            {
                L = new List<AttributeValue>
                {
                    new AttributeValue
                    {
                        M = new Dictionary<string, AttributeValue>
                        {
                            ["turnNumber"] = new AttributeValue { N = "1" },
                            ["player"] = new AttributeValue { S = "White" },
                            ["diceRolled"] = new AttributeValue
                            {
                                L = new List<AttributeValue>
                                {
                                    new AttributeValue { N = "3" },
                                    new AttributeValue { N = "5" }
                                }
                            },
                            ["moves"] = new AttributeValue
                            {
                                L = new List<AttributeValue>
                                {
                                    new AttributeValue { S = "24/21" },
                                    new AttributeValue { S = "13/8" }
                                }
                            },
                            ["cubeValue"] = new AttributeValue { N = "1" },
                            ["cubeOwner"] = new AttributeValue { NULL = true },
                            ["doublingAction"] = new AttributeValue { NULL = true }
                        }
                    }
                }
            }
        };

        // Act
        var game = DynamoDbHelpers.UnmarshalGame(item);

        // Assert
        Assert.Single(game.Turns);
        Assert.Equal(1, game.Turns[0].TurnNumber);
        Assert.Equal("White", game.Turns[0].Player);
        Assert.Equal(new[] { 3, 5 }, game.Turns[0].DiceRolled);
        Assert.Equal(2, game.Turns[0].Moves.Count);
        Assert.Equal("24/21", game.Turns[0].Moves[0]);
        Assert.Equal("13/8", game.Turns[0].Moves[1]);
    }

    [Fact]
    public void UnmarshalGame_NoTurns_ReturnsEmptyList()
    {
        // Arrange
        var item = new Dictionary<string, AttributeValue>
        {
            ["gameId"] = new AttributeValue { S = "test-game-6" },
            ["status"] = new AttributeValue { S = "InProgress" },
            ["currentPlayer"] = new AttributeValue { S = "White" },
            ["doublingCubeValue"] = new AttributeValue { N = "1" },
            ["stakes"] = new AttributeValue { N = "0" },
            ["moveCount"] = new AttributeValue { N = "0" },
            ["createdAt"] = new AttributeValue { S = DateTime.UtcNow.ToString("O") },
            ["lastUpdatedAt"] = new AttributeValue { S = DateTime.UtcNow.ToString("O") }
        };

        // Act
        var game = DynamoDbHelpers.UnmarshalGame(item);

        // Assert
        Assert.Empty(game.Turns);
    }

    // ==================== Round-Trip Tests ====================

    [Fact]
    public void MarshalUnmarshal_GameWithTurns_PreservesData()
    {
        // Arrange
        var original = new Game
        {
            GameId = "roundtrip-test",
            Status = "InProgress",
            CurrentPlayer = "Red",
            DoublingCubeValue = 2,
            Turns = new List<TurnSnapshotDto>
            {
                new TurnSnapshotDto
                {
                    TurnNumber = 1,
                    Player = "White",
                    DiceRolled = new[] { 6, 4 },
                    PositionSgf = "(;GM[6])",
                    Moves = new List<string> { "24/18", "13/9" },
                    CubeValue = 1
                },
                new TurnSnapshotDto
                {
                    TurnNumber = 2,
                    Player = "Red",
                    DiceRolled = new[] { 5, 5, 5, 5 },
                    Moves = new List<string> { "1/6", "1/6", "12/17", "12/17" },
                    CubeValue = 1
                },
                new TurnSnapshotDto
                {
                    TurnNumber = 3,
                    Player = "White",
                    DoublingAction = "Offered",
                    CubeValue = 2,
                    CubeOwner = "White"
                },
                new TurnSnapshotDto
                {
                    TurnNumber = 4,
                    Player = "Red",
                    DoublingAction = "Accepted",
                    CubeValue = 2,
                    CubeOwner = "Red"
                }
            }
        };

        // Act
        var item = DynamoDbHelpers.MarshalGame(original);
        var restored = DynamoDbHelpers.UnmarshalGame(item);

        // Assert
        Assert.Equal(original.GameId, restored.GameId);
        Assert.Equal(original.Status, restored.Status);
        Assert.Equal(original.Turns.Count, restored.Turns.Count);

        for (int i = 0; i < original.Turns.Count; i++)
        {
            Assert.Equal(original.Turns[i].TurnNumber, restored.Turns[i].TurnNumber);
            Assert.Equal(original.Turns[i].Player, restored.Turns[i].Player);
            Assert.Equal(original.Turns[i].DiceRolled, restored.Turns[i].DiceRolled);
            Assert.Equal(original.Turns[i].Moves, restored.Turns[i].Moves);
            Assert.Equal(original.Turns[i].CubeValue, restored.Turns[i].CubeValue);
            Assert.Equal(original.Turns[i].CubeOwner, restored.Turns[i].CubeOwner);
            Assert.Equal(original.Turns[i].DoublingAction, restored.Turns[i].DoublingAction);
        }
    }
}
