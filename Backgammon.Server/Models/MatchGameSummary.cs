using System.Text.Json.Serialization;
using Tapper;

namespace Backgammon.Server.Models;

/// <summary>
/// Summary of a completed game within a match.
/// Stored embedded in the Match document to avoid hollow object issues.
/// </summary>
[TranspilationSource]
public class MatchGameSummary
{
    /// <summary>
    /// The game ID
    /// </summary>
    [JsonPropertyName("gameId")]
    public string GameId { get; set; } = string.Empty;

    /// <summary>
    /// Winner color: "White", "Red", or null if game not yet complete
    /// </summary>
    [JsonPropertyName("winner")]
    public string? Winner { get; set; }

    /// <summary>
    /// Points won in this game
    /// </summary>
    [JsonPropertyName("stakes")]
    public int Stakes { get; set; }

    /// <summary>
    /// Type of win: "Normal", "Gammon", "Backgammon", or null if game not yet complete
    /// </summary>
    [JsonPropertyName("winType")]
    public string? WinType { get; set; }

    /// <summary>
    /// Whether this was the Crawford game
    /// </summary>
    [JsonPropertyName("isCrawford")]
    public bool IsCrawford { get; set; }

    /// <summary>
    /// When the game was completed
    /// </summary>
    [JsonPropertyName("completedAt")]
    public DateTime? CompletedAt { get; set; }
}
