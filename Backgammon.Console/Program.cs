using Backgammon.Core;
using Spectre.Console;

namespace Backgammon.ConsoleApp;

internal class Program
{
    private static void Main(string[] args)
    {
        AnsiConsole.Clear();

        var rule = new Rule("[cyan]B A C K G A M M O N[/]");
        rule.Style = Style.Parse("cyan");
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();

        var game = new GameEngine("White", "Red");
        game.StartNewGame();

        AnsiConsole.MarkupLine($"[yellow]Opening roll: {game.Dice.ToString().EscapeMarkup()}[/]");

        var playerMarkup = game.CurrentPlayer.Color == CheckerColor.White ? "white" : "red";
        AnsiConsole.MarkupLine($"[{playerMarkup}]{game.CurrentPlayer.Name.EscapeMarkup()} goes first![/]");
        AnsiConsole.WriteLine();

        // Game loop
        while (!game.GameOver)
        {
            DrawBoard(game);
            AnsiConsole.WriteLine();

            // Show current player and dice
            var currentMarkup = game.CurrentPlayer.Color == CheckerColor.White ? "white" : "red";
            var panel = new Panel($"[{currentMarkup}]{game.CurrentPlayer.Name.EscapeMarkup()}'s Turn[/]")
            {
                Border = BoxBorder.Rounded
            };
            AnsiConsole.Write(panel);

            if (game.RemainingMoves.Count == 0)
            {
                AnsiConsole.MarkupLine("[grey]Press Enter to roll dice...[/]");
                Console.ReadLine();
                game.RollDice();
                AnsiConsole.MarkupLine($"[yellow]🎲 Rolled: {game.Dice.ToString().EscapeMarkup()}[/]");
                AnsiConsole.MarkupLine($"[yellow]📋 Moves available: {string.Join(", ", game.RemainingMoves).EscapeMarkup()}[/]");
            }

            // Get valid moves
            var validMoves = game.GetValidMoves();

            if (validMoves.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]❌ No valid moves available![/]");
                AnsiConsole.MarkupLine("[grey]Press Enter to end turn...[/]");
                Console.ReadLine();
                game.EndTurn();
                continue;
            }

            // Show valid moves
            AnsiConsole.MarkupLine("\n[green]✓ Valid moves:[/]");
            for (int i = 0; i < validMoves.Count; i++)
            {
                AnsiConsole.MarkupLine($"[cyan]{i + 1}.[/] {validMoves[i]}");
            }

            int menuOffset = validMoves.Count + 1;

            // Add undo option if there are moves to undo
            if (game.MoveHistory.Count > 0)
            {
                AnsiConsole.MarkupLine($"[yellow]{menuOffset}. Undo last move[/]");
                menuOffset++;
            }

            AnsiConsole.MarkupLine($"[grey]{menuOffset}. End turn[/]");

            // Get player choice
            int choice2;
            while (true)
            {
                Console.Write($"\nChoose move (1-{menuOffset}): ");
                if (int.TryParse(Console.ReadLine(), out choice2) && choice2 >= 1 && choice2 <= menuOffset)
                {
                    break;
                }

                Console.WriteLine("Invalid choice!");
            }

            // Check if user chose to end turn
            if (choice2 == menuOffset)
            {
                game.EndTurn();
            }

            // Check if user chose to undo
            else if (game.MoveHistory.Count > 0 && choice2 == validMoves.Count + 1)
            {
                if (game.UndoLastMove())
                {
                    AnsiConsole.MarkupLine("[yellow]⬅️  Move undone[/]");
                }
            }

            // Execute the selected move
            else
            {
                var move = validMoves[choice2 - 1];
                game.ExecuteMove(move);
                AnsiConsole.MarkupLine($"[green]✓ Executed: {move.ToString().EscapeMarkup()}[/]");

                if (move.IsHit)
                {
                    AnsiConsole.MarkupLine($"[magenta]💥 Hit! {game.GetOpponent().Name.EscapeMarkup()} has {game.GetOpponent().CheckersOnBar} checker(s) on the bar.[/]");
                }

                if (game.RemainingMoves.Count > 0)
                {
                    AnsiConsole.MarkupLine($"[yellow]📋 Remaining moves: {string.Join(", ", game.RemainingMoves).EscapeMarkup()}[/]");
                }
            }
        }

        // Game over
        AnsiConsole.Clear();
        DrawBoard(game);
        AnsiConsole.WriteLine();

        var gameOverRule = new Rule("[yellow]G A M E   O V E R[/]");
        gameOverRule.Style = Style.Parse("yellow");
        AnsiConsole.Write(gameOverRule);
        AnsiConsole.WriteLine();

        var winnerMarkup = game.Winner!.Color == CheckerColor.White ? "white" : "red";
        AnsiConsole.MarkupLine($"[{winnerMarkup}]🏆 {game.Winner.Name.EscapeMarkup()} wins![/]");

