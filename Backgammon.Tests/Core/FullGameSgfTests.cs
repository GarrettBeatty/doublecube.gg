using Backgammon.Core;
using Xunit;

namespace Backgammon.Tests.Core;

/// <summary>
/// Tests for full game SGF recording and parsing
/// </summary>
public class FullGameSgfTests
{
    [Fact]
    public void CreateGameHeader_WithPlayerNames_IncludesNames()
    {
        // Arrange & Act
        var sgf = SgfSerializer.CreateGameHeader("Alice", "Bob");

        // Assert
        Assert.Contains("(;FF[4]GM[6]", sgf);
        Assert.Contains("PW[Alice]", sgf);
        Assert.Contains("PB[Bob]", sgf);
    }

    [Fact]
    public void CreateGameHeader_WithMatchInfo_IncludesMatchInfo()
    {
        // Arrange & Act
        var sgf = SgfSerializer.CreateGameHeader(
            "Alice",
            "Bob",
            matchLength: 5,
            gameNumber: 2,
            whiteScore: 1,
            blackScore: 2);

        // Assert
        Assert.Contains("MI[length:5]", sgf);
        Assert.Contains("[game:2]", sgf);
        Assert.Contains("[ws:1]", sgf);
        Assert.Contains("[bs:2]", sgf);
    }

    [Fact]
    public void CreateGameHeader_WithCrawford_IncludesCrawfordRule()
    {
        // Arrange & Act
        var sgf = SgfSerializer.CreateGameHeader("Alice", "Bob", isCrawford: true);

        // Assert
        Assert.Contains("RU[Crawford:CrawfordGame]", sgf);
    }

    [Fact]
    public void AppendTurn_WithDiceAndMoves_FormatsCorrectly()
    {
        // Arrange
        var header = SgfSerializer.CreateGameHeader("Alice", "Bob");
        var moves = new List<Move>
        {
            new Move(24, 20, 4),
            new Move(13, 10, 3)
        };

        // Act
        var sgf = SgfSerializer.AppendTurn(header, CheckerColor.White, 4, 3, moves);

        // Assert
        Assert.Contains(";W[43xt", sgf); // 24->20 = x->t for White, 13->10 = m->j
    }

    [Fact]
    public void AppendCubeAction_Double_FormatsCorrectly()
    {
        // Arrange
        var header = SgfSerializer.CreateGameHeader("Alice", "Bob");

        // Act
        var sgf = SgfSerializer.AppendCubeAction(header, CheckerColor.White, CubeAction.Double);

        // Assert
        Assert.Contains(";W[double]", sgf);
    }

    [Fact]
    public void AppendCubeAction_Take_FormatsCorrectly()
    {
        // Arrange
        var header = SgfSerializer.CreateGameHeader("Alice", "Bob");

        // Act
        var sgf = SgfSerializer.AppendCubeAction(header, CheckerColor.Red, CubeAction.Take);

        // Assert
        Assert.Contains(";B[take]", sgf);
    }

    [Fact]
    public void FinalizeGame_WithWinner_AddsResultAndCloses()
    {
        // Arrange
        var header = SgfSerializer.CreateGameHeader("Alice", "Bob");

        // Act
        var sgf = SgfSerializer.FinalizeGame(header, CheckerColor.White, WinType.Gammon);

        // Assert
        Assert.Contains("RE[W+2]", sgf);
        Assert.EndsWith(")", sgf);
    }

    [Fact]
    public void FinalizeGame_WithBackgammon_Shows3Points()
    {
        // Arrange
        var header = SgfSerializer.CreateGameHeader("Alice", "Bob");

        // Act
        var sgf = SgfSerializer.FinalizeGame(header, CheckerColor.Red, WinType.Backgammon);

        // Assert
        Assert.Contains("RE[B+3]", sgf);
    }

    [Fact]
    public void ParseGameSgf_WithHeader_ExtractsPlayerNames()
    {
        // Arrange
        var sgf = "(;FF[4]GM[6]CA[UTF-8]PW[Alice]PB[Bob])";

        // Act
        var record = SgfSerializer.ParseGameSgf(sgf);

        // Assert
        Assert.Equal("Alice", record.WhitePlayer);
        Assert.Equal("Bob", record.BlackPlayer);
    }

