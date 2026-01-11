using Backgammon.Core;
using Backgammon.Plugins.Abstractions;

namespace Backgammon.Plugins.Base;

/// <summary>
/// Base class for bots that use an evaluator for decision making.
/// Provides common implementation patterns for move selection and cube decisions.
/// </summary>
public abstract class EvaluatorBackedBot : IEvaluatorBackedBot
{
    protected EvaluatorBackedBot(IPositionEvaluator evaluator)
    {
        Evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
    }

    /// <summary>
    /// The evaluator this bot uses for decision making
    /// </summary>
    public IPositionEvaluator Evaluator { get; }

    /// <summary>
    /// Unique identifier for this bot type
    /// </summary>
    public abstract string BotId { get; }

    /// <summary>
    /// Human-friendly display name
    /// </summary>
    public abstract string DisplayName { get; }

    /// <summary>
    /// Description of bot's play style
    /// </summary>
    public abstract string Description { get; }

    /// <summary>
    /// Estimated ELO rating for matchmaking hints
    /// </summary>
    public abstract int EstimatedElo { get; }

    /// <summary>
    /// Choose moves using the evaluator's FindBestMovesAsync.
    /// Executes the best move sequence one move at a time.
    /// </summary>
    public virtual async Task<List<Move>> ChooseMovesAsync(
        GameEngine engine,
        CancellationToken ct = default)
    {
        var chosenMoves = new List<Move>();

        while (engine.RemainingMoves.Count > 0)
        {
            ct.ThrowIfCancellationRequested();

            // Find the best moves for current position
            var analysis = await Evaluator.FindBestMovesAsync(engine, ct);

            if (analysis.TopMoves.Count == 0 || analysis.BestMove == null)
            {
                break;
            }

            // Get the best move sequence
            var bestSequence = analysis.BestMove;

            if (bestSequence.Moves.Count == 0)
            {
                break;
            }

            // Execute the first move from the best sequence
            var move = bestSequence.Moves[0];

            if (engine.ExecuteMove(move))
            {
                chosenMoves.Add(move);
            }
            else
            {
                // Move failed - stop trying
                break;
            }
        }

        return chosenMoves;
    }

    /// <summary>
    /// Decide whether to accept a double using the evaluator's cube analysis.
    /// Returns true if the recommendation is "Take" or "Beaver".
    /// </summary>
    public virtual async Task<bool> ShouldAcceptDoubleAsync(
        GameEngine engine,
        CancellationToken ct = default)
    {
        var cubeDecision = await Evaluator.AnalyzeCubeDecisionAsync(engine, ct);
        return cubeDecision.Recommendation is "Take" or "Beaver";
    }

    /// <summary>
    /// Decide whether to offer a double using the evaluator's cube analysis.
    /// Returns true if the recommendation is "Double".
    /// </summary>
    public virtual async Task<bool> ShouldOfferDoubleAsync(
        GameEngine engine,
        CancellationToken ct = default)
    {
        var cubeDecision = await Evaluator.AnalyzeCubeDecisionAsync(engine, ct);
        return cubeDecision.Recommendation == "Double";
    }
}
