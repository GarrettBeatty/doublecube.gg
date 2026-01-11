using Backgammon.Plugins.Abstractions;
using Backgammon.Plugins.Configuration;
using Backgammon.Plugins.Registration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Backgammon.Plugins.Extensions;

/// <summary>
/// Extension methods for registering plugin services with DI
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add the core plugin infrastructure to DI
    /// </summary>
    public static IServiceCollection AddBackgammonPlugins(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register configuration
        services.Configure<PluginSettings>(
            configuration.GetSection(PluginSettings.SectionName));

        // Register registry as singleton
        services.AddSingleton<IPluginRegistry, PluginRegistry>();

        return services;
    }

    /// <summary>
    /// Register a bot with the plugin system
    /// </summary>
    /// <typeparam name="TBot">The bot implementation type</typeparam>
    /// <param name="services">Service collection</param>
    /// <param name="botId">Unique bot identifier</param>
    /// <param name="displayName">Human-friendly name</param>
    /// <param name="description">Description of play style</param>
    /// <param name="estimatedElo">Estimated ELO rating</param>
    /// <param name="requiresExternalResources">Whether bot needs external resources</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddBot<TBot>(
        this IServiceCollection services,
        string botId,
        string displayName,
        string description,
        int estimatedElo,
        bool requiresExternalResources = false)
        where TBot : class, IGameBot
    {
        // Register the bot type itself
        services.AddTransient<TBot>();

        // Register metadata for discovery by the registry
        services.AddSingleton(new BotRegistration(
            botId,
            displayName,
            description,
            estimatedElo,
            requiresExternalResources,
            typeof(TBot)));

        return services;
    }

    /// <summary>
    /// Register an evaluator with the plugin system
    /// </summary>
    /// <typeparam name="TEvaluator">The evaluator implementation type</typeparam>
    /// <param name="services">Service collection</param>
    /// <param name="evaluatorId">Unique evaluator identifier</param>
    /// <param name="displayName">Human-friendly name</param>
    /// <param name="requiresExternalResources">Whether evaluator needs external resources</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddEvaluator<TEvaluator>(
        this IServiceCollection services,
        string evaluatorId,
        string displayName,
        bool requiresExternalResources = false)
        where TEvaluator : class, IPositionEvaluator
    {
        // Register the evaluator type itself
        services.AddTransient<TEvaluator>();

        // Register metadata for discovery by the registry
        services.AddSingleton(new EvaluatorRegistration(
            evaluatorId,
            displayName,
            requiresExternalResources,
            typeof(TEvaluator)));

        return services;
    }
}
