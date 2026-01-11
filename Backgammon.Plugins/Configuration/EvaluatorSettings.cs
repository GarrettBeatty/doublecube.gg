namespace Backgammon.Plugins.Configuration;

/// <summary>
/// Configuration for a specific evaluator
/// </summary>
public class EvaluatorSettings
{
    /// <summary>
    /// Whether this evaluator is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Evaluator-specific options (e.g., ply depth, timeout)
    /// </summary>
    public Dictionary<string, object> Options { get; set; } = new();
}
