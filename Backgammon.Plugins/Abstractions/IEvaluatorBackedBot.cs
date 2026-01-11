namespace Backgammon.Plugins.Abstractions;

/// <summary>
/// A bot that uses an evaluator for move selection.
/// This allows creating bots of varying strength by using different evaluators.
/// </summary>
public interface IEvaluatorBackedBot : IGameBot
{
    /// <summary>
    /// The evaluator this bot uses for decision making
    /// </summary>
    IPositionEvaluator Evaluator { get; }
}
