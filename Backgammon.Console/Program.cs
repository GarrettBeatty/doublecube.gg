using Backgammon.Core;

namespace Backgammon.ConsoleApp;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== BACKGAMMON ===");
        Console.WriteLine();

        var game = new GameEngine("White", "Red");
        game.StartNewGame();

        Console.WriteLine($"Opening roll: {game.Dice}");
        Console.WriteLine($"{game.CurrentPlayer.Name} goes first!");
        Console.WriteLine();

        // Game loop
        while (!game.GameOver)
        {
            DrawBoard(game);
            Console.WriteLine();
            
            // Show current player and dice
            Console.WriteLine($"--- {game.CurrentPlayer.Name}'s Turn ---");
            
            if (game.RemainingMoves.Count == 0)
            {
                Console.WriteLine("Press Enter to roll dice...");
                Console.ReadLine();
                game.RollDice();
                Console.WriteLine($"Rolled: {game.Dice}");
                Console.WriteLine($"Moves available: {string.Join(", ", game.RemainingMoves)}");
            }

            // Get valid moves
            var validMoves = game.GetValidMoves();
            
            if (validMoves.Count == 0)
            {
                Console.WriteLine("No valid moves available!");
                Console.WriteLine("Press Enter to end turn...");
                Console.ReadLine();
                game.EndTurn();
                continue;
            }

            // Show valid moves
            Console.WriteLine("\nValid moves:");
            for (int i = 0; i < validMoves.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {validMoves[i]}");
            }
            Console.WriteLine($"{validMoves.Count + 1}. End turn");

            // Get player choice
            int choice2;
            while (true)
            {
                Console.Write($"\nChoose move (1-{validMoves.Count + 1}): ");
                if (int.TryParse(Console.ReadLine(), out choice2) && choice2 >= 1 && choice2 <= validMoves.Count + 1)
                    break;
                Console.WriteLine("Invalid choice!");
            }

            if (choice2 == validMoves.Count + 1)
            {
                game.EndTurn();
            }
            else
            {
                var move = validMoves[choice2 - 1];
                game.ExecuteMove(move);
                Console.WriteLine($"Executed: {move}");
                
                if (move.IsHit)
                {
                    Console.WriteLine($"Hit! {game.GetOpponent().Name} has {game.GetOpponent().CheckersOnBar} checker(s) on the bar.");
                }
                
                if (game.RemainingMoves.Count > 0)
                {
                    Console.WriteLine($"Remaining moves: {string.Join(", ", game.RemainingMoves)}");
                }
            }
        }

        // Game over
        Console.Clear();
        DrawBoard(game);
        Console.WriteLine();
        Console.WriteLine("=== GAME OVER ===");
        Console.WriteLine($"{game.Winner!.Name} wins!");
        
        int result = game.GetGameResult();
        string resultType = result == 3 * game.DoublingCube.Value ? "Backgammon!" :
                           result == 2 * game.DoublingCube.Value ? "Gammon!" : "";
        Console.WriteLine($"Points: {result} {resultType}");
    }

    static void DrawBoard(GameEngine game)
    {
        Console.WriteLine();
        Console.WriteLine(" 13 14 15 16 17 18   19 20 21 22 23 24");
        Console.WriteLine("+------------------+------------------+");
        
        // Top half (points 13-24)
        for (int row = 0; row < 5; row++)
        {
            Console.Write("|");
            for (int point = 13; point <= 24; point++)
            {
                if (point == 19) Console.Write("|");
                
                var p = game.Board.GetPoint(point);
                if (p.Count > row)
                {
                    Console.Write(p.Color == CheckerColor.White ? " W" : " R");
                }
                else if (row == 4 && p.Count > 5)
                {
                    Console.Write($" {p.Count}");
                }
                else
                {
                    Console.Write("  ");
                }
            }
            Console.WriteLine("|");
        }
        
        // Middle bar
        Console.Write("|");
        Console.Write("       BAR        |");
        Console.WriteLine("                  |");
        
        // Show checkers on bar
        if (game.WhitePlayer.CheckersOnBar > 0 || game.RedPlayer.CheckersOnBar > 0)
        {
            Console.Write($"| W:{game.WhitePlayer.CheckersOnBar} R:{game.RedPlayer.CheckersOnBar}");
            Console.Write("           |");
            Console.WriteLine($" W Off:{game.WhitePlayer.CheckersBornOff,2} R Off:{game.RedPlayer.CheckersBornOff,2} |");
        }
        else
        {
            Console.Write("|                  |");
            Console.WriteLine($" W Off:{game.WhitePlayer.CheckersBornOff,2} R Off:{game.RedPlayer.CheckersBornOff,2} |");
        }
        
        // Bottom half (points 12-1)
        for (int row = 4; row >= 0; row--)
        {
            Console.Write("|");
            for (int point = 12; point >= 1; point--)
            {
                if (point == 6) Console.Write("|");
                
                var p = game.Board.GetPoint(point);
                if (p.Count > row)
                {
                    Console.Write(p.Color == CheckerColor.White ? " W" : " R");
                }
                else if (row == 4 && p.Count > 5)
                {
                    Console.Write($" {p.Count}");
                }
                else
                {
                    Console.Write("  ");
                }
            }
            Console.WriteLine("|");
        }
        
        Console.WriteLine("+------------------+------------------+");
        Console.WriteLine(" 12 11 10  9  8  7    6  5  4  3  2  1");
        Console.WriteLine();
    }
}
