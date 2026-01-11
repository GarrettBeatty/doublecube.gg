using Backgammon.Core;
using Backgammon.Plugins.Abstractions;
using Backgammon.Plugins.Models;

namespace Backgammon.Analysis.Evaluators;

/// <summary>
/// Heuristic-based position evaluator for backgammon.
/// Uses hand-crafted features and weights to estimate position equity.
/// </summary>
public class HeuristicEvaluator : IPositionEvaluator
{
    /// <summary>
    /// Unique identifier for this evaluator
    /// </summary>
    public string EvaluatorId => "heuristic";

    /// <summary>
    /// Human-friendly display name
    /// </summary>
    public string DisplayName => "Heuristic Evaluator";

    /// <summary>
    /// This evaluator does not require external resources
    /// </summary>
    public bool RequiresExternalResources => false;

    /// <summary>
    /// Evaluate the current position
    /// </summary>
    public Task<PositionEvaluation> EvaluateAsync(GameEngine engine, CancellationToken ct = default)
    {
        var features = ExtractFeatures(engine);
        var equity = CalculateEquity(features, engine);

        return Task.FromResult(new PositionEvaluation
        {
            Equity = equity,
            WinProbability = EstimateWinProbability(equity),
            GammonProbability = EstimateGammonProbability(features, equity),
            BackgammonProbability = EstimateBackgammonProbability(features),
            Features = features
        });
    }

    /// <summary>
    /// Find the best move sequences
    /// </summary>
    public Task<BestMovesAnalysis> FindBestMovesAsync(GameEngine engine, CancellationToken ct = default)
    {
        if (engine.RemainingMoves.Count == 0)
        {
            return Task.FromResult(new BestMovesAnalysis
            {
                InitialEvaluation = Evaluate(engine),
                TopMoves = new List<MoveSequenceEvaluation>(),
                TotalSequencesExplored = 0
            });
        }

        var initialEval = Evaluate(engine);
        var allSequences = new List<MoveSequenceEvaluation>();

        // Explore all possible move sequences
        ExploreAllSequences(engine, new List<Move>(), allSequences, 0);

        // Sort by equity (best first)
        var sorted = allSequences.OrderByDescending(s => s.FinalEvaluation.Equity).ToList();

        // Deduplicate by normalized notation (same moves, different order = same sequence)
        var deduplicated = sorted
            .GroupBy(s => s.NormalizedNotation)
            .Select(g => g.First())
            .ToList();

        return Task.FromResult(new BestMovesAnalysis
        {
            InitialEvaluation = initialEval,
            TopMoves = deduplicated.Take(5).ToList(),
            WorstValidMove = deduplicated.LastOrDefault(),
            TotalSequencesExplored = allSequences.Count
        });
    }

    /// <summary>
    /// Analyze the doubling cube decision for the current position
    /// </summary>
    public Task<CubeDecision> AnalyzeCubeDecisionAsync(GameEngine engine, CancellationToken ct = default)
    {
        var evaluation = Evaluate(engine);
        var cubeValue = engine.DoublingCube.Value;
        var currentPlayer = engine.CurrentPlayer.Color;
        var cubeOwner = engine.DoublingCube.Owner;

        // Check if player can legally double
        bool canDouble = engine.DoublingCube.CanDouble(currentPlayer);

        // Calculate equities for different scenarios
        double noDoubleEquity = evaluation.Equity * cubeValue;
        double doubleTakeEquity = evaluation.Equity * (cubeValue * 2);
        double doublePassEquity = cubeValue; // Win current cube value

        // Calculate take point (opponent's perspective)
        // Opponent should take if their equity after doubling > losing the current stake
        // Take point ≈ 25% for money games (standard theory)
        double takePoint = CalculateTakePoint(evaluation, engine);

        // Determine recommendation based on equity thresholds
        string recommendation = DetermineRecommendation(
            evaluation,
            canDouble,
            cubeOwner,
            currentPlayer,
            takePoint);

        // Build detailed explanation
        string details = BuildCubeDecisionDetails(
            evaluation,
            canDouble,
            cubeOwner,
            currentPlayer,
            cubeValue,
            takePoint);

        return Task.FromResult(new CubeDecision
        {
            NoDoubleEquity = noDoubleEquity,
            DoubleTakeEquity = doubleTakeEquity,
            DoublePassEquity = doublePassEquity,
            Recommendation = recommendation,
            TakePoint = takePoint,
            Details = details
        });
    }

