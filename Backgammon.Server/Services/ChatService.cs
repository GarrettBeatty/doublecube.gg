using Backgammon.Core;
using Backgammon.Server.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Backgammon.Server.Services;

/// <summary>
/// Handles in-game chat functionality
/// </summary>
public class ChatService : IChatService
{
    private readonly IGameSessionManager _sessionManager;
    private readonly IHubContext<GameHub> _hubContext;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        IGameSessionManager sessionManager,
        IHubContext<GameHub> hubContext,
        ILogger<ChatService> logger)
    {
        _sessionManager = sessionManager;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendChatMessageAsync(string connectionId, string message)
    {
        // Validate message is not empty
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        // Get the game session
        var session = _sessionManager.GetGameByPlayer(connectionId);
        if (session == null)
        {
            await _hubContext.Clients.Client(connectionId).SendAsync("Error", "Not in a game");
            return;
        }

        // Determine sender's color and name
        var senderColor = session.GetPlayerColor(connectionId);
        if (senderColor == null)
        {
            return;
        }

        var senderName = senderColor == CheckerColor.White
            ? (session.WhitePlayerName ?? "White")
            : (session.RedPlayerName ?? "Red");

        // Broadcast to all players in the game
        await _hubContext.Clients.Group(session.Id).SendAsync(
            "ReceiveChatMessage",
            senderName,
            message,
            connectionId);

        _logger.LogInformation(
            "Chat message from {Sender} in game {GameId}",
            senderName,
            session.Id);
    }
}
