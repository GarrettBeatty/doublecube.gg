using Backgammon.Core;
using Backgammon.Server.Models;

namespace Backgammon.Server.Services;

/// <summary>
/// Service for analyzing backgammon positions and moves.
/// </summary>
public interface IAnalysisService
{
    /// <summary>
    /// Evaluate the current position.
    /// </summary>
    /// <param name="engine">Game engine to evaluate.</param>
    /// <param name="evaluatorType">Optional evaluator type ("Heuristic" or "Gnubg").</param>
    /// <param name="ct">Cancellation token.</param>
    Task<PositionEvaluationDto> EvaluatePositionAsync(
        GameEngine engine,
        string? evaluatorType = null,
        CancellationToken ct = default);

    /// <summary>
    /// Find the best moves for the current position.
    /// </summary>
    /// <param name="engine">Game engine to analyze.</param>
    /// <param name="evaluatorType">Optional evaluator type ("Heuristic" or "Gnubg").</param>
    /// <param name="ct">Cancellation token.</param>
    Task<BestMovesAnalysisDto> FindBestMovesAsync(
        GameEngine engine,
        string? evaluatorType = null,
        CancellationToken ct = default);
}
