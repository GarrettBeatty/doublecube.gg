using Backgammon.Core;
using Backgammon.Plugins.Abstractions;
using Backgammon.Plugins.Models;
using Backgammon.Server.Models;
using Microsoft.Extensions.Logging;

namespace Backgammon.Server.Services;

/// <summary>
/// Service for analyzing backgammon positions and moves
/// </summary>
public class AnalysisService
{
    private readonly PositionEvaluatorFactory _evaluatorFactory;
    private readonly ILogger<AnalysisService> _logger;

    public AnalysisService(PositionEvaluatorFactory evaluatorFactory, ILogger<AnalysisService> logger)
    {
        _evaluatorFactory = evaluatorFactory ?? throw new ArgumentNullException(nameof(evaluatorFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Evaluate the current position
    /// </summary>
    /// <param name="engine">Game engine to evaluate</param>
    /// <param name="evaluatorType">Optional evaluator type ("Heuristic" or "Gnubg")</param>
    /// <param name="ct">Cancellation token</param>
    public async Task<PositionEvaluationDto> EvaluatePositionAsync(
        GameEngine engine,
        string? evaluatorType = null,
        CancellationToken ct = default)
    {
        var evaluator = _evaluatorFactory.GetEvaluator(evaluatorType);
        var evaluation = await evaluator.EvaluateAsync(engine, ct);
        return MapToDto(evaluation, evaluator);
    }

    /// <summary>
    /// Find the best moves for the current position
    /// </summary>
    /// <param name="engine">Game engine to analyze</param>
    /// <param name="evaluatorType">Optional evaluator type ("Heuristic" or "Gnubg")</param>
    /// <param name="ct">Cancellation token</param>
    public async Task<BestMovesAnalysisDto> FindBestMovesAsync(
        GameEngine engine,
        string? evaluatorType = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "Finding best moves using {EvaluatorType}. Remaining moves: {Count}",
                evaluatorType ?? "default",
                engine.RemainingMoves.Count);

            var evaluator = _evaluatorFactory.GetEvaluator(evaluatorType);
            var analysis = await evaluator.FindBestMovesAsync(engine, ct);

            _logger.LogInformation("Found {Count} best move sequences", analysis.TopMoves.Count);

            return MapToDto(analysis, evaluator);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Failed to find best moves using {EvaluatorType}",
                evaluatorType ?? "default");
            throw;
        }
    }

    /// <summary>
    /// Map PositionFeatures to DTO
    /// </summary>
    private static PositionFeaturesDto MapToDto(PositionFeatures features)
    {
        return new PositionFeaturesDto
        {
            PipCount = features.PipCount,
            PipDifference = features.PipDifference,
            BlotCount = features.BlotCount,
            BlotExposure = features.BlotExposure,
            CheckersOnBar = features.CheckersOnBar,
            PrimeLength = features.PrimeLength,
            AnchorsInOpponentHome = features.AnchorsInOpponentHome,
            HomeboardCoverage = features.HomeboardCoverage,
            Distribution = features.Distribution,
            IsContact = features.IsContact,
            IsRace = features.IsRace,
            WastedPips = features.WastedPips,
            BearoffEfficiency = features.BearoffEfficiency,
            CheckersBornOff = features.CheckersBornOff
        };
    }

    /// <summary>
    /// Map MoveSequenceEvaluation to DTO
    /// </summary>
    private static MoveSequenceDto MapToDto(MoveSequenceEvaluation sequence)
    {
        return new MoveSequenceDto
        {
            Moves = sequence.Moves.Select(m => new MoveDto
            {
                From = m.From,
                To = m.To,
                DieValue = m.DieValue,
                IsHit = m.IsHit
            }).ToList(),
            Notation = sequence.Notation,
            Equity = sequence.FinalEvaluation.Equity,
            EquityGain = sequence.EquityGain
        };
    }

    /// <summary>
    /// Map PositionEvaluation to DTO
    /// </summary>
    private PositionEvaluationDto MapToDto(PositionEvaluation evaluation, IPositionEvaluator evaluator)
    {
        return new PositionEvaluationDto
        {
            Equity = evaluation.Equity,
            WinProbability = evaluation.WinProbability,
            GammonProbability = evaluation.GammonProbability,
            BackgammonProbability = evaluation.BackgammonProbability,
            Features = MapToDto(evaluation.Features),
            EvaluatorName = evaluator.DisplayName
        };
    }

    /// <summary>
    /// Map BestMovesAnalysis to DTO
    /// </summary>
    private BestMovesAnalysisDto MapToDto(BestMovesAnalysis analysis, IPositionEvaluator evaluator)
    {
        return new BestMovesAnalysisDto
        {
            InitialEvaluation = MapToDto(analysis.InitialEvaluation, evaluator),
            TopMoves = analysis.TopMoves.Select(MapToDto).ToList(),
            TotalSequencesExplored = analysis.TotalSequencesExplored
        };
    }
}
