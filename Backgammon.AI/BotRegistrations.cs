using Backgammon.AI.Bots;
using Backgammon.Analysis.Evaluators;
using Backgammon.Plugins.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Backgammon.AI;

/// <summary>
/// The single place to register all bots with the plugin system.
///
/// ─────────────────────────────────────────────────────────────────────────
/// HOW TO ADD A NEW BOT
/// ─────────────────────────────────────────────────────────────────────────
/// 1. Create your bot class in Backgammon.AI/Bots/, implementing IGameBot
///    (or extend EvaluatorBackedBot if you want to use a position evaluator).
///
/// 2. Add one line here inside AddAllBots():
///
///      services.AddBot<MyBot>(
///          botId:        "my-bot",
///          displayName:  "My Bot",
///          description:  "Short description of play style.",
///          estimatedElo: 1300);
///
///    If your bot needs constructor arguments resolved from DI, use the
///    factory overload at the bottom of this file as a reference.
///
/// 3. That's it — the bot will appear in the lobby automatically.
/// ─────────────────────────────────────────────────────────────────────────
/// </summary>
public static class BotRegistrations
{
    /// <summary>
    /// Registers all available bots with the plugin system.
    /// Called once from Program.cs.
    /// </summary>
    public static IServiceCollection AddAllBots(this IServiceCollection services)
    {
        // ── Built-in bots (no evaluator needed) ───────────────────────────
        services.AddBot<RandomBot>(
            botId: "random",
            displayName: "Random Bot",
            description: "Makes random valid moves. Good for beginners.",
            estimatedElo: 800);

        services.AddBot<GreedyBot>(
            botId: "greedy",
            displayName: "Greedy Bot",
            description: "Prioritizes bearing off, hitting blots, and advancing. Fast and reliable.",
            estimatedElo: 1200);

        // ── Evaluator-backed bots ──────────────────────────────────────────
        services.AddBot<HeuristicBot>(
            botId: "heuristic-bot",
            displayName: "Heuristic Bot",
            description: "Uses position evaluation heuristics for strategic play.",
            estimatedElo: 1400);

        // GnubgBot requires HttpGnubgEvaluator — resolved explicitly from DI
        // so it gets the correctly configured HttpClient, not the generic IPositionEvaluator.
        services.AddTransient<GnubgBot>(sp =>
        {
            var evaluator = sp.GetRequiredService<HttpGnubgEvaluator>();
            var logger = sp.GetService<ILogger<GnubgBot>>();
            return new GnubgBot(evaluator, logger);
        });

        services.AddSingleton(new Backgammon.Plugins.Registration.BotRegistration(
            BotId: "gnubg-bot",
            DisplayName: "Expert Bot (GNUBG)",
            Description: "Uses GNU Backgammon neural network for expert play.",
            EstimatedElo: 2000,
            RequiresExternalResources: true,
            ImplementationType: typeof(GnubgBot)));

        return services;
    }
}
