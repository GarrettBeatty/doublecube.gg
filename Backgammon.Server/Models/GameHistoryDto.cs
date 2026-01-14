using System.Text.Json.Serialization;
using Tapper;

namespace Backgammon.Server.Models;

/// <summary>
/// Data transfer object for complete game history with turn-by-turn replay data.
/// Used to load completed games into the analysis board for move-by-move review.
/// </summary>
[TranspilationSource]
public class GameHistoryDto
{
    /// <summary>
    /// Unique game session ID
    /// </summary>
    [JsonPropertyName("gameId")]
    public string GameId { get; set; } = string.Empty;

    /// <summary>
    /// Match ID if this game was part of a match (null for standalone games)
    /// </summary>
    [JsonPropertyName("matchId")]
    public string? MatchId { get; set; }

    /// <summary>
    /// Complete turn-by-turn history for game analysis and replay.
    /// Each turn includes position SGF, dice rolled, moves made, and doubling actions.
    /// </summary>
    [JsonPropertyName("turnHistory")]
    public List<TurnSnapshotDto> TurnHistory { get; set; } = new();

    /// <summary>
    /// Display name for white player at time of game
    /// </summary>
    [JsonPropertyName("whitePlayerName")]
    public string? WhitePlayerName { get; set; }

    /// <summary>
    /// Display name for red player at time of game
    /// </summary>
    [JsonPropertyName("redPlayerName")]
    public string? RedPlayerName { get; set; }

    /// <summary>
    /// Winning player color (White or Red)
    /// </summary>
    [JsonPropertyName("winner")]
    public string? Winner { get; set; }

    /// <summary>
    /// Type of win (Normal, Gammon, or Backgammon)
    /// </summary>
    [JsonPropertyName("winType")]
    public string? WinType { get; set; }

    /// <summary>
    /// When the game was created
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the game ended
    /// </summary>
    [JsonPropertyName("completedAt")]
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Value of the doubling cube at game end
    /// </summary>
    [JsonPropertyName("doublingCubeValue")]
    public int DoublingCubeValue { get; set; } = 1;
}
