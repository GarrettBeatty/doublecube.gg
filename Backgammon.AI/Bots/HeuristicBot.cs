using Backgammon.Plugins.Abstractions;
using Backgammon.Plugins.Base;

namespace Backgammon.AI.Bots;

/// <summary>
/// A bot that uses the heuristic evaluator for move selection.
/// Provides intermediate-strength play by evaluating positions.
/// </summary>
public class HeuristicBot : EvaluatorBackedBot
{
    public HeuristicBot(IPositionEvaluator evaluator)
        : base(evaluator)
    {
    }

    public override string BotId => "heuristic-bot";

    public override string DisplayName => "Heuristic Bot";

    public override string Description => "Uses position evaluation heuristics for strategic play.";

    public override int EstimatedElo => 1400;
}
