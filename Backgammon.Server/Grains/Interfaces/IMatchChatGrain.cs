using Backgammon.Server.Models;
using Orleans;

namespace Backgammon.Server.Grains.Interfaces;

/// <summary>
/// Per-match chat grain. Owns the in-memory message history (bounded FIFO) and the
/// per-connection rate-limit state. Subsumes the old <c>IMatchChatStorage</c> singleton
/// plus the <c>_messageHistory</c> dict inside <c>ChatService</c>.
///
/// Key = matchId. Ephemeral — no <c>IPersistentState</c>; chat is session-scoped and
/// grain deactivation (e.g. after match ends + idle window) clears everything.
/// </summary>
public interface IMatchChatGrain : IGrainWithStringKey
{
    /// <summary>
    /// Validate, rate-limit, sanitize, and record a chat message. On success returns
    /// the message that should be broadcast; on rejection returns an error string.
    /// </summary>
    Task<TrySendChatResult> TrySendMessageAsync(string connectionId, string senderName, string message);

    /// <summary>Snapshot of the chat history for the match (in send order).</summary>
    Task<List<ChatMessage>> GetHistoryAsync();

    /// <summary>Clear all chat state for this match (called when the match completes or is abandoned).</summary>
    Task ClearAsync();
}
