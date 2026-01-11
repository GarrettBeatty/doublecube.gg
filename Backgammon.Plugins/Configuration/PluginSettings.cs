namespace Backgammon.Plugins.Configuration;

/// <summary>
/// Configuration for the plugin system
/// </summary>
public class PluginSettings
{
    public const string SectionName = "Plugins";

    /// <summary>
    /// Default bot ID for AI matches when not specified
    /// </summary>
    public string DefaultBotId { get; set; } = "greedy";

    /// <summary>
    /// Default evaluator ID for analysis when not specified
    /// </summary>
    public string DefaultEvaluatorId { get; set; } = "heuristic";

    /// <summary>
    /// Allow users to select bots per-match
    /// </summary>
    public bool AllowBotSelection { get; set; } = true;

    /// <summary>
    /// Allow users to select evaluators for analysis
    /// </summary>
    public bool AllowEvaluatorSelection { get; set; } = true;

    /// <summary>
    /// Bot-specific configurations
    /// </summary>
    public Dictionary<string, BotSettings> Bots { get; set; } = new();

    /// <summary>
    /// Evaluator-specific configurations
    /// </summary>
    public Dictionary<string, EvaluatorSettings> Evaluators { get; set; } = new();
}
