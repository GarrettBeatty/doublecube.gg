namespace Backgammon.Analysis.Configuration;

/// <summary>
/// Configuration settings for position analysis
/// </summary>
public class AnalysisSettings
{
    public const string SectionName = "Analysis";

    /// <summary>
    /// Evaluator type to use: "Heuristic" or "Gnubg"
    /// </summary>
    public string EvaluatorType { get; set; } = "Heuristic";

    /// <summary>
    /// Allow users to switch evaluators in the UI (future feature)
    /// </summary>
    public bool AllowUserSelection { get; set; } = false;
}
