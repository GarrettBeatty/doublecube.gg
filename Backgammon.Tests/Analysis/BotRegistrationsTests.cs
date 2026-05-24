using Backgammon.AI;
using Backgammon.AI.Bots;
using Backgammon.Analysis.Configuration;
using Backgammon.Analysis.Evaluators;
using Backgammon.Analysis.Extensions;
using Backgammon.Plugins.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Backgammon.Tests.Analysis;

/// <summary>
/// Tests for BotRegistrations — the single file contributors edit to add bots.
/// </summary>
public class BotRegistrationsTests
{
    // ==================== Built-in bots ====================

    [Fact]
    public void AddAllBots_RegistersRandomBot()
    {
        var provider = BuildProvider();
        Assert.NotNull(provider.GetService<RandomBot>());
    }

    [Fact]
    public void AddAllBots_RegistersGreedyBot()
    {
        var provider = BuildProvider();
        Assert.NotNull(provider.GetService<GreedyBot>());
    }

    [Fact]
    public void AddAllBots_RegistersHeuristicBot()
    {
        var provider = BuildProvider();
        Assert.NotNull(provider.GetService<HeuristicBot>());
    }

    // ==================== Evaluator-backed bots ====================

    [Fact]
    public void AddAllBots_WithGnubg_RegistersGnubgBot()
    {
        var provider = BuildProvider(includeGnubg: true);
        var bot = provider.GetService<GnubgBot>();
        Assert.NotNull(bot);
    }

    [Fact]
    public void AddAllBots_WithGnubg_GnubgBotUsesHttpGnubgEvaluator()
    {
        var provider = BuildProvider(includeGnubg: true);
        var bot = provider.GetRequiredService<GnubgBot>();
        Assert.IsType<HttpGnubgEvaluator>(bot.Evaluator);
    }

    // ==================== Bot IDs ====================

    [Fact]
    public void AddAllBots_RandomBot_HasCorrectBotId()
    {
        var bot = new RandomBot();
        Assert.Equal("random", bot.BotId);
    }

    [Fact]
    public void AddAllBots_GreedyBot_HasCorrectBotId()
    {
        var bot = new GreedyBot();
        Assert.Equal("greedy", bot.BotId);
    }

    [Fact]
    public void AddAllBots_HeuristicBot_HasCorrectBotId()
    {
        var provider = BuildProvider();
        var bot = provider.GetRequiredService<HeuristicBot>();
        Assert.Equal("heuristic-bot", bot.BotId);
    }

    [Fact]
    public void AddAllBots_WithGnubg_GnubgBotHasCorrectBotId()
    {
        var provider = BuildProvider(includeGnubg: true);
        var bot = provider.GetRequiredService<GnubgBot>();
        Assert.Equal("gnubg-bot", bot.BotId);
    }

    private static IServiceProvider BuildProvider(bool includeGnubg = false)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<GnubgSettings>(s =>
        {
            s.ServiceUrl = "http://localhost:5000";
            s.TimeoutMs = 5000;
        });
        services.AddBackgammonPlugins(new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());
        services.AddAnalysisPlugins(includeGnubg);
        services.AddAllBots();
        return services.BuildServiceProvider();
    }
}
