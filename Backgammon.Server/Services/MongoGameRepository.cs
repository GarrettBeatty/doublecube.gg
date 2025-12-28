using MongoDB.Driver;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Backgammon.Server.Models;

namespace Backgammon.Server.Services;

/// <summary>
/// MongoDB implementation of game storage.
/// Handles persistence and queries for completed backgammon games.
/// </summary>
public class MongoGameRepository : IGameRepository
{
    private readonly IMongoCollection<CompletedGame> _games;
    private readonly ILogger<MongoGameRepository> _logger;

    public MongoGameRepository(IMongoClient mongoClient, IConfiguration configuration, ILogger<MongoGameRepository> logger)
    {
        _logger = logger;
        
        var databaseName = configuration["MongoDB:DatabaseName"] ?? "backgammon";
        var database = mongoClient.GetDatabase(databaseName);
        _games = database.GetCollection<CompletedGame>("games");
        
        // Create indexes for efficient queries
        CreateIndexes().Wait();
    }

    private async Task CreateIndexes()
    {
        try
        {
            // Index on gameId for fast lookups
            await _games.Indexes.CreateOneAsync(
                new CreateIndexModel<CompletedGame>(
                    Builders<CompletedGame>.IndexKeys.Ascending(g => g.GameId),
                    new CreateIndexOptions { Unique = true }
                )
            );
            
            // Compound index for player queries
            await _games.Indexes.CreateOneAsync(
                new CreateIndexModel<CompletedGame>(
                    Builders<CompletedGame>.IndexKeys
                        .Ascending(g => g.WhitePlayerId)
                        .Descending(g => g.CompletedAt)
                )
            );
            
            await _games.Indexes.CreateOneAsync(
                new CreateIndexModel<CompletedGame>(
                    Builders<CompletedGame>.IndexKeys
                        .Ascending(g => g.RedPlayerId)
                        .Descending(g => g.CompletedAt)
                )
            );
            
            // Index for recent games
            await _games.Indexes.CreateOneAsync(
                new CreateIndexModel<CompletedGame>(
                    Builders<CompletedGame>.IndexKeys.Descending(g => g.CompletedAt)
                )
            );
            
            _logger.LogInformation("MongoDB indexes created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create indexes (may already exist)");
        }
    }

    public async Task SaveCompletedGameAsync(CompletedGame game)
    {
        try
        {
            await _games.InsertOneAsync(game);
            _logger.LogInformation("Saved completed game {GameId} to MongoDB", game.GameId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save game {GameId} to MongoDB", game.GameId);
            throw;
        }
    }

    public async Task<CompletedGame?> GetGameByIdAsync(string gameId)
    {
        try
        {
            var filter = Builders<CompletedGame>.Filter.Eq(g => g.GameId, gameId);
            return await _games.Find(filter).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve game {GameId}", gameId);
            return null;
        }
    }

    public async Task<List<CompletedGame>> GetPlayerGamesAsync(string playerId, int limit = 50, int skip = 0)
    {
        try
        {
            var filter = Builders<CompletedGame>.Filter.Or(
                Builders<CompletedGame>.Filter.Eq(g => g.WhitePlayerId, playerId),
                Builders<CompletedGame>.Filter.Eq(g => g.RedPlayerId, playerId)
            );
            
            return await _games.Find(filter)
                .SortByDescending(g => g.CompletedAt)
                .Skip(skip)
                .Limit(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve games for player {PlayerId}", playerId);
            return new List<CompletedGame>();
        }
    }

    public async Task<PlayerStats> GetPlayerStatsAsync(string playerId)
    {
        try
        {
            var filter = Builders<CompletedGame>.Filter.Or(
                Builders<CompletedGame>.Filter.Eq(g => g.WhitePlayerId, playerId),
                Builders<CompletedGame>.Filter.Eq(g => g.RedPlayerId, playerId)
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
            
            // Calculate stakes (only for wins)
            foreach (var game in games)
            {
                bool isWinner = (game.Winner == "White" && game.WhitePlayerId == playerId) ||
                                (game.Winner == "Red" && game.RedPlayerId == playerId);
                
                if (isWinner)
                {
                    stats.TotalStakes += game.Stakes;
                    
                    if (game.Stakes == 1) stats.NormalWins++;
                    else if (game.Stakes == 2) stats.GammonWins++;
                    else if (game.Stakes == 3) stats.BackgammonWins++;
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

    public async Task<List<CompletedGame>> GetRecentGamesAsync(int limit = 20)
    {
        try
        {
            return await _games.Find(_ => true)
                .SortByDescending(g => g.CompletedAt)
                .Limit(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve recent games");
            return new List<CompletedGame>();
        }
    }

    public async Task<long> GetTotalGameCountAsync()
    {
        try
        {
            return await _games.CountDocumentsAsync(_ => true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to count total games");
            return 0;
        }
    }
}
