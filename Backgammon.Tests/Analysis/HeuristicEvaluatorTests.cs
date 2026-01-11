using Backgammon.Analysis.Evaluators;
using Backgammon.Core;
using Xunit;

namespace Backgammon.Tests.Analysis;

public class HeuristicEvaluatorTests
{
    [Fact]
    public async Task EvaluateAsync_OpeningPosition_ReturnsNearZeroEquity()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetGameStarted(true);
        var evaluator = new HeuristicEvaluator();

        // Act
        var evaluation = await evaluator.EvaluateAsync(engine);

        // Assert
        Assert.InRange(evaluation.Equity, -0.5, 0.5); // Opening position should be roughly equal
        Assert.InRange(evaluation.WinProbability, 0.3, 0.7); // Roughly 50% win chance
        Assert.NotNull(evaluation.Features);
        Assert.Equal(167, evaluation.Features.PipCount); // Standard opening pip count
    }

    [Fact]
    public async Task EvaluateAsync_PlayerAhead_ReturnsPositiveEquity()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetGameStarted(true);

        // Remove some of opponent's checkers to create advantage
        engine.RedPlayer.CheckersBornOff = 5;

        var evaluator = new HeuristicEvaluator();

        // Act
        var evaluation = await evaluator.EvaluateAsync(engine);

        // Assert
        Assert.True(evaluation.Equity > 0, "White should have positive equity with opponent behind");
        Assert.True(evaluation.WinProbability > 0.5, "Win probability should be > 50%");
    }

    [Fact]
    public async Task FindBestMovesAsync_WithDiceRolled_ReturnsValidMoveSequences()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetGameStarted(true);
        engine.Dice.SetDice(3, 4);
        engine.RemainingMoves.AddRange(new[] { 3, 4 });

        var evaluator = new HeuristicEvaluator();

        // Act
        var analysis = await evaluator.FindBestMovesAsync(engine);

        // Assert
        Assert.NotNull(analysis);
        Assert.True(analysis.TotalSequencesExplored > 0, "Should explore at least one move sequence");
        Assert.NotEmpty(analysis.TopMoves);
        Assert.NotNull(analysis.BestMove);
        Assert.NotEmpty(analysis.BestMove.Moves);
    }

    [Fact]
    public async Task FindBestMovesAsync_NoDiceRolled_ReturnsEmptyAnalysis()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetGameStarted(true);
        var evaluator = new HeuristicEvaluator();

        // Act
        var analysis = await evaluator.FindBestMovesAsync(engine);

        // Assert
        Assert.Equal(0, analysis.TotalSequencesExplored);
        Assert.Empty(analysis.TopMoves);
    }

    [Fact]
    public async Task EvaluateAsync_CheckersOnBar_NegativeEquity()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetGameStarted(true);

        // Put White player on bar (bad position)
        engine.WhitePlayer.CheckersOnBar = 2;

        var evaluator = new HeuristicEvaluator();

        // Act
        var evaluation = await evaluator.EvaluateAsync(engine);

        // Assert
        Assert.True(evaluation.Equity < 0, "Having checkers on bar should result in negative equity");
        Assert.Equal(2, evaluation.Features.CheckersOnBar);
    }
}
