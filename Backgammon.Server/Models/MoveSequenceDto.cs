using System.Text.Json.Serialization;

namespace Backgammon.Server.Models;

/// <summary>
/// Move sequence evaluation for client
/// </summary>
public class MoveSequenceDto
{
    [JsonPropertyName("moves")]
    public List<MoveDto> Moves { get; set; } = new();

    [JsonPropertyName("notation")]
    public string Notation { get; set; } = string.Empty;

    [JsonPropertyName("equity")]
    public double Equity { get; set; }

    [JsonPropertyName("equityGain")]
    public double EquityGain { get; set; }
}
