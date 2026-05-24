namespace Backgammon.Server.Models;

/// <summary>
/// Represents a chat message within a match.
/// </summary>
[GenerateSerializer]
public class ChatMessage
{
    /// <summary>
    /// Gets the display name of the sender.
    /// </summary>
    [Id(0)]
    public required string SenderName { get; init; }

    /// <summary>
    /// Gets the sanitized message content.
    /// </summary>
    [Id(1)]
    public required string Message { get; init; }

    /// <summary>
    /// Gets the connection ID of the sender.
    /// </summary>
    [Id(2)]
    public required string SenderConnectionId { get; init; }

    /// <summary>
    /// Gets the timestamp when the message was sent.
    /// </summary>
    [Id(3)]
    public required DateTime Timestamp { get; init; }
}
