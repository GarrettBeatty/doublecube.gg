namespace Backgammon.Server.Services;

/// <summary>
/// Aggregated player statistics
/// </summary>
public class PlayerStats
{
    public string PlayerId { get; set; } = string.Empty;

    public int TotalGames { get; set; }

    public int Wins { get; set; }

    public int Losses { get; set; }

    public int TotalStakes { get; set; }

    public int NormalWins { get; set; }

    public int GammonWins { get; set; }

    public int BackgammonWins { get; set; }

    public double WinRate => TotalGames > 0 ? (double)Wins / TotalGames : 0;
}
