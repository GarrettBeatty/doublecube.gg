using Backgammon.Server.Data;
using Backgammon.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace Backgammon.Server.Services.Postgres;

/// <summary>
/// PostgreSQL implementation of <see cref="IGameRepository"/> using EF Core.
/// </summary>
public class PostgresGameRepository : IGameRepository
{
    private readonly IDbContextFactory<BackgammonDbContext> _dbFactory;

    /// <summary>
    /// Initializes a new instance of <see cref="PostgresGameRepository"/>.
    /// </summary>
    public PostgresGameRepository(IDbContextFactory<BackgammonDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    /// <inheritdoc />
    public async Task SaveGameAsync(Game game)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var existing = await db.Games.FindAsync(game.GameId);
        if (existing is null)
        {
            db.Games.Add(game);
        }
        else
        {
            db.Entry(existing).CurrentValues.SetValues(game);
        }

        await db.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<Game?> GetGameByGameIdAsync(string gameId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Games.AsNoTracking().FirstOrDefaultAsync(g => g.GameId == gameId);
    }

    /// <inheritdoc />
    public async Task<List<Game>> GetActiveGamesAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Games
            .AsNoTracking()
            .Where(g => g.Status == "InProgress")
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task UpdateGameStatusAsync(string gameId, string status)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.Games
            .Where(g => g.GameId == gameId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(g => g.Status, status)
                .SetProperty(g => g.LastUpdatedAt, DateTime.UtcNow));
    }

    /// <inheritdoc />
    public async Task<List<Game>> GetPlayerGamesAsync(
        string playerId,
        string? status = null,
        int limit = 50,
        int skip = 0)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var query = db.Games
            .AsNoTracking()
            .Where(g => g.WhitePlayerId == playerId || g.RedPlayerId == playerId);

        if (status is not null)
        {
            query = query.Where(g => g.Status == status);
        }

        return await query
            .OrderByDescending(g => g.CreatedAt)
            .Skip(skip)
            .Take(limit)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<PlayerStats> GetPlayerStatsAsync(string playerId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var games = await db.Games
            .AsNoTracking()
            .Where(g => (g.WhitePlayerId == playerId || g.RedPlayerId == playerId)
                        && g.Status == "Completed")
            .Select(g => new { g.WhitePlayerId, g.CoreGame })
            .ToListAsync();

        var stats = new PlayerStats();
        foreach (var g in games)
        {
            stats.TotalGames++;
            var wonAsWhite = g.WhitePlayerId == playerId &&
                             g.CoreGame.Winner == Core.CheckerColor.White;
            var wonAsRed = g.WhitePlayerId != playerId &&
                           g.CoreGame.Winner == Core.CheckerColor.Red;
            if (wonAsWhite || wonAsRed)
            {
                stats.Wins++;
                stats.TotalStakes += g.CoreGame.Stakes;
            }
            else
            {
                stats.Losses++;
            }
        }

        return stats;
    }

    /// <inheritdoc />
    public async Task<List<Game>> GetRecentGamesAsync(string? status = "Completed", int limit = 20)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var query = db.Games.AsNoTracking();

        if (status is not null)
        {
            query = query.Where(g => g.Status == status);
        }

        return await query
            .OrderByDescending(g => g.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<long> GetTotalGameCountAsync(string? status = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var query = db.Games.AsNoTracking();

        if (status is not null)
        {
            query = query.Where(g => g.Status == status);
        }

        return await query.LongCountAsync();
    }

    /// <inheritdoc />
    public async Task DeleteGameAsync(string gameId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.Games.Where(g => g.GameId == gameId).ExecuteDeleteAsync();
    }

    /// <inheritdoc />
    public async Task<List<Game>> GetGamesLastUpdatedBeforeAsync(DateTime timestamp, string status)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Games
            .AsNoTracking()
            .Where(g => g.Status == status && g.LastUpdatedAt < timestamp)
            .ToListAsync();
    }
}
