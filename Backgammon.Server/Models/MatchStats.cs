using System.Text.Json.Serialization;

namespace Backgammon.Server.Models;

/// <summary>
/// Player statistics for a match
/// </summary>
public class MatchStats
{
    [JsonPropertyName("playerId")]
    public string PlayerId { get; set; } = string.Empty;

    [JsonPropertyName("totalMatches")]
    public int TotalMatches { get; set; }

    [JsonPropertyName("matchesWon")]
    public int MatchesWon { get; set; }

    [JsonPropertyName("matchesLost")]
    public int MatchesLost { get; set; }

    [JsonPropertyName("matchesAbandoned")]
    public int MatchesAbandoned { get; set; }

    [JsonPropertyName("totalPointsScored")]
    public int TotalPointsScored { get; set; }

    [JsonPropertyName("totalPointsConceded")]
    public int TotalPointsConceded { get; set; }

    [JsonPropertyName("averageMatchLength")]
    public double AverageMatchLength { get; set; }
}
