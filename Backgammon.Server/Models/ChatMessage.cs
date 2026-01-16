namespace Backgammon.Server.Models;

/// <summary>
/// Represents a chat message within a match.
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// Gets the display name of the sender.
    /// </summary>
    public required string SenderName { get; init; }

    /// <summary>
    /// Gets the sanitized message content.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the connection ID of the sender.
    /// </summary>
    public required string SenderConnectionId { get; init; }

    /// <summary>
    /// Gets the timestamp when the message was sent.
    /// </summary>
    public required DateTime Timestamp { get; init; }
}
