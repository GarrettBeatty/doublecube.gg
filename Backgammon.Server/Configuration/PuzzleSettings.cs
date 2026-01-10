namespace Backgammon.Server.Configuration;

/// <summary>
/// Configuration settings for daily puzzle generation
/// </summary>
public class PuzzleSettings
{
    public const string SectionName = "Puzzle";

    /// <summary>
    /// Evaluator type to use for puzzle solution analysis: "Heuristic" or "Gnubg"
    /// </summary>
    public string EvaluatorType { get; set; } = "Gnubg";

    /// <summary>
    /// Number of gnubg evaluation plies (0-3). Higher = more accurate but slower.
    /// Only used when EvaluatorType is "Gnubg".
    /// </summary>
    public int GnubgPlies { get; set; } = 2;

    /// <summary>
    /// Equity tolerance for accepting alternative moves as correct.
    /// Moves within this equity difference from optimal are accepted.
    /// </summary>
    public double EquityTolerance { get; set; } = 0.02;

    /// <summary>
    /// Time of day (UTC) when the daily puzzle is generated.
    /// </summary>
    public TimeSpan GenerationTimeUtc { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Whether to generate puzzles automatically at the scheduled time.
    /// Set to false to disable automatic generation (useful for testing).
    /// </summary>
    public bool EnableAutomaticGeneration { get; set; } = true;
}
