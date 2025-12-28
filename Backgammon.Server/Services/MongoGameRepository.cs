using MongoDB.Driver;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Backgammon.Server.Models;

namespace Backgammon.Server.Services;

/// <summary>
/// MongoDB implementation of game storage.
/// Handles persistence and queries for both in-progress and completed backgammon games.
/// </summary>
public class MongoGameRepository : IGameRepository
{
    private readonly IMongoCollection<Game> _games;
    private readonly ILogger<MongoGameRepository> _logger;

    public MongoGameRepository(IMongoClient mongoClient, IConfiguration configuration, ILogger<MongoGameRepository> logger)
    {
        _logger = logger;

        var databaseName = configuration["MongoDB:DatabaseName"] ?? "backgammon";
        var database = mongoClient.GetDatabase(databaseName);
        _games = database.GetCollection<Game>("games");

        // Create indexes for efficient queries
        CreateIndexes().Wait();
    }

    private async Task CreateIndexes()
    {
        try
        {
            // Index on gameId for fast lookups (unique)
            await _games.Indexes.CreateOneAsync(
                new CreateIndexModel<Game>(
                    Builders<Game>.IndexKeys.Ascending(g => g.GameId),
                    new CreateIndexOptions { Unique = true }
                )
            );

            // Index on status for filtering active/completed games
            await _games.Indexes.CreateOneAsync(
                new CreateIndexModel<Game>(
                    Builders<Game>.IndexKeys.Ascending(g => g.Status)
                )
            );

            // Compound index for player queries (white player + status + completion time)
            await _games.Indexes.CreateOneAsync(
                new CreateIndexModel<Game>(
                    Builders<Game>.IndexKeys
                        .Ascending(g => g.WhitePlayerId)
                        .Ascending(g => g.Status)
                        .Descending(g => g.LastUpdatedAt)
                )
            );

            // Compound index for player queries (red player + status + completion time)
            await _games.Indexes.CreateOneAsync(
                new CreateIndexModel<Game>(
                    Builders<Game>.IndexKeys
                        .Ascending(g => g.RedPlayerId)
                        .Ascending(g => g.Status)
                        .Descending(g => g.LastUpdatedAt)
                )
            );

            // Index for recent games
            await _games.Indexes.CreateOneAsync(
                new CreateIndexModel<Game>(
                    Builders<Game>.IndexKeys.Descending(g => g.LastUpdatedAt)
                )
            );

            // Index for completed games sorted by completion time
            await _games.Indexes.CreateOneAsync(
                new CreateIndexModel<Game>(
                    Builders<Game>.IndexKeys
                        .Ascending(g => g.Status)
                        .Descending(g => g.CompletedAt)
                )
            );

            _logger.LogInformation("MongoDB indexes created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create indexes (may already exist)");
        }
    }

