using Backgammon.AI.Bots;
using Backgammon.Plugins.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Backgammon.Server.Services;

/// <summary>
/// Resolves game bots by player ID prefix.
/// Maps player ID prefixes (e.g., "ai_greedy_") to bot types.
/// </summary>
public class BotResolver : IBotResolver
{
    private static readonly Dictionary<string, Type> BotPrefixes = new()
    {
        ["ai_greedy_"] = typeof(GreedyBot),
        ["ai_random_"] = typeof(RandomBot),
        ["ai_gnubg_"] = typeof(GnubgBot)
    };

    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="BotResolver"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving bot instances.</param>
    public BotResolver(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    public bool IsBot(string? playerId)
    {
        if (playerId == null)
        {
            return false;
        }

        return BotPrefixes.Keys.Any(prefix => playerId.StartsWith(prefix));
    }

    /// <inheritdoc/>
    public IGameBot? GetBot(string playerId)
    {
        foreach (var (prefix, botType) in BotPrefixes)
        {
            if (playerId.StartsWith(prefix))
            {
                return (IGameBot)_serviceProvider.GetRequiredService(botType);
            }
        }

        return null;
    }
}
