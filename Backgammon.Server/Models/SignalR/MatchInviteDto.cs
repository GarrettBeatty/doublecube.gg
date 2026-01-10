using Tapper;

namespace Backgammon.Server.Models.SignalR;

/// <summary>
/// Data sent when a friend challenges you to a match
/// </summary>
[TranspilationSource]
public class MatchInviteDto
{
    public string MatchId { get; set; } = string.Empty;

    public string GameId { get; set; } = string.Empty;

    public int TargetScore { get; set; }

    public string ChallengerName { get; set; } = string.Empty;

    public string ChallengerId { get; set; } = string.Empty;
}
