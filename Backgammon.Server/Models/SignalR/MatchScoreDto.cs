using Tapper;

namespace Backgammon.Server.Models.SignalR;

/// <summary>
/// Match score data
/// </summary>
[TranspilationSource]
public class MatchScoreDto
{
    public int Player1 { get; set; }

    public int Player2 { get; set; }
}
