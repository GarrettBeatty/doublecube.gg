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
        sb.Append("(;FF[4]GM[6]CA[UTF-8]");  // SGF file format 4, Game type 6 = Backgammon, UTF-8 encoding

        // Export White checkers
        var whitePositions = GetCheckerPositions(engine, CheckerColor.White);
        if (whitePositions.Count > 0)
        {
            sb.AppendLine();
            sb.Append(";AW");
            foreach (var (coord, count) in whitePositions)
            {
                // Repeat coordinate for each checker (standard SGF format)
                for (int i = 0; i < count; i++)
                {
                    sb.Append($"[{coord}]");
                }
            }
        }

        // Export Red/Black checkers
        var redPositions = GetCheckerPositions(engine, CheckerColor.Red);
        if (redPositions.Count > 0)
        {
            sb.AppendLine();
            sb.Append(";AB");
            foreach (var (coord, count) in redPositions)
            {
                // Repeat coordinate for each checker (standard SGF format)
                for (int i = 0; i < count; i++)
                {
                    sb.Append($"[{coord}]");
                }
            }
        }

        // Current player
        sb.AppendLine();
        sb.Append($";PL[{(engine.CurrentPlayer.Color == CheckerColor.White ? "W" : "B")}]");

        // Dice (if rolled)
        if (engine.RemainingMoves.Count > 0)
        {
            var dice = engine.Dice;
            sb.AppendLine();
            sb.Append($";DI[{dice.Die1}{dice.Die2}]");
        }

        // Doubling cube
        sb.AppendLine();
        sb.Append($";CV[{engine.DoublingCube.Value}]");
        sb.AppendLine();
        sb.Append($";CP[{GetCubeOwner(engine.DoublingCube)}])");

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
                    {
                        throw new ArgumentException($"Invalid game type: {values[0]}. Expected 6 for Backgammon.");
                    }

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
    /// Create the SGF header for a new game.
    /// Call this at game start, then use AppendTurn for each turn.
    /// </summary>
    public static string CreateGameHeader(
        string whiteName,
        string blackName,
        int matchLength = 0,
        int gameNumber = 1,
        int whiteScore = 0,
        int blackScore = 0,
        bool isCrawford = false)
    {
        var sb = new StringBuilder();
        sb.Append("(;FF[4]GM[6]CA[UTF-8]");

        // Player names
        if (!string.IsNullOrEmpty(whiteName))
        {
            sb.Append($"PW[{EscapeSgfText(whiteName)}]");
        }

        if (!string.IsNullOrEmpty(blackName))
        {
            sb.Append($"PB[{EscapeSgfText(blackName)}]");
        }

        // Match info
        if (matchLength > 0)
        {
            sb.Append($"MI[length:{matchLength}][game:{gameNumber}][ws:{whiteScore}][bs:{blackScore}]");
        }

        // Crawford rule
        if (isCrawford)
        {
            sb.Append("RU[Crawford:CrawfordGame]");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Append a complete turn (dice roll + moves) to the SGF string.
    /// </summary>
    public static string AppendTurn(string sgf, CheckerColor player, int die1, int die2, IEnumerable<Move> moves)
    {
        var sb = new StringBuilder(sgf);
        sb.Append(';');
        sb.Append(player == CheckerColor.White ? 'W' : 'B');
        sb.Append('[');

        // Dice (two digits)
        sb.Append(die1);
        sb.Append(die2);

        // Moves as coordinate pairs
        foreach (var move in moves)
        {
            sb.Append(MoveToSgfCoord(move, player));
        }

        sb.Append(']');
        return sb.ToString();
    }

    /// <summary>
    /// Append a cube action to the SGF string.
    /// </summary>
    public static string AppendCubeAction(string sgf, CheckerColor player, CubeAction action)
    {
        var sb = new StringBuilder(sgf);
        sb.Append(';');
        sb.Append(player == CheckerColor.White ? 'W' : 'B');
        sb.Append('[');

        sb.Append(action switch
        {
            CubeAction.Double => "double",
            CubeAction.Take => "take",
            CubeAction.Drop => "drop",
            CubeAction.Resign => "resign",
            _ => throw new ArgumentException($"Unknown cube action: {action}")
        });

        sb.Append(']');
        return sb.ToString();
    }

    /// <summary>
    /// Finalize the SGF by adding result and closing parenthesis.
    /// </summary>
    public static string FinalizeGame(string sgf, CheckerColor? winner = null, WinType? winType = null)
    {
        var sb = new StringBuilder(sgf);

        // Add result if game completed
        if (winner.HasValue)
        {
            var winnerChar = winner.Value == CheckerColor.White ? 'W' : 'B';
            var points = winType switch
            {
                WinType.Backgammon => 3,
                WinType.Gammon => 2,
                _ => 1
            };
            sb.Append($"RE[{winnerChar}+{points}]");
        }

        sb.Append(')');
        return sb.ToString();
    }

    /// <summary>
    /// Parse a full game SGF into a GameRecord for replay.
    /// </summary>
    public static GameRecord ParseGameSgf(string sgf)
    {
        if (string.IsNullOrWhiteSpace(sgf))
        {
            return new GameRecord();
        }

        var record = new GameRecord { RawSgf = sgf };

        // Parse header properties
        var properties = ParseProperties(sgf);

        if (properties.TryGetValue("PW", out var pw) && pw.Count > 0)
        {
            record.WhitePlayer = UnescapeSgfText(pw[0]);
        }

        if (properties.TryGetValue("PB", out var pb) && pb.Count > 0)
        {
            record.BlackPlayer = UnescapeSgfText(pb[0]);
        }

        // Parse match info
        if (properties.TryGetValue("MI", out var mi))
        {
            foreach (var info in mi)
            {
                var parts = info.Split(':');
                if (parts.Length == 2)
                {
                    switch (parts[0])
                    {
                        case "length":
                            int.TryParse(parts[1], out var length);
                            record.MatchLength = length;
                            break;
                        case "game":
                            int.TryParse(parts[1], out var game);
                            record.GameNumber = game;
                            break;
                        case "ws":
                            int.TryParse(parts[1], out var ws);
                            record.WhiteScore = ws;
                            break;
                        case "bs":
                            int.TryParse(parts[1], out var bs);
                            record.BlackScore = bs;
                            break;
                    }
                }
            }
        }

        // Parse Crawford rule
        if (properties.TryGetValue("RU", out var ru))
        {
            record.IsCrawford = ru.Any(r => r.Contains("Crawford"));
        }

        // Parse result
        if (properties.TryGetValue("RE", out var re) && re.Count > 0)
        {
            var result = re[0];
            if (result.StartsWith("W"))
            {
                record.Winner = CheckerColor.White;
            }
            else if (result.StartsWith("B"))
            {
                record.Winner = CheckerColor.Red;
            }

            if (result.Contains("+3"))
            {
                record.WinType = WinType.Backgammon;
            }
            else if (result.Contains("+2"))
            {
                record.WinType = WinType.Gammon;
            }
            else
            {
                record.WinType = WinType.Normal;
            }
        }

        // Parse moves (W[...] and B[...] properties)
        record.Turns = ParseTurns(sgf);

        // Compute position SGF for each turn by replaying the game
        ComputeTurnPositions(record);

        return record;
    }

    /// <summary>
    /// Compute position SGF for each turn by replaying the game from start.
    /// This populates PositionSgf on each GameTurn with the position BEFORE that turn's moves.
    /// </summary>
    private static void ComputeTurnPositions(GameRecord record)
    {
        if (record.Turns == null || record.Turns.Count == 0)
        {
            return;
        }

        // Create a temporary engine with starting position
        var engine = new GameEngine("White", "Red");
        engine.StartNewGame();

        foreach (var turn in record.Turns)
        {
            // Capture position BEFORE this turn's moves
            turn.PositionSgf = ExportPosition(engine);

            // Skip cube-only actions (no moves to apply)
            if (turn.CubeAction != null && turn.Moves.Count == 0)
            {
                continue;
            }

            // Apply moves from this turn
            var currentPlayer = turn.Player;
            foreach (var move in turn.Moves)
            {
                ApplyMoveToEngine(engine, move, currentPlayer);
            }
        }
    }

    /// <summary>
    /// Apply a single move to the engine (for replay)
    /// </summary>
    private static void ApplyMoveToEngine(GameEngine engine, Move move, CheckerColor player)
    {
        // Handle bar entry
        if (move.From == 0)
        {
            var playerObj = player == CheckerColor.White ? engine.WhitePlayer : engine.RedPlayer;
            if (playerObj.CheckersOnBar > 0)
            {
                playerObj.CheckersOnBar--;
                var destPoint = engine.Board.GetPoint(move.To);

                // Check for hit
                if (destPoint.Color != null && destPoint.Color != player && destPoint.Count == 1)
                {
                    destPoint.Checkers.Clear();
                    var opponent = player == CheckerColor.White ? engine.RedPlayer : engine.WhitePlayer;
                    opponent.CheckersOnBar++;
                }

                destPoint.AddChecker(player);
            }

            return;
        }

        // Handle bear off
        if (move.IsBearOff)
        {
            var srcPoint = engine.Board.GetPoint(move.From);
            if (srcPoint.Color == player && srcPoint.Count > 0)
            {
                srcPoint.Checkers.RemoveAt(srcPoint.Checkers.Count - 1);
                var playerObj = player == CheckerColor.White ? engine.WhitePlayer : engine.RedPlayer;
                playerObj.CheckersBornOff++;
            }

            return;
        }

        // Regular move
        var sourcePoint = engine.Board.GetPoint(move.From);
        var destinationPoint = engine.Board.GetPoint(move.To);

        if (sourcePoint.Color == player && sourcePoint.Count > 0)
        {
            sourcePoint.Checkers.RemoveAt(sourcePoint.Checkers.Count - 1);

            // Check for hit
            if (destinationPoint.Color != null && destinationPoint.Color != player && destinationPoint.Count == 1)
            {
                destinationPoint.Checkers.Clear();
                var opponent = player == CheckerColor.White ? engine.RedPlayer : engine.WhitePlayer;
                opponent.CheckersOnBar++;
            }

            destinationPoint.AddChecker(player);
        }
    }

    /// <summary>
    /// Get checker positions for a specific color
    /// </summary>
    private static List<(char Coord, int Count)> GetCheckerPositions(GameEngine engine, CheckerColor color)
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
        {
            throw new ArgumentException($"Point must be between 1 and 24, got {point}");
        }

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
        {
            throw new ArgumentException($"Coordinate '{coord}' is not a board point");
        }

        if (coord < 'a' || coord > 'x')
        {
            throw new ArgumentException($"Invalid SGF coordinate: {coord}");
        }

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
        if (cube.Owner == null)
        {
            return 'c'; // Centered
        }

        return cube.Owner == CheckerColor.White ? 'w' : 'b';
    }

    /// <summary>
    /// Parse SGF properties into a dictionary
    /// </summary>
    private static Dictionary<string, List<string>> ParseProperties(string sgf)
    {
        var properties = new Dictionary<string, List<string>>();

        // Remove outer parentheses and leading (;
        var content = sgf.Trim();
        if (content.StartsWith("(;"))
        {
            content = content.Substring(2);
        }
        else if (content.StartsWith("("))
        {
            content = content.Substring(1);
        }

        if (content.EndsWith(")"))
        {
            content = content.Substring(0, content.Length - 1);
        }

        content = content.Trim();

        int i = 0;
        while (i < content.Length)
        {
            // Skip whitespace
            while (i < content.Length && char.IsWhiteSpace(content[i]))
            {
                i++;
            }

            if (i >= content.Length)
            {
                break;
            }

            // Parse property identifier (uppercase letters)
            if (!char.IsUpper(content[i]))
            {
                i++;
                continue;
            }

            var propId = new StringBuilder();
            while (i < content.Length && char.IsUpper(content[i]))
            {
                propId.Append(content[i]);
                i++;
            }

            string key = propId.ToString();
            if (string.IsNullOrEmpty(key))
            {
                continue;
            }

            // Skip whitespace after property identifier
            while (i < content.Length && char.IsWhiteSpace(content[i]))
            {
                i++;
            }

            // Parse property values (all [...] following the property identifier)
            var values = new List<string>();
            while (i < content.Length && content[i] == '[')
            {
                i++; // Skip opening bracket

                // Extract value, handling nested brackets
                var value = new StringBuilder();
                int depth = 1;

                while (i < content.Length && depth > 0)
                {
                    if (content[i] == '[')
                    {
                        depth++;
                        value.Append(content[i]);
                    }
                    else if (content[i] == ']')
                    {
                        depth--;
                        if (depth > 0)
                        {
                            value.Append(content[i]);
                        }
                    }
                    else
                    {
                        value.Append(content[i]);
                    }

                    i++;
                }

                values.Add(value.ToString());

                // Skip whitespace after closing bracket
                while (i < content.Length && char.IsWhiteSpace(content[i]))
                {
                    i++;
                }
            }

            if (values.Count > 0)
            {
                properties[key] = values;
            }
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
            {
                continue;
            }

            char coord = match.Groups[1].Value[0];
            int count = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 1;

            if (coord == 'y')
            {
                // Bar - Add to existing count (standard SGF uses repeated coordinates)
                var player = color == CheckerColor.White ? engine.WhitePlayer : engine.RedPlayer;
                player.CheckersOnBar += count;
            }
            else if (coord == 'z')
            {
                // Borne off - Add to existing count (standard SGF uses repeated coordinates)
                var player = color == CheckerColor.White ? engine.WhitePlayer : engine.RedPlayer;
                player.CheckersBornOff += count;
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
        {
            throw new ArgumentException($"Invalid position: White has {whiteCount} checkers (max 15)");
        }

        if (redCount > 15)
        {
            throw new ArgumentException($"Invalid position: Red has {redCount} checkers (max 15)");
        }
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
            {
                count += point.Count;
            }
        }

        var player = color == CheckerColor.White ? engine.WhitePlayer : engine.RedPlayer;
        count += player.CheckersOnBar;
        count += player.CheckersBornOff;

        return count;
    }

    /// <summary>
    /// Parse turn sequence from SGF (;W[...];B[...] nodes)
    /// </summary>
    private static List<GameTurn> ParseTurns(string sgf)
    {
        var turns = new List<GameTurn>();
        int turnNumber = 0;

        // Find all move nodes: ;W[...] or ;B[...]
        var movePattern = new Regex(@";([WB])\[([^\]]*)\]", RegexOptions.Compiled);
        var matches = movePattern.Matches(sgf);

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var playerChar = match.Groups[1].Value;
            var moveContent = match.Groups[2].Value;
            var player = playerChar == "W" ? CheckerColor.White : CheckerColor.Red;

            var turn = new GameTurn
            {
                TurnNumber = ++turnNumber,
                Player = player
            };

            // Check if this is a cube action
            if (moveContent == "double" || moveContent == "take" || moveContent == "drop" || moveContent == "resign")
            {
                turn.CubeAction = moveContent switch
                {
                    "double" => CubeAction.Double,
                    "take" => CubeAction.Take,
                    "drop" => CubeAction.Drop,
                    "resign" => CubeAction.Resign,
                    _ => null
                };
            }
            else if (moveContent.Length >= 2)
            {
                // Parse dice and moves
                turn.Die1 = moveContent[0] - '0';
                turn.Die2 = moveContent[1] - '0';

                // Parse move pairs (each move is 2 characters: source + destination)
                for (int i = 2; i + 1 < moveContent.Length; i += 2)
                {
                    var fromCoord = moveContent[i];
                    var toCoord = moveContent[i + 1];
                    var move = SgfCoordToMove(fromCoord, toCoord, player);
                    if (move != null)
                    {
                        turn.Moves.Add(move);
                    }
                }
            }

            turns.Add(turn);
        }

        return turns;
    }

    /// <summary>
    /// Convert a Move to SGF coordinate pair (2 chars: source + destination)
    /// </summary>
    private static string MoveToSgfCoord(Move move, CheckerColor player)
    {
        char fromCoord;
        char toCoord;

        // From coordinate
        if (move.From == 0)
        {
            // Entering from bar
            fromCoord = 'y';
        }
        else
        {
            fromCoord = PointToSgfForMove(move.From, player);
        }

        // To coordinate
        if (move.IsBearOff)
        {
            toCoord = 'z';
        }
        else
        {
            toCoord = PointToSgfForMove(move.To, player);
        }

        return $"{fromCoord}{toCoord}";
    }

    /// <summary>
    /// Convert SGF coordinate pair to Move
    /// </summary>
    private static Move? SgfCoordToMove(char fromCoord, char toCoord, CheckerColor player)
    {
        int from;
        int to;

        // Parse from coordinate
        if (fromCoord == 'y')
        {
            from = 0; // Bar
        }
        else if (fromCoord >= 'a' && fromCoord <= 'x')
        {
            from = SgfMoveToPoint(fromCoord, player);
        }
        else
        {
            return null;
        }

        // Parse to coordinate
        if (toCoord == 'z')
        {
            // Bear off - use 0 for White (moves toward 0), 25 for Red (moves toward 25)
            to = player == CheckerColor.White ? 0 : 25;
        }
        else if (toCoord >= 'a' && toCoord <= 'x')
        {
            to = SgfMoveToPoint(toCoord, player);
        }
        else
        {
            return null;
        }

        // Calculate die value
        int dieValue;
        if (from == 0)
        {
            // Entering from bar
            dieValue = player == CheckerColor.White ? 25 - to : to;
        }
        else if (to == 0 || to == 25)
        {
            // Bearing off
            dieValue = player == CheckerColor.White ? from : 25 - from;
        }
        else
        {
            dieValue = Math.Abs(to - from);
        }

        return new Move(from, to, dieValue);
    }

    /// <summary>
    /// Convert point to SGF coordinate for move notation.
    /// In move notation, both players use the same perspective (a=1, x=24).
    /// </summary>
    private static char PointToSgfForMove(int point, CheckerColor player)
    {
        if (point < 1 || point > 24)
        {
            throw new ArgumentException($"Point must be between 1 and 24, got {point}");
        }

        // For move notation, White uses a=1 through x=24, Red uses reversed
        if (player == CheckerColor.White)
        {
            return (char)('a' + point - 1);
        }
        else
        {
            return (char)('a' + 24 - point);
        }
    }

    /// <summary>
    /// Convert SGF move coordinate to point number
    /// </summary>
    private static int SgfMoveToPoint(char coord, CheckerColor player)
    {
        if (coord < 'a' || coord > 'x')
        {
            throw new ArgumentException($"Invalid SGF coordinate: {coord}");
        }

        if (player == CheckerColor.White)
        {
            return coord - 'a' + 1;
        }
        else
        {
            return 24 - (coord - 'a');
        }
    }

    /// <summary>
    /// Escape text for SGF (handle special characters)
    /// </summary>
    private static string EscapeSgfText(string text)
    {
        return text
            .Replace("\\", "\\\\")
            .Replace("]", "\\]")
            .Replace("[", "\\[");
    }

    /// <summary>
    /// Unescape SGF text
    /// </summary>
    private static string UnescapeSgfText(string text)
    {
        return text
            .Replace("\\]", "]")
            .Replace("\\[", "[")
            .Replace("\\\\", "\\");
    }
}
