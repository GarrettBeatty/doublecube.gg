using Tapper;

namespace Backgammon.Server.Models.SignalR;

/// <summary>
/// Data sent when match score/state changes
/// </summary>
[TranspilationSource]
public class MatchUpdateDto
{
    public string MatchId { get; set; } = string.Empty;

    public int Player1Score { get; set; }

    public int Player2Score { get; set; }

    public int TargetScore { get; set; }

    public bool IsCrawfordGame { get; set; }

    public bool MatchComplete { get; set; }

    public string? MatchWinner { get; set; }
}
