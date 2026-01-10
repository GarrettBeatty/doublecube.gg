using Tapper;

namespace Backgammon.Server.Models.SignalR;

/// <summary>
/// Data for a recent completed game returned by GetRecentGames
/// </summary>
[TranspilationSource]
public class RecentGameDto
{
    public string MatchId { get; set; } = string.Empty;

    public string? OpponentId { get; set; }

    public string OpponentName { get; set; } = string.Empty;

    public int OpponentRating { get; set; }

    public string Result { get; set; } = string.Empty;

    public int MyScore { get; set; }

    public int OpponentScore { get; set; }

    public string MatchScore { get; set; } = string.Empty;

    public int TargetScore { get; set; }

    public string MatchLength { get; set; } = string.Empty;

    public string TimeControl { get; set; } = string.Empty;

    public int RatingChange { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
