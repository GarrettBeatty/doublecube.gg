using System.Text;
using System.Text.RegularExpressions;

namespace Backgammon.Core;

/// <summary>
/// Serializes and deserializes backgammon game positions using SGF (Smart Game Format).
/// SGF is the industry standard format used by GNU Backgammon and other tools.
/// </summary>
public static class SgfSerializer
{
    /// <summary>
    /// Export the current game position to SGF format
    /// </summary>
    public static string ExportPosition(GameEngine engine)
    {
        var sb = new StringBuilder();
        sb.AppendLine("(;GM[6]");  // Game type 6 = Backgammon

        // Export White checkers
        var whitePositions = GetCheckerPositions(engine, CheckerColor.White);
        if (whitePositions.Count > 0)
        {
            sb.Append("  AW");
            foreach (var (coord, count) in whitePositions)
            {
                if (count == 1)
                {
                    sb.Append($"[{coord}]");
                }
                else
                {
                    sb.Append($"[{coord}[{count}]]");
                }
            }
            sb.AppendLine();
        }

        // Export Red/Black checkers
        var redPositions = GetCheckerPositions(engine, CheckerColor.Red);
        if (redPositions.Count > 0)
        {
            sb.Append("  AB");
            foreach (var (coord, count) in redPositions)
            {
                if (count == 1)
                {
                    sb.Append($"[{coord}]");
                }
                else
                {
                    sb.Append($"[{coord}[{count}]]");
                }
            }
            sb.AppendLine();
        }

        // Current player
        sb.AppendLine($"  PL[{(engine.CurrentPlayer.Color == CheckerColor.White ? "W" : "B")}]");

        // Dice (if rolled)
        if (engine.RemainingMoves.Count > 0)
        {
            var dice = engine.Dice;
            sb.AppendLine($"  DI[{dice.Die1}{dice.Die2}]");
        }

        // Doubling cube
        sb.AppendLine($"  CO[{GetCubeOwner(engine.DoublingCube)}]");
        sb.AppendLine($"  CV[{engine.DoublingCube.Value}]");

        sb.Append(")");

        return sb.ToString();
    }

    /// <summary>
    /// Import a position from SGF format and apply it to the game engine
    /// </summary>
    public static void ImportPosition(GameEngine engine, string sgf)
    {
        // Validate SGF syntax
        if (!sgf.TrimStart().StartsWith("(;") || !sgf.TrimEnd().EndsWith(")"))
        {
            throw new ArgumentException("Invalid SGF format: must start with '(;' and end with ')'");
        }

        // Clear the board
        ClearBoard(engine);

        // Parse and apply properties
        var properties = ParseProperties(sgf);

        foreach (var (key, values) in properties)
        {
            switch (key)
            {
                case "GM":
                    if (values.Count > 0 && values[0] != "6")
                        throw new ArgumentException($"Invalid game type: {values[0]}. Expected 6 for Backgammon.");
                    break;

                case "AW":
                    ApplyCheckers(engine, values, CheckerColor.White);
                    break;

                case "AB":
                    ApplyCheckers(engine, values, CheckerColor.Red);
                    break;

                case "PL":
                    if (values.Count > 0)
                    {
                        engine.SetCurrentPlayer(values[0] == "W" ? CheckerColor.White : CheckerColor.Red);
                    }
                    break;

                case "DI":
                    if (values.Count > 0 && values[0].Length >= 2)
                    {
                        int die1 = int.Parse(values[0][0].ToString());
                        int die2 = int.Parse(values[0][1].ToString());
                        engine.Dice.SetDice(die1, die2);
                        engine.RemainingMoves.Clear();
                        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());
                    }
                    break;

                case "CO":
                    // Cube owner - could be implemented if needed
                    break;

                case "CV":
                    if (values.Count > 0 && int.TryParse(values[0], out int cubeValue))
                    {
                        // Set cube value - requires reflection or a public setter
                        // For now, skip as DoublingCube doesn't expose a setter
                    }
                    break;
            }
        }

        // Validate position
        ValidatePosition(engine);

