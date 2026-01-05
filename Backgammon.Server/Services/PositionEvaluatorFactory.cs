using Backgammon.Analysis;
using Backgammon.Analysis.Configuration;
using Backgammon.Analysis.Gnubg;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Backgammon.Server.Services;

/// <summary>
/// Factory for creating position evaluators based on evaluator type
/// </summary>
public class PositionEvaluatorFactory : IDisposable
{
    private readonly GnubgProcessManager _gnubgProcessManager;
    private readonly GnubgSettings _gnubgSettings;
    private readonly AnalysisSettings _analysisSettings;
    private readonly ILogger<GnubgEvaluator> _gnubgLogger;
    private readonly HeuristicEvaluator _heuristicEvaluator;
    private readonly object _lock = new();
    private GnubgEvaluator? _gnubgEvaluator;
    private bool _disposed;

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

    /// <summary>
    /// Dispose resources
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            _gnubgEvaluator = null;
            _disposed = true;
        }
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
                        _gnubgLogger.LogError(
                            "Gnubg not available at {Path}. Please ensure GNU Backgammon is installed and the path is configured correctly.",
                            _gnubgSettings.ExecutablePath);
                        throw new InvalidOperationException(
                            $"GNU Backgammon evaluator requested but not available at: {_gnubgSettings.ExecutablePath}. " +
                            "Please install GNU Backgammon or use the Heuristic evaluator instead.");
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
