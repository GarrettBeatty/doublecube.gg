using System.Text.Json.Serialization;
using Tapper;

namespace Backgammon.Server.Models;

/// <summary>
/// Position features DTO
/// </summary>
[TranspilationSource]
public class PositionFeaturesDto
{
    [JsonPropertyName("pipCount")]
    public int PipCount { get; set; }

    [JsonPropertyName("pipDifference")]
    public int PipDifference { get; set; }

    [JsonPropertyName("blotCount")]
    public int BlotCount { get; set; }

    [JsonPropertyName("blotExposure")]
    public int BlotExposure { get; set; }

    [JsonPropertyName("checkersOnBar")]
    public int CheckersOnBar { get; set; }

    [JsonPropertyName("primeLength")]
    public int PrimeLength { get; set; }

    [JsonPropertyName("anchorsInOpponentHome")]
    public int AnchorsInOpponentHome { get; set; }

    [JsonPropertyName("homeboardCoverage")]
    public int HomeboardCoverage { get; set; }

    [JsonPropertyName("distribution")]
    public double Distribution { get; set; }

    [JsonPropertyName("isContact")]
    public bool IsContact { get; set; }

    [JsonPropertyName("isRace")]
    public bool IsRace { get; set; }

    [JsonPropertyName("wastedPips")]
    public int WastedPips { get; set; }

    [JsonPropertyName("bearoffEfficiency")]
    public double BearoffEfficiency { get; set; }

    [JsonPropertyName("checkersBornOff")]
    public int CheckersBornOff { get; set; }
}
