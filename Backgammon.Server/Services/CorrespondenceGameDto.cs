namespace Backgammon.Server.Services;

/// <summary>
/// DTO for correspondence game information
/// </summary>
public class CorrespondenceGameDto
{
    public string MatchId { get; set; } = string.Empty;

    public string GameId { get; set; } = string.Empty;

    public string OpponentId { get; set; } = string.Empty;

    public string OpponentName { get; set; } = string.Empty;

    public int OpponentRating { get; set; }

    public bool IsYourTurn { get; set; }

    public int TimePerMoveDays { get; set; }

    public DateTime? TurnDeadline { get; set; }

    public TimeSpan? TimeRemaining { get; set; }

    public int MoveCount { get; set; }

    public string MatchScore { get; set; } = string.Empty;

    public int TargetScore { get; set; }

    public bool IsRated { get; set; }

    public DateTime LastUpdatedAt { get; set; }
}
