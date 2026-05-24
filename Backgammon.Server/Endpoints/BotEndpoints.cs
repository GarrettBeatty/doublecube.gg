using Backgammon.Plugins.Registration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Backgammon.Server.Endpoints;

/// <summary>
/// Endpoints exposing the registered AI bots and position evaluators.
/// </summary>
public static class BotEndpoints
{
    /// <summary>
    /// Maps the /api/bots and /api/evaluators endpoints onto the given route builder.
    /// </summary>
    /// <param name="app">The route builder to register endpoints on.</param>
    /// <param name="corsPolicy">The CORS policy name to require on every endpoint in this group.</param>
    public static void MapBotEndpoints(this IEndpointRouteBuilder app, string corsPolicy)
    {
        var group = app.MapGroup("/api").RequireCors(corsPolicy);

        group.MapGet("/bots", (IPluginRegistry registry) =>
        {
            return registry.GetAvailableBots()
                .Select(b => new
                {
                    botId = b.BotId,
                    displayName = b.DisplayName,
                    description = b.Description,
                    estimatedElo = b.EstimatedElo,
                    requiresExternalResources = b.RequiresExternalResources
                })
                .ToList();
        });

        group.MapGet("/evaluators", (IPluginRegistry registry) =>
        {
            return registry.GetAvailableEvaluators()
                .Select(e => new
                {
                    evaluatorId = e.EvaluatorId,
                    displayName = e.DisplayName,
                    requiresExternalResources = e.RequiresExternalResources
                })
                .ToList();
        });
    }
}
