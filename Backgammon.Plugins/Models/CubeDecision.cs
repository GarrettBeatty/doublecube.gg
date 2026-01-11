namespace Backgammon.Plugins.Models;

/// <summary>
/// Represents a doubling cube decision analysis
/// </summary>
public class CubeDecision
{
    /// <summary>
    /// Equity if no double is offered
    /// </summary>
    public double NoDoubleEquity { get; set; }

    /// <summary>
    /// Equity if double is offered and opponent takes
    /// </summary>
    public double DoubleTakeEquity { get; set; }

    /// <summary>
    /// Equity if double is offered and opponent passes
    /// </summary>
    public double DoublePassEquity { get; set; }

    /// <summary>
    /// Recommended action: "NoDouble", "Double", "TooGood", "Take", "Pass"
    /// </summary>
    public string Recommendation { get; set; } = string.Empty;

    /// <summary>
    /// Probability that the opponent should take if doubled (0.0-1.0)
    /// </summary>
    public double TakePoint { get; set; }

    /// <summary>
    /// Additional analysis or explanation from the evaluator
    /// </summary>
    public string? Details { get; set; }
}
