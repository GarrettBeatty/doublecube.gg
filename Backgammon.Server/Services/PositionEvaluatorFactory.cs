using Backgammon.Analysis.Configuration;
using Backgammon.Analysis.Evaluators;
using Backgammon.Plugins.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Backgammon.Server.Services;

/// <summary>
/// Factory for creating position evaluators based on evaluator type
/// </summary>
public class PositionEvaluatorFactory : IDisposable
{
    private readonly HttpGnubgEvaluator _httpGnubgEvaluator;
    private readonly AnalysisSettings _analysisSettings;
    private readonly ILogger<PositionEvaluatorFactory> _logger;
    private readonly HeuristicEvaluator _heuristicEvaluator;
    private bool _disposed;

    public PositionEvaluatorFactory(
        HttpGnubgEvaluator httpGnubgEvaluator,
        IOptions<AnalysisSettings> analysisSettings,
        ILogger<PositionEvaluatorFactory> logger)
    {
        _httpGnubgEvaluator = httpGnubgEvaluator ?? throw new ArgumentNullException(nameof(httpGnubgEvaluator));
        _analysisSettings = analysisSettings.Value ?? throw new ArgumentNullException(nameof(analysisSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            _logger.LogDebug("Using HttpGnubgEvaluator for position evaluation");
            return _httpGnubgEvaluator;
        }

        // Default to heuristic for any other value
        _logger.LogDebug("Using HeuristicEvaluator for position evaluation");
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

        _disposed = true;
    }
}
