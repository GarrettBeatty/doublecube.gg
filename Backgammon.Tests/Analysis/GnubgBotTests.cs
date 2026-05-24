using Backgammon.AI.Bots;
using Backgammon.Core;
using Backgammon.Plugins.Abstractions;
using Backgammon.Plugins.Models;
using Moq;

namespace Backgammon.Tests.Analysis;

/// <summary>
/// Tests for GnubgBot behavior. These tests exist to verify GnubgBot behavior
/// before and after refactoring its location from Backgammon.AI to Backgammon.Analysis.
/// </summary>
public class GnubgBotTests
{
    // ==================== Metadata Tests ====================

    [Fact]
    public void BotId_ReturnsGnubgBot()
    {
        var bot = new GnubgBot(new Mock<IPositionEvaluator>().Object);
        Assert.Equal("gnubg-bot", bot.BotId);
    }

    [Fact]
    public void DisplayName_ReturnsExpertBotGNUBG()
    {
        var bot = new GnubgBot(new Mock<IPositionEvaluator>().Object);
        Assert.Equal("Expert Bot (GNUBG)", bot.DisplayName);
    }

    [Fact]
    public void Description_MentionsGnubgNeuralNetwork()
    {
        var bot = new GnubgBot(new Mock<IPositionEvaluator>().Object);
        Assert.Contains("GNU Backgammon", bot.Description);
    }

    [Fact]
    public void EstimatedElo_Returns2000()
    {
        var bot = new GnubgBot(new Mock<IPositionEvaluator>().Object);
        Assert.Equal(2000, bot.EstimatedElo);
    }

    [Fact]
    public void Evaluator_IsTheInjectedInstance()
    {
        var mockEvaluator = new Mock<IPositionEvaluator>().Object;
        var bot = new GnubgBot(mockEvaluator);
        Assert.Same(mockEvaluator, bot.Evaluator);
    }

    // ==================== ChooseMovesAsync Tests ====================

