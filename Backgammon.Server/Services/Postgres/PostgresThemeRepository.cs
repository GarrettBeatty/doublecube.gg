using Backgammon.Server.Data;
using Backgammon.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace Backgammon.Server.Services.Postgres;

/// <summary>
/// PostgreSQL implementation of <see cref="IThemeRepository"/> using EF Core.
/// </summary>
public class PostgresThemeRepository : IThemeRepository
{
    private readonly IDbContextFactory<BackgammonDbContext> _dbFactory;

    /// <summary>
    /// Initializes a new instance of <see cref="PostgresThemeRepository"/>.
    /// </summary>
    public PostgresThemeRepository(IDbContextFactory<BackgammonDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    /// <inheritdoc />
    public async Task<BoardTheme?> GetByIdAsync(string themeId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.BoardThemes.AsNoTracking().FirstOrDefaultAsync(t => t.ThemeId == themeId);
    }

    /// <inheritdoc />
    public async Task<(List<BoardTheme> Themes, string? NextCursor)> GetPublicThemesAsync(
        int limit = 50,
        string? cursor = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var query = db.BoardThemes
            .AsNoTracking()
            .Where(t => t.Visibility == ThemeVisibility.Public);

        if (cursor is not null)
        {
            query = query.Where(t => string.Compare(t.ThemeId, cursor, StringComparison.Ordinal) > 0);
        }

        var themes = await query
            .OrderByDescending(t => t.LikeCount)
            .ThenBy(t => t.ThemeId)
            .Take(limit + 1)
            .ToListAsync();

        string? nextCursor = null;
        if (themes.Count > limit)
        {
            nextCursor = themes[limit].ThemeId;
            themes.RemoveAt(limit);
        }

        return (themes, nextCursor);
    }

    /// <inheritdoc />
    public async Task<List<BoardTheme>> GetThemesByAuthorAsync(string authorId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.BoardThemes
            .AsNoTracking()
            .Where(t => t.AuthorId == authorId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<BoardTheme>> GetDefaultThemesAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.BoardThemes
            .AsNoTracking()
            .Where(t => t.IsDefault)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task CreateThemeAsync(BoardTheme theme)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.BoardThemes.Add(theme);
        await db.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateThemeAsync(BoardTheme theme)
    {
        theme.UpdatedAt = DateTime.UtcNow;
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.BoardThemes.Update(theme);
        await db.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteThemeAsync(string themeId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.ThemeLikes.Where(l => l.ThemeId == themeId).ExecuteDeleteAsync();
        await db.BoardThemes.Where(t => t.ThemeId == themeId).ExecuteDeleteAsync();
    }

    /// <inheritdoc />
    public async Task IncrementUsageCountAsync(string themeId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.BoardThemes
            .Where(t => t.ThemeId == themeId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.UsageCount, t => t.UsageCount + 1));
    }

    /// <inheritdoc />
    public async Task DecrementUsageCountAsync(string themeId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.BoardThemes
            .Where(t => t.ThemeId == themeId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.UsageCount, t => Math.Max(0, t.UsageCount - 1)));
    }

    /// <inheritdoc />
    public async Task<bool> HasUserLikedThemeAsync(string themeId, string userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.ThemeLikes.AnyAsync(l => l.ThemeId == themeId && l.UserId == userId);
    }

    /// <inheritdoc />
    public async Task LikeThemeAsync(string themeId, string userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var already = await db.ThemeLikes.AnyAsync(l => l.ThemeId == themeId && l.UserId == userId);
        if (already)
        {
            return;
        }

        db.ThemeLikes.Add(new ThemeLike { ThemeId = themeId, UserId = userId });
        await db.BoardThemes
            .Where(t => t.ThemeId == themeId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.LikeCount, t => t.LikeCount + 1));
        await db.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UnlikeThemeAsync(string themeId, string userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var deleted = await db.ThemeLikes
            .Where(l => l.ThemeId == themeId && l.UserId == userId)
            .ExecuteDeleteAsync();

        if (deleted > 0)
        {
            await db.BoardThemes
                .Where(t => t.ThemeId == themeId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.LikeCount, t => Math.Max(0, t.LikeCount - 1)));
        }
    }

    /// <inheritdoc />
    public async Task<List<string>> GetUserLikedThemeIdsAsync(string userId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.ThemeLikes
            .AsNoTracking()
            .Where(l => l.UserId == userId)
            .Select(l => l.ThemeId)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<BoardTheme>> SearchThemesAsync(string query, int limit = 20)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.BoardThemes
            .AsNoTracking()
            .Where(t => t.Visibility == ThemeVisibility.Public &&
                        EF.Functions.ILike(t.Name, $"%{query}%"))
            .OrderByDescending(t => t.LikeCount)
            .Take(limit)
            .ToListAsync();
    }
}