    /// <summary>
    /// Synchronous evaluation helper for internal use
    /// </summary>
    private PositionEvaluation Evaluate(GameEngine engine)
    {
        var features = ExtractFeatures(engine);
        var equity = CalculateEquity(features, engine);

        return new PositionEvaluation
        {
            Equity = equity,
            WinProbability = EstimateWinProbability(equity),
            GammonProbability = EstimateGammonProbability(features, equity),
            BackgammonProbability = EstimateBackgammonProbability(features),
            Features = features
        };
    }

    /// <summary>
    /// Recursively explore all possible move sequences using backtracking
    /// </summary>
    private void ExploreAllSequences(
        GameEngine engine,
        List<Move> currentSequence,
        List<MoveSequenceEvaluation> results,
        int depth)
    {
        // Safety limit to prevent infinite recursion
        if (depth > 20)
        {
            return;
        }

        var validMoves = engine.GetValidMoves();

        // Terminal state - no more moves possible
        if (validMoves.Count == 0 || engine.RemainingMoves.Count == 0)
        {
            var finalEval = Evaluate(engine);
            results.Add(new MoveSequenceEvaluation
            {
                Moves = new List<Move>(currentSequence),
                FinalEvaluation = finalEval,
                EquityGain = finalEval.Equity
            });
            return;
        }

        // Try each valid move
        foreach (var move in validMoves)
        {
            // Execute move
            engine.ExecuteMove(move);
            currentSequence.Add(move);

            // Recurse
            ExploreAllSequences(engine, currentSequence, results, depth + 1);

            // Undo move (backtrack)
            currentSequence.RemoveAt(currentSequence.Count - 1);
            engine.UndoLastMove();
        }
    }

    /// <summary>
    /// Extract all features from the position
    /// </summary>
    private PositionFeatures ExtractFeatures(GameEngine engine)
    {
        var features = new PositionFeatures();

        // Calculate pip counts
        features.PipCount = CalculatePipCount(engine, engine.CurrentPlayer.Color);
        int opponentPips = CalculatePipCount(engine, engine.GetOpponent().Color);
        features.PipDifference = opponentPips - features.PipCount;

        // Detect position type
        features.IsContact = IsContactPosition(engine);
        features.IsRace = !features.IsContact;

        // Count checkers
        features.CheckersOnBar = engine.CurrentPlayer.CheckersOnBar;
        features.CheckersBornOff = engine.CurrentPlayer.CheckersBornOff;

        // Tactical features
        features.BlotCount = CountBlots(engine);
        features.BlotExposure = CalculateBlotExposure(engine);

        // Strategic features
        features.PrimeLength = CalculateLongestPrime(engine);
        features.AnchorsInOpponentHome = CountAnchorsInOpponentHome(engine);
        features.HomeboardCoverage = CountHomeboardPoints(engine);

        // Distribution
        features.Distribution = CalculateDistribution(engine);
        features.WastedPips = CalculateWastedPips(engine);

        // Bearoff efficiency (only relevant if in bearoff)
        if (engine.Board.AreAllCheckersInHomeBoard(engine.CurrentPlayer, engine.CurrentPlayer.CheckersOnBar))
        {
            features.BearoffEfficiency = CalculateBearoffEfficiency(engine);
        }

        return features;
    }

