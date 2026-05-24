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
/// Extension methods for registering Analysis evaluators with DI.
/// Bot registrations live in Backgammon.AI/BotRegistrations.cs.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add analysis evaluators to the plugin registry.
    /// </summary>
    public static IServiceCollection AddAnalysisEvaluators(this IServiceCollection services)
    {
        services.AddEvaluator<HeuristicEvaluator>(
            "heuristic",
            "Heuristic Evaluator");

        // Also register as IPositionEvaluator for bots that depend on the interface
        services.AddSingleton<IPositionEvaluator, HeuristicEvaluator>();

        return services;
    }

    /// <summary>
    /// Add the GNU Backgammon HTTP evaluator.
    /// </summary>
    public static IServiceCollection AddGnubgEvaluator(this IServiceCollection services)
    {
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

        services.AddSingleton(new Backgammon.Plugins.Registration.EvaluatorRegistration(
            "gnubg",
            "GNU Backgammon",
            RequiresExternalResources: true,
            typeof(HttpGnubgEvaluator)));

        return services;
    }

    /// <summary>
    /// Add the GNU Backgammon local process evaluator (requires gnubg installed locally).
    /// </summary>
    public static IServiceCollection AddGnubgProcessEvaluator(this IServiceCollection services)
    {
        services.AddSingleton<GnubgProcessManager>();
        services.AddTransient<GnubgEvaluator>();

        services.AddSingleton(new Backgammon.Plugins.Registration.EvaluatorRegistration(
            "gnubg-local",
            "GNU Backgammon (Local)",
            RequiresExternalResources: true,
            typeof(GnubgEvaluator)));

        return services;
    }

    /// <summary>
    /// Add all analysis evaluators (heuristic + gnubg).
    /// </summary>
    public static IServiceCollection AddAnalysisPlugins(
        this IServiceCollection services,
        bool includeGnubg = true)
    {
        services.AddAnalysisEvaluators();

        if (includeGnubg)
        {
            services.AddGnubgEvaluator();
        }

        return services;
    }
}
