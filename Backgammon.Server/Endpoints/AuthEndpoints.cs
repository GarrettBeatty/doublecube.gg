using Backgammon.Server.Models;
using Backgammon.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Backgammon.Server.Endpoints;

/// <summary>
/// Authentication endpoints (register, login, current-user lookup).
/// </summary>
public static class AuthEndpoints
{
    /// <summary>
    /// Maps the /api/auth/* endpoints onto the given route builder.
    /// </summary>
    /// <param name="app">The route builder to register endpoints on.</param>
    /// <param name="corsPolicy">The CORS policy name to require on every endpoint in this group.</param>
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app, string corsPolicy)
    {
        var group = app.MapGroup("/api/auth").RequireCors(corsPolicy);

        group.MapPost("/register", async (RegisterRequest request, IAuthService authService) =>
        {
            var result = await authService.RegisterAsync(request);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        });

        group.MapPost("/register-anonymous", async (AnonymousRegisterRequest request, IAuthService authService) =>
        {
            var result = await authService.RegisterAnonymousUserAsync(request.PlayerId, request.DisplayName);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        });

        group.MapPost("/login", async (LoginRequest request, IAuthService authService) =>
        {
            var result = await authService.LoginAsync(request);
            return result.Success ? Results.Ok(result) : Results.Unauthorized();
        });

        group.MapGet("/me", async (HttpContext context, IAuthService authService) =>
        {
            var token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", string.Empty);
            if (string.IsNullOrEmpty(token))
            {
                return Results.Unauthorized();
            }

            var user = await authService.GetUserFromTokenAsync(token);
            return user != null ? Results.Ok(user) : Results.Unauthorized();
        }).RequireAuthorization();
    }
}
