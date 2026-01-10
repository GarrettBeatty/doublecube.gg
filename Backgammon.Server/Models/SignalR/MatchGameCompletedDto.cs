using Tapper;

namespace Backgammon.Server.Models.SignalR;

/// <summary>
/// Data sent when an individual game in a match completes
/// </summary>
[TranspilationSource]
public class MatchGameCompletedDto
{
    public string MatchId { get; set; } = string.Empty;

    public string GameId { get; set; } = string.Empty;

    public string Winner { get; set; } = string.Empty;

    public int Points { get; set; }
}
