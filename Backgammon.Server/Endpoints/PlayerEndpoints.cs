using Backgammon.Server.Configuration;
using Backgammon.Server.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace Backgammon.Server.Endpoints;

/// <summary>
/// Player-scoped read endpoints (active games, history, active match, stats).
/// </summary>
public static class PlayerEndpoints
{
    /// <summary>
    /// Maps the /api/player/{playerId}/* endpoints onto the given route builder.
    /// </summary>
    /// <param name="app">The route builder to register endpoints on.</param>
    /// <param name="corsPolicy">The CORS policy name to require on every endpoint in this group.</param>
    public static void MapPlayerEndpoints(this IEndpointRouteBuilder app, string corsPolicy)
    {
        var group = app.MapGroup("/api/player").RequireCors(corsPolicy);

        group.MapGet("/{playerId}/active-games", async (string playerId, IGameRepository gameRepository) =>
        {
            var dbGames = await gameRepository.GetPlayerGamesAsync(playerId, "InProgress", limit: 50);
            return dbGames.Select(g => new
            {
                gameId = g.GameId,
                myColor = g.WhitePlayerId == playerId ? "White" : "Red",
                opponent = g.WhitePlayerId == playerId
                    ? (g.RedPlayerName ?? "Waiting for opponent")
                    : (g.WhitePlayerName ?? "Waiting for opponent"),
                isFull = !string.IsNullOrEmpty(g.WhitePlayerId) && !string.IsNullOrEmpty(g.RedPlayerId),
                isMyTurn = g.CurrentPlayer == (g.WhitePlayerId == playerId ? "White" : "Red"),
                createdAt = g.CreatedAt,
                lastActivity = g.LastUpdatedAt
            })
            .OrderByDescending(g => g.lastActivity)
            .ToList();
        });

        group.MapGet("/{playerId}/games", async (
            string playerId,
            HybridCache cache,
            CacheSettings cacheSettings,
            IGameRepository gameRepository,
            ILogger<Program> logger,
            int limit = 20,
            int skip = 0) =>
        {
            var cacheKey = $"player:games:{playerId}:completed:limit={limit}:skip={skip}";

            try
            {
                var games = await cache.GetOrCreateAsync(
                    cacheKey,
                    async ct =>
                    {
                        try
                        {
                            return await gameRepository.GetPlayerGamesAsync(playerId, "Completed", limit, skip);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Failed to fetch games for player {PlayerId}", playerId);
                            throw;
                        }
                    },
                    new HybridCacheEntryOptions
                    {
                        Expiration = cacheSettings.PlayerGames.Expiration,
                        LocalCacheExpiration = cacheSettings.PlayerGames.LocalCacheExpiration
                    },
                    tags: [$"player:{playerId}"]);

                return Results.Ok(games);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving game history for player {PlayerId}", playerId);
                return Results.Problem("Failed to retrieve game history", statusCode: 500);
            }
        });

        group.MapGet("/{playerId}/active-match", async (string playerId, IMatchRepository matchRepository) =>
        {
            var matches = await matchRepository.GetPlayerMatchesAsync(playerId, "InProgress", limit: 1);
            var activeMatch = matches.FirstOrDefault();

            if (activeMatch == null)
            {
                return Results.Ok(new { hasActiveMatch = false });
            }

            return Results.Ok(new
            {
                hasActiveMatch = true,
                matchId = activeMatch.MatchId,
                targetScore = activeMatch.TargetScore,
                player1Id = activeMatch.Player1Id,
                player2Id = activeMatch.Player2Id,
                player1Score = activeMatch.Player1Score,
                player2Score = activeMatch.Player2Score,
                status = activeMatch.Status,
                currentGameId = activeMatch.CurrentGameId,
                isCrawfordGame = activeMatch.IsCrawfordGame,
                hasCrawfordGameBeenPlayed = activeMatch.HasCrawfordGameBeenPlayed
            });
        });

        group.MapGet("/{playerId}/stats", async (
            string playerId,
            HybridCache cache,
            CacheSettings cacheSettings,
            IGameRepository gameRepository) =>
        {
            var stats = await cache.GetOrCreateAsync(
                $"player:stats:{playerId}",
                async ct => await gameRepository.GetPlayerStatsAsync(playerId),
                new HybridCacheEntryOptions
                {
                    Expiration = cacheSettings.PlayerStats.Expiration,
                    LocalCacheExpiration = cacheSettings.PlayerStats.LocalCacheExpiration
                },
                tags: [$"player:{playerId}"]);

            return stats;
        });
    }
}
