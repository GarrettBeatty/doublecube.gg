using Backgammon.Analysis.Configuration;
using Backgammon.Analysis.Gnubg;
using Backgammon.Core;
using Backgammon.Plugins.Abstractions;
using Backgammon.Plugins.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Backgammon.Analysis.Evaluators;

/// <summary>
/// Position evaluator using GNU Backgammon (gnubg)
/// </summary>
public class GnubgEvaluator : IPositionEvaluator
{
    private readonly GnubgProcessManager _processManager;
    private readonly GnubgSettings _settings;
    private readonly ILogger<GnubgEvaluator>? _logger;

    public GnubgEvaluator(
        GnubgProcessManager processManager,
        IOptions<GnubgSettings> settings,
        ILogger<GnubgEvaluator>? logger = null)
    {
        _processManager = processManager ?? throw new ArgumentNullException(nameof(processManager));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger;
    }

    /// <summary>
    /// Unique identifier for this evaluator
    /// </summary>
    public string EvaluatorId => "gnubg";

    /// <summary>
    /// Human-friendly display name
    /// </summary>
    public string DisplayName => "GNU Backgammon";

    /// <summary>
    /// This evaluator requires the gnubg executable
    /// </summary>
    public bool RequiresExternalResources => true;

    /// <summary>
    /// Evaluate a position using gnubg
    /// </summary>
    public async Task<PositionEvaluation> EvaluateAsync(GameEngine engine, CancellationToken ct = default)
    {
        try
        {
            // Export position to SGF
            var sgf = SgfSerializer.ExportPosition(engine);
            _logger?.LogDebug("Evaluating position with gnubg. SGF: {Sgf}", sgf);

            // Build gnubg commands
            var commands = GnubgCommandBuilder.BuildEvaluationCommand(_settings);

            // Execute gnubg with SGF file
            var output = await _processManager.ExecuteWithSgfFileAsync(sgf, commands, ct);

            // Parse output
            var evaluation = GnubgOutputParser.ParseEvaluation(output);

            _logger?.LogDebug("Gnubg evaluation complete. Equity: {Equity}", evaluation.Equity);

            return evaluation;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger?.LogError(ex, "Failed to evaluate position with gnubg");
            throw new Exception("Gnubg evaluation failed. See inner exception for details.", ex);
        }
    }

    /// <summary>
    /// Find best moves using gnubg
    /// </summary>
    public async Task<BestMovesAnalysis> FindBestMovesAsync(GameEngine engine, CancellationToken ct = default)
    {
        try
        {
            // Export position to SGF
            var sgf = SgfSerializer.ExportPosition(engine);
            _logger?.LogDebug("Finding best moves with gnubg. SGF: {Sgf}", sgf);

            // Get initial evaluation
            var initialEvaluation = await EvaluateAsync(engine, ct);

            // Build gnubg hint commands
            var commands = GnubgCommandBuilder.BuildHintCommand(_settings);

            // Execute gnubg with SGF file
            var output = await _processManager.ExecuteWithSgfFileAsync(sgf, commands, ct);

            _logger?.LogDebug("Gnubg hint output:\n{Output}", output);

            // Parse move analysis
            var moveAnalyses = GnubgOutputParser.ParseMoveAnalysis(output);

            _logger?.LogDebug("Parsed {Count} move analyses from gnubg output", moveAnalyses.Count);

            // Convert to BestMovesAnalysis format
            var topMoves = new List<MoveSequenceEvaluation>();

            foreach (var moveAnalysis in moveAnalyses.Take(5))
            {
                // Parse move notation into Move objects
                // Pass ORIGINAL dice (not RemainingMoves which may be partially consumed)
                // so parser can expand abbreviated moves (e.g., "12/5" with dice 1,6)
                var availableDice = engine.Dice.GetMoves();
                var moves = GnubgOutputParser.ParseMoveNotation(
                    moveAnalysis.Notation,
                    engine.CurrentPlayer.Color,
                    availableDice);

                // Create evaluation for this move sequence
                var moveEvaluation = new PositionEvaluation
                {
                    Equity = moveAnalysis.Equity,
                    Features = new PositionFeatures()
                };

                var sequence = new MoveSequenceEvaluation
                {
                    Moves = moves,
                    FinalEvaluation = moveEvaluation,
                    EquityGain = moveAnalysis.Equity - initialEvaluation.Equity
                };

                topMoves.Add(sequence);
            }

            _logger?.LogDebug("Gnubg found {Count} best moves", topMoves.Count);

            return new BestMovesAnalysis
            {
                InitialEvaluation = initialEvaluation,
                TopMoves = topMoves,
                TotalSequencesExplored = moveAnalyses.Count
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger?.LogError(ex, "Failed to find best moves with gnubg");
            throw new Exception("Gnubg move analysis failed. See inner exception for details.", ex);
        }
    }

    /// <summary>
    /// Analyze the doubling cube decision for the current position
    /// </summary>
    public async Task<CubeDecision> AnalyzeCubeDecisionAsync(GameEngine engine, MatchContext matchContext, CancellationToken ct = default)
    {
        try
        {
            // Export position to SGF
            var sgf = SgfSerializer.ExportPosition(engine);
            _logger?.LogDebug(
                "Evaluating cube decision with gnubg. SGF: {Sgf}, Match: {Target}-point, Score: {P1}-{P2}, Crawford: {Crawford}",
                sgf,
                matchContext.TargetScore,
                matchContext.Player1Score,
                matchContext.Player2Score,
                matchContext.IsCrawfordGame);

            // Build gnubg commands - start with match context, then cube analysis
            var commands = new List<string>();
            commands.AddRange(GnubgCommandBuilder.BuildMatchContextCommands(matchContext));
            commands.AddRange(GnubgCommandBuilder.BuildCubeCommand(_settings));

            // Execute gnubg with SGF file
            var output = await _processManager.ExecuteWithSgfFileAsync(sgf, commands, ct);

            // Parse cube decision
            var decision = GnubgOutputParser.ParseCubeDecision(output);

            _logger?.LogDebug("Gnubg cube decision: {Recommendation}", decision.Recommendation);

            return decision;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger?.LogError(ex, "Failed to evaluate cube decision with gnubg");
            throw new Exception("Gnubg cube decision analysis failed. See inner exception for details.", ex);
        }
    }
}
