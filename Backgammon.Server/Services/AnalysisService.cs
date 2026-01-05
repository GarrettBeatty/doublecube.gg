using Backgammon.Analysis;
using Backgammon.Analysis.Models;
using Backgammon.Core;
using Backgammon.Server.Models;

namespace Backgammon.Server.Services;

/// <summary>
/// Service for analyzing backgammon positions and moves
/// </summary>
public class AnalysisService
{
    private readonly IPositionEvaluator _evaluator;

    public AnalysisService()
    {
        _evaluator = new HeuristicEvaluator();
    }

    /// <summary>
    /// Evaluate the current position
    /// </summary>
    public PositionEvaluationDto EvaluatePosition(GameEngine engine)
    {
        var evaluation = _evaluator.Evaluate(engine);
        return MapToDto(evaluation);
    }

    /// <summary>
    /// Find the best moves for the current position
    /// </summary>
    public BestMovesAnalysisDto FindBestMoves(GameEngine engine)
    {
        var analysis = _evaluator.FindBestMoves(engine);
        return MapToDto(analysis);
    }

    /// <summary>
    /// Map PositionEvaluation to DTO
    /// </summary>
    private PositionEvaluationDto MapToDto(PositionEvaluation evaluation)
    {
        return new PositionEvaluationDto
        {
            Equity = evaluation.Equity,
            WinProbability = evaluation.WinProbability,
            GammonProbability = evaluation.GammonProbability,
            BackgammonProbability = evaluation.BackgammonProbability,
            Features = MapToDto(evaluation.Features)
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
    private BestMovesAnalysisDto MapToDto(BestMovesAnalysis analysis)
    {
        return new BestMovesAnalysisDto
        {
            InitialEvaluation = MapToDto(analysis.InitialEvaluation),
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
