using Backgammon.Core;
using Backgammon.Plugins.Abstractions;

namespace Backgammon.AI;

/// <summary>
/// Simulates games between bot players.
/// </summary>
public class AISimulator
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AISimulator"/> class.
    /// </summary>
    /// <param name="whiteBot">The bot playing as White.</param>
    /// <param name="redBot">The bot playing as Red.</param>
    /// <param name="verbose">Whether to print verbose output.</param>
    public AISimulator(IGameBot whiteBot, IGameBot redBot, bool verbose = false)
    {
        WhiteBot = whiteBot;
        RedBot = redBot;
        Verbose = verbose;
    }

    /// <summary>
    /// Gets the bot playing as White.
    /// </summary>
    public IGameBot WhiteBot { get; }

    /// <summary>
    /// Gets the bot playing as Red.
    /// </summary>
    public IGameBot RedBot { get; }

    /// <summary>
    /// Gets or sets whether to print verbose output.
    /// </summary>
    public bool Verbose { get; set; }

    /// <summary>
    /// Run a single game between the two bots.
    /// </summary>
    /// <returns>The game result.</returns>
    public GameResult RunGame()
    {
        var engine = new GameEngine(WhiteBot.DisplayName, RedBot.DisplayName);
        engine.StartNewGame();

        if (Verbose)
        {
            Console.WriteLine($"=== New Game: {WhiteBot.DisplayName} (White) vs {RedBot.DisplayName} (Red) ===");
            Console.WriteLine($"Opening roll: {engine.Dice}");
            Console.WriteLine($"{engine.CurrentPlayer.Name} goes first");
        }

        int turnCount = 0;

        while (!engine.GameOver)
        {
            turnCount++;
            var currentBot = engine.CurrentPlayer.Color == CheckerColor.White ? WhiteBot : RedBot;

            if (Verbose)
            {
                Console.WriteLine($"\n--- Turn {turnCount}: {engine.CurrentPlayer.Name} ---");
            }

            // If no moves remaining, roll dice
            if (engine.RemainingMoves.Count == 0)
            {
                engine.RollDice();
                if (Verbose)
                {
                    Console.WriteLine($"Rolled: {engine.Dice}");
                    Console.WriteLine($"Moves available: {string.Join(", ", engine.RemainingMoves)}");
                }
            }

            // Let bot choose and execute moves (sync call for simulation)
            var moves = currentBot.ChooseMovesAsync(engine).GetAwaiter().GetResult();

            if (Verbose)
            {
                if (moves.Count > 0)
                {
                    Console.WriteLine($"Executed {moves.Count} move(s):");
                    foreach (var move in moves)
                    {
                        Console.WriteLine($"  {FormatMove(move)}");
                    }
                }
                else
                {
                    Console.WriteLine("No valid moves");
                }

                if (engine.RemainingMoves.Count > 0)
                {
                    Console.WriteLine($"Unused moves: {string.Join(", ", engine.RemainingMoves)}");
                }
            }

            // End turn
            engine.EndTurn();

            // Safety check for infinite loops
            if (turnCount > 1000)
            {
                if (Verbose)
                {
                    Console.WriteLine("Game exceeded 1000 turns - aborting");
                }

                break;
            }
        }

        if (engine.GameOver && engine.Winner != null)
        {
            var result = engine.GetGameResult();

            if (Verbose)
            {
                Console.WriteLine($"\n=== Game Over ===");
                Console.WriteLine($"Winner: {engine.Winner.Name}");
                Console.WriteLine($"Result: {Math.Abs(result)}x ({GetResultType(Math.Abs(result) / engine.DoublingCube.Value)})");
                Console.WriteLine($"Turns: {turnCount}");
                Console.WriteLine($"White bore off: {engine.WhitePlayer.CheckersBornOff}/15");
                Console.WriteLine($"Red bore off: {engine.RedPlayer.CheckersBornOff}/15");
            }

            return new GameResult
            {
                Winner = engine.Winner.Color,
                Points = result,
                Turns = turnCount,
                WhiteBornOff = engine.WhitePlayer.CheckersBornOff,
                RedBornOff = engine.RedPlayer.CheckersBornOff
            };
        }

        // Should not happen in normal gameplay
        return new GameResult
        {
            Winner = CheckerColor.White,
            Points = 0,
            Turns = turnCount,
            WhiteBornOff = 0,
            RedBornOff = 0
        };
    }

    /// <summary>
    /// Run multiple games and return statistics.
    /// </summary>
    /// <param name="gameCount">Number of games to simulate.</param>
    /// <returns>The simulation statistics.</returns>
    public SimulationStats RunSimulation(int gameCount)
    {
        var stats = new SimulationStats
        {
            TotalGames = gameCount,
            WhiteAIName = WhiteBot.DisplayName,
            RedAIName = RedBot.DisplayName
        };

        Console.WriteLine($"Running {gameCount} games: {WhiteBot.DisplayName} vs {RedBot.DisplayName}");

        for (int i = 0; i < gameCount; i++)
        {
            if ((i + 1) % 100 == 0)
            {
                Console.WriteLine($"Progress: {i + 1}/{gameCount} games completed");
            }

            var result = RunGame();

            if (result.Winner == CheckerColor.White)
            {
                stats.WhiteWins++;
                stats.WhitePoints += result.Points;
            }
            else
            {
                stats.RedWins++;
                stats.RedPoints += Math.Abs(result.Points);
            }

            stats.TotalTurns += result.Turns;
        }

        return stats;
    }

    private static string FormatMove(Move move)
    {
        if (move.From == 0)
        {
            return $"Bar → {move.To} (die: {move.DieValue})";
        }
        else if (move.IsBearOff)
        {
            return $"{move.From} → Off (die: {move.DieValue})";
        }
        else
        {
            return $"{move.From} → {move.To} (die: {move.DieValue}){(move.IsHit ? " [HIT]" : string.Empty)}";
        }
    }

    private static string GetResultType(int multiplier)
    {
        return multiplier switch
        {
            1 => "Normal",
            2 => "Gammon",
            3 => "Backgammon",
            _ => "Unknown"
        };
    }
}
