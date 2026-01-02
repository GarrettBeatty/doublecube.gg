namespace Backgammon.AI;

/// <summary>
/// Statistics from running multiple games
/// </summary>
public class SimulationStats
{
    public int TotalGames { get; set; }

    public string WhiteAIName { get; set; } = string.Empty;

    public string RedAIName { get; set; } = string.Empty;

    public int WhiteWins { get; set; }

    public int RedWins { get; set; }

    public int WhitePoints { get; set; }

    public int RedPoints { get; set; }

    public int TotalTurns { get; set; }

    public double WhiteWinPercentage => TotalGames > 0 ? (double)WhiteWins / TotalGames * 100 : 0;

    public double RedWinPercentage => TotalGames > 0 ? (double)RedWins / TotalGames * 100 : 0;

    public double AverageTurnsPerGame => TotalGames > 0 ? (double)TotalTurns / TotalGames : 0;

    public void PrintSummary()
    {
        Console.WriteLine("\n=== Simulation Results ===");
        Console.WriteLine($"Total Games: {TotalGames}");
        Console.WriteLine($"\n{WhiteAIName} (White):");
        Console.WriteLine($"  Wins: {WhiteWins} ({WhiteWinPercentage:F2}%)");
        Console.WriteLine($"  Points: {WhitePoints}");
        Console.WriteLine($"\n{RedAIName} (Red):");
        Console.WriteLine($"  Wins: {RedWins} ({RedWinPercentage:F2}%)");
        Console.WriteLine($"  Points: {RedPoints}");
        Console.WriteLine($"\nAverage turns per game: {AverageTurnsPerGame:F1}");
    }
}