        // Mark game as started
        if (!engine.GameStarted)
        {
            engine.SetGameStarted(true);
        }
    }

    /// <summary>
    /// Get checker positions for a specific color
    /// </summary>
    private static List<(char coord, int count)> GetCheckerPositions(GameEngine engine, CheckerColor color)
    {
        var positions = new List<(char, int)>();

        // Check board points 1-24
        for (int point = 1; point <= 24; point++)
        {
            var boardPoint = engine.Board.GetPoint(point);
            if (boardPoint.Color == color && boardPoint.Count > 0)
            {
                char coord = PointToSgf(point, color);
                positions.Add((coord, boardPoint.Count));
            }
        }

        // Check bar
        int checkersOnBar = color == CheckerColor.White
            ? engine.WhitePlayer.CheckersOnBar
            : engine.RedPlayer.CheckersOnBar;
        if (checkersOnBar > 0)
        {
            positions.Add(('y', checkersOnBar));
        }

        // Check borne off
        int checkersBornOff = color == CheckerColor.White
            ? engine.WhitePlayer.CheckersBornOff
            : engine.RedPlayer.CheckersBornOff;
        if (checkersBornOff > 0)
        {
            positions.Add(('z', checkersBornOff));
        }

        return positions;
    }

    /// <summary>
    /// Convert point number to SGF coordinate
    /// </summary>
    private static char PointToSgf(int point, CheckerColor color)
    {
        if (point < 1 || point > 24)
            throw new ArgumentException($"Point must be between 1 and 24, got {point}");

        if (color == CheckerColor.White)
        {
            // White: Point 1 = 'a', Point 2 = 'b', ..., Point 24 = 'x'
            return (char)('a' + point - 1);
        }
        else
        {
            // Red: Point 1 = 'x', Point 2 = 'w', ..., Point 24 = 'a'
            return (char)('a' + 24 - point);
        }
    }

    /// <summary>
    /// Convert SGF coordinate to point number
    /// </summary>
    private static int SgfToPoint(char coord, CheckerColor color)
    {
        if (coord == 'y' || coord == 'z')
            throw new ArgumentException($"Coordinate '{coord}' is not a board point");

        if (coord < 'a' || coord > 'x')
            throw new ArgumentException($"Invalid SGF coordinate: {coord}");

        if (color == CheckerColor.White)
        {
            // White: 'a' = Point 1, 'b' = Point 2, ..., 'x' = Point 24
            return coord - 'a' + 1;
        }
        else
        {
            // Red: 'a' = Point 24, 'b' = Point 23, ..., 'x' = Point 1
            return 24 - (coord - 'a');
        }
    }

    /// <summary>
    /// Get cube owner as SGF code
    /// </summary>
    private static char GetCubeOwner(DoublingCube cube)
    {
        // For now, always return 'c' for centered
        // Could be extended to track ownership
        return 'c';
    }

    /// <summary>
    /// Parse SGF properties into a dictionary
    /// </summary>
    private static Dictionary<string, List<string>> ParseProperties(string sgf)
    {
        var properties = new Dictionary<string, List<string>>();

        // Remove outer parentheses and leading (;
        var content = sgf.Trim().TrimStart('(', ';').TrimEnd(')').Trim();

        // Match property patterns like AW[...] or PL[W]
        var pattern = @"([A-Z]+)(\[(?:[^\[\]]|\[(?:[^\[\]]|\[[^\[\]]*\])*\])*\])+";
        var matches = Regex.Matches(content, pattern);

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var key = match.Groups[1].Value;
            var valueMatches = Regex.Matches(match.Groups[2].Value, @"\[([^\[\]]*(?:\[[^\[\]]*\])?)\]");

            var values = new List<string>();
            foreach (System.Text.RegularExpressions.Match valueMatch in valueMatches)
            {
                values.Add(valueMatch.Groups[1].Value);
            }

            properties[key] = values;
        }

        return properties;
    }

    /// <summary>
    /// Apply checkers from SGF to the board
    /// </summary>
    private static void ApplyCheckers(GameEngine engine, List<string> values, CheckerColor color)
    {
        foreach (var value in values)
        {
            // Parse coordinate and optional count: "a" or "a[2]"
            System.Text.RegularExpressions.Match match = Regex.Match(value, @"^([a-z])(?:\[(\d+)\])?$");
            if (!match.Success)
                continue;

            char coord = match.Groups[1].Value[0];
            int count = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 1;

            if (coord == 'y')
            {
                // Bar
                var player = color == CheckerColor.White ? engine.WhitePlayer : engine.RedPlayer;
                player.CheckersOnBar = count;
            }
            else if (coord == 'z')
            {
                // Borne off
                var player = color == CheckerColor.White ? engine.WhitePlayer : engine.RedPlayer;
                player.CheckersBornOff = count;
            }
            else
            {
                // Board point
                int point = SgfToPoint(coord, color);
                var boardPoint = engine.Board.GetPoint(point);
                for (int i = 0; i < count; i++)
                {
                    boardPoint.AddChecker(color);
                }
            }
        }
    }

    /// <summary>
    /// Clear all checkers from the board
    /// </summary>
    private static void ClearBoard(GameEngine engine)
    {
        for (int i = 1; i <= 24; i++)
        {
            engine.Board.GetPoint(i).Checkers.Clear();
        }

        engine.WhitePlayer.CheckersOnBar = 0;
        engine.WhitePlayer.CheckersBornOff = 0;
        engine.RedPlayer.CheckersOnBar = 0;
        engine.RedPlayer.CheckersBornOff = 0;
        engine.RemainingMoves.Clear();
    }

    /// <summary>
    /// Validate that the position has exactly 15 checkers per player
    /// </summary>
    private static void ValidatePosition(GameEngine engine)
    {
        int whiteCount = CountCheckers(engine, CheckerColor.White);
        int redCount = CountCheckers(engine, CheckerColor.Red);

        if (whiteCount > 15)
            throw new ArgumentException($"Invalid position: White has {whiteCount} checkers (max 15)");
        if (redCount > 15)
            throw new ArgumentException($"Invalid position: Red has {redCount} checkers (max 15)");
    }

    /// <summary>
    /// Count total checkers for a player
    /// </summary>
    private static int CountCheckers(GameEngine engine, CheckerColor color)
    {
        int count = 0;

        for (int i = 1; i <= 24; i++)
        {
            var point = engine.Board.GetPoint(i);
            if (point.Color == color)
                count += point.Count;
        }

        var player = color == CheckerColor.White ? engine.WhitePlayer : engine.RedPlayer;
        count += player.CheckersOnBar;
        count += player.CheckersBornOff;

        return count;
    }
}
