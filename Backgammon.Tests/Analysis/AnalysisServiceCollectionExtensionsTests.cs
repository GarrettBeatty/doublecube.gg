using Backgammon.AI;
using Backgammon.AI.Bots;
using Backgammon.Analysis.Configuration;
using Backgammon.Analysis.Evaluators;
using Backgammon.Analysis.Extensions;
using Backgammon.Plugins.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Backgammon.Tests.Analysis;

/// <summary>
/// Tests for Analysis ServiceCollectionExtensions DI registrations (evaluators only).
/// Bot registrations are tested in BotRegistrationsTests.cs.
/// </summary>
public class AnalysisServiceCollectionExtensionsTests
{
    // ==================== AddAnalysisEvaluators ====================

    [Fact]
    public void AddAnalysisEvaluators_RegistersHeuristicEvaluatorAsIPositionEvaluator()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAnalysisEvaluators();

        var provider = services.BuildServiceProvider();
        var evaluator = provider.GetService<IPositionEvaluator>();

        Assert.NotNull(evaluator);
        Assert.IsType<HeuristicEvaluator>(evaluator);
    }

    [Fact]
    public void AddAnalysisEvaluators_RegistersHeuristicEvaluatorDirectly()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAnalysisEvaluators();

        var provider = services.BuildServiceProvider();
        var evaluator = provider.GetService<HeuristicEvaluator>();

        Assert.NotNull(evaluator);
    }

    [Fact]
    public void AddAnalysisEvaluators_HeuristicEvaluatorHasCorrectId()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAnalysisEvaluators();

        var provider = services.BuildServiceProvider();
        var evaluator = provider.GetRequiredService<IPositionEvaluator>();

        Assert.Equal("heuristic", evaluator.EvaluatorId);
    }

    // ==================== AddGnubgEvaluator ====================

    [Fact]
    public void AddGnubgEvaluator_RegistersHttpGnubgEvaluator()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<GnubgSettings>(s => s.ServiceUrl = "http://localhost:5000");
        services.AddGnubgEvaluator();

        var provider = services.BuildServiceProvider();
        var evaluator = provider.GetService<HttpGnubgEvaluator>();

        Assert.NotNull(evaluator);
    }

    // ==================== AddAnalysisPlugins ====================

    [Fact]
    public void AddAnalysisPlugins_WithGnubg_RegistersEvaluators()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<GnubgSettings>(s =>
        {
            s.ServiceUrl = "http://localhost:5000";
            s.TimeoutMs = 5000;
        });
        services.AddAnalysisPlugins(includeGnubg: true);

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IPositionEvaluator>());
        Assert.NotNull(provider.GetService<HeuristicEvaluator>());
        Assert.NotNull(provider.GetService<HttpGnubgEvaluator>());
    }

    [Fact]
    public void AddAnalysisPlugins_WithoutGnubg_OnlyRegistersHeuristicEvaluator()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAnalysisPlugins(includeGnubg: false);

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IPositionEvaluator>());
        Assert.NotNull(provider.GetService<HeuristicEvaluator>());
        Assert.Null(provider.GetService<HttpGnubgEvaluator>());
    }
}
