using System.Text.Json.Serialization;
using Tapper;

namespace Backgammon.Server.Models;

/// <summary>
/// Represents a single point on the backgammon board for storage
/// </summary>
[TranspilationSource]
public class PointStateDto
{
    /// <summary>
    /// Point position (1-24)
    /// </summary>
    [JsonPropertyName("position")]
    public int Position { get; set; }

    /// <summary>
    /// Color of checkers on this point: null (empty), "White", or "Red"
    /// </summary>
    [JsonPropertyName("color")]
    public string? Color { get; set; }

    /// <summary>
    /// Number of checkers on this point
    /// </summary>
    [JsonPropertyName("count")]
    public int Count { get; set; }
}