    /// <summary>
    /// Calculate equity based on extracted features
    /// </summary>
    private double CalculateEquity(PositionFeatures features, GameEngine engine)
    {
        // Racing position - use simplified evaluation
        if (features.IsRace)
        {
            return EvaluateRace(features);
        }

        // Contact position - full evaluation
        double score = 0.0;

        // Pip count (each pip worth ~1.5% equity)
        score += features.PipDifference * 0.015;

        // Blot exposure (very dangerous)
        score -= features.BlotExposure * 0.08;

        // Checkers on bar (extremely bad)
        score -= features.CheckersOnBar * 0.30;

        // Prime strength (valuable for blocking)
        score += features.PrimeLength * 0.10;

        // Anchors in opponent home (good for hitting)
        score += features.AnchorsInOpponentHome * 0.15;

        // Homeboard coverage (important for safe bearing off)
        score += features.HomeboardCoverage * 0.05;

        // Distribution quality
        score += features.Distribution * 0.20;

        // Wasted pips penalty
        score -= features.WastedPips * 0.01;

        // Checkers borne off (significant advantage)
        score += features.CheckersBornOff * 0.10;

        // Clamp to reasonable range
        return Math.Clamp(score, -3.0, 3.0);
    }

    /// <summary>
    /// Evaluate a pure racing position
    /// </summary>
    private double EvaluateRace(PositionFeatures features)
    {
        // Simplified Thorp count formula
        double pipDiff = features.PipDifference;
        double wastedPenalty = features.WastedPips * 0.5;
        double bearoffBonus = features.CheckersBornOff * 0.15;

        double equity = (pipDiff * 0.02) + bearoffBonus - (wastedPenalty * 0.01);

        return Math.Clamp(equity, -3.0, 3.0);
    }

    /// <summary>
    /// Calculate pip count for a player
    /// </summary>
    private int CalculatePipCount(GameEngine engine, CheckerColor color)
    {
        int pips = 0;

        for (int point = 1; point <= 24; point++)
        {
            var boardPoint = engine.Board.GetPoint(point);
            if (boardPoint.Color == color && boardPoint.Count > 0)
            {
                if (color == CheckerColor.White)
                {
                    pips += boardPoint.Count * point; // White moves 24→1
                }
                else
                {
                    pips += boardPoint.Count * (25 - point); // Red moves 1→24
                }
            }
        }

        // Checkers on bar count as 25 pips
        int checkersOnBar = color == CheckerColor.White
            ? engine.WhitePlayer.CheckersOnBar
            : engine.RedPlayer.CheckersOnBar;
        pips += checkersOnBar * 25;

        return pips;
    }

    /// <summary>
    /// Determine if this is a contact position
    /// </summary>
    private bool IsContactPosition(GameEngine engine)
    {
        int whiteHighest = GetHighestChecker(engine, CheckerColor.White);
        int redLowest = GetLowestChecker(engine, CheckerColor.Red);

        // Contact if White's furthest checker is ahead of Red's furthest
        return whiteHighest >= redLowest;
    }

    private int GetHighestChecker(GameEngine engine, CheckerColor color)
    {
        if (color == CheckerColor.White)
        {
            for (int i = 24; i >= 1; i--)
            {
                if (engine.Board.GetPoint(i).Color == color)
                {
                    return i;
                }
            }
        }
        else
        {
            for (int i = 1; i <= 24; i++)
            {
                if (engine.Board.GetPoint(i).Color == color)
                {
                    return i;
                }
            }
        }

        return 0;
    }

    private int GetLowestChecker(GameEngine engine, CheckerColor color)
    {
        if (color == CheckerColor.White)
        {
            for (int i = 1; i <= 24; i++)
            {
                if (engine.Board.GetPoint(i).Color == color)
                {
                    return i;
                }
            }
        }
        else
        {
            for (int i = 24; i >= 1; i--)
            {
                if (engine.Board.GetPoint(i).Color == color)
                {
                    return i;
                }
            }
        }

        return 25;
    }

    private int CountBlots(GameEngine engine)
    {
        int count = 0;
        for (int i = 1; i <= 24; i++)
        {
            var point = engine.Board.GetPoint(i);
            if (point.Color == engine.CurrentPlayer.Color && point.Count == 1)
            {
                count++;
            }
        }

        return count;
    }

