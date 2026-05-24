using Backgammon.Server.Data;
using Backgammon.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace Backgammon.Server.Services.Postgres;

/// <summary>
/// PostgreSQL implementation of <see cref="IPuzzleRepository"/> using EF Core.
/// </summary>
public class PostgresPuzzleRepository : IPuzzleRepository
{
    private readonly IDbContextFactory<BackgammonDbContext> _dbFactory;

    /// <summary>
    /// Initializes a new instance of <see cref="PostgresPuzzleRepository"/>.
    /// </summary>
    public PostgresPuzzleRepository(IDbContextFactory<BackgammonDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    /// <inheritdoc />
    public async Task SavePuzzleAsync(DailyPuzzle puzzle)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var existing = await db.DailyPuzzles.FindAsync(puzzle.PuzzleId);
        if (existing is null)
        {
            db.DailyPuzzles.Add(puzzle);
        }
        else
        {
            db.Entry(existing).CurrentValues.SetValues(puzzle);
        }

        await db.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<DailyPuzzle?> GetPuzzleByDateAsync(string date)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.DailyPuzzles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PuzzleDate == date);
    }

    /// <inheritdoc />
    public async Task<bool> PuzzleExistsAsync(string date)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.DailyPuzzles.AnyAsync(p => p.PuzzleDate == date);
    }

    /// <inheritdoc />
    public async Task IncrementSolvedCountAsync(string puzzleDate)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.DailyPuzzles
            .Where(p => p.PuzzleDate == puzzleDate)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.SolvedCount, p => p.SolvedCount + 1));
    }

    /// <inheritdoc />
    public async Task IncrementAttemptCountAsync(string puzzleDate)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.DailyPuzzles
            .Where(p => p.PuzzleDate == puzzleDate)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.AttemptCount, p => p.AttemptCount + 1));
    }

    /// <inheritdoc />
    public async Task SaveAttemptAsync(PuzzleAttempt attempt)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.PuzzleAttempts.Add(attempt);
        await db.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<PuzzleAttempt?> GetAttemptAsync(string userId, string puzzleDate)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.PuzzleAttempts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserId == userId && a.PuzzleDate == puzzleDate);
    }

    /// <inheritdoc />
    public async Task UpdateAttemptAsync(PuzzleAttempt attempt)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.PuzzleAttempts.Update(attempt);
        await db.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<PuzzleStreakInfo?> GetStreakInfoAsync(string userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.PuzzleStreaks
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId);
    }

    /// <inheritdoc />
    public async Task SaveStreakInfoAsync(PuzzleStreakInfo streakInfo)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var existing = await db.PuzzleStreaks.FindAsync(streakInfo.UserId);
        if (existing is null)
        {
            db.PuzzleStreaks.Add(streakInfo);
        }
        else
        {
            db.Entry(existing).CurrentValues.SetValues(streakInfo);
        }

        await db.SaveChangesAsync();
    }
}
