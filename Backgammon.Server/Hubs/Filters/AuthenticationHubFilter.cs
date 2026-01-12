using Microsoft.AspNetCore.SignalR;

namespace Backgammon.Server.Hubs.Filters;

/// <summary>
/// SignalR Hub Filter that enforces authentication on all hub method invocations.
/// This eliminates the need for manual null checks in every hub method.
/// </summary>
public class AuthenticationHubFilter : IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var httpContext = invocationContext.Context.GetHttpContext();

        // Check if user is authenticated
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            throw new HubException("Authentication required");
        }

        // Check if user has a valid ID claim
        var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            throw new HubException("Invalid authentication token - missing user ID");
        }

        // User is authenticated, proceed with method invocation
        return await next(invocationContext);
    }
}
