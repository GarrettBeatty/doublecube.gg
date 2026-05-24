using Backgammon.Server.Grains;
using Backgammon.Server.Grains.Interfaces;
using Backgammon.Server.Hubs.Interfaces;
using Backgammon.Server.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Backgammon.Server.Hubs;

/// <summary>
/// GameHub partial class - Analysis Session Operations.
/// Thin SignalR adapter over <see cref="IAnalysisSessionGrain"/> (per-user grain).
/// </summary>
public partial class GameHub
{
    /// <summary>Resolve the analysis grain key for the current connection.</summary>
    private string GetAnalysisGrainKey() => GetAuthenticatedUserId() ?? Context.ConnectionId;

    /// <summary>Get the analysis grain for the current connection's user.</summary>
    private IAnalysisSessionGrain GetAnalysisGrain() =>
        _grainFactory.GetGrain<IAnalysisSessionGrain>(GetAnalysisGrainKey());

    /// <summary>Broadcast a state update to every connection in an analysis SignalR group.</summary>
    private Task BroadcastAnalysisAsync(string sessionId, GameState state) =>
        Clients.Group($"analysis-{sessionId}").GameUpdate(state);

    /// <summary>
    /// Create a new analysis session.
    /// </summary>
    public async Task<string> CreateAnalysisSession()
    {
        try
        {
            var connectionId = Context.ConnectionId;
            var result = await GetAnalysisGrain().CreateSessionAsync(connectionId);

            if (!string.IsNullOrEmpty(result.Error) || string.IsNullOrEmpty(result.SessionId))
            {
                throw new HubException(result.Error ?? "Failed to create analysis session");
            }

            await Groups.AddToGroupAsync(connectionId, $"analysis-{result.SessionId}");
            await Clients.Caller.GameStart(result.State!);

            _logger.LogInformation("Created analysis session {SessionId}", result.SessionId);
            return result.SessionId;
        }
        catch (HubException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating analysis session");
            throw new HubException("Failed to create analysis session");
        }
    }

    /// <summary>
    /// Join an existing analysis session (multi-tab).
    /// </summary>
    public async Task JoinAnalysisSession(string sessionId)
    {
        try
        {
            var connectionId = Context.ConnectionId;
            var result = await GetAnalysisGrain().JoinSessionAsync(sessionId, connectionId);

            if (!string.IsNullOrEmpty(result.Error) || string.IsNullOrEmpty(result.SessionId))
            {
                await Clients.Caller.Error(result.Error ?? "Failed to join analysis session");
                return;
            }

            await Groups.AddToGroupAsync(connectionId, $"analysis-{result.SessionId}");
            await Clients.Caller.GameUpdate(result.State!);
            _logger.LogInformation("Connection {ConnectionId} joined analysis session {SessionId}", connectionId, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining analysis session {SessionId}", sessionId);
            await Clients.Caller.Error("Failed to join analysis session");
        }
    }

    /// <summary>
    /// Leave the current analysis session.
    /// </summary>
    public async Task LeaveAnalysisSession()
    {
        try
        {
            var connectionId = Context.ConnectionId;
            var sessionId = await GetAnalysisGrain().LeaveSessionAsync(connectionId);
            if (sessionId == null) return;

            await Groups.RemoveFromGroupAsync(connectionId, $"analysis-{sessionId}");
            _logger.LogInformation("Connection {ConnectionId} left analysis session {SessionId}", connectionId, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving analysis session");
        }
    }

    /// <summary>
    /// Dispatch an analysis-mode action: if the caller is in an analysis session, run
    /// <paramref name="action"/> against it and broadcast; returns true if dispatched.
    /// </summary>
    private async Task<bool> TryDispatchAnalysisAsync(Func<IAnalysisSessionGrain, string, Task<AnalysisActionResult>> action)
    {
        var grain = GetAnalysisGrain();
        var sessionId = await grain.GetSessionIdForConnectionAsync(Context.ConnectionId);
        if (sessionId == null) return false;

        var result = await action(grain, sessionId);
        if (!string.IsNullOrEmpty(result.Error))
        {
            await Clients.Caller.Error(result.Error);
        }
        else if (result.State != null)
        {
            await BroadcastAnalysisAsync(sessionId, result.State);
        }

        return true;
    }
}
