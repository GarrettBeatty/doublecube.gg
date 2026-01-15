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
        Assert.Equal(CubeAction.Double, record.Turns[0].CubeAction);
        Assert.Equal(CubeAction.Take, record.Turns[1].CubeAction);
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
}
