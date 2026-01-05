using System.Text.Json.Serialization;

namespace Backgammon.Server.Models;

/// <summary>
/// Position evaluation data transfer object for client
/// </summary>
public class PositionEvaluationDto
{
    [JsonPropertyName("equity")]
    public double Equity { get; set; }

    [JsonPropertyName("winProbability")]
    public double WinProbability { get; set; }

    [JsonPropertyName("gammonProbability")]
    public double GammonProbability { get; set; }

    [JsonPropertyName("backgammonProbability")]
    public double BackgammonProbability { get; set; }

    [JsonPropertyName("features")]
    public PositionFeaturesDto Features { get; set; } = new();

    [JsonPropertyName("evaluatorName")]
    public string EvaluatorName { get; set; } = "Heuristic";
}
