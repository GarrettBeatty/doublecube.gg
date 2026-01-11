using Backgammon.Plugins.Abstractions;

namespace Backgammon.Plugins.Registration;

/// <summary>
/// Registry for discovering and creating bots and evaluators.
/// Provides a centralized way to manage all available plugins.
/// </summary>
public interface IPluginRegistry
{
    // Bot operations

    /// <summary>
    /// Get metadata for all registered bots
    /// </summary>
    IReadOnlyList<BotMetadata> GetAvailableBots();

    /// <summary>
    /// Get metadata for a specific bot by ID
    /// </summary>
    BotMetadata? GetBotMetadata(string botId);

    /// <summary>
    /// Create a bot instance by ID
    /// </summary>
    IGameBot CreateBot(string botId);

    /// <summary>
    /// Check if a bot is available (registered and resources available)
    /// </summary>
    bool IsBotAvailable(string botId);

    // Evaluator operations

    /// <summary>
    /// Get metadata for all registered evaluators
    /// </summary>
    IReadOnlyList<EvaluatorMetadata> GetAvailableEvaluators();

    /// <summary>
    /// Get metadata for a specific evaluator by ID
    /// </summary>
    EvaluatorMetadata? GetEvaluatorMetadata(string evaluatorId);

    /// <summary>
    /// Create an evaluator instance by ID
    /// </summary>
    IPositionEvaluator CreateEvaluator(string evaluatorId);

    /// <summary>
    /// Check if an evaluator is available (registered and resources available)
    /// </summary>
    bool IsEvaluatorAvailable(string evaluatorId);
}
