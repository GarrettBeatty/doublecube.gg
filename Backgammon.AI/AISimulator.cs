using Backgammon.Core;

namespace Backgammon.AI;

/// <summary>
/// Simulates games between AI players
/// </summary>
public class AISimulator
{
    public AISimulator(IBackgammonAI whiteAI, IBackgammonAI redAI, bool verbose = false)
    {
        WhiteAI = whiteAI;
        RedAI = redAI;
        Verbose = verbose;
    }

    public IBackgammonAI WhiteAI { get; }

    public IBackgammonAI RedAI { get; }

    public bool Verbose { get; set; }

    /// <summary>
    /// Run a single game between the two AIs
    /// </summary>
    /// <returns>The game result (positive if white wins, negative if red wins)</returns>
    public GameResult RunGame()
    {
        var engine = new GameEngine(WhiteAI.Name, RedAI.Name);
        engine.StartNewGame();

        if (Verbose)
        {
            Console.WriteLine($"=== New Game: {WhiteAI.Name} (White) vs {RedAI.Name} (Red) ===");
            Console.WriteLine($"Opening roll: {engine.Dice}");
            Console.WriteLine($"{engine.CurrentPlayer.Name} goes first");
        }

        int turnCount = 0;

        while (!engine.GameOver)
        {
            turnCount++;
            var currentAI = engine.CurrentPlayer.Color == CheckerColor.White ? WhiteAI : RedAI;

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

            // Let AI choose and execute moves
            var moves = currentAI.ChooseMoves(engine);

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
            var winnerAI = engine.Winner.Color == CheckerColor.White ? WhiteAI : RedAI;

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
    /// Run multiple games and return statistics
    /// </summary>
    public SimulationStats RunSimulation(int gameCount)
    {
        var stats = new SimulationStats
        {
            TotalGames = gameCount,
            WhiteAIName = WhiteAI.Name,
            RedAIName = RedAI.Name
        };

        Console.WriteLine($"Running {gameCount} games: {WhiteAI.Name} vs {RedAI.Name}");

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

    private string FormatMove(Move move)
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

    private string GetResultType(int multiplier)
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
