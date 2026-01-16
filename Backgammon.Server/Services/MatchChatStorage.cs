using System.Collections.Concurrent;
using Backgammon.Server.Models;

namespace Backgammon.Server.Services;

/// <summary>
/// In-memory implementation of match chat storage.
/// Thread-safe for concurrent access from multiple connections.
/// </summary>
public class MatchChatStorage : IMatchChatStorage
{
    /// <summary>
    /// Maximum number of messages to store per match to prevent unbounded growth.
    /// </summary>
    private const int MaxMessagesPerMatch = 500;

    private readonly ConcurrentDictionary<string, List<ChatMessage>> _matchMessages = new();

    /// <inheritdoc />
    public void AddMessage(string matchId, ChatMessage message)
    {
        var messages = _matchMessages.GetOrAdd(matchId, _ => new List<ChatMessage>());
        lock (messages)
        {
            messages.Add(message);

            // Prevent unbounded growth by removing oldest messages
            if (messages.Count > MaxMessagesPerMatch)
            {
                messages.RemoveAt(0);
            }
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<ChatMessage> GetMessages(string matchId)
    {
        if (_matchMessages.TryGetValue(matchId, out var messages))
        {
            lock (messages)
            {
                return messages.ToList().AsReadOnly();
            }
        }

        return Array.Empty<ChatMessage>();
    }

    /// <inheritdoc />
    public void ClearMatch(string matchId)
    {
        _matchMessages.TryRemove(matchId, out _);
    }
}
