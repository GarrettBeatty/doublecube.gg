using Tapper;

namespace Backgammon.Server.Models.SignalR;

/// <summary>
/// Data sent when a match game is starting
/// </summary>
[TranspilationSource]
public class MatchGameStartingDto
{
    public string MatchId { get; set; } = string.Empty;

    public string GameId { get; set; } = string.Empty;
}
