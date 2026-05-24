using Backgammon.Server.Models;

namespace Backgammon.Server.Grains;

/// <summary>
/// Result of an <see cref="Interfaces.IAnalysisSessionGrain"/> action: either the
/// updated state to broadcast to the session's connections, or an error message
/// describing why the action was rejected.
/// </summary>
[GenerateSerializer]
public sealed class AnalysisActionResult
{
    /// <summary>The session id the action targeted; useful for the caller's SignalR group.</summary>
    [Id(0)]
    public string? SessionId { get; set; }

    /// <summary>Updated state, populated on success.</summary>
    [Id(1)]
    public GameState? State { get; set; }

    /// <summary>Error string, populated on failure.</summary>
    [Id(2)]
    public string? Error { get; set; }
}
