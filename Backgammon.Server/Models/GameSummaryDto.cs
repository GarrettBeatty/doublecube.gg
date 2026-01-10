using Tapper;

namespace Backgammon.Server.Models;

/// <summary>
/// Game summary for profile page
/// </summary>
[TranspilationSource]
public class GameSummaryDto
{
    /// <summary>
    /// Game ID
    /// </summary>
    public string GameId { get; set; } = string.Empty;

    /// <summary>
    /// Opponent's username
    /// </summary>
    public string OpponentUsername { get; set; } = string.Empty;

    /// <summary>
    /// Whether this player won
    /// </summary>
    public bool Won { get; set; }

    /// <summary>
    /// Final score/stakes
    /// </summary>
    public int Stakes { get; set; }

    /// <summary>
    /// When the game ended
    /// </summary>
    public DateTime CompletedAt { get; set; }

    /// <summary>
    /// Type of win (normal, gammon, backgammon)
    /// </summary>
    public string? WinType { get; set; }
}
