using Backgammon.Analysis;
using Backgammon.Analysis.Models;
using Backgammon.Core;
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
    public PositionEvaluationDto EvaluatePosition(GameEngine engine, string? evaluatorType = null)
    {
        var evaluator = _evaluatorFactory.GetEvaluator(evaluatorType);
        var evaluation = evaluator.Evaluate(engine);
        return MapToDto(evaluation, evaluator);
    }

    /// <summary>
    /// Find the best moves for the current position
    /// </summary>
    /// <param name="engine">Game engine to analyze</param>
    /// <param name="evaluatorType">Optional evaluator type ("Heuristic" or "Gnubg")</param>
    public BestMovesAnalysisDto FindBestMoves(GameEngine engine, string? evaluatorType = null)
    {
        var evaluator = _evaluatorFactory.GetEvaluator(evaluatorType);
        var analysis = evaluator.FindBestMoves(engine);
        return MapToDto(analysis, evaluator);
    }

    /// <summary>
    /// Map PositionEvaluation to DTO
    /// </summary>
    private PositionEvaluationDto MapToDto(PositionEvaluation evaluation, IPositionEvaluator evaluator)
    {
        // Determine evaluator name from type
        var evaluatorName = evaluator.GetType().Name switch
        {
            "GnubgEvaluator" => "GNU Backgammon",
            "HeuristicEvaluator" => "Heuristic",
            _ => "Unknown"
        };

        return new PositionEvaluationDto
        {
            Equity = evaluation.Equity,
            WinProbability = evaluation.WinProbability,
            GammonProbability = evaluation.GammonProbability,
            BackgammonProbability = evaluation.BackgammonProbability,
            Features = MapToDto(evaluation.Features),
            EvaluatorName = evaluatorName
        };
    }

    /// <summary>
    /// Map PositionFeatures to DTO
    /// </summary>
    private PositionFeaturesDto MapToDto(PositionFeatures features)
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

    /// <summary>
    /// Map MoveSequenceEvaluation to DTO
    /// </summary>
    private MoveSequenceDto MapToDto(MoveSequenceEvaluation sequence)
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
}
