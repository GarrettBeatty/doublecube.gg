using Backgammon.Server.Models;

namespace Backgammon.Server.Services;

/// <summary>
/// Interface for board theme data access operations.
/// </summary>
public interface IThemeRepository
{
    /// <summary>
    /// Get a theme by its unique ID.
    /// </summary>
    Task<BoardTheme?> GetByIdAsync(string themeId);

    /// <summary>
    /// Get all public themes, paginated and sorted by popularity.
    /// </summary>
    /// <param name="limit">Maximum number of themes to return.</param>
    /// <param name="cursor">Optional cursor for pagination.</param>
    /// <returns>List of public themes.</returns>
    Task<(List<BoardTheme> Themes, string? NextCursor)> GetPublicThemesAsync(int limit = 50, string? cursor = null);

    /// <summary>
    /// Get all themes created by a specific user.
    /// </summary>
    Task<List<BoardTheme>> GetThemesByAuthorAsync(string authorId);

    /// <summary>
    /// Get all system default themes.
    /// </summary>
    Task<List<BoardTheme>> GetDefaultThemesAsync();

    /// <summary>
    /// Create a new theme.
    /// </summary>
    Task CreateThemeAsync(BoardTheme theme);

    /// <summary>
    /// Update an existing theme.
    /// </summary>
    Task UpdateThemeAsync(BoardTheme theme);

    /// <summary>
    /// Delete a theme by ID.
    /// </summary>
    Task DeleteThemeAsync(string themeId);

    /// <summary>
    /// Increment the usage count for a theme.
    /// </summary>
    Task IncrementUsageCountAsync(string themeId);

    /// <summary>
    /// Decrement the usage count for a theme.
    /// </summary>
    Task DecrementUsageCountAsync(string themeId);

    /// <summary>
    /// Check if a user has liked a specific theme.
    /// </summary>
    Task<bool> HasUserLikedThemeAsync(string themeId, string userId);

    /// <summary>
    /// Add a like from a user to a theme.
    /// </summary>
    Task LikeThemeAsync(string themeId, string userId);

    /// <summary>
    /// Remove a like from a user on a theme.
    /// </summary>
    Task UnlikeThemeAsync(string themeId, string userId);

    /// <summary>
    /// Get the IDs of themes liked by a user.
    /// </summary>
    Task<List<string>> GetUserLikedThemeIdsAsync(string userId);

    /// <summary>
    /// Search themes by name.
    /// </summary>
    /// <param name="query">Search query string.</param>
    /// <param name="limit">Maximum results to return.</param>
    Task<List<BoardTheme>> SearchThemesAsync(string query, int limit = 20);
}
