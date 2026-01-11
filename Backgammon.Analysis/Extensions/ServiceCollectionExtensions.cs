using Backgammon.Analysis.Bots;
using Backgammon.Analysis.Configuration;
using Backgammon.Analysis.Evaluators;
using Backgammon.Analysis.Gnubg;
using Backgammon.Plugins.Abstractions;
using Backgammon.Plugins.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Backgammon.Analysis.Extensions;

/// <summary>
/// Extension methods for registering Analysis services with DI
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add analysis evaluators to the plugin registry
    /// </summary>
    public static IServiceCollection AddAnalysisEvaluators(this IServiceCollection services)
    {
        // Register the heuristic evaluator (always available)
        services.AddEvaluator<HeuristicEvaluator>(
            "heuristic",
            "Heuristic Evaluator");

        // Also register as IPositionEvaluator for bots that depend on the interface
        services.AddSingleton<IPositionEvaluator, HeuristicEvaluator>();

        return services;
    }

    /// <summary>
    /// Add GNUBG evaluator and bot (requires gnubg to be installed)
    /// </summary>
    public static IServiceCollection AddGnubgEvaluator(this IServiceCollection services)
    {
        // Register gnubg infrastructure with factory to extract settings from IOptions
        services.AddSingleton<GnubgProcessManager>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<GnubgSettings>>().Value;
            var logger = sp.GetRequiredService<ILogger<GnubgProcessManager>>();
            return new GnubgProcessManager(settings, msg => logger.LogDebug(msg));
        });

        // Register the gnubg evaluator with factory
        services.AddTransient<GnubgEvaluator>(sp =>
        {
            var processManager = sp.GetRequiredService<GnubgProcessManager>();
            var settings = sp.GetRequiredService<IOptions<GnubgSettings>>().Value;
            var logger = sp.GetRequiredService<ILogger<GnubgEvaluator>>();
            return new GnubgEvaluator(processManager, settings, msg => logger.LogDebug(msg));
        });

        // Register metadata for discovery by the registry
        services.AddSingleton(new Backgammon.Plugins.Registration.EvaluatorRegistration(
            "gnubg",
            "GNU Backgammon",
            RequiresExternalResources: true,
            typeof(GnubgEvaluator)));

        return services;
    }

    /// <summary>
    /// Add the GNUBG bot (requires gnubg evaluator to be registered)
    /// </summary>
    public static IServiceCollection AddGnubgBot(this IServiceCollection services)
    {
        // Register GnubgBot with factory (depends on GnubgEvaluator)
        services.AddTransient<GnubgBot>(sp =>
        {
            var evaluator = sp.GetRequiredService<GnubgEvaluator>();
            return new GnubgBot(evaluator);
        });

        // Register metadata for discovery by the registry
        services.AddSingleton(new Backgammon.Plugins.Registration.BotRegistration(
            "gnubg-bot",
            "Expert Bot (GNUBG)",
            "Uses GNU Backgammon neural network for expert play.",
            2000,
            RequiresExternalResources: true,
            typeof(GnubgBot)));

        return services;
    }

    /// <summary>
    /// Add all analysis plugins (evaluators and bots)
    /// </summary>
    public static IServiceCollection AddAnalysisPlugins(
        this IServiceCollection services,
        bool includeGnubg = true)
    {
        services.AddAnalysisEvaluators();

        if (includeGnubg)
        {
            services.AddGnubgEvaluator();
            services.AddGnubgBot();
        }

        return services;
    }
}
