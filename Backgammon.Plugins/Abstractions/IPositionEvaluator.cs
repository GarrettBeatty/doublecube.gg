using Backgammon.Core;
using Backgammon.Plugins.Models;

namespace Backgammon.Plugins.Abstractions;

/// <summary>
/// Interface for position evaluators (analysis engines).
/// Evaluators assess positions and suggest moves.
/// All methods are async to support external engines like GNU Backgammon.
/// </summary>
public interface IPositionEvaluator
{
    /// <summary>
    /// Unique identifier for this evaluator (e.g., "heuristic", "gnubg")
    /// </summary>
    string EvaluatorId { get; }

    /// <summary>
    /// Human-friendly display name
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Whether this evaluator requires external processes or resources.
    /// Used to determine if the evaluator is available at runtime.
    /// </summary>
    bool RequiresExternalResources { get; }

    /// <summary>
    /// Evaluate a position and return equity estimate with probabilities
    /// </summary>
    /// <param name="engine">Game engine with the position to evaluate</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Complete position evaluation</returns>
    Task<PositionEvaluation> EvaluateAsync(GameEngine engine, CancellationToken ct = default);

    /// <summary>
    /// Find the best move sequences for the current position
    /// </summary>
    /// <param name="engine">Game engine with dice rolled and valid moves available</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Analysis of top moves ranked by equity</returns>
    Task<BestMovesAnalysis> FindBestMovesAsync(GameEngine engine, CancellationToken ct = default);

    /// <summary>
    /// Analyze the doubling cube decision for the current position
    /// </summary>
    /// <param name="engine">Game engine with the position to analyze</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Cube decision analysis with recommendations</returns>
    Task<CubeDecision> AnalyzeCubeDecisionAsync(GameEngine engine, CancellationToken ct = default);
}
