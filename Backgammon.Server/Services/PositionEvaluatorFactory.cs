using Backgammon.Analysis;
using Backgammon.Analysis.Configuration;
using Backgammon.Analysis.Gnubg;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Backgammon.Server.Services;

/// <summary>
/// Factory for creating position evaluators based on evaluator type
/// </summary>
public class PositionEvaluatorFactory
{
    private readonly GnubgProcessManager _gnubgProcessManager;
    private readonly GnubgSettings _gnubgSettings;
    private readonly AnalysisSettings _analysisSettings;
    private readonly ILogger<GnubgEvaluator> _gnubgLogger;
    private readonly HeuristicEvaluator _heuristicEvaluator;
    private GnubgEvaluator? _gnubgEvaluator;
    private readonly object _lock = new();

    public PositionEvaluatorFactory(
        GnubgProcessManager gnubgProcessManager,
        IOptions<GnubgSettings> gnubgSettings,
        IOptions<AnalysisSettings> analysisSettings,
        ILogger<GnubgEvaluator> gnubgLogger)
    {
        _gnubgProcessManager = gnubgProcessManager ?? throw new ArgumentNullException(nameof(gnubgProcessManager));
        _gnubgSettings = gnubgSettings.Value ?? throw new ArgumentNullException(nameof(gnubgSettings));
        _analysisSettings = analysisSettings.Value ?? throw new ArgumentNullException(nameof(analysisSettings));
        _gnubgLogger = gnubgLogger ?? throw new ArgumentNullException(nameof(gnubgLogger));
        _heuristicEvaluator = new HeuristicEvaluator();
    }

    /// <summary>
    /// Get evaluator by type name
    /// </summary>
    /// <param name="evaluatorType">Type of evaluator ("Heuristic" or "Gnubg")</param>
    /// <returns>The requested evaluator, or default evaluator if type not recognized</returns>
    public IPositionEvaluator GetEvaluator(string? evaluatorType = null)
    {
        // If user selection is not allowed, always use default
        if (!_analysisSettings.AllowUserSelection)
        {
            evaluatorType = _analysisSettings.EvaluatorType;
        }

        // Default to configured evaluator if not specified
        evaluatorType ??= _analysisSettings.EvaluatorType;

        if (evaluatorType.Equals("Gnubg", StringComparison.OrdinalIgnoreCase))
        {
            return GetGnubgEvaluator();
        }

        // Default to heuristic for any other value
        return _heuristicEvaluator;
    }

    private IPositionEvaluator GetGnubgEvaluator()
    {
        // Lazy initialization with thread safety
        if (_gnubgEvaluator == null)
        {
            lock (_lock)
            {
                if (_gnubgEvaluator == null)
                {
                    // Check if gnubg is available
                    if (!_gnubgProcessManager.IsAvailableAsync().GetAwaiter().GetResult())
                    {
                        _gnubgLogger.LogWarning(
                            "Gnubg not available at {Path}, falling back to HeuristicEvaluator",
                            _gnubgSettings.ExecutablePath);
                        return _heuristicEvaluator;
                    }

                    _gnubgEvaluator = new GnubgEvaluator(
                        _gnubgProcessManager,
                        _gnubgSettings,
                        msg => _gnubgLogger.LogDebug(msg));
                }
            }
        }

        return _gnubgEvaluator;
    }
}
