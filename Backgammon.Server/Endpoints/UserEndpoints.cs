using System.Security.Claims;
using Backgammon.Server.Models;
using Backgammon.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Backgammon.Server.Endpoints;

/// <summary>
/// User profile and search endpoints.
/// </summary>
public static class UserEndpoints
{
    /// <summary>
    /// Maps the /api/users/* endpoints onto the given route builder.
    /// </summary>
    /// <param name="app">The route builder to register endpoints on.</param>
    /// <param name="corsPolicy">The CORS policy name to require on every endpoint in this group.</param>
    public static void MapUserEndpoints(this IEndpointRouteBuilder app, string corsPolicy)
    {
        var group = app.MapGroup("/api/users").RequireCors(corsPolicy);

        group.MapGet("/{userId}", async (string userId, IUserRepository userRepository) =>
        {
            var user = await userRepository.GetByUserIdAsync(userId);
            if (user == null)
            {
                return Results.NotFound(new { error = "User not found" });
            }

            return Results.Ok(UserDto.FromUser(user));
        });

        group.MapPut("/profile", async (UpdateProfileRequest request, HttpContext context, IUserRepository userRepository) =>
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            var user = await userRepository.GetByUserIdAsync(userId);
            if (user == null)
            {
                return Results.NotFound(new { error = "User not found" });
            }

            if (!string.IsNullOrWhiteSpace(request.DisplayName))
            {
                user.DisplayName = request.DisplayName;
            }

            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                if (await userRepository.EmailExistsAsync(request.Email) &&
                    user.EmailNormalized != request.Email.ToLowerInvariant())
                {
                    return Results.BadRequest(new { error = "Email already in use" });
                }

                user.Email = request.Email;
                user.EmailNormalized = request.Email.ToLowerInvariant();
            }

            if (request.ProfilePrivacy.HasValue)
            {
                user.ProfilePrivacy = request.ProfilePrivacy.Value;
            }

            if (request.GameHistoryPrivacy.HasValue)
            {
                user.GameHistoryPrivacy = request.GameHistoryPrivacy.Value;
            }

            if (request.FriendsListPrivacy.HasValue)
            {
                user.FriendsListPrivacy = request.FriendsListPrivacy.Value;
            }

            await userRepository.UpdateUserAsync(user);
            return Results.Ok(UserDto.FromUser(user));
        }).RequireAuthorization();

        group.MapGet("/search", async (string q, IUserRepository userRepository) =>
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            {
                return Results.BadRequest(new { error = "Search query must be at least 2 characters" });
            }

            var users = await userRepository.SearchUsersAsync(q, 10);
            return Results.Ok(users.Select(u => new
            {
                userId = u.UserId,
                username = u.Username,
                displayName = u.DisplayName
            }));
        }).RequireAuthorization();
    }
}
