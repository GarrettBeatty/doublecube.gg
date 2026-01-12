using Backgammon.Core;
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

    public CheckerColor? Winner { get; set; }

    public int Points { get; set; }

    public bool IsGamemon { get; set; }

    public bool IsBackgammon { get; set; }

    public bool IsCrawfordGame { get; set; }

    public DateTime? CompletedAt { get; set; }
}
