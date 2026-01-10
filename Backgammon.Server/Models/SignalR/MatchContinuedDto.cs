using Tapper;

namespace Backgammon.Server.Models.SignalR;

/// <summary>
/// Data sent when continuing to next game in a match
/// </summary>
[TranspilationSource]
public class MatchContinuedDto
{
    public string MatchId { get; set; } = string.Empty;

    public string GameId { get; set; } = string.Empty;

    public int Player1Score { get; set; }

    public int Player2Score { get; set; }

    public int TargetScore { get; set; }

    public bool IsCrawfordGame { get; set; }
}