    [Fact]
    public void ParseGameSgf_WithTurns_ExtractsMoves()
    {
        // Arrange
        var sgf = "(;FF[4]GM[6]PW[Alice]PB[Bob];W[31hefe];B[42jm])";

        // Act
        var record = SgfSerializer.ParseGameSgf(sgf);

        // Assert
        Assert.Equal(2, record.Turns.Count);
        Assert.Equal(CheckerColor.White, record.Turns[0].Player);
        Assert.Equal(3, record.Turns[0].Die1);
        Assert.Equal(1, record.Turns[0].Die2);
        Assert.Equal(CheckerColor.Red, record.Turns[1].Player);
    }

    [Fact]
    public void ParseGameSgf_WithCubeActions_ExtractsActions()
    {
        // Arrange
        var sgf = "(;FF[4]GM[6];W[double];B[take])";

        // Act
        var record = SgfSerializer.ParseGameSgf(sgf);

        // Assert
        Assert.Equal(2, record.Turns.Count);
        Assert.Equal(DoublingAction.Offered, record.Turns[0].DoublingAction);
        Assert.Equal(DoublingAction.Accepted, record.Turns[1].DoublingAction);
    }

    [Fact]
    public void ParseGameSgf_WithResult_ExtractsWinner()
    {
        // Arrange
        var sgf = "(;FF[4]GM[6]RE[W+2])";

        // Act
        var record = SgfSerializer.ParseGameSgf(sgf);

        // Assert
        Assert.Equal(CheckerColor.White, record.Winner);
        Assert.Equal(WinType.Gammon, record.WinType);
    }

    [Fact]
    public void GameEngine_TracksGameSgf_DuringPlay()
    {
        // Arrange
        var engine = new GameEngine("Alice", "Bob");
        engine.StartNewGame();

        // Act - simulate a turn
        engine.Dice.SetDice(3, 1);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        // Simulate rolling by setting dice state for SGF tracking
        var sgfBefore = engine.GameSgf;

        // Assert - SGF header should be initialized
        Assert.Contains("(;FF[4]GM[6]", sgfBefore);
        Assert.Contains("PW[Alice]", sgfBefore);
        Assert.Contains("PB[Bob]", sgfBefore);
    }

    [Fact]
    public void RoundTrip_CreateAndParse_PreservesData()
    {
        // Arrange - create a game SGF
        var sgf = SgfSerializer.CreateGameHeader("Alice", "Bob", 5, 1, 0, 0, false);
        sgf = SgfSerializer.AppendTurn(sgf, CheckerColor.White, 6, 5, new List<Move>
        {
            new Move(24, 18, 6),
            new Move(13, 8, 5)
        });
        sgf = SgfSerializer.AppendTurn(sgf, CheckerColor.Red, 4, 3, new List<Move>
        {
            new Move(1, 5, 4),
            new Move(1, 4, 3)
        });
        sgf = SgfSerializer.FinalizeGame(sgf, CheckerColor.White, WinType.Normal);

        // Act - parse the SGF
        var record = SgfSerializer.ParseGameSgf(sgf);

        // Assert
        Assert.Equal("Alice", record.WhitePlayer);
        Assert.Equal("Bob", record.BlackPlayer);
        Assert.Equal(5, record.MatchLength);
        Assert.Equal(2, record.Turns.Count);
        Assert.Equal(CheckerColor.White, record.Winner);
        Assert.Equal(WinType.Normal, record.WinType);
    }

    [Fact]
    public void ParseGameSgf_ComputesPositionSgfForEachTurn()
    {
        // Arrange - create a game SGF with a few turns
        var sgf = SgfSerializer.CreateGameHeader("Alice", "Bob");
        sgf = SgfSerializer.AppendTurn(sgf, CheckerColor.White, 6, 5, new List<Move>
        {
            new Move(24, 18, 6),
            new Move(13, 8, 5)
        });
        sgf = SgfSerializer.AppendTurn(sgf, CheckerColor.Red, 4, 3, new List<Move>
        {
            new Move(1, 5, 4),
            new Move(1, 4, 3)
        });
        sgf = SgfSerializer.FinalizeGame(sgf, CheckerColor.White, WinType.Normal);

        // Act - parse the SGF
        var record = SgfSerializer.ParseGameSgf(sgf);

        // Assert - each turn should have a position SGF
        Assert.Equal(2, record.Turns.Count);

        // First turn should have starting position
        Assert.NotNull(record.Turns[0].PositionSgf);
        Assert.NotEmpty(record.Turns[0].PositionSgf!);
        Assert.Contains("(;FF[4]GM[6]", record.Turns[0].PositionSgf);

        // Second turn should have a different position (after first turn's moves)
        Assert.NotNull(record.Turns[1].PositionSgf);
        Assert.NotEmpty(record.Turns[1].PositionSgf!);

        // The positions should be different (first has moved checkers)
        Assert.NotEqual(record.Turns[0].PositionSgf, record.Turns[1].PositionSgf);
    }

