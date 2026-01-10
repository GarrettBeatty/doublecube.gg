using Tapper;

namespace Backgammon.Server.Models.SignalR;

/// <summary>
/// Data broadcast when a new open lobby is created
/// </summary>
[TranspilationSource]
public class LobbyCreatedDto
{
    public string MatchId { get; set; } = string.Empty;

    public string GameId { get; set; } = string.Empty;

    public string CreatorName { get; set; } = string.Empty;

    public int TargetScore { get; set; }

    public bool IsRated { get; set; }
}
