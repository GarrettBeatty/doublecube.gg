using Tapper;

namespace Backgammon.Server.Models.SignalR;

/// <summary>
/// Data for a match lobby returned by GetMatchLobbies
/// </summary>
[TranspilationSource]
public class MatchLobbyDto
{
    public string MatchId { get; set; } = string.Empty;

    public string CreatorPlayerId { get; set; } = string.Empty;

    public string CreatorUsername { get; set; } = string.Empty;

    public string OpponentType { get; set; } = string.Empty;

    public int TargetScore { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? OpponentPlayerId { get; set; }

    public string? OpponentUsername { get; set; }

    public string CreatedAt { get; set; } = string.Empty;

    public bool IsOpenLobby { get; set; }

    public bool IsCorrespondence { get; set; }

    public int? TimePerMoveDays { get; set; }
}
