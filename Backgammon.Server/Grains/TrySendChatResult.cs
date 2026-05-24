namespace Backgammon.Server.Grains;

/// <summary>
/// Result of <see cref="Interfaces.IMatchChatGrain.TrySendMessageAsync"/>: either the
/// sanitized message that was accepted (so the caller can broadcast it) or an error
/// describing why it was rejected (e.g. rate limited).
/// </summary>
[GenerateSerializer]
public sealed class TrySendChatResult
{
    /// <summary>Sanitized + truncated message, populated when <see cref="Error"/> is null.</summary>
    [Id(0)]
    public string? Message { get; set; }

    /// <summary>Error message describing why the send was rejected, or null on success.</summary>
    [Id(1)]
    public string? Error { get; set; }
}