        int result = game.GetGameResult();
        string resultType = result == 3 * game.DoublingCube.Value ? "Backgammon!" :
                           result == 2 * game.DoublingCube.Value ? "Gammon!" : string.Empty;
        AnsiConsole.MarkupLine($"[green]💰 Points: {result} {resultType.EscapeMarkup()}[/]");
    }

    private static void DrawBoard(GameEngine game)
    {
        var table = new Table();
        table.Border = TableBorder.Double;
        table.BorderStyle = new Style(foreground: Color.Cyan);

        // Add columns
        table.AddColumn(new TableColumn("13").Centered());
        table.AddColumn(new TableColumn("14").Centered());
        table.AddColumn(new TableColumn("15").Centered());
        table.AddColumn(new TableColumn("16").Centered());
        table.AddColumn(new TableColumn("17").Centered());
        table.AddColumn(new TableColumn("18").Centered());
        table.AddColumn(new TableColumn(string.Empty).Width(1));
        table.AddColumn(new TableColumn("19").Centered());
        table.AddColumn(new TableColumn("20").Centered());
        table.AddColumn(new TableColumn("21").Centered());
        table.AddColumn(new TableColumn("22").Centered());
        table.AddColumn(new TableColumn("23").Centered());
        table.AddColumn(new TableColumn("24").Centered());

        // Top half (points 13-24) - 5 rows of checkers
        for (int row = 0; row < 5; row++)
        {
            var columns = new List<string>();

            for (int point = 13; point <= 18; point++)
            {
                columns.Add(GetCheckerMarkup(game.Board.GetPoint(point), row));
            }

            columns.Add(string.Empty); // Bar separator

            for (int point = 19; point <= 24; point++)
            {
                columns.Add(GetCheckerMarkup(game.Board.GetPoint(point), row));
            }

            table.AddRow(columns.ToArray());
        }

        // Middle separator row with bar/off info
        var midRow = new List<string>();
        for (int i = 0; i < 6; i++)
        {
            midRow.Add(string.Empty);
        }

        midRow.Add($"[grey]BAR[/]\n[white]W:{game.WhitePlayer.CheckersOnBar}[/] [red]R:{game.RedPlayer.CheckersOnBar}[/]");
        for (int i = 0; i < 5; i++)
        {
            midRow.Add(string.Empty);
        }

        midRow.Add($"[grey]RED OFF[/]\n[red]{game.RedPlayer.CheckersBornOff}[/]");
        table.AddRow(midRow.ToArray());

        // Bottom half (points 12-1) - 5 rows of checkers
        for (int row = 4; row >= 0; row--)
        {
            var columns = new List<string>();

            for (int point = 12; point >= 7; point--)
            {
                columns.Add(GetCheckerMarkup(game.Board.GetPoint(point), row));
            }

            columns.Add(string.Empty); // Bar separator

            for (int point = 6; point >= 1; point--)
            {
                columns.Add(GetCheckerMarkup(game.Board.GetPoint(point), row));
            }

            table.AddRow(columns.ToArray());
        }

        // Add White's borne-off checkers row at the bottom
        var bottomRow = new List<string>();
        for (int i = 0; i < 12; i++)
        {
            bottomRow.Add(string.Empty);
        }

        bottomRow.Add($"[grey]WHITE OFF[/]\n[white]{game.WhitePlayer.CheckersBornOff}[/]");
        table.AddRow(bottomRow.ToArray());

        // Update footer with point numbers
        table.Columns[0].Footer = new Markup("[yellow]12[/]");
        table.Columns[1].Footer = new Markup("[yellow]11[/]");
        table.Columns[2].Footer = new Markup("[yellow]10[/]");
        table.Columns[3].Footer = new Markup("[yellow]9[/]");
        table.Columns[4].Footer = new Markup("[yellow]8[/]");
        table.Columns[5].Footer = new Markup("[yellow]7[/]");
        table.Columns[6].Footer = new Markup(string.Empty);
        table.Columns[7].Footer = new Markup("[yellow]6[/]");
        table.Columns[8].Footer = new Markup("[yellow]5[/]");
        table.Columns[9].Footer = new Markup("[yellow]4[/]");
        table.Columns[10].Footer = new Markup("[yellow]3[/]");
        table.Columns[11].Footer = new Markup("[yellow]2[/]");
        table.Columns[12].Footer = new Markup("[yellow]1[/]");

        AnsiConsole.Write(table);
    }

    private static string GetCheckerMarkup(Point point, int row)
    {
        if (point.Count > row)
        {
            if (point.Color == CheckerColor.White)
            {
                return "[white on grey] ◯ [/]";
            }
            else
            {
                return "[red on maroon] ● [/]";
            }
        }
        else if (row == 4 && point.Count > 5)
        {
            var color = point.Color == CheckerColor.White ? "white" : "red";
            return $"[{color}]{point.Count,2}[/]";
        }

        return "   ";
    }
}