    // ==================== TurnSnapshot-specific Tests ====================

    [Fact]
    public void ParseGameSgf_RegularDice_SetsDiceRolledArray()
    {
        // Arrange
        var sgf = "(;FF[4]GM[6]PW[Alice]PB[Bob];W[35hefe])";

        // Act
        var record = SgfSerializer.ParseGameSgf(sgf);

        // Assert
        var turn = record.Turns[0];
        Assert.Equal(2, turn.DiceRolled.Length);
        Assert.Equal(3, turn.DiceRolled[0]);
        Assert.Equal(5, turn.DiceRolled[1]);
        // Die1/Die2 should work as convenience properties
        Assert.Equal(3, turn.Die1);
        Assert.Equal(5, turn.Die2);
    }

    [Fact]
    public void ParseGameSgf_Doubles_SetsFourDiceValues()
    {
        // Arrange - doubles (6,6) with 4 moves
        var sgf = "(;FF[4]GM[6]PW[Alice]PB[Bob];W[66xrrmhb])";

        // Act
        var record = SgfSerializer.ParseGameSgf(sgf);

        // Assert
        var turn = record.Turns[0];
        Assert.Equal(4, turn.DiceRolled.Length);
        Assert.All(turn.DiceRolled, d => Assert.Equal(6, d));
        Assert.Equal(6, turn.Die1);
        Assert.Equal(6, turn.Die2);
    }

    [Fact]
    public void ParseGameSgf_DropAction_MapsToDeclined()
    {
        // Arrange
        var sgf = "(;FF[4]GM[6];W[double];B[drop])";

        // Act
        var record = SgfSerializer.ParseGameSgf(sgf);

        // Assert
        Assert.Equal(2, record.Turns.Count);
        Assert.Equal(DoublingAction.Offered, record.Turns[0].DoublingAction);
        Assert.Equal(DoublingAction.Declined, record.Turns[1].DoublingAction);
    }

    [Fact]
    public void ParseGameSgf_ResignAction_MapsToDeclined()
    {
        // Arrange
        var sgf = "(;FF[4]GM[6];W[resign])";

        // Act
        var record = SgfSerializer.ParseGameSgf(sgf);

        // Assert
        Assert.Single(record.Turns);
        Assert.Equal(DoublingAction.Declined, record.Turns[0].DoublingAction);
    }

    [Fact]
    public void ParseGameSgf_CubeOnlyTurn_HasEmptyDiceArray()
    {
        // Arrange - cube action only, no dice
        var sgf = "(;FF[4]GM[6];W[double])";

        // Act
        var record = SgfSerializer.ParseGameSgf(sgf);

        // Assert
        var turn = record.Turns[0];
        Assert.Empty(turn.DiceRolled);
        Assert.Equal(0, turn.Die1);
        Assert.Equal(0, turn.Die2);
    }

    [Fact]
    public void ParseGameSgf_MovesStoredAsMoveObjects()
    {
        // Arrange - White moves 24->18 and 13->8
        var sgf = "(;FF[4]GM[6]PW[Alice]PB[Bob];W[65xrme])";

        // Act
        var record = SgfSerializer.ParseGameSgf(sgf);

        // Assert
        var turn = record.Turns[0];
        Assert.Equal(2, turn.Moves.Count);
        // Verify moves are proper Move objects with From/To
        Assert.All(turn.Moves, m =>
        {
            Assert.True(m.From >= 0 && m.From <= 24);
            Assert.True(m.To >= 0 && m.To <= 25);
        });
    }

