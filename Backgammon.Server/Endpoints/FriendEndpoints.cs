using System.Security.Claims;
using Backgammon.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Backgammon.Server.Endpoints;

/// <summary>
/// Endpoints for managing friend lists, requests, blocks, and game invites.
/// </summary>
public static class FriendEndpoints
{
    /// <summary>
    /// Maps the /api/friends/* endpoints onto the given route builder.
    /// </summary>
    /// <param name="app">The route builder to register endpoints on.</param>
    /// <param name="corsPolicy">The CORS policy name to require on every endpoint in this group.</param>
    public static void MapFriendEndpoints(this IEndpointRouteBuilder app, string corsPolicy)
    {
        var group = app.MapGroup("/api/friends").RequireCors(corsPolicy).RequireAuthorization();

        group.MapGet(string.Empty, async (HttpContext context, IFriendService friendService) =>
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            var friends = await friendService.GetFriendsAsync(userId);
            return Results.Ok(friends);
        });

        group.MapGet("/requests", async (HttpContext context, IFriendService friendService) =>
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            var requests = await friendService.GetPendingRequestsAsync(userId);
            return Results.Ok(requests);
        });

        group.MapPost("/request/{toUserId}", async (string toUserId, HttpContext context, IFriendService friendService) =>
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            var (success, error) = await friendService.SendFriendRequestAsync(userId, toUserId);
            return success ? Results.Ok() : Results.BadRequest(new { error });
        });

        group.MapPost("/accept/{friendUserId}", async (string friendUserId, HttpContext context, IFriendService friendService) =>
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            var (success, error) = await friendService.AcceptFriendRequestAsync(userId, friendUserId);
            return success ? Results.Ok() : Results.BadRequest(new { error });
        });

        group.MapPost("/decline/{friendUserId}", async (string friendUserId, HttpContext context, IFriendService friendService) =>
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            var (success, error) = await friendService.DeclineFriendRequestAsync(userId, friendUserId);
            return success ? Results.Ok() : Results.BadRequest(new { error });
        });

        group.MapDelete("/{friendUserId}", async (string friendUserId, HttpContext context, IFriendService friendService) =>
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            var (success, error) = await friendService.RemoveFriendAsync(userId, friendUserId);
            return success ? Results.Ok() : Results.BadRequest(new { error });
        });

        group.MapPost("/block/{blockedUserId}", async (string blockedUserId, HttpContext context, IFriendService friendService) =>
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            var (success, error) = await friendService.BlockUserAsync(userId, blockedUserId);
            return success ? Results.Ok() : Results.BadRequest(new { error });
        });

        group.MapPost("/invite/{friendUserId}/game/{gameId}", async (string friendUserId, string gameId, HttpContext context, IFriendService friendService) =>
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            var (success, error) = await friendService.InviteFriendToGameAsync(userId, friendUserId, gameId);
            return success ? Results.Ok() : Results.BadRequest(new { error });
        });
    }
}
