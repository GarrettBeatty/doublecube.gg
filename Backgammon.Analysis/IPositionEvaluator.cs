using Backgammon.Analysis.Models;
using Backgammon.Core;

namespace Backgammon.Analysis;

/// <summary>
/// Interface for position evaluators.
/// Allows swapping between heuristic and GNU Backgammon evaluators without code changes.
/// </summary>
public interface IPositionEvaluator
{
    /// <summary>
    /// Evaluate a position and return equity estimate with probabilities
    /// </summary>
    /// <param name="engine">Game engine with the position to evaluate</param>
    /// <returns>Complete position evaluation</returns>
    PositionEvaluation Evaluate(GameEngine engine);

    /// <summary>
    /// Find the best move sequences for the current position
    /// </summary>
    /// <param name="engine">Game engine with dice rolled and valid moves available</param>
    /// <returns>Analysis of top moves ranked by equity</returns>
    BestMovesAnalysis FindBestMoves(GameEngine engine);

    /// <summary>
    /// Analyze the doubling cube decision for the current position
    /// </summary>
    /// <param name="engine">Game engine with the position to analyze</param>
    /// <returns>Cube decision analysis with recommendations</returns>
    CubeDecision AnalyzeCubeDecision(GameEngine engine);
}
