using Backgammon.AI;

namespace BackgammonAI;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("=== Backgammon AI Simulator ===\n");

        // Select AIs
        Console.WriteLine("Available AI players:");
        Console.WriteLine("1. RandomAI - Makes random valid moves");
        Console.WriteLine("2. GreedyAI - Prioritizes bearing off and hitting");

        Console.Write("\nSelect White AI (1-2, default: 1): ");
        var whiteAI = CreateAI(Console.ReadLine(), "White");

        Console.Write("Select Red AI (1-2, default: 2): ");
        var redAI = CreateAI(Console.ReadLine(), "Red");

        Console.WriteLine($"\nMatchup: {whiteAI.Name} vs {redAI.Name}\n");

        // Create simulator
        var simulator = new AISimulator(whiteAI, redAI, verbose: false);

        // Ask user how many games to run
        Console.Write("How many games to simulate? (default: 100): ");
        var input = Console.ReadLine();
        int gameCount = 100;

        if (!string.IsNullOrWhiteSpace(input) && int.TryParse(input, out int parsed) && parsed > 0)
        {
            gameCount = parsed;
        }

        Console.WriteLine();

        // Run simulation
        var stats = simulator.RunSimulation(gameCount);

        // Print results
        stats.PrintSummary();

        Console.WriteLine("\n--- Example Game ---");
        Console.WriteLine("Running one game with verbose output:\n");

        // Run one verbose game as example
        simulator.Verbose = true;
        simulator.RunGame();
    }

    private static IBackgammonAI CreateAI(string? choice, string color)
    {
        return choice?.Trim() switch
        {
            "2" => new GreedyAI($"Greedy-{color}"),
            _ => new RandomAI($"Random-{color}")
        };
    }
}
