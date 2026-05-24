using Backgammon.Server.Data;
using Backgammon.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace Backgammon.Server.Services.Postgres;

/// <summary>
/// PostgreSQL implementation of <see cref="IMatchRepository"/> using EF Core.
/// </summary>
public class PostgresMatchRepository : IMatchRepository
{
    private readonly IDbContextFactory<BackgammonDbContext> _dbFactory;

    /// <summary>
    /// Initializes a new instance of <see cref="PostgresMatchRepository"/>.
    /// </summary>
    public PostgresMatchRepository(IDbContextFactory<BackgammonDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    /// <inheritdoc />
    public async Task SaveMatchAsync(Match match)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var existing = await db.Matches.FindAsync(match.MatchId);
        if (existing is null)
        {
            db.Matches.Add(match);
        }
        else
        {
            db.Entry(existing).CurrentValues.SetValues(match);
        }

        await db.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<Match?> GetMatchByIdAsync(string matchId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Matches.AsNoTracking().FirstOrDefaultAsync(m => m.MatchId == matchId);
    }

    /// <inheritdoc />
    public async Task UpdateMatchAsync(Match match)
    {
        match.LastUpdatedAt = DateTime.UtcNow;
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.Matches.Update(match);
        await db.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<List<Match>> GetPlayerMatchesAsync(
        string playerId,
        string? status = null,
        int limit = 50,
        int skip = 0)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var query = db.Matches
            .AsNoTracking()
            .Where(m => m.Player1Id == playerId || m.Player2Id == playerId);

        if (status is not null)
        {
            query = query.Where(m => m.Status == status);
        }

        return await query
            .OrderByDescending(m => m.LastUpdatedAt)
            .Skip(skip)
            .Take(limit)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<Match>> GetActiveMatchesAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Matches
            .AsNoTracking()
            .Where(m => m.Status == "InProgress")
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<Match>> GetRecentMatchesAsync(string? status = "Completed", int limit = 20)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var query = db.Matches.AsNoTracking();

        if (status is not null)
        {
            query = query.Where(m => m.Status == status);
        }

        return await query
            .OrderByDescending(m => m.LastUpdatedAt)
            .Take(limit)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<MatchStats> GetPlayerMatchStatsAsync(string playerId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var matches = await db.Matches
            .AsNoTracking()
            .Where(m => (m.Player1Id == playerId || m.Player2Id == playerId)
                        && m.Status == "Completed")
            .Select(m => new { m.Player1Id, m.WinnerId })
            .ToListAsync();

        var stats = new MatchStats
        {
            TotalMatches = matches.Count,
            MatchesWon = matches.Count(m => m.WinnerId == playerId),
            MatchesLost = matches.Count(m => m.WinnerId != null && m.WinnerId != playerId),
        };

        return stats;
    }

    /// <inheritdoc />
    public async Task<List<Match>> GetOpenLobbiesAsync(int limit = 50, bool? isCorrespondence = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var query = db.Matches
            .AsNoTracking()
            .Where(m => m.LobbyStatus == "WaitingForOpponent" && m.IsOpenLobby);

        if (isCorrespondence.HasValue)
        {
            query = query.Where(m => m.IsCorrespondence == isCorrespondence.Value);
        }

        return await query
            .OrderByDescending(m => m.LastUpdatedAt)
            .Take(limit)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task DeleteMatchAsync(string matchId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.Matches.Where(m => m.MatchId == matchId).ExecuteDeleteAsync();
    }

    /// <inheritdoc />
    public async Task AddGameToMatchAsync(string matchId, string gameId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var match = await db.Matches.FirstOrDefaultAsync(m => m.MatchId == matchId);
        if (match is null)
        {
            return;
        }

        if (!match.GameIds.Contains(gameId))
        {
            match.GameIds.Add(gameId);
            match.LastUpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task UpdateMatchStatusAsync(string matchId, string status)
    {
        var now = DateTime.UtcNow;
        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.Matches
            .Where(m => m.MatchId == matchId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.Status, status)
                .SetProperty(m => m.LastUpdatedAt, now));
    }

    /// <inheritdoc />
    public async Task<List<Match>> GetCorrespondenceMatchesForTurnAsync(string playerId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Matches
            .AsNoTracking()
            .Where(m => m.IsCorrespondence
                        && m.Status == "InProgress"
                        && m.CurrentTurnPlayerId == playerId)
            .OrderBy(m => m.TurnDeadline)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<Match>> GetCorrespondenceMatchesWaitingAsync(string playerId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Matches
            .AsNoTracking()
            .Where(m => m.IsCorrespondence
                        && m.Status == "InProgress"
                        && (m.Player1Id == playerId || m.Player2Id == playerId)
                        && m.CurrentTurnPlayerId != playerId)
            .OrderByDescending(m => m.LastUpdatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<Match>> GetExpiredCorrespondenceMatchesAsync()
    {
        var now = DateTime.UtcNow;
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Matches
            .AsNoTracking()
            .Where(m => m.IsCorrespondence
                        && m.Status == "InProgress"
                        && m.TurnDeadline.HasValue
                        && m.TurnDeadline.Value < now)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task UpdateCorrespondenceTurnAsync(
        string matchId,
        string currentTurnPlayerId,
        DateTime turnDeadline)
    {
        var now = DateTime.UtcNow;
        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.Matches
            .Where(m => m.MatchId == matchId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.CurrentTurnPlayerId, currentTurnPlayerId)
                .SetProperty(m => m.TurnDeadline, turnDeadline)
                .SetProperty(m => m.LastUpdatedAt, now));
    }

    /// <inheritdoc />
    public async Task CreatePlayerMatchIndexAsync(
        string playerId,
        string matchId,
        string opponentId,
        string status,
        DateTime createdAt)
    {
        // In PostgreSQL, player-match relationships are implicit via Player1Id/Player2Id columns.
        // This method is a no-op since the match row already captures both player IDs.
        // Left here to satisfy the interface contract during migration.
        await Task.CompletedTask;
    }
}
