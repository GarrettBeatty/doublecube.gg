namespace Backgammon.Server.Services;

/// <summary>
/// Handles in-game chat functionality
/// </summary>
public interface IChatService
{
    /// <summary>
    /// Send a chat message to all players in the game
    /// </summary>
    Task SendChatMessageAsync(string connectionId, string message);
}
