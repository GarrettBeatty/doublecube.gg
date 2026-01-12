using Tapper;

namespace Backgammon.Server.Models.SignalR;

/// <summary>
/// Complete match results with all games
/// </summary>
[TranspilationSource]
public class MatchResultsDto
{
    public string MatchId { get; set; } = string.Empty;

    public string? WinnerUserId { get; set; }

    public string? WinnerUsername { get; set; }

    public string? LoserUsername { get; set; }

    public MatchScoreDto FinalScore { get; set; } = new();

    public int TargetScore { get; set; }

    public List<MatchGameDto> Games { get; set; } = new();

    public int TotalGames { get; set; }

    public string Duration { get; set; } = string.Empty;

    public DateTime? CompletedAt { get; set; }
}
