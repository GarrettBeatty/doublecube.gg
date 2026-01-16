using Backgammon.AI.Bots;
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
    /// Add GNUBG evaluator via HTTP service (preferred approach)
    /// </summary>
    public static IServiceCollection AddGnubgEvaluator(this IServiceCollection services)
    {
        // Register HttpGnubgEvaluator with typed HttpClient
        // Note: AddHttpClient<T> automatically registers T as a transient service
        // DO NOT add another AddTransient<T> as it would override the HttpClient-injected version
        services.AddHttpClient<HttpGnubgEvaluator>((sp, client) =>
        {
            var settings = sp.GetRequiredService<IOptions<GnubgSettings>>().Value;
            var logger = sp.GetService<ILogger<HttpGnubgEvaluator>>();

            logger?.LogInformation("Configuring HttpGnubgEvaluator with ServiceUrl: {ServiceUrl}", settings.ServiceUrl ?? "(not set)");

            if (string.IsNullOrEmpty(settings.ServiceUrl))
            {
                logger?.LogWarning("GnubgSettings.ServiceUrl is not configured. HTTP requests will fail.");
            }
            else
            {
                client.BaseAddress = new Uri(settings.ServiceUrl);
                logger?.LogInformation("HttpGnubgEvaluator BaseAddress set to: {BaseAddress}", client.BaseAddress);
            }

            client.Timeout = TimeSpan.FromMilliseconds(settings.TimeoutMs);
        });

        // Register metadata for discovery by the registry
        services.AddSingleton(new Backgammon.Plugins.Registration.EvaluatorRegistration(
            "gnubg",
            "GNU Backgammon",
            RequiresExternalResources: true,
            typeof(HttpGnubgEvaluator)));

        return services;
    }

    /// <summary>
    /// Add GNUBG evaluator via local process (legacy, for when gnubg is installed locally)
    /// </summary>
    public static IServiceCollection AddGnubgProcessEvaluator(this IServiceCollection services)
    {
        // Register gnubg infrastructure - DI resolves IOptions and ILogger automatically
        services.AddSingleton<GnubgProcessManager>();

        // Register the gnubg evaluator - DI resolves dependencies automatically
        services.AddTransient<GnubgEvaluator>();

        // Register metadata for discovery by the registry
        services.AddSingleton(new Backgammon.Plugins.Registration.EvaluatorRegistration(
            "gnubg-local",
            "GNU Backgammon (Local)",
            RequiresExternalResources: true,
            typeof(GnubgEvaluator)));

        return services;
    }

    /// <summary>
    /// Add the GNUBG bot (requires gnubg evaluator to be registered)
    /// </summary>
    public static IServiceCollection AddGnubgBot(this IServiceCollection services)
    {
        // Register GnubgBot - injects HttpGnubgEvaluator as IPositionEvaluator and logger
        services.AddTransient<GnubgBot>(sp =>
        {
            // Get the HttpGnubgEvaluator and pass it as the IPositionEvaluator
            var evaluator = sp.GetRequiredService<HttpGnubgEvaluator>();
            var logger = sp.GetService<ILogger<GnubgBot>>();
            return new GnubgBot(evaluator, logger);
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