    public async Task SaveGameAsync(Game game)
    {
        try
        {
            game.LastUpdatedAt = DateTime.UtcNow;

            var filter = Builders<Game>.Filter.Eq(g => g.GameId, game.GameId);
            var options = new ReplaceOptions { IsUpsert = true };

            await _games.ReplaceOneAsync(filter, game, options);
            _logger.LogDebug("Saved game {GameId} with status {Status}", game.GameId, game.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save game {GameId} to MongoDB", game.GameId);
            throw;
        }
    }

    public async Task<Game?> GetGameByGameIdAsync(string gameId)
    {
        try
        {
            var filter = Builders<Game>.Filter.Eq(g => g.GameId, gameId);
            return await _games.Find(filter).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve game {GameId}", gameId);
            return null;
        }
    }

    public async Task<List<Game>> GetActiveGamesAsync()
    {
        try
        {
            var filter = Builders<Game>.Filter.Eq(g => g.Status, "InProgress");
            var games = await _games.Find(filter).ToListAsync();
            _logger.LogInformation("Retrieved {Count} active games from database", games.Count);
            return games;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve active games");
            return new List<Game>();
        }
    }

    public async Task UpdateGameStatusAsync(string gameId, string status)
    {
        try
        {
            var filter = Builders<Game>.Filter.Eq(g => g.GameId, gameId);
            var update = Builders<Game>.Update
                .Set(g => g.Status, status)
                .Set(g => g.LastUpdatedAt, DateTime.UtcNow);

            if (status == "Completed")
            {
                update = update.Set(g => g.CompletedAt, DateTime.UtcNow);
            }

            await _games.UpdateOneAsync(filter, update);
            _logger.LogInformation("Updated game {GameId} status to {Status}", gameId, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update status for game {GameId}", gameId);
            throw;
        }
    }

    public async Task<List<Game>> GetPlayerGamesAsync(string playerId, string? status = null, int limit = 50, int skip = 0)
    {
        try
        {
            var playerFilter = Builders<Game>.Filter.Or(
                Builders<Game>.Filter.Eq(g => g.WhitePlayerId, playerId),
                Builders<Game>.Filter.Eq(g => g.RedPlayerId, playerId)
            );

            var filter = status != null
                ? Builders<Game>.Filter.And(
                    playerFilter,
                    Builders<Game>.Filter.Eq(g => g.Status, status))
                : playerFilter;

            return await _games.Find(filter)
                .SortByDescending(g => g.LastUpdatedAt)
                .Skip(skip)
                .Limit(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve games for player {PlayerId}", playerId);
            return new List<Game>();
        }
    }

    public async Task<PlayerStats> GetPlayerStatsAsync(string playerId)
    {
        try
        {
            var filter = Builders<Game>.Filter.And(
                Builders<Game>.Filter.Or(
                    Builders<Game>.Filter.Eq(g => g.WhitePlayerId, playerId),
                    Builders<Game>.Filter.Eq(g => g.RedPlayerId, playerId)
                ),
                Builders<Game>.Filter.Eq(g => g.Status, "Completed")
            );

            var games = await _games.Find(filter).ToListAsync();

            var stats = new PlayerStats
            {
                PlayerId = playerId,
                TotalGames = games.Count,
                Wins = games.Count(g =>
                    (g.Winner == "White" && g.WhitePlayerId == playerId) ||
                    (g.Winner == "Red" && g.RedPlayerId == playerId)
                ),
                Losses = games.Count(g =>
                    (g.Winner == "White" && g.RedPlayerId == playerId) ||
                    (g.Winner == "Red" && g.WhitePlayerId == playerId)
                )
            };

            // Calculate stakes and win types (only for wins)
            foreach (var game in games)
            {
                bool isWinner = (game.Winner == "White" && game.WhitePlayerId == playerId) ||
                                (game.Winner == "Red" && game.RedPlayerId == playerId);

                if (isWinner)
                {
                    stats.TotalStakes += game.Stakes;

                    // Determine win type based on stakes/doubling cube
                    // Stakes includes doubling cube, so divide by cube value to get base multiplier
                    var baseMultiplier = game.DoublingCubeValue > 0 ? game.Stakes / game.DoublingCubeValue : game.Stakes;

                    if (baseMultiplier == 1) stats.NormalWins++;
                    else if (baseMultiplier == 2) stats.GammonWins++;
                    else if (baseMultiplier == 3) stats.BackgammonWins++;
                }
            }

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate stats for player {PlayerId}", playerId);
            return new PlayerStats { PlayerId = playerId };
        }
    }

    public async Task<List<Game>> GetRecentGamesAsync(string? status = "Completed", int limit = 20)
    {
        try
        {
            var filter = status != null
                ? Builders<Game>.Filter.Eq(g => g.Status, status)
                : Builders<Game>.Filter.Empty;

            return await _games.Find(filter)
                .SortByDescending(g => g.CompletedAt ?? g.LastUpdatedAt)
                .Limit(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve recent games");
            return new List<Game>();
        }
    }

    public async Task<long> GetTotalGameCountAsync(string? status = null)
    {
        try
        {
            var filter = status != null
                ? Builders<Game>.Filter.Eq(g => g.Status, status)
                : Builders<Game>.Filter.Empty;

            return await _games.CountDocumentsAsync(filter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to count total games");
            return 0;
        }
    }

    public async Task DeleteGameAsync(string gameId)
    {
        try
        {
            var filter = Builders<Game>.Filter.Eq(g => g.GameId, gameId);
            await _games.DeleteOneAsync(filter);
            _logger.LogInformation("Deleted game {GameId}", gameId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete game {GameId}", gameId);
            throw;
        }
    }
}
