using System.Text.Json.Serialization;
using Tapper;

namespace Backgammon.Server.Models;

/// <summary>
/// Best moves analysis result for client
/// </summary>
[TranspilationSource]
[GenerateSerializer]
public class BestMovesAnalysisDto
{
    [Id(0)]
    [JsonPropertyName("initialEvaluation")]
    public PositionEvaluationDto InitialEvaluation { get; set; } = new();

    [Id(1)]
    [JsonPropertyName("topMoves")]
    public List<MoveSequenceDto> TopMoves { get; set; } = new();

    [Id(2)]
    [JsonPropertyName("totalSequencesExplored")]
    public int TotalSequencesExplored { get; set; }
}
