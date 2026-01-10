using System.Text.Json.Serialization;
using Tapper;

namespace Backgammon.Server.Models;

/// <summary>
/// Best moves analysis result for client
/// </summary>
[TranspilationSource]
public class BestMovesAnalysisDto
{
    [JsonPropertyName("initialEvaluation")]
    public PositionEvaluationDto InitialEvaluation { get; set; } = new();

    [JsonPropertyName("topMoves")]
    public List<MoveSequenceDto> TopMoves { get; set; } = new();

    [JsonPropertyName("totalSequencesExplored")]
    public int TotalSequencesExplored { get; set; }
}
