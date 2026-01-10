using System.Collections.Concurrent;
using System.Net;
using Backgammon.Core;
using Backgammon.Server.Hubs;
using Backgammon.Server.Hubs.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Backgammon.Server.Services;

/// <summary>
/// Handles in-game chat functionality with validation, rate limiting, and content sanitization.
/// </summary>
public class ChatService : IChatService
{
    /// <summary>
    /// Maximum allowed message length in characters.
    /// </summary>
    public const int MaxMessageLength = 500;

    /// <summary>
    /// Maximum number of messages allowed per rate limit window.
    /// </summary>
    public const int MaxMessagesPerWindow = 10;

    /// <summary>
    /// Rate limit window duration in minutes.
    /// </summary>
    public const int RateLimitWindowMinutes = 1;

    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(RateLimitWindowMinutes);

    private readonly IGameSessionManager _sessionManager;
    private readonly IHubContext<GameHub, IGameHubClient> _hubContext;
    private readonly ILogger<ChatService> _logger;

    /// <summary>
    /// Tracks message timestamps per connection for rate limiting.
    /// Key: connectionId, Value: list of message timestamps.
    /// </summary>
    private readonly ConcurrentDictionary<string, List<DateTime>> _messageHistory = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatService"/> class.
    /// </summary>
    /// <param name="sessionManager">The game session manager.</param>
    /// <param name="hubContext">The SignalR hub context.</param>
    /// <param name="logger">The logger instance.</param>
    public ChatService(
        IGameSessionManager sessionManager,
        IHubContext<GameHub, IGameHubClient> hubContext,
        ILogger<ChatService> logger)
    {
        _sessionManager = sessionManager;
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SendChatMessageAsync(string connectionId, string message)
    {
        // Validate message is not empty
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        // Check rate limit
        if (IsRateLimited(connectionId))
        {
            _logger.LogWarning("Rate limit exceeded for connection {ConnectionId}", connectionId);
            await _hubContext.Clients.Client(connectionId).Error(
                $"Rate limit exceeded. Maximum {MaxMessagesPerWindow} messages per minute.");
            return;
        }

        // Truncate message if too long
        if (message.Length > MaxMessageLength)
        {
            message = message[..MaxMessageLength];
            _logger.LogDebug(
                "Message truncated to {MaxLength} characters for connection {ConnectionId}",
                MaxMessageLength,
                connectionId);
        }

        // Sanitize message content (HTML encode to prevent XSS)
        message = SanitizeMessage(message);

        // Get the game session
        var session = _sessionManager.GetGameByPlayer(connectionId);
        if (session == null)
        {
            await _hubContext.Clients.Client(connectionId).Error("Not in a game");
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

        // Record message for rate limiting
        RecordMessage(connectionId);

        // Broadcast to all players in the game
        await _hubContext.Clients.Group(session.Id).ReceiveChatMessage(
            senderName,
            message,
            connectionId);

        _logger.LogInformation(
            "Chat message from {Sender} in game {GameId}",
            senderName,
            session.Id);
    }

    /// <inheritdoc />
    public void CleanupConnection(string connectionId)
    {
        _messageHistory.TryRemove(connectionId, out _);
    }

    /// <summary>
    /// Sanitizes a message to prevent XSS attacks.
    /// </summary>
    /// <param name="message">The raw message.</param>
    /// <returns>The sanitized message.</returns>
    private static string SanitizeMessage(string message)
    {
        // HTML encode to prevent XSS (React also escapes by default, but defense in depth)
        return WebUtility.HtmlEncode(message);
    }

    /// <summary>
    /// Checks if a connection has exceeded the rate limit.
    /// </summary>
    /// <param name="connectionId">The connection ID to check.</param>
    /// <returns>True if rate limited, false otherwise.</returns>
    private bool IsRateLimited(string connectionId)
    {
        var now = DateTime.UtcNow;
        var cutoff = now - RateLimitWindow;

        if (!_messageHistory.TryGetValue(connectionId, out var timestamps))
        {
            return false;
        }

        lock (timestamps)
        {
            // Remove expired timestamps
            timestamps.RemoveAll(t => t < cutoff);

            return timestamps.Count >= MaxMessagesPerWindow;
        }
    }

    /// <summary>
    /// Records a message timestamp for rate limiting.
    /// </summary>
    /// <param name="connectionId">The connection ID.</param>
    private void RecordMessage(string connectionId)
    {
        var now = DateTime.UtcNow;
        var timestamps = _messageHistory.GetOrAdd(connectionId, _ => new List<DateTime>());

        lock (timestamps)
        {
            timestamps.Add(now);
        }
    }
}
