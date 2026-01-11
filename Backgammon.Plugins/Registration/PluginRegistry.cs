using Backgammon.Plugins.Abstractions;
using Backgammon.Plugins.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Backgammon.Plugins.Registration;

/// <summary>
/// Default implementation of plugin registry using DI.
/// Manages registration and creation of bots and evaluators.
/// </summary>
public class PluginRegistry : IPluginRegistry
{
    private readonly IServiceProvider _serviceProvider;
    private readonly PluginSettings _settings;
    private readonly Dictionary<string, BotMetadata> _bots = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, EvaluatorMetadata> _evaluators = new(StringComparer.OrdinalIgnoreCase);

    public PluginRegistry(
        IServiceProvider serviceProvider,
        IOptions<PluginSettings> settings,
        IEnumerable<BotRegistration> botRegistrations,
        IEnumerable<EvaluatorRegistration> evaluatorRegistrations)
    {
        _serviceProvider = serviceProvider;
        _settings = settings.Value;

        // Register all bots from DI registrations
        foreach (var reg in botRegistrations)
        {
            if (IsBotEnabled(reg.BotId))
            {
                _bots[reg.BotId] = new BotMetadata(
                    reg.BotId,
                    reg.DisplayName,
                    reg.Description,
                    reg.EstimatedElo,
                    reg.RequiresExternalResources,
                    reg.ImplementationType);
            }
        }

        // Register all evaluators from DI registrations
        foreach (var reg in evaluatorRegistrations)
        {
            if (IsEvaluatorEnabled(reg.EvaluatorId))
            {
                _evaluators[reg.EvaluatorId] = new EvaluatorMetadata(
                    reg.EvaluatorId,
                    reg.DisplayName,
                    reg.RequiresExternalResources,
                    reg.ImplementationType);
            }
        }
    }

    // Bot operations

    public IReadOnlyList<BotMetadata> GetAvailableBots()
    {
        return _bots.Values.ToList();
    }

    public BotMetadata? GetBotMetadata(string botId)
    {
        return _bots.TryGetValue(botId, out var metadata) ? metadata : null;
    }

    public IGameBot CreateBot(string botId)
    {
        if (!_bots.TryGetValue(botId, out var metadata))
        {
            throw new InvalidOperationException($"Bot '{botId}' is not registered");
        }

        var bot = (IGameBot)_serviceProvider.GetRequiredService(metadata.ImplementationType);
        return bot;
    }

    public bool IsBotAvailable(string botId)
    {
        if (!_bots.TryGetValue(botId, out var metadata))
        {
            return false;
        }

        // If bot requires external resources, check availability
        if (metadata.RequiresExternalResources)
        {
            // For now, assume available. Could add async check later.
            return true;
        }

        return true;
    }

    // Evaluator operations

    public IReadOnlyList<EvaluatorMetadata> GetAvailableEvaluators()
    {
        return _evaluators.Values.ToList();
    }

    public EvaluatorMetadata? GetEvaluatorMetadata(string evaluatorId)
    {
        return _evaluators.TryGetValue(evaluatorId, out var metadata) ? metadata : null;
    }

    public IPositionEvaluator CreateEvaluator(string evaluatorId)
    {
        if (!_evaluators.TryGetValue(evaluatorId, out var metadata))
        {
            throw new InvalidOperationException($"Evaluator '{evaluatorId}' is not registered");
        }

        var evaluator = (IPositionEvaluator)_serviceProvider.GetRequiredService(metadata.ImplementationType);
        return evaluator;
    }

    public bool IsEvaluatorAvailable(string evaluatorId)
    {
        if (!_evaluators.TryGetValue(evaluatorId, out var metadata))
        {
            return false;
        }

        // If evaluator requires external resources, check availability
        if (metadata.RequiresExternalResources)
        {
            // For now, assume available. Could add async check later.
            return true;
        }

        return true;
    }

    private bool IsBotEnabled(string botId)
    {
        if (_settings.Bots.TryGetValue(botId, out var botSettings))
        {
            return botSettings.Enabled;
        }

        return true; // Enabled by default if not in config
    }

    private bool IsEvaluatorEnabled(string evaluatorId)
    {
        if (_settings.Evaluators.TryGetValue(evaluatorId, out var evalSettings))
        {
            return evalSettings.Enabled;
        }

        return true; // Enabled by default if not in config
    }
}
