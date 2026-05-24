using System.Text.Json.Serialization;
using Tapper;

namespace Backgammon.Server.Models;

/// <summary>
/// Move sequence evaluation for client
/// </summary>
[TranspilationSource]
[GenerateSerializer]
public class MoveSequenceDto
{
    [Id(0)]
    [JsonPropertyName("moves")]
    public List<MoveDto> Moves { get; set; } = new();

    [Id(1)]
    [JsonPropertyName("notation")]
    public string Notation { get; set; } = string.Empty;

    [Id(2)]
    [JsonPropertyName("equity")]
    public double Equity { get; set; }

    [Id(3)]
    [JsonPropertyName("equityGain")]
    public double EquityGain { get; set; }
}