    [Fact]
    public void ParseGameSgf_TurnSnapshot_HasCorrectTurnNumbers()
    {
        // Arrange
        var sgf = "(;FF[4]GM[6]PW[A]PB[B];W[31ab];B[42cd];W[53ef])";

        // Act
        var record = SgfSerializer.ParseGameSgf(sgf);

        // Assert
        Assert.Equal(3, record.Turns.Count);
        Assert.Equal(1, record.Turns[0].TurnNumber);
        Assert.Equal(2, record.Turns[1].TurnNumber);
        Assert.Equal(3, record.Turns[2].TurnNumber);
    }

    [Fact]
    public void ParseGameSgf_AlternatingPlayers_PreservesOrder()
    {
        // Arrange
        var sgf = "(;FF[4]GM[6]PW[A]PB[B];W[31ab];B[42cd];W[53ef];B[64gh])";

        // Act
        var record = SgfSerializer.ParseGameSgf(sgf);

        // Assert
        Assert.Equal(4, record.Turns.Count);
        Assert.Equal(CheckerColor.White, record.Turns[0].Player);
        Assert.Equal(CheckerColor.Red, record.Turns[1].Player);
        Assert.Equal(CheckerColor.White, record.Turns[2].Player);
        Assert.Equal(CheckerColor.Red, record.Turns[3].Player);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 3)]
    [InlineData(4, 4)]
    [InlineData(5, 5)]
    [InlineData(6, 6)]
    public void ParseGameSgf_VariousDoubles_AllGetFourDice(int dieValue, int expected)
    {
        // Arrange - doubles of various values
        var diceChar = (char)('0' + dieValue);
        var sgf = $"(;FF[4]GM[6]PW[A]PB[B];W[{diceChar}{diceChar}ab])";

        // Act
        var record = SgfSerializer.ParseGameSgf(sgf);

        // Assert
        var turn = record.Turns[0];
        Assert.Equal(4, turn.DiceRolled.Length);
        Assert.All(turn.DiceRolled, d => Assert.Equal(expected, d));
    }

    [Fact]
    public void ParseGameSgf_MixedTurns_PreservesDiceAndMoves()
    {
        // Arrange - a game with cube actions and regular moves
        var sgf = SgfSerializer.CreateGameHeader("Alice", "Bob");
        sgf = SgfSerializer.AppendTurn(sgf, CheckerColor.White, 6, 5, new List<Move>
        {
            new Move(24, 18, 6),
            new Move(13, 8, 5)
        });
        sgf = SgfSerializer.AppendCubeAction(sgf, CheckerColor.Red, CubeAction.Double);
        sgf = SgfSerializer.AppendCubeAction(sgf, CheckerColor.White, CubeAction.Take);
        sgf = SgfSerializer.AppendTurn(sgf, CheckerColor.Red, 4, 4, new List<Move>
        {
            new Move(1, 5, 4),
            new Move(1, 5, 4),
            new Move(12, 16, 4),
            new Move(12, 16, 4)
        });
        sgf = SgfSerializer.FinalizeGame(sgf, CheckerColor.White, WinType.Normal);

        // Act
        var record = SgfSerializer.ParseGameSgf(sgf);

        // Assert
        Assert.Equal(4, record.Turns.Count);

        // Turn 1: White's regular move
        Assert.Equal(new[] { 6, 5 }, record.Turns[0].DiceRolled);
        Assert.Equal(2, record.Turns[0].Moves.Count);
        Assert.Null(record.Turns[0].DoublingAction);

        // Turn 2: Red's double
        Assert.Empty(record.Turns[1].DiceRolled);
        Assert.Empty(record.Turns[1].Moves);
        Assert.Equal(DoublingAction.Offered, record.Turns[1].DoublingAction);

        // Turn 3: White's take
        Assert.Empty(record.Turns[2].DiceRolled);
        Assert.Empty(record.Turns[2].Moves);
        Assert.Equal(DoublingAction.Accepted, record.Turns[2].DoublingAction);

        // Turn 4: Red's doubles move
        Assert.Equal(4, record.Turns[3].DiceRolled.Length);
        Assert.All(record.Turns[3].DiceRolled, d => Assert.Equal(4, d));
        Assert.Equal(4, record.Turns[3].Moves.Count);
        Assert.Null(record.Turns[3].DoublingAction);
    }
}