    private int CalculateBlotExposure(GameEngine engine)
    {
        // TODO: Calculate actual exposure based on opponent's position
        // For now, simple count
        return CountBlots(engine);
    }

    private int CalculateLongestPrime(GameEngine engine)
    {
        int longest = 0;
        int current = 0;

        for (int i = 1; i <= 24; i++)
        {
            var point = engine.Board.GetPoint(i);
            if (point.Color == engine.CurrentPlayer.Color && point.Count >= 2)
            {
                current++;
                longest = Math.Max(longest, current);
            }
            else
            {
                current = 0;
            }
        }

        return longest;
    }

    private int CountAnchorsInOpponentHome(GameEngine engine)
    {
        var (homeStart, homeEnd) = engine.GetOpponent().GetHomeBoardRange();
        int count = 0;

        for (int i = homeStart; i <= homeEnd; i++)
        {
            var point = engine.Board.GetPoint(i);
            if (point.Color == engine.CurrentPlayer.Color && point.Count >= 2)
            {
                count++;
            }
        }

        return count;
    }

    private int CountHomeboardPoints(GameEngine engine)
    {
        var (homeStart, homeEnd) = engine.CurrentPlayer.GetHomeBoardRange();
        int count = 0;

        for (int i = homeStart; i <= homeEnd; i++)
        {
            var point = engine.Board.GetPoint(i);
            if (point.Color == engine.CurrentPlayer.Color && point.Count > 0)
            {
                count++;
            }
        }

        return count;
    }

    private double CalculateDistribution(GameEngine engine)
    {
        // Simple distribution metric: penalize stacking
        int totalCheckers = 0;
        int pointsWithCheckers = 0;

        for (int i = 1; i <= 24; i++)
        {
            var point = engine.Board.GetPoint(i);
            if (point.Color == engine.CurrentPlayer.Color)
            {
                totalCheckers += point.Count;
                pointsWithCheckers++;
            }
        }

        if (totalCheckers == 0)
        {
            return 1.0;
        }

        // Good distribution = checkers spread across points
        double avgPerPoint = (double)totalCheckers / Math.Max(1, pointsWithCheckers);
        return 1.0 / avgPerPoint; // Lower avg per point = better distribution
    }

    private int CalculateWastedPips(GameEngine engine)
    {
        int wasted = 0;
        for (int i = 1; i <= 24; i++)
        {
            var point = engine.Board.GetPoint(i);
            if (point.Color == engine.CurrentPlayer.Color && point.Count > 3)
            {
                // Penalize excessive stacking
                wasted += point.Count - 3;
            }
        }

        return wasted;
    }

    private double CalculateBearoffEfficiency(GameEngine engine)
    {
        // TODO: Implement proper bearoff efficiency calculation
        return 0.5;
    }

    /// <summary>
    /// Calculate the take point - minimum win probability where opponent should accept double
    /// </summary>
    private double CalculateTakePoint(PositionEvaluation evaluation, GameEngine engine)
    {
        // Standard take point in money games is around 25%
        // This can be adjusted based on gammon/backgammon threats

        double baseTakePoint = 0.25;

        // Adjust for gammon threats
        // If there's significant gammon risk, opponent needs better equity to take
        if (evaluation.GammonProbability > 0.15)
        {
            baseTakePoint += evaluation.GammonProbability * 0.1;
        }

        // Adjust for backgammon threats (rare but impactful)
        if (evaluation.BackgammonProbability > 0.05)
        {
            baseTakePoint += evaluation.BackgammonProbability * 0.2;
        }

        return Math.Clamp(baseTakePoint, 0.20, 0.35);
    }

