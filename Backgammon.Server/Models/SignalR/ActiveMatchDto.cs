using Tapper;

namespace Backgammon.Server.Models.SignalR;

/// <summary>
/// Data for an active match returned by GetActiveMatches
/// </summary>
[TranspilationSource]
public class ActiveMatchDto
{
    public string MatchId { get; set; } = string.Empty;

    public string OpponentName { get; set; } = string.Empty;

    public int MyScore { get; set; }

    public int OpponentScore { get; set; }

    public int TargetScore { get; set; }

    public string? CurrentGameId { get; set; }

    public int GamesPlayed { get; set; }

    public bool IsCrawford { get; set; }

    public bool IsCorrespondence { get; set; }

    public DateTime CreatedAt { get; set; }
}
