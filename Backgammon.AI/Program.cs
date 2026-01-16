using Backgammon.AI;
using Backgammon.AI.Bots;
using Backgammon.Plugins.Abstractions;

namespace BackgammonAI;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("=== Backgammon AI Simulator ===\n");

        // Select AIs
        Console.WriteLine("Available AI players:");
        Console.WriteLine("1. Random Bot - Makes random valid moves");
        Console.WriteLine("2. Greedy Bot - Prioritizes bearing off and hitting");

        Console.Write("\nSelect White AI (1-2, default: 1): ");
        var whiteBot = CreateBot(Console.ReadLine());

        Console.Write("Select Red AI (1-2, default: 2): ");
        var redBot = CreateBot(Console.ReadLine());

        Console.WriteLine($"\nMatchup: {whiteBot.DisplayName} vs {redBot.DisplayName}\n");

        // Create simulator
        var simulator = new AISimulator(whiteBot, redBot, verbose: false);

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

    private static IGameBot CreateBot(string? choice)
    {
        return choice?.Trim() switch
        {
            "2" => new GreedyBot(),
            _ => new RandomBot()
        };
    }
}
