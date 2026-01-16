using Tapper;

namespace Backgammon.Server.Models.SignalR;

/// <summary>
/// Represents a single chat message in the history.
/// </summary>
[TranspilationSource]
public class ChatMessageDto
{
    /// <summary>
    /// Gets or sets the display name of the sender.
    /// </summary>
    public string SenderName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message content.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the message was sent.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets whether this message was sent by the receiving player.
    /// </summary>
    public bool IsOwn { get; set; }
}
