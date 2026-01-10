using Tapper;

namespace Backgammon.Server.Models.SignalR;

/// <summary>
/// Data sent when an entire match completes
/// </summary>
[TranspilationSource]
public class MatchCompletedDto
{
    public string MatchId { get; set; } = string.Empty;

    public string Winner { get; set; } = string.Empty;

    public MatchFinalScoreDto FinalScore { get; set; } = new();
}
