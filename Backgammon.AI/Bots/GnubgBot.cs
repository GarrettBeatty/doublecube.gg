using Backgammon.Plugins.Abstractions;
using Backgammon.Plugins.Base;
using Microsoft.Extensions.Logging;

namespace Backgammon.AI.Bots;

/// <summary>
/// Expert-level bot using GNU Backgammon for move selection.
/// Provides world-class play using the gnubg neural network.
/// </summary>
public class GnubgBot : EvaluatorBackedBot
{
    public GnubgBot(IPositionEvaluator evaluator, ILogger<GnubgBot>? logger = null)
        : base(evaluator, logger)
    {
    }

    public override string BotId => "gnubg-bot";

    public override string DisplayName => "Expert Bot (GNUBG)";

    public override string Description => "Uses GNU Backgammon neural network for expert play.";

    public override int EstimatedElo => 2000;
}
