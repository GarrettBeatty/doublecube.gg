using Tapper;

namespace Backgammon.Server.Models.SignalR;

/// <summary>
/// Individual game within a match
/// </summary>
[TranspilationSource]
public class MatchGameDto
{
    public string GameId { get; set; } = string.Empty;

    public int GameNumber { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? Winner { get; set; }

    public string? WinType { get; set; }

    public int PointsScored { get; set; }

    public bool IsCrawford { get; set; }

    public DateTime? CompletedAt { get; set; }
}
