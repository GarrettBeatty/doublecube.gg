using Backgammon.Analysis.Configuration;
using Backgammon.Analysis.Models;
using Backgammon.Core;

namespace Backgammon.Analysis.Gnubg;

/// <summary>
/// Position evaluator using GNU Backgammon (gnubg)
/// </summary>
public class GnubgEvaluator : IPositionEvaluator
{
    private readonly GnubgProcessManager _processManager;
    private readonly GnubgSettings _settings;
    private readonly Action<string>? _logger;

    public GnubgEvaluator(
        GnubgProcessManager processManager,
        GnubgSettings settings,
        Action<string>? logger = null)
    {
        _processManager = processManager ?? throw new ArgumentNullException(nameof(processManager));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger;
    }

    /// <summary>
    /// Evaluate a position using gnubg
    /// </summary>
    public PositionEvaluation Evaluate(GameEngine engine)
    {
        return EvaluateAsync(engine).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Find best moves using gnubg
    /// </summary>
    public BestMovesAnalysis FindBestMoves(GameEngine engine)
    {
        return FindBestMovesAsync(engine).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Analyze the doubling cube decision for the current position
    /// </summary>
    public CubeDecision AnalyzeCubeDecision(GameEngine engine)
    {
        return AnalyzeCubeDecisionAsync(engine).GetAwaiter().GetResult();
    }

    private async Task<PositionEvaluation> EvaluateAsync(GameEngine engine)
    {
        try
        {
            // Export position to SGF
            var sgf = SgfSerializer.ExportPosition(engine);
            _logger?.Invoke($"Evaluating position with gnubg. SGF: {sgf}");

            // Build gnubg commands
            var commands = GnubgCommandBuilder.BuildEvaluationCommand(sgf, _settings);

            // Execute gnubg
            var output = await _processManager.ExecuteCommandAsync(commands, CancellationToken.None);

            // Parse output
            var evaluation = GnubgOutputParser.ParseEvaluation(output);

            _logger?.Invoke($"Gnubg evaluation complete. Equity: {evaluation.Equity}");

            return evaluation;
        }
        catch (Exception ex)
        {
            _logger?.Invoke($"Failed to evaluate position with gnubg: {ex.Message}");
            throw new Exception("Gnubg evaluation failed. See inner exception for details.", ex);
        }
    }

    private async Task<BestMovesAnalysis> FindBestMovesAsync(GameEngine engine)
    {
        try
        {
            // Export position to SGF
            var sgf = SgfSerializer.ExportPosition(engine);
            _logger?.Invoke($"Finding best moves with gnubg. SGF: {sgf}");

            // Get initial evaluation
            var initialEvaluation = await EvaluateAsync(engine);

            // Build gnubg hint commands
            var commands = GnubgCommandBuilder.BuildHintCommand(sgf, _settings);

            // Execute gnubg
            var output = await _processManager.ExecuteCommandAsync(commands, CancellationToken.None);

            // Parse move analysis
            var moveAnalyses = GnubgOutputParser.ParseMoveAnalysis(output);

            // Convert to BestMovesAnalysis format
            var topMoves = new List<MoveSequenceEvaluation>();

            foreach (var moveAnalysis in moveAnalyses.Take(5))
            {
                // Parse move notation into Move objects
                var moves = GnubgOutputParser.ParseMoveNotation(moveAnalysis.Notation, engine.CurrentPlayer.Color);

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

            _logger?.Invoke($"Gnubg found {topMoves.Count} best moves");

            return new BestMovesAnalysis
            {
                InitialEvaluation = initialEvaluation,
                TopMoves = topMoves,
                TotalSequencesExplored = moveAnalyses.Count
            };
        }
        catch (Exception ex)
        {
            _logger?.Invoke($"Failed to find best moves with gnubg: {ex.Message}");
            throw new Exception("Gnubg move analysis failed. See inner exception for details.", ex);
        }
    }

    private async Task<CubeDecision> AnalyzeCubeDecisionAsync(GameEngine engine)
    {
        try
        {
            // Export position to SGF
            var sgf = SgfSerializer.ExportPosition(engine);
            _logger?.Invoke($"Evaluating cube decision with gnubg. SGF: {sgf}");

            // Build gnubg cube commands
            var commands = GnubgCommandBuilder.BuildCubeCommand(sgf, _settings);

            // Execute gnubg
            var output = await _processManager.ExecuteCommandAsync(commands, CancellationToken.None);

            // Parse cube decision
            var decision = GnubgOutputParser.ParseCubeDecision(output);

            _logger?.Invoke($"Gnubg cube decision: {decision.Recommendation}");

            return decision;
        }
        catch (Exception ex)
        {
            _logger?.Invoke($"Failed to evaluate cube decision with gnubg: {ex.Message}");
            throw new Exception("Gnubg cube decision analysis failed. See inner exception for details.", ex);
        }
    }
}