    /// <summary>
    /// Determine the recommended cube action
    /// </summary>
    private string DetermineRecommendation(
        PositionEvaluation evaluation,
        bool canDouble,
        CheckerColor? cubeOwner,
        CheckerColor currentPlayer,
        double takePoint)
    {
        // If player can't double (opponent owns cube), analyze if they should take/pass
        if (!canDouble)
        {
            // Player is on the receiving end of a potential double
            double opponentWinProb = 1.0 - evaluation.WinProbability;
            return opponentWinProb >= takePoint ? "Take" : "Pass";
        }

        // Player owns cube or it's centered - should they double?
        double winProb = evaluation.WinProbability;
        double equity = evaluation.Equity;

        // Too good to double threshold (opponent would pass, but we can win more by playing on)
        // This typically happens when win prob > 85-90% and gammon chances are high
        if (winProb > 0.85 && evaluation.GammonProbability > 0.20)
        {
            return "TooGood"; // Too good to double - play on for gammon
        }

        // Minimum double point - equity must be positive and win prob > ~55%
        if (winProb >= 0.68)
        {
            return "Double"; // Clear double
        }

        // Borderline doubling window (55-68% win probability)
        if (winProb >= 0.55)
        {
            // Consider position volatility, gammon chances, etc.
            if (evaluation.GammonProbability > 0.10)
            {
                return "Double"; // Double with gammon threat
            }

            return "Double/NoDouble"; // Borderline - could go either way
        }

        // Not good enough to double yet
        return "NoDouble";
    }

    /// <summary>
    /// Build detailed explanation of cube decision
    /// </summary>
    private string BuildCubeDecisionDetails(
        PositionEvaluation evaluation,
        bool canDouble,
        CheckerColor? cubeOwner,
        CheckerColor currentPlayer,
        int cubeValue,
        double takePoint)
    {
        var details = new System.Text.StringBuilder();

        details.AppendLine($"Cube Value: {cubeValue}");
        details.AppendLine($"Cube Owner: {cubeOwner?.ToString() ?? "Centered"}");
        details.AppendLine($"Can Double: {canDouble}");
        details.AppendLine();
        details.AppendLine($"Win Probability: {evaluation.WinProbability:P1}");
        details.AppendLine($"Gammon Probability: {evaluation.GammonProbability:P1}");
        details.AppendLine($"Backgammon Probability: {evaluation.BackgammonProbability:P1}");
        details.AppendLine();
        details.AppendLine($"Position Equity: {evaluation.Equity:F3}");
        details.AppendLine($"Take Point: {takePoint:P1}");

        if (!canDouble)
        {
            double opponentWinProb = 1.0 - evaluation.WinProbability;
            details.AppendLine();
            details.AppendLine($"Opponent Win Probability: {opponentWinProb:P1}");
            details.AppendLine($"Should Take: {(opponentWinProb >= takePoint ? "Yes" : "No")}");
        }
        else
        {
            details.AppendLine();
            if (evaluation.WinProbability >= 0.68)
            {
                details.AppendLine("Strong position - clear double");
            }
            else if (evaluation.WinProbability >= 0.55)
            {
                details.AppendLine("Borderline doubling position");
            }
            else
            {
                details.AppendLine("Not strong enough to double yet");
            }
        }

        return details.ToString();
    }

    private double EstimateWinProbability(double equity)
    {
        // Simple sigmoid-like conversion from equity to win probability
        // Equity of 0 = 50% win chance
        // Equity of +1.0 = ~75% win chance
        // Equity of -1.0 = ~25% win chance
        return 0.5 + (equity * 0.25);
    }

    private double EstimateGammonProbability(PositionFeatures features, double equity)
    {
        if (equity < 0)
        {
            return 0.0; // Losing, no gammon chance
        }

        // Higher equity and opponent with no borne off = higher gammon chance
        double baseChance = Math.Min(equity * 0.1, 0.3);

        if (features.CheckersBornOff > 5 && features.PipDifference > 50)
        {
            return Math.Min(baseChance * 2, 0.5);
        }

        return baseChance;
    }

    private double EstimateBackgammonProbability(PositionFeatures features)
    {
        // Very rare, only with huge advantage
        if (features.PipDifference > 100 && features.CheckersBornOff > 10)
        {
            return 0.05;
        }

        return 0.0;
    }
}
