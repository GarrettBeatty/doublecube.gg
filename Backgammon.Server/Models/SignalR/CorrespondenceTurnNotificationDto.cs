using Tapper;

namespace Backgammon.Server.Models.SignalR;

/// <summary>
/// Data sent when it's your turn in a correspondence game
/// </summary>
[TranspilationSource]
public class CorrespondenceTurnNotificationDto
{
    public string MatchId { get; set; } = string.Empty;

    public string? GameId { get; set; }

    public string Message { get; set; } = string.Empty;
}
