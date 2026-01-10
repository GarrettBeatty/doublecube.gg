using Tapper;

namespace Backgammon.Server.Models.SignalR;

/// <summary>
/// Final score details for a completed match
/// </summary>
[TranspilationSource]
public class MatchFinalScoreDto
{
    public int Player1 { get; set; }

    public int Player2 { get; set; }
}
