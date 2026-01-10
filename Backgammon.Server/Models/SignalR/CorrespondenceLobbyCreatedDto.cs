using Tapper;

namespace Backgammon.Server.Models.SignalR;

/// <summary>
/// Data broadcast when a new correspondence lobby is created
/// </summary>
[TranspilationSource]
public class CorrespondenceLobbyCreatedDto
{
    public string MatchId { get; set; } = string.Empty;

    public string GameId { get; set; } = string.Empty;

    public string CreatorPlayerId { get; set; } = string.Empty;

    public string CreatorUsername { get; set; } = string.Empty;

    public int TargetScore { get; set; }

    public int TimePerMoveDays { get; set; }

    public bool IsRated { get; set; }
}
