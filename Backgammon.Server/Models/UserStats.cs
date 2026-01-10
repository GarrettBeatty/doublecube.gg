using System.Text.Json.Serialization;
using Tapper;

namespace Backgammon.Server.Models;

/// <summary>
/// User statistics - denormalized on User document for fast reads
/// </summary>
[TranspilationSource]
public class UserStats
{
    [JsonPropertyName("totalGames")]
    public int TotalGames { get; set; }

    [JsonPropertyName("wins")]
    public int Wins { get; set; }

    [JsonPropertyName("losses")]
    public int Losses { get; set; }

    [JsonPropertyName("totalStakes")]
    public int TotalStakes { get; set; }

    [JsonPropertyName("normalWins")]
    public int NormalWins { get; set; }

    [JsonPropertyName("gammonWins")]
    public int GammonWins { get; set; }

    [JsonPropertyName("backgammonWins")]
    public int BackgammonWins { get; set; }

    [JsonPropertyName("winStreak")]
    public int WinStreak { get; set; }

    [JsonPropertyName("bestWinStreak")]
    public int BestWinStreak { get; set; }
}
