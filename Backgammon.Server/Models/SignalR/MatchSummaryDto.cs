using Tapper;

namespace Backgammon.Server.Models.SignalR;

/// <summary>
/// Summary of a match for the MyMatches list
/// </summary>
[TranspilationSource]
public class MatchSummaryDto
{
    public string MatchId { get; set; } = string.Empty;

    public int TargetScore { get; set; }

    public string? OpponentId { get; set; }

    public string? OpponentName { get; set; }

    public int MyScore { get; set; }

    public int OpponentScore { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public int TotalGames { get; set; }
}
