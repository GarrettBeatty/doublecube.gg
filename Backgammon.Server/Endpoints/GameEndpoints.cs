using Backgammon.Server.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Backgammon.Server.Endpoints;

/// <summary>
/// Read-only endpoints exposing the list of active and bot games plus single-game lookup.
/// </summary>
public static class GameEndpoints
{
    /// <summary>
    /// Maps the /api/games and /api/game endpoints onto the given route builder.
    /// </summary>
    /// <param name="app">The route builder to register endpoints on.</param>
    /// <param name="corsPolicy">The CORS policy name to require on every endpoint in this group.</param>
    public static void MapGameEndpoints(this IEndpointRouteBuilder app, string corsPolicy)
    {
        var group = app.MapGroup("/api").RequireCors(corsPolicy);

        group.MapGet("/games", async (IGameRepository gameRepository) =>
        {
            var dbActiveGames = await gameRepository.GetActiveGamesAsync();
            var activeGamesList = dbActiveGames.Select(g => new
            {
                gameId = g.GameId,
                whitePlayer = g.WhitePlayerName ?? "Player 1",
                redPlayer = g.RedPlayerName ?? "Player 2",
                whiteUsername = g.WhitePlayerName,
                redUsername = g.RedPlayerName,
                status = "playing",
                createdAt = g.CreatedAt
            }).ToList();

            return new { activeGames = activeGamesList };
        });

        group.MapGet("/bot-games", async (IGameRepository gameRepository) =>
        {
            var botGames = await gameRepository.GetActiveGamesAsync();
            return botGames
                .Where(g => g.IsAiOpponent)
                .Select(g => new
                {
                    gameId = g.GameId,
                    whitePlayer = g.WhitePlayerName,
                    redPlayer = g.RedPlayerName,
                    currentPlayer = g.CurrentPlayer ?? "Unknown",
                    status = g.Status
                })
                .ToList();
        });

        group.MapGet("/game/{gameId}", async (string gameId, IGameRepository gameRepository) =>
        {
            var game = await gameRepository.GetGameByGameIdAsync(gameId);
            if (game == null)
            {
                return Results.NotFound(new { error = "Game not found" });
            }

            return Results.Ok(game);
        });
    }
}
