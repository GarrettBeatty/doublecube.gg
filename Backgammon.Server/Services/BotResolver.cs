using Backgammon.AI.Bots;
using Backgammon.Plugins.Abstractions;
using Backgammon.Plugins.Base;
using Microsoft.Extensions.DependencyInjection;

namespace Backgammon.Server.Services;

/// <summary>
/// Resolves game bots by player ID prefix.
/// Maps player ID prefixes (e.g., "ai_greedy_") to bot types.
/// Supports gnubg difficulty levels via player ID encoding.
/// </summary>
public class BotResolver : IBotResolver
{
    /// <summary>
    /// Maps player ID prefixes to bot types.
    /// Gnubg difficulty variants all map to GnubgBot with different plies.
    /// </summary>
    private static readonly Dictionary<string, Type> BotPrefixes = new()
    {
        ["ai_greedy_"] = typeof(GreedyBot),
        ["ai_random_"] = typeof(RandomBot),
        ["ai_gnubg_easy_"] = typeof(GnubgBot),
        ["ai_gnubg_medium_"] = typeof(GnubgBot),
        ["ai_gnubg_hard_"] = typeof(GnubgBot),
        ["ai_gnubg_expert_"] = typeof(GnubgBot),
        ["ai_gnubg_"] = typeof(GnubgBot) // Fallback for legacy IDs
    };

    /// <summary>
    /// Maps gnubg difficulty prefixes to ply counts.
    /// </summary>
    private static readonly Dictionary<string, int> GnubgPliesMapping = new()
    {
        ["ai_gnubg_easy_"] = 0,
        ["ai_gnubg_medium_"] = 1,
        ["ai_gnubg_hard_"] = 2,
        ["ai_gnubg_expert_"] = 3
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
                var bot = (IGameBot)_serviceProvider.GetRequiredService(botType);

                // Configure plies override for gnubg difficulty variants
                if (bot is EvaluatorBackedBot evaluatorBot)
                {
                    var plies = GetGnubgPliesFromPlayerId(playerId);
                    if (plies.HasValue)
                    {
                        evaluatorBot.PliesOverride = plies;
                    }
                }

                return bot;
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts the gnubg plies setting from a player ID.
    /// </summary>
    /// <param name="playerId">The player ID to parse.</param>
    /// <returns>The plies count, or null if not a gnubg difficulty variant.</returns>
    private static int? GetGnubgPliesFromPlayerId(string playerId)
    {
        foreach (var (prefix, plies) in GnubgPliesMapping)
        {
            if (playerId.StartsWith(prefix))
            {
                return plies;
            }
        }

        return null;
    }
}
