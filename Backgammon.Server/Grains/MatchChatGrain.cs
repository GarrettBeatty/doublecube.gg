using Backgammon.Server.Grains.Interfaces;
using Backgammon.Server.Models;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Backgammon.Server.Grains;

/// <summary>
/// Per-match chat grain (key = matchId). Holds history + per-connection rate-limit
/// timestamps. Ephemeral; grain deactivation drops the state.
/// </summary>
public class MatchChatGrain : Grain, IMatchChatGrain
{
    /// <summary>Maximum chat history retained per match (FIFO).</summary>
    public const int MaxMessagesPerMatch = 500;

    /// <summary>Maximum message length in characters; longer messages are truncated.</summary>
    public const int MaxMessageLength = 500;

    /// <summary>Messages allowed per rate-limit window, per connection.</summary>
    public const int MaxMessagesPerWindow = 10;

    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(1);

    private readonly List<ChatMessage> _history = new();
    private readonly Dictionary<string, List<DateTime>> _rateLimits = new();
    private readonly ILogger<MatchChatGrain> _logger;

    public MatchChatGrain(ILogger<MatchChatGrain> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<TrySendChatResult> TrySendMessageAsync(string connectionId, string senderName, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return Task.FromResult(new TrySendChatResult { Error = "Empty message" });
        }

        if (IsRateLimited(connectionId))
        {
            _logger.LogWarning(
                "Rate limit exceeded for connection {ConnectionId} in match {MatchId}",
                connectionId,
                this.GetPrimaryKeyString());
            return Task.FromResult(new TrySendChatResult
            {
                Error = $"Rate limit exceeded. Maximum {MaxMessagesPerWindow} messages per minute.",
            });
        }

        if (message.Length > MaxMessageLength)
        {
            message = message[..MaxMessageLength];
        }

        var sanitized = message.Trim();
        RecordSend(connectionId);

        _history.Add(new ChatMessage
        {
            SenderName = senderName,
            Message = sanitized,
            SenderConnectionId = connectionId,
            Timestamp = DateTime.UtcNow,
        });

        if (_history.Count > MaxMessagesPerMatch)
        {
            _history.RemoveAt(0);
        }

        return Task.FromResult(new TrySendChatResult { Message = sanitized });
    }

    /// <inheritdoc/>
    public Task<List<ChatMessage>> GetHistoryAsync()
    {
        return Task.FromResult(_history.ToList());
    }

    /// <inheritdoc/>
    public Task ClearAsync()
    {
        _history.Clear();
        _rateLimits.Clear();
        _logger.LogInformation("Cleared chat for match {MatchId}", this.GetPrimaryKeyString());
        return Task.CompletedTask;
    }

    private bool IsRateLimited(string connectionId)
    {
        if (!_rateLimits.TryGetValue(connectionId, out var timestamps))
        {
            return false;
        }

        var cutoff = DateTime.UtcNow - RateLimitWindow;
        timestamps.RemoveAll(t => t < cutoff);
        return timestamps.Count >= MaxMessagesPerWindow;
    }

    private void RecordSend(string connectionId)
    {
        if (!_rateLimits.TryGetValue(connectionId, out var timestamps))
        {
            timestamps = new List<DateTime>();
            _rateLimits[connectionId] = timestamps;
        }

        timestamps.Add(DateTime.UtcNow);
    }
}
