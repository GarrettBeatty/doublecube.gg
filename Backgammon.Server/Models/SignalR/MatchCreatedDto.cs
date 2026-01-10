using Tapper;

namespace Backgammon.Server.Models.SignalR;

/// <summary>
/// Data sent when a match is created
/// </summary>
[TranspilationSource]
public class MatchCreatedDto
{
    public string MatchId { get; set; } = string.Empty;

    public string GameId { get; set; } = string.Empty;

    public int TargetScore { get; set; }

    public string OpponentType { get; set; } = string.Empty;

    public string Player1Id { get; set; } = string.Empty;

    public string? Player2Id { get; set; }

    public string Player1Name { get; set; } = string.Empty;

    public string? Player2Name { get; set; }

    // Correspondence-specific fields (optional)
    public bool IsCorrespondence { get; set; }

    public int? TimePerMoveDays { get; set; }

    public DateTime? TurnDeadline { get; set; }
}
