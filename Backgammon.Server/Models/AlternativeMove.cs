using System.Text.Json.Serialization;

namespace Backgammon.Server.Models;

/// <summary>
/// Alternative move that is also accepted as correct.
/// </summary>
public class AlternativeMove
{
    /// <summary>
    /// Gets or sets the move sequence.
    /// </summary>
    [JsonPropertyName("moves")]
    public List<MoveDto> Moves { get; set; } = new();

    /// <summary>
    /// Gets or sets the move notation (e.g., "24/18 13/11").
    /// </summary>
    [JsonPropertyName("notation")]
    public string Notation { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the equity of this move sequence.
    /// </summary>
    [JsonPropertyName("equity")]
    public double Equity { get; set; }

    /// <summary>
    /// Gets or sets the equity difference from the best move (always >= 0).
    /// </summary>
    [JsonPropertyName("equityLoss")]
    public double EquityLoss { get; set; }
}
