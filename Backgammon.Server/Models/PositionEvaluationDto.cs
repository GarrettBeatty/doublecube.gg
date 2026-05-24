using System.Text.Json.Serialization;
using Tapper;

namespace Backgammon.Server.Models;

/// <summary>
/// Position evaluation data transfer object for client
/// </summary>
[TranspilationSource]
[GenerateSerializer]
public class PositionEvaluationDto
{
    [Id(0)]
    [JsonPropertyName("equity")]
    public double Equity { get; set; }

    [Id(1)]
    [JsonPropertyName("winProbability")]
    public double WinProbability { get; set; }

    [Id(2)]
    [JsonPropertyName("gammonProbability")]
    public double GammonProbability { get; set; }

    [Id(3)]
    [JsonPropertyName("backgammonProbability")]
    public double BackgammonProbability { get; set; }

    [Id(4)]
    [JsonPropertyName("features")]
    public PositionFeaturesDto Features { get; set; } = new();

    [Id(5)]
    [JsonPropertyName("evaluatorName")]
    public string EvaluatorName { get; set; } = "Heuristic";
}
