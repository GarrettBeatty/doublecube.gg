using Backgammon.Core;
using Backgammon.Plugins.Abstractions;
using Backgammon.Plugins.Models;
using Microsoft.Extensions.Logging;

namespace Backgammon.Plugins.Base;

/// <summary>
/// Base class for bots that use an evaluator for decision making.
/// Provides common implementation patterns for move selection and cube decisions.
/// </summary>
public abstract class EvaluatorBackedBot : IEvaluatorBackedBot
{
    private readonly ILogger? _logger;

    protected EvaluatorBackedBot(IPositionEvaluator evaluator, ILogger? logger = null)
    {
        Evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        _logger = logger;
    }

    /// <summary>
    /// The evaluator this bot uses for decision making
    /// </summary>
    public IPositionEvaluator Evaluator { get; }

    /// <summary>
    /// Optional override for evaluation plies. When set, configures the evaluator
    /// to use this ply depth instead of its default. Used for difficulty levels.
    /// </summary>
    public int? PliesOverride { get; set; }

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
    /// For abbreviated moves with multiple possible orderings, validates each
    /// alternative against the board state and executes the first valid one.
    /// </summary>
    public virtual async Task<List<Move>> ChooseMovesAsync(
        GameEngine engine,
        CancellationToken ct = default)
    {
        var chosenMoves = new List<Move>();

        _logger?.LogInformation(
            "EvaluatorBackedBot starting. Player: {Player}, RemainingMoves: [{Moves}]",
            engine.CurrentPlayer.Color,
            string.Join(", ", engine.RemainingMoves));

        if (engine.RemainingMoves.Count == 0)
        {
            _logger?.LogWarning("No remaining moves - returning empty list");
            return chosenMoves;
        }

        ct.ThrowIfCancellationRequested();

        // Apply plies override to the evaluator if set
        ApplyPliesOverride();
        if (PliesOverride.HasValue)
        {
            _logger?.LogInformation("Using PliesOverride: {Plies}", PliesOverride.Value);
        }

        // Find the best moves for current position (query once for the full dice roll)
        var analysis = await Evaluator.FindBestMovesAsync(engine, ct);

        _logger?.LogInformation(
            "Analysis returned {TopMovesCount} top moves, BestMove is {BestMoveStatus}",
            analysis.TopMoves.Count,
            analysis.BestMove == null ? "null" : $"{analysis.BestMove.Moves.Count} moves");

        if (analysis.BestMove == null || analysis.BestMove.Moves.Count == 0)
        {
            _logger?.LogWarning("No best move returned - returning empty list");
            return chosenMoves;
        }

        // Get all alternatives (different orderings for abbreviated moves)
        var alternatives = analysis.BestMove.Alternatives;
        if (alternatives.Count == 0)
        {
            alternatives = new List<List<Move>> { analysis.BestMove.Moves };
        }

        _logger?.LogInformation(
            "Best move has {Count} alternatives to try",
            alternatives.Count);

        // Try each alternative until one succeeds
        foreach (var moveSequence in alternatives)
        {
            _logger?.LogInformation(
                "Trying alternative: [{Moves}]",
                string.Join(", ", moveSequence.Select(m => $"{m.From}->{m.To}(die:{m.DieValue})")));

            // Validate all moves before executing any
            bool allValid = ValidateMoveSequence(moveSequence, engine);

            if (!allValid)
            {
                _logger?.LogDebug("Alternative not valid, trying next");
                continue;
            }

            // Execute the valid sequence
            foreach (var move in moveSequence)
            {
                ct.ThrowIfCancellationRequested();

                if (engine.ExecuteMove(move))
                {
                    chosenMoves.Add(move);
                }
                else
                {
                    _logger?.LogWarning(
                        "ExecuteMove unexpectedly FAILED for {From}->{To} (die:{Die})",
                        move.From,
                        move.To,
                        move.DieValue);
                    break;
                }
            }

            // Successfully executed this alternative
            break;
        }

        _logger?.LogInformation(
            "EvaluatorBackedBot finished with {Count} moves",
            chosenMoves.Count);

        return chosenMoves;
    }

    /// <summary>
    /// Decide whether to accept a double using the evaluator's cube analysis.
    /// Returns true if the recommendation is "Take" or "Beaver".
    /// </summary>
    public virtual async Task<bool> ShouldAcceptDoubleAsync(
        GameEngine engine,
        MatchContext matchContext,
        CancellationToken ct = default)
    {
        ApplyPliesOverride();
        var cubeDecision = await Evaluator.AnalyzeCubeDecisionAsync(engine, matchContext, ct);
        return cubeDecision.Recommendation is "Take" or "Beaver";
    }

    /// <summary>
    /// Decide whether to offer a double using the evaluator's cube analysis.
    /// Returns true if the recommendation is "Double".
    /// </summary>
    public virtual async Task<bool> ShouldOfferDoubleAsync(
        GameEngine engine,
        MatchContext matchContext,
        CancellationToken ct = default)
    {
        ApplyPliesOverride();
        var cubeDecision = await Evaluator.AnalyzeCubeDecisionAsync(engine, matchContext, ct);
        return cubeDecision.Recommendation == "Double";
    }

    /// <summary>
    /// Applies the PliesOverride to the evaluator if it supports it.
    /// </summary>
    private void ApplyPliesOverride()
    {
        if (PliesOverride.HasValue && Evaluator is IPliesConfigurable configurable)
        {
            configurable.PliesOverride = PliesOverride;
        }
    }

    /// <summary>
    /// Validates that all moves in a sequence can be executed.
    /// Checks each move against the current valid moves, simulating state changes.
    /// </summary>
    private bool ValidateMoveSequence(List<Move> moves, GameEngine engine)
    {
        // Get a fresh copy of valid moves for checking
        var validMoves = engine.GetValidMoves();

        foreach (var move in moves)
        {
            var matchingMove = validMoves.FirstOrDefault(v =>
                v.From == move.From && v.To == move.To && v.DieValue == move.DieValue);

            if (matchingMove == null)
            {
                _logger?.LogDebug(
                    "Move {From}->{To}(die:{Die}) not in valid moves",
                    move.From,
                    move.To,
                    move.DieValue);
                return false;
            }

            // For multi-move sequences, we can't easily simulate the state change
            // without actually executing. For now, just check the first move is valid.
            // The rest will be validated during execution.
            break;
        }

        return true;
    }
}
