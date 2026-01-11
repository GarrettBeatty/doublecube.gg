using Backgammon.AI.Bots;
using Backgammon.Plugins.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Backgammon.AI.Extensions;

/// <summary>
/// Extension methods for registering AI bots with DI
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add standard AI bots to the plugin registry
    /// </summary>
    public static IServiceCollection AddStandardBots(this IServiceCollection services)
    {
        services.AddBot<RandomBot>(
            "random",
            "Random Bot",
            "Makes random valid moves. Good for beginners.",
            800);

        services.AddBot<GreedyBot>(
            "greedy",
            "Greedy Bot",
            "Prioritizes bearing off, hitting blots, and advancing. Fast and reliable.",
            1200);

        return services;
    }

    /// <summary>
    /// Add the heuristic bot (requires a heuristic evaluator to be registered)
    /// </summary>
    public static IServiceCollection AddHeuristicBot(this IServiceCollection services)
    {
        services.AddBot<HeuristicBot>(
            "heuristic-bot",
            "Heuristic Bot",
            "Uses position evaluation heuristics for strategic play.",
            1400);

        return services;
    }
}
