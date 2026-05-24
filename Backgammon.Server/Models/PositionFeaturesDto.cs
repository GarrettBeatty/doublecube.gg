using System.Text.Json.Serialization;
using Tapper;

namespace Backgammon.Server.Models;

/// <summary>
/// Position features DTO
/// </summary>
[TranspilationSource]
[GenerateSerializer]
public class PositionFeaturesDto
{
    [Id(0)]
    [JsonPropertyName("pipCount")]
    public int PipCount { get; set; }

    [Id(1)]
    [JsonPropertyName("pipDifference")]
    public int PipDifference { get; set; }

    [Id(2)]
    [JsonPropertyName("blotCount")]
    public int BlotCount { get; set; }

    [Id(3)]
    [JsonPropertyName("blotExposure")]
    public int BlotExposure { get; set; }

    [Id(4)]
    [JsonPropertyName("checkersOnBar")]
    public int CheckersOnBar { get; set; }

    [Id(5)]
    [JsonPropertyName("primeLength")]
    public int PrimeLength { get; set; }

    [Id(6)]
    [JsonPropertyName("anchorsInOpponentHome")]
    public int AnchorsInOpponentHome { get; set; }

    [Id(7)]
    [JsonPropertyName("homeboardCoverage")]
    public int HomeboardCoverage { get; set; }

    [Id(8)]
    [JsonPropertyName("distribution")]
    public double Distribution { get; set; }

    [Id(9)]
    [JsonPropertyName("isContact")]
    public bool IsContact { get; set; }

    [Id(10)]
    [JsonPropertyName("isRace")]
    public bool IsRace { get; set; }

    [Id(11)]
    [JsonPropertyName("wastedPips")]
    public int WastedPips { get; set; }

    [Id(12)]
    [JsonPropertyName("bearoffEfficiency")]
    public double BearoffEfficiency { get; set; }

    [Id(13)]
    [JsonPropertyName("checkersBornOff")]
    public int CheckersBornOff { get; set; }
}
