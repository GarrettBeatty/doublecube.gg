namespace Backgammon.Server.Services;

/// <summary>
/// Manages ephemeral analysis sessions for single-user position exploration.
/// Unlike game sessions, analysis sessions are not persisted.
/// </summary>
public interface IAnalysisSessionManager
{
    /// <summary>
    /// Create a new analysis session for a user.
    /// </summary>
    AnalysisSession CreateSession(string userId, string connectionId);

    /// <summary>
    /// Get an analysis session by its ID.
    /// </summary>
    AnalysisSession? GetSession(string sessionId);

    /// <summary>
    /// Get the analysis session associated with a SignalR connection.
    /// </summary>
    AnalysisSession? GetSessionByConnection(string connectionId);

    /// <summary>
    /// Join an existing analysis session.
    /// </summary>
    AnalysisSession? JoinSession(string sessionId, string userId, string connectionId);

    /// <summary>
    /// Remove a connection from any analysis session it belongs to.
    /// </summary>
    void RemoveConnection(string connectionId);

    /// <summary>
    /// Remove an analysis session entirely.
    /// </summary>
    void RemoveSession(string sessionId);

    /// <summary>
    /// Clean up sessions that have been inactive longer than the specified timeout.
    /// </summary>
    int CleanupInactiveSessions(TimeSpan? maxInactivity = null);

    /// <summary>
    /// Get the number of active analysis sessions.
    /// </summary>
    int GetSessionCount();

    /// <summary>
    /// Get all analysis sessions for a specific user.
    /// </summary>
    IReadOnlyList<AnalysisSession> GetUserSessions(string userId);
}
