namespace Backgammon.Server.Services;

/// <summary>
/// Handles in-game chat functionality with validation, rate limiting, and content sanitization.
/// </summary>
public interface IChatService
{
    /// <summary>
    /// Send a chat message to all players in the game.
    /// Messages are validated for length (max 500 chars), rate limited (10/min), and sanitized.
    /// </summary>
    /// <param name="connectionId">The SignalR connection ID of the sender.</param>
    /// <param name="message">The message to send.</param>
    Task SendChatMessageAsync(string connectionId, string message);

    /// <summary>
    /// Cleans up rate limit history for a disconnected connection.
    /// Call this when a connection is terminated to prevent memory leaks.
    /// </summary>
    /// <param name="connectionId">The connection ID to clean up.</param>
    void CleanupConnection(string connectionId);
}
