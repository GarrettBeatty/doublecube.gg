using Tapper;

namespace Backgammon.Server.Models.SignalR;

/// <summary>
/// Data sent when a player runs out of time
/// </summary>
[TranspilationSource]
public class PlayerTimedOutDto
{
    public string GameId { get; set; } = string.Empty;

    public string TimedOutPlayer { get; set; } = string.Empty;

    public string Winner { get; set; } = string.Empty;
}
