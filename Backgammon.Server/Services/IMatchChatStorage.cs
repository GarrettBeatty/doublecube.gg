using Backgammon.Server.Models;

namespace Backgammon.Server.Services;

/// <summary>
/// In-memory storage for chat messages within a match.
/// Messages persist across games within a match but are cleared when the match ends or server restarts.
/// </summary>
public interface IMatchChatStorage
{
    /// <summary>
    /// Store a chat message for a match.
    /// </summary>
    /// <param name="matchId">The match ID.</param>
    /// <param name="message">The chat message to store.</param>
    void AddMessage(string matchId, ChatMessage message);

    /// <summary>
    /// Get all chat messages for a match.
    /// </summary>
    /// <param name="matchId">The match ID.</param>
    /// <returns>A read-only list of chat messages.</returns>
    IReadOnlyList<ChatMessage> GetMessages(string matchId);

    /// <summary>
    /// Clear all messages for a match (called when match ends).
    /// </summary>
    /// <param name="matchId">The match ID.</param>
    void ClearMatch(string matchId);
}
