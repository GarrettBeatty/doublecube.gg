using Tapper;

namespace Backgammon.Server.Models.SignalR;

/// <summary>
/// Data sent in response to GetMatchStatus request
/// </summary>
[TranspilationSource]
public class MatchStatusDto
{
    public string MatchId { get; set; } = string.Empty;

    public int TargetScore { get; set; }

    public string Player1Name { get; set; } = string.Empty;

    public string Player2Name { get; set; } = string.Empty;

    public int Player1Score { get; set; }

    public int Player2Score { get; set; }

    public bool IsCrawfordGame { get; set; }

    public bool HasCrawfordGameBeenPlayed { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? WinnerId { get; set; }

    public int TotalGames { get; set; }

    public string? CurrentGameId { get; set; }
}
