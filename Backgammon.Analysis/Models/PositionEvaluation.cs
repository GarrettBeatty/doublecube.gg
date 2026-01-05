namespace Backgammon.Analysis.Models;

/// <summary>
/// Complete evaluation of a backgammon position.
/// Includes equity estimate and win probabilities.
/// </summary>
public class PositionEvaluation
{
    /// <summary>
    /// Equity estimate from current player's perspective.
    /// Range: -3.0 to +3.0 (accounts for gammons and backgammons).
    /// Positive = winning, Negative = losing.
    /// </summary>
    public double Equity { get; set; }

    /// <summary>
    /// Probability of winning the game (0.0 to 1.0)
    /// </summary>
    public double WinProbability { get; set; }

    /// <summary>
    /// Probability of winning a gammon (0.0 to 1.0)
    /// </summary>
    public double GammonProbability { get; set; }

    /// <summary>
    /// Probability of winning a backgammon (0.0 to 1.0)
    /// </summary>
    public double BackgammonProbability { get; set; }

    /// <summary>
    /// Extracted position features used to calculate this evaluation
    /// </summary>
    public PositionFeatures Features { get; set; } = new();
}