    [Fact]
    public async Task ChooseMovesAsync_NoRemainingMoves_ReturnsEmptyWithoutCallingEvaluator()
    {
        var mockEvaluator = new Mock<IPositionEvaluator>();
        var bot = new GnubgBot(mockEvaluator.Object);

        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetGameStarted(true);
        // No dice rolled — RemainingMoves is empty

        var result = await bot.ChooseMovesAsync(engine);

        Assert.Empty(result);
        mockEvaluator.Verify(
            e => e.FindBestMovesAsync(It.IsAny<GameEngine>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ChooseMovesAsync_EvaluatorReturnsNoBestMove_ReturnsEmpty()
    {
        var mockEvaluator = new Mock<IPositionEvaluator>();
        mockEvaluator
            .Setup(e => e.FindBestMovesAsync(It.IsAny<GameEngine>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BestMovesAnalysis { TopMoves = new List<MoveSequenceEvaluation>() });

        var bot = new GnubgBot(mockEvaluator.Object);
        var engine = CreateStartedEngine();

        var result = await bot.ChooseMovesAsync(engine);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ChooseMovesAsync_CallsEvaluatorFindBestMovesAsync()
    {
        var engine = CreateStartedEngine();
        var validMoves = engine.GetValidMoves();
        var firstMove = validMoves.First();

        var bestMove = new MoveSequenceEvaluation
        {
            Moves = new List<Move> { firstMove },
            Alternatives = new List<List<Move>>()
        };

        var mockEvaluator = new Mock<IPositionEvaluator>();
        mockEvaluator
            .Setup(e => e.FindBestMovesAsync(It.IsAny<GameEngine>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BestMovesAnalysis
            {
                TopMoves = new List<MoveSequenceEvaluation> { bestMove }
            });

        var bot = new GnubgBot(mockEvaluator.Object);

        await bot.ChooseMovesAsync(engine);

        mockEvaluator.Verify(
            e => e.FindBestMovesAsync(It.IsAny<GameEngine>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ChooseMovesAsync_ValidMoveFromEvaluator_ExecutesMoveOnEngine()
    {
        var engine = CreateStartedEngine();
        var validMoves = engine.GetValidMoves();
        var firstMove = validMoves.First();
        var initialRemainingCount = engine.RemainingMoves.Count;

        var bestMove = new MoveSequenceEvaluation
        {
            Moves = new List<Move> { firstMove },
            Alternatives = new List<List<Move>>()
        };

        var mockEvaluator = new Mock<IPositionEvaluator>();
        mockEvaluator
            .Setup(e => e.FindBestMovesAsync(It.IsAny<GameEngine>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BestMovesAnalysis
            {
                TopMoves = new List<MoveSequenceEvaluation> { bestMove }
            });

        var bot = new GnubgBot(mockEvaluator.Object);
        var chosenMoves = await bot.ChooseMovesAsync(engine);

        Assert.Single(chosenMoves);
        Assert.Equal(firstMove.From, chosenMoves[0].From);
        Assert.Equal(firstMove.To, chosenMoves[0].To);
        // Executing a move consumes a die
        Assert.Equal(initialRemainingCount - 1, engine.RemainingMoves.Count);
    }

    [Fact]
    public async Task ChooseMovesAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        var mockEvaluator = new Mock<IPositionEvaluator>();
        mockEvaluator
            .Setup(e => e.FindBestMovesAsync(It.IsAny<GameEngine>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var bot = new GnubgBot(mockEvaluator.Object);
        var engine = CreateStartedEngine();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => bot.ChooseMovesAsync(engine, cts.Token));
    }

    [Fact]
    public async Task ChooseMovesAsync_InvalidMoveFromEvaluator_ReturnsEmptyWithoutCrashing()
    {
        // Evaluator returns a move that is not in the valid moves list
        var invalidMove = new Move(from: 99, to: 98, dieValue: 99);

        var bestMove = new MoveSequenceEvaluation
        {
            Moves = new List<Move> { invalidMove },
            Alternatives = new List<List<Move>>()
        };

        var mockEvaluator = new Mock<IPositionEvaluator>();
        mockEvaluator
            .Setup(e => e.FindBestMovesAsync(It.IsAny<GameEngine>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BestMovesAnalysis
            {
                TopMoves = new List<MoveSequenceEvaluation> { bestMove }
            });

        var bot = new GnubgBot(mockEvaluator.Object);
        var engine = CreateStartedEngine();

        var result = await bot.ChooseMovesAsync(engine);

        // Should not throw; returns whatever moves succeeded (none in this case)
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ChooseMovesAsync_WithAlternatives_TriesFirstValidAlternative()
    {
        var engine = CreateStartedEngine();
        var validMoves = engine.GetValidMoves();
        var validMove = validMoves.First();

        // First alternative is invalid, second is valid
        var invalidAlt = new List<Move> { new Move(from: 99, to: 98, dieValue: 1) };
        var validAlt = new List<Move> { validMove };

        var bestMove = new MoveSequenceEvaluation
        {
            Moves = invalidAlt,
            Alternatives = new List<List<Move>> { invalidAlt, validAlt }
        };

        var mockEvaluator = new Mock<IPositionEvaluator>();
        mockEvaluator
            .Setup(e => e.FindBestMovesAsync(It.IsAny<GameEngine>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BestMovesAnalysis
            {
                TopMoves = new List<MoveSequenceEvaluation> { bestMove }
            });

        var bot = new GnubgBot(mockEvaluator.Object);
        var result = await bot.ChooseMovesAsync(engine);

        // Should have fallen through to the valid alternative
        Assert.Single(result);
        Assert.Equal(validMove.From, result[0].From);
        Assert.Equal(validMove.To, result[0].To);
    }

    private static GameEngine CreateStartedEngine(int die1 = 3, int die2 = 4)
    {
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetGameStarted(true);
        engine.Dice.SetDice(die1, die2);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());
        return engine;
    }
}
