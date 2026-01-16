using Microsoft.Extensions.Logging;

namespace Backgammon.Server.Services;

/// <summary>
/// Manages analysis sessions. Unlike GameSessionManager, this handles
/// single-user analysis sessions that are ephemeral and not persisted.
/// </summary>
public class AnalysisSessionManager
{
    /// <summary>
    /// Default timeout for inactive analysis sessions (30 minutes).
    /// </summary>
    public static readonly TimeSpan DefaultInactiveTimeout = TimeSpan.FromMinutes(30);

    private readonly Dictionary<string, AnalysisSession> _sessions = new();
    private readonly Dictionary<string, string> _connectionToSession = new();
    private readonly object _lock = new();
    private readonly ILogger<AnalysisSessionManager> _logger;

    public AnalysisSessionManager(ILogger<AnalysisSessionManager> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Create a new analysis session for a user.
    /// </summary>
    /// <param name="userId">The user creating the session.</param>
    /// <param name="connectionId">The SignalR connection ID.</param>
    /// <returns>The new analysis session.</returns>
    public AnalysisSession CreateSession(string userId, string connectionId)
    {
        lock (_lock)
        {
            var sessionId = Guid.NewGuid().ToString();
            var session = new AnalysisSession(sessionId, userId);
            session.AddConnection(connectionId);

            _sessions[sessionId] = session;
            _connectionToSession[connectionId] = sessionId;

            _logger.LogInformation(
                "Created analysis session {SessionId} for user {UserId}",
                sessionId,
                userId);
            return session;
        }
    }

    /// <summary>
    /// Get an analysis session by ID.
    /// </summary>
    public AnalysisSession? GetSession(string sessionId)
    {
        lock (_lock)
        {
            return _sessions.GetValueOrDefault(sessionId);
        }
    }

    /// <summary>
    /// Get an analysis session by connection ID.
    /// </summary>
    public AnalysisSession? GetSessionByConnection(string connectionId)
    {
        lock (_lock)
        {
            if (_connectionToSession.TryGetValue(connectionId, out var sessionId))
            {
                return _sessions.GetValueOrDefault(sessionId);
            }

            return null;
        }
    }

    /// <summary>
    /// Add a connection to an existing session (e.g., opening in another tab).
    /// </summary>
    /// <param name="sessionId">The session to join.</param>
    /// <param name="userId">The user ID (must match session owner).</param>
    /// <param name="connectionId">The new connection ID.</param>
    /// <returns>The session if found and user matches, null otherwise.</returns>
    public AnalysisSession? JoinSession(string sessionId, string userId, string connectionId)
    {
        lock (_lock)
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
            {
                _logger.LogWarning("Analysis session {SessionId} not found", sessionId);
                return null;
            }

            // Verify the user owns this session
            if (session.UserId != userId)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to join session {SessionId} owned by {OwnerId}",
                    userId,
                    sessionId,
                    session.UserId);
                return null;
            }

            session.AddConnection(connectionId);
            _connectionToSession[connectionId] = sessionId;

            _logger.LogInformation(
                "Connection {ConnectionId} joined analysis session {SessionId}",
                connectionId,
                sessionId);
            return session;
        }
    }

    /// <summary>
    /// Remove a connection from its session.
    /// If the session has no more connections, it will be cleaned up by the cleanup task.
    /// </summary>
    public void RemoveConnection(string connectionId)
    {
        lock (_lock)
        {
            if (_connectionToSession.TryGetValue(connectionId, out var sessionId))
            {
                _connectionToSession.Remove(connectionId);

                if (_sessions.TryGetValue(sessionId, out var session))
                {
                    session.RemoveConnection(connectionId);
                    _logger.LogInformation(
                        "Removed connection {ConnectionId} from analysis session {SessionId}",
                        connectionId,
                        sessionId);
                }
            }
        }
    }

    /// <summary>
    /// Explicitly remove an analysis session.
    /// </summary>
    public void RemoveSession(string sessionId)
    {
        lock (_lock)
        {
            if (_sessions.TryGetValue(sessionId, out var session))
            {
                // Remove all connection mappings
                foreach (var connectionId in session.Connections)
                {
                    _connectionToSession.Remove(connectionId);
                }

                _sessions.Remove(sessionId);
                _logger.LogInformation("Removed analysis session {SessionId}", sessionId);
            }
        }
    }

    /// <summary>
    /// Clean up inactive sessions based on the specified timeout.
    /// </summary>
    /// <param name="maxInactivity">Maximum allowed inactivity before cleanup.</param>
    /// <returns>Number of sessions removed.</returns>
    public int CleanupInactiveSessions(TimeSpan? maxInactivity = null)
    {
        var timeout = maxInactivity ?? DefaultInactiveTimeout;
        var cutoff = DateTime.UtcNow - timeout;

        lock (_lock)
        {
            var inactiveSessions = _sessions
                .Where(kvp => kvp.Value.LastActivityAt < cutoff)
                .ToList();

            foreach (var (sessionId, session) in inactiveSessions)
            {
                // Remove connection mappings
                foreach (var connectionId in session.Connections)
                {
                    _connectionToSession.Remove(connectionId);
                }

                _sessions.Remove(sessionId);
                _logger.LogInformation(
                    "Cleaned up inactive analysis session {SessionId} (last activity: {LastActivity})",
                    sessionId,
                    session.LastActivityAt);
            }

            return inactiveSessions.Count;
        }
    }

    /// <summary>
    /// Get the total number of active analysis sessions.
    /// </summary>
    public int GetSessionCount()
    {
        lock (_lock)
        {
            return _sessions.Count;
        }
    }

    /// <summary>
    /// Get all sessions for a specific user.
    /// </summary>
    public IReadOnlyList<AnalysisSession> GetUserSessions(string userId)
    {
        lock (_lock)
        {
            return _sessions.Values
                .Where(s => s.UserId == userId)
                .ToList();
        }
    }
}
