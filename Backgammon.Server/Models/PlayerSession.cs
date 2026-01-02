namespace Backgammon.Server.Models;

/// <summary>
/// Player session information for tracking across reconnections
/// </summary>
public class PlayerSession
{
    public string PlayerId { get; set; } = string.Empty;

    public string ConnectionId { get; set; } = string.Empty;

    public string? Username { get; set; }

    public DateTime LastSeen { get; set; }
}
