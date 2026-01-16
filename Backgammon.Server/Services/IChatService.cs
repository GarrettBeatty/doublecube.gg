using Backgammon.Server.Models;

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

    /// <summary>
    /// Gets the chat history for a match.
    /// </summary>
    /// <param name="matchId">The match ID.</param>
    /// <returns>A read-only list of chat messages for the match.</returns>
    IReadOnlyList<ChatMessage> GetMatchChatHistory(string matchId);

    /// <summary>
    /// Clears the chat history for a match.
    /// Call this when a match ends to free up memory.
    /// </summary>
    /// <param name="matchId">The match ID.</param>
    void ClearMatchChat(string matchId);
}
