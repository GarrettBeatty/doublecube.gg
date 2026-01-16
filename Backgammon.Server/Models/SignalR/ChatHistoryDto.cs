using Tapper;

namespace Backgammon.Server.Models.SignalR;

/// <summary>
/// Data sent when a player joins a game to provide chat history from previous games in the match.
/// </summary>
[TranspilationSource]
public class ChatHistoryDto
{
    /// <summary>
    /// Gets or sets the list of chat messages.
    /// </summary>
    public List<ChatMessageDto> Messages { get; set; } = new();
}
