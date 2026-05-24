using System.Security.Claims;
using Backgammon.Server.Models;
using Backgammon.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Backgammon.Server.Endpoints;

/// <summary>
/// Endpoints for board-theme browsing, authoring, liking, and per-user preference.
/// </summary>
public static class ThemeEndpoints
{
    /// <summary>
    /// Maps the /api/themes/* endpoints onto the given route builder.
    /// </summary>
    /// <param name="app">The route builder to register endpoints on.</param>
    /// <param name="corsPolicy">The CORS policy name to require on every endpoint in this group.</param>
    public static void MapThemeEndpoints(this IEndpointRouteBuilder app, string corsPolicy)
    {
        var group = app.MapGroup("/api/themes").RequireCors(corsPolicy);

        group.MapGet(string.Empty, async (IThemeRepository themeRepository, int limit = 50, string? cursor = null) =>
        {
            var (themes, nextCursor) = await themeRepository.GetPublicThemesAsync(limit, cursor);
            return Results.Ok(new { themes, nextCursor });
        });

        group.MapGet("/defaults", async (IThemeRepository themeRepository) =>
        {
            var themes = await themeRepository.GetDefaultThemesAsync();
            return Results.Ok(themes);
        });

        group.MapGet("/search", async (string q, IThemeRepository themeRepository) =>
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            {
                return Results.BadRequest(new { error = "Search query must be at least 2 characters" });
            }

            var themes = await themeRepository.SearchThemesAsync(q);
            return Results.Ok(themes);
        });

        group.MapGet("/my", async (HttpContext context, IThemeRepository themeRepository) =>
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            var themes = await themeRepository.GetThemesByAuthorAsync(userId);
            return Results.Ok(themes);
        }).RequireAuthorization();

        group.MapGet("/preference", async (HttpContext context, IUserRepository userRepository) =>
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            var user = await userRepository.GetByUserIdAsync(userId);
            if (user == null)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(new { selectedThemeId = user.SelectedThemeId });
        }).RequireAuthorization();

        group.MapPut("/preference", async (HttpContext context, IUserRepository userRepository, IThemeRepository themeRepository, string? themeId) =>
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            var user = await userRepository.GetByUserIdAsync(userId);
            if (user == null)
            {
                return Results.Unauthorized();
            }

            if (!string.IsNullOrEmpty(themeId))
            {
                var theme = await themeRepository.GetByIdAsync(themeId);
                if (theme == null)
                {
                    return Results.NotFound(new { error = "Theme not found" });
                }

                if (!string.IsNullOrEmpty(user.SelectedThemeId) && user.SelectedThemeId != themeId)
                {
                    await themeRepository.DecrementUsageCountAsync(user.SelectedThemeId);
                }

                if (user.SelectedThemeId != themeId)
                {
                    await themeRepository.IncrementUsageCountAsync(themeId);
                }
            }
            else if (!string.IsNullOrEmpty(user.SelectedThemeId))
            {
                await themeRepository.DecrementUsageCountAsync(user.SelectedThemeId);
            }

            user.SelectedThemeId = themeId;
            await userRepository.UpdateUserAsync(user);
            return Results.Ok(new { selectedThemeId = themeId });
        }).RequireAuthorization();

        group.MapGet("/{themeId}", async (string themeId, IThemeRepository themeRepository) =>
        {
            var theme = await themeRepository.GetByIdAsync(themeId);
            if (theme == null)
            {
                return Results.NotFound(new { error = "Theme not found" });
            }

            return Results.Ok(theme);
        });

        group.MapPost(string.Empty, async (BoardTheme theme, HttpContext context, IThemeRepository themeRepository, IUserRepository userRepository) =>
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            var user = await userRepository.GetByUserIdAsync(userId);
            if (user == null)
            {
                return Results.Unauthorized();
            }

            theme.ThemeId = Guid.NewGuid().ToString();
            theme.AuthorId = userId;
            theme.AuthorUsername = user.Username;
            theme.IsDefault = false;
            theme.CreatedAt = DateTime.UtcNow;
            theme.UpdatedAt = DateTime.UtcNow;
            theme.UsageCount = 0;
            theme.LikeCount = 0;

            await themeRepository.CreateThemeAsync(theme);
            return Results.Created($"/api/themes/{theme.ThemeId}", theme);
        }).RequireAuthorization();

        group.MapPut("/{themeId}", async (string themeId, BoardTheme updatedTheme, HttpContext context, IThemeRepository themeRepository) =>
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            var existingTheme = await themeRepository.GetByIdAsync(themeId);
            if (existingTheme == null)
            {
                return Results.NotFound(new { error = "Theme not found" });
            }

            if (existingTheme.AuthorId != userId)
            {
                return Results.Forbid();
            }

            if (existingTheme.IsDefault)
            {
                return Results.BadRequest(new { error = "Cannot modify default themes" });
            }

            existingTheme.Name = updatedTheme.Name;
            existingTheme.Description = updatedTheme.Description;
            existingTheme.Visibility = updatedTheme.Visibility;
            existingTheme.Colors = updatedTheme.Colors;
            existingTheme.UpdatedAt = DateTime.UtcNow;

            await themeRepository.UpdateThemeAsync(existingTheme);
            return Results.Ok(existingTheme);
        }).RequireAuthorization();

        group.MapDelete("/{themeId}", async (string themeId, HttpContext context, IThemeRepository themeRepository) =>
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            var theme = await themeRepository.GetByIdAsync(themeId);
            if (theme == null)
            {
                return Results.NotFound(new { error = "Theme not found" });
            }

            if (theme.AuthorId != userId)
            {
                return Results.Forbid();
            }

            if (theme.IsDefault)
            {
                return Results.BadRequest(new { error = "Cannot delete default themes" });
            }

            await themeRepository.DeleteThemeAsync(themeId);
            return Results.Ok();
        }).RequireAuthorization();

        group.MapPost("/{themeId}/like", async (string themeId, HttpContext context, IThemeRepository themeRepository) =>
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            var theme = await themeRepository.GetByIdAsync(themeId);
            if (theme == null)
            {
                return Results.NotFound(new { error = "Theme not found" });
            }

            await themeRepository.LikeThemeAsync(themeId, userId);
            return Results.Ok();
        }).RequireAuthorization();

        group.MapDelete("/{themeId}/like", async (string themeId, HttpContext context, IThemeRepository themeRepository) =>
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            await themeRepository.UnlikeThemeAsync(themeId, userId);
            return Results.Ok();
        }).RequireAuthorization();
    }
}
