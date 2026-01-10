using Tapper;

namespace Backgammon.Server.Models.SignalR;

/// <summary>
/// Data sent when an opponent joins a match
/// </summary>
[TranspilationSource]
public class OpponentJoinedMatchDto
{
    public string MatchId { get; set; } = string.Empty;

    public string Player2Id { get; set; } = string.Empty;

    public string Player2Name { get; set; } = string.Empty;
}
