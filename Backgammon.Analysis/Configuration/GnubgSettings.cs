namespace Backgammon.Analysis.Configuration;

/// <summary>
/// Configuration settings for GNU Backgammon (gnubg) integration
/// </summary>
public class GnubgSettings
{
    public const string SectionName = "Gnubg";

    /// <summary>
    /// URL of the gnubg HTTP service. When set, HTTP mode is used instead of local process.
    /// Example: "http://gnubg-service" or "http://localhost:8080"
    /// </summary>
    public string? ServiceUrl { get; set; }

    /// <summary>
    /// Full path to gnubg executable. Only used when ServiceUrl is not set.
    /// Default is "gnubg" (assumes it's in PATH)
    /// </summary>
    public string ExecutablePath { get; set; } = "gnubg";

    /// <summary>
    /// Timeout for gnubg operations in milliseconds
    /// </summary>
    public int TimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Enable verbose gnubg output for debugging
    /// </summary>
    public bool VerboseLogging { get; set; } = false;

    /// <summary>
    /// Number of evaluation plies (depth). Higher = slower but more accurate.
    /// 0 = 0-ply (fast), 1 = 1-ply, 2 = 2-ply (standard), 3+ = very slow
    /// </summary>
    public int EvaluationPlies { get; set; } = 2;

    /// <summary>
    /// Enable gnubg's neural network evaluation (recommended)
    /// </summary>
    public bool UseNeuralNet { get; set; } = true;
}
