using Backgammon.Server.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Backgammon.Server.Endpoints;

/// <summary>
/// Server health, root, and aggregate statistics endpoints.
/// </summary>
public static class HealthEndpoints
{
    /// <summary>
    /// Maps the health / root / stats endpoints onto the given route builder.
    /// </summary>
    /// <param name="app">The route builder to register endpoints on.</param>
    /// <param name="corsPolicy">The CORS policy name to require on every endpoint in this group.</param>
    public static void MapHealthEndpoints(this IEndpointRouteBuilder app, string corsPolicy)
    {
        var root = app.MapGroup(string.Empty).RequireCors(corsPolicy);

        root.MapGet("/", () => "Backgammon SignalR Server Running - Connect via /gamehub");
        root.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow });

        root.MapGet("/stats", async (IGameRepository gameRepository) =>
        {
            var totalGames = await gameRepository.GetTotalGameCountAsync(null);
            var activeGamesCount = await gameRepository.GetTotalGameCountAsync("InProgress");
            var completedGamesCount = await gameRepository.GetTotalGameCountAsync("Completed");
            var abandonedGamesCount = await gameRepository.GetTotalGameCountAsync("Abandoned");

            return new
            {
                totalGames,
                activeGames = activeGamesCount,
                completedGames = completedGamesCount,
                abandonedGames = abandonedGamesCount
            };
        });

        root.MapGet("/api/stats/db", async (IGameRepository gameRepository) =>
        {
            var totalGames = await gameRepository.GetTotalGameCountAsync("Completed");
            var recentGames = await gameRepository.GetRecentGamesAsync("Completed", 5);

            return new
            {
                totalCompletedGames = totalGames,
                recentGames = recentGames.Select(g => new
                {
                    gameId = g.GameId,
                    winner = g.Winner,
                    stakes = g.Stakes,
                    moveCount = g.MoveCount,
                    duration = g.DurationSeconds,
                    completedAt = g.CompletedAt
                })
            };
        });
    }
}
