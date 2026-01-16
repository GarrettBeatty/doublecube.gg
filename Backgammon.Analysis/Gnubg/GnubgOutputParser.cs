using System.Globalization;
using System.Text.RegularExpressions;
using Backgammon.Core;
using Backgammon.Plugins.Models;

namespace Backgammon.Analysis.Gnubg;

/// <summary>
/// Parses GNU Backgammon (gnubg) text output
/// </summary>
public static class GnubgOutputParser
{
    /// <summary>
    /// Parse position evaluation from gnubg output
    /// Example gnubg eval output:
    /// Equity: +0.234
    /// Win: 56.2%
    /// Win G: 12.3%
    /// Win BG: 0.8%
    /// </summary>
    public static PositionEvaluation ParseEvaluation(string gnubgOutput)
    {
        var evaluation = new PositionEvaluation
        {
            Features = new PositionFeatures()
        };

        try
        {
            // Parse equity - look for patterns like "Equity: +0.234" or "Equity  0.234"
            var equityMatch = Regex.Match(gnubgOutput, @"Equity[:\s]+([-+]?\d+\.\d+)", RegexOptions.IgnoreCase);
            if (equityMatch.Success)
            {
                evaluation.Equity = double.Parse(equityMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            }

            // Parse win probability - look for "Win:" followed by percentage
            var winMatch = Regex.Match(gnubgOutput, @"Win[:\s]+([\d.]+)%", RegexOptions.IgnoreCase);
            if (winMatch.Success)
            {
                evaluation.WinProbability = double.Parse(winMatch.Groups[1].Value, CultureInfo.InvariantCulture) / 100.0;
            }

            // Parse gammon probability - look for "Win G:" or "Gammon:"
            var gammonMatch = Regex.Match(gnubgOutput, @"(?:Win\s+G|Gammon)[:\s]+([\d.]+)%", RegexOptions.IgnoreCase);
            if (gammonMatch.Success)
            {
                evaluation.GammonProbability = double.Parse(gammonMatch.Groups[1].Value, CultureInfo.InvariantCulture) / 100.0;
            }

            // Parse backgammon probability - look for "Win BG:" or "Backgammon:"
            var backgammonMatch = Regex.Match(gnubgOutput, @"(?:Win\s+BG|Backgammon)[:\s]+([\d.]+)%", RegexOptions.IgnoreCase);
            if (backgammonMatch.Success)
            {
                evaluation.BackgammonProbability = double.Parse(backgammonMatch.Groups[1].Value, CultureInfo.InvariantCulture) / 100.0;
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse gnubg evaluation output: {ex.Message}", ex);
        }

        return evaluation;
    }

    /// <summary>
    /// Parse move analysis from gnubg hint output
    /// Example gnubg hint output:
    ///     1. Cubeful 2-ply    8/5 8/4                      Eq.: +0.200
    ///        0.571 0.000 0.000 - 0.429 0.000 0.000
    ///     2. Cubeful 2-ply    8/4 6/3                      Eq.: +0.177 (-0.023)
    ///        0.565 0.000 0.000 - 0.435 0.000 0.000
    /// </summary>
    public static List<MoveAnalysis> ParseMoveAnalysis(string gnubgOutput)
    {
        var moveAnalyses = new List<MoveAnalysis>();

        try
        {
            var lines = gnubgOutput.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                // Look for lines starting with a number followed by a period (move rank)
                if (!char.IsDigit(trimmed.FirstOrDefault()))
                {
                    continue;
                }

                var dotIndex = trimmed.IndexOf('.');
                if (dotIndex == -1)
                {
                    continue;
                }

                // Parse rank
                if (!int.TryParse(trimmed.Substring(0, dotIndex), out var rank))
                {
                    continue;
                }

                // Find equity marker "Eq.:"
                var equityIndex = trimmed.IndexOf("Eq.:", StringComparison.OrdinalIgnoreCase);
                if (equityIndex == -1)
                {
                    continue;
                }

                // Extract the section between rank and equity (contains move notation)
                var moveSection = trimmed.Substring(dotIndex + 1, equityIndex - dotIndex - 1).Trim();

                // Remove evaluation type prefix (Cubeful/Cubeless N-ply)
                var moveParts = moveSection.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var notation = string.Empty;

                // Skip evaluation type tokens and collect move notation (format: "N/N N/N")
                foreach (var part in moveParts)
                {
                    if (part.Contains('/'))
                    {
                        notation += (notation.Length > 0 ? " " : string.Empty) + part;
                    }
                }

                if (string.IsNullOrEmpty(notation))
                {
                    continue;
                }

                // Parse equity value after "Eq.:"
                var equityStart = equityIndex + 4;
                var equitySection = trimmed.Substring(equityStart).Trim();

                // Extract first number (may have +/- sign)
                var equityEnd = 0;
                for (int i = 0; i < equitySection.Length; i++)
                {
                    var c = equitySection[i];
                    if (!(char.IsDigit(c) || c == '.' || c == '+' || c == '-'))
                    {
                        if (i > 0)
                        {
                            equityEnd = i;
                        }

                        break;
                    }
                }

                if (equityEnd == 0)
                {
                    equityEnd = equitySection.Length;
                }

                var equityStr = equitySection.Substring(0, equityEnd).Trim();
                if (!double.TryParse(equityStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var equity))
                {
                    continue;
                }

                moveAnalyses.Add(new MoveAnalysis
                {
                    Rank = rank,
                    Notation = notation,
                    Equity = equity
                });
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse gnubg move analysis output: {ex.Message}", ex);
        }

        return moveAnalyses;
    }

    /// <summary>
    /// Parse cube decision from gnubg output
    /// </summary>
    public static CubeDecision ParseCubeDecision(string gnubgOutput)
    {
        var decision = new CubeDecision();

        try
        {
            // Parse "No double" equity
            var noDoubleMatch = Regex.Match(gnubgOutput, @"No\s+double[:\s]+([-+]?\d+\.\d+)", RegexOptions.IgnoreCase);
            if (noDoubleMatch.Success)
            {
                decision.NoDoubleEquity = double.Parse(noDoubleMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            }

            // Parse "Double, take" equity
            var doubleTakeMatch = Regex.Match(gnubgOutput, @"Double,\s+take[:\s]+([-+]?\d+\.\d+)", RegexOptions.IgnoreCase);
            if (doubleTakeMatch.Success)
            {
                decision.DoubleTakeEquity = double.Parse(doubleTakeMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            }

            // Parse "Double, pass" equity
            var doublePassMatch = Regex.Match(gnubgOutput, @"Double,\s+pass[:\s]+([-+]?\d+\.\d+)", RegexOptions.IgnoreCase);
            if (doublePassMatch.Success)
            {
                decision.DoublePassEquity = double.Parse(doublePassMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            }

            // Determine recommendation based on equities
            if (decision.DoubleTakeEquity > decision.NoDoubleEquity &&
                decision.DoubleTakeEquity > decision.DoublePassEquity)
            {
                decision.Recommendation = "Double";
            }
            else if (decision.DoublePassEquity > decision.NoDoubleEquity &&
                     decision.DoublePassEquity > decision.DoubleTakeEquity)
            {
                decision.Recommendation = "TooGood";
            }
            else
            {
                decision.Recommendation = "NoDouble";
            }

            // Look for explicit recommendation in output
            if (gnubgOutput.Contains("Correct cube action: Double", StringComparison.OrdinalIgnoreCase))
            {
                decision.Recommendation = "Double";
            }
            else if (gnubgOutput.Contains("Correct cube action: No double", StringComparison.OrdinalIgnoreCase))
            {
                decision.Recommendation = "NoDouble";
            }
            else if (gnubgOutput.Contains("too good", StringComparison.OrdinalIgnoreCase))
            {
                decision.Recommendation = "TooGood";
            }

            decision.Details = gnubgOutput;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse gnubg cube decision output: {ex.Message}", ex);
        }

        return decision;
    }

    /// <summary>
    /// Convert gnubg move notation (e.g., "24/20 13/9") to list of Move objects.
    /// Handles abbreviated notation where a single move like "12/5" with dice 1,6
    /// means two moves: 12->11->5 or 12->6->5
    /// </summary>
    /// <remarks>
    /// Gnubg returns moves from the current player's perspective where the player
    /// always moves toward their home board (point 1-6 in gnubg's view).
    /// We convert by computing the destination based on die value and player direction:
    /// - White moves descending (from → from - die)
    /// - Red moves ascending (from → from + die)
    ///
    /// Gnubg uses * to indicate hits (e.g., "6/5*" means move to 5 and hit).
    /// We strip the * since we only need coordinates.
    /// </remarks>
    public static List<Move> ParseMoveNotation(string notation, CheckerColor color, List<int> availableDice)
    {
        var moves = new List<Move>();

        // Determine move direction: Red moves ascending (+), White moves descending (-)
        int direction = color == CheckerColor.Red ? 1 : -1;
        bool transformForRed = color == CheckerColor.Red;

        try
        {
            // Remove hit indicators (*) - we only need the coordinates
            notation = notation.Replace("*", string.Empty);

            // Split notation by spaces: "24/20 13/9" -> ["24/20", "13/9"]
            var parts = notation.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // Track which dice we've used
            var remainingDice = new List<int>(availableDice);

            foreach (var part in parts)
            {
                // Handle bar entry: "bar/20" or "bar/24" or abbreviated "bar/17" (using multiple dice)
                // gnubg uses the current player's perspective for point numbers in hint notation
                // For White (O): bar/20 means enter at gnubg point 20 = our point 20, die = 25-20 = 5
                // For Red (X): bar/24 means enter at X's point 24 = our point 1, die = 25-24 = 1
                // Abbreviated: bar/17 for Red with [3,5] means enter at 5, then move to 8 (total 8 = 3+5)
                if (part.Contains("bar", StringComparison.OrdinalIgnoreCase))
                {
                    var moveParts = part.Split('/');
                    if (moveParts.Length == 2 && moveParts[0].Contains("bar", StringComparison.OrdinalIgnoreCase))
                    {
                        var gnubgTo = int.Parse(moveParts[1]);

                        // For Red, gnubg shows X's perspective where point 24 = our point 1
                        var to = transformForRed ? 25 - gnubgTo : gnubgTo;

                        // Total distance from bar to final destination
                        // For Red entering at our point N, total distance = N
                        // For White entering at our point N, total distance = 25 - N
                        var totalDistance = transformForRed ? to : 25 - to;

                        // Try single die entry first
                        if (remainingDice.Contains(totalDistance))
                        {
                            moves.Add(new Move(0, to, totalDistance));
                            remainingDice.Remove(totalDistance);
                        }
                        else
                        {
                            // Abbreviated bar entry - need to expand using multiple dice
                            // Find dice that sum to totalDistance and create intermediate moves
                            bool expanded = ExpandAbbreviatedBarEntry(
                                to, totalDistance, direction, remainingDice, moves);

                            if (!expanded)
                            {
                                // Couldn't expand - log but don't throw (move might not be valid)
                            }
                        }
                    }

                    continue;
                }

                // Handle bearing off: "6/off" or "3/off(2)" (with repetition)
                if (part.Contains("off", StringComparison.OrdinalIgnoreCase))
                {
                    // Parse bear-off with optional repetition: "3/off" or "3/off(2)"
                    var bearOffMatch = Regex.Match(part, @"^(\d+)/off(?:\((\d+)\))?$", RegexOptions.IgnoreCase);
                    if (bearOffMatch.Success)
                    {
                        var gnubgFrom = int.Parse(bearOffMatch.Groups[1].Value);
                        var repetitionCount = bearOffMatch.Groups[2].Success
                            ? int.Parse(bearOffMatch.Groups[2].Value)
                            : 1;

                        var from = transformForRed ? 25 - gnubgFrom : gnubgFrom;

                        // Bear off target: White bears off to 0, Red bears off to 25
                        var bearOffTarget = color == CheckerColor.White ? 0 : 25;

                        // Execute bear-off for each repetition
                        for (int rep = 0; rep < repetitionCount; rep++)
                        {
                            // Die value is gnubg's from (distance from gnubg home board 1-6)
                            var dieValue = gnubgFrom;

                            // For bearing off, might need exact die or higher
                            if (remainingDice.Contains(dieValue))
                            {
                                moves.Add(new Move(from, bearOffTarget, dieValue));
                                remainingDice.Remove(dieValue);
                            }
                            else
                            {
                                // Use any higher die for bearing off
                                var higherDie = remainingDice.FirstOrDefault(d => d >= dieValue);
                                if (higherDie > 0)
                                {
                                    moves.Add(new Move(from, bearOffTarget, higherDie));
                                    remainingDice.Remove(higherDie);
                                }
                            }
                        }
                    }

                    continue;
                }

                // Handle compound moves like "6/5/2" (move through multiple points)
                // This means: 6->5 with one die, then 5->2 with another die
                var slashParts = part.Split('/');
                if (slashParts.Length > 2)
                {
                    // Compound move through multiple points
                    for (int i = 0; i < slashParts.Length - 1; i++)
                    {
                        var gnubgFrom = int.Parse(slashParts[i]);
                        var gnubgTo = int.Parse(slashParts[i + 1]);

                        var from = transformForRed ? 25 - gnubgFrom : gnubgFrom;
                        var to = transformForRed ? 25 - gnubgTo : gnubgTo;
                        var distance = Math.Abs(from - to);

                        if (remainingDice.Contains(distance))
                        {
                            moves.Add(new Move(from, to, distance));
                            remainingDice.Remove(distance);
                        }
                    }

                    continue;
                }

                // Parse regular move: "24/20" or abbreviated "12/5" or with repetition "6/5(3)"
                // Regex to extract: from/to and optional (count)
                var moveMatch = Regex.Match(part, @"^(\d+)/(\d+)(?:\((\d+)\))?$");
                if (moveMatch.Success)
                {
                    var gnubgFrom = int.Parse(moveMatch.Groups[1].Value);
                    var gnubgTo = int.Parse(moveMatch.Groups[2].Value);
                    var repetitionCount = moveMatch.Groups[3].Success ? int.Parse(moveMatch.Groups[3].Value) : 1;

                    // Transform coordinates for Red
                    var from = transformForRed ? 25 - gnubgFrom : gnubgFrom;
                    var to = transformForRed ? 25 - gnubgTo : gnubgTo;
                    var distance = Math.Abs(from - to);

                    // Handle repetition (for doubles like "6/5(3)" meaning move 6->5 three times)
                    for (int rep = 0; rep < repetitionCount; rep++)
                    {
                        // Check if this is a single-die move
                        if (remainingDice.Contains(distance))
                        {
                            moves.Add(new Move(from, to, distance));
                            remainingDice.Remove(distance);
                        }
                        else
                        {
                            // This is an abbreviated move - need to expand it
                            // Try to find multiple dice that sum to the distance
                            bool expanded = ExpandAbbreviatedMove(
                                from, to, distance, direction, remainingDice, moves);

                            if (!expanded)
                            {
                                // Couldn't expand - this shouldn't happen with valid gnubg output
                                throw new Exception($"Could not expand move {part} (rep {rep + 1}/{repetitionCount}) with remaining dice: {string.Join(",", remainingDice)}");
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse move notation '{notation}': {ex.Message}", ex);
        }

        return moves;
    }

    /// <summary>
    /// Parse move notation and return all possible orderings for abbreviated moves.
    /// For two-dice abbreviated moves like "24/15" with dice [5,4], returns both orderings:
    /// - 24→19→15 (using 5 first, then 4)
    /// - 24→20→15 (using 4 first, then 5)
    /// The caller (bot) can validate which ordering is actually executable.
    /// </summary>
    /// <remarks>
    /// gnubg only uses abbreviated notation when the intermediate point doesn't matter for hits.
    /// The ambiguity is only about which intermediate point is blocked, not about making hits.
    /// </remarks>
    public static List<List<Move>> ParseMoveNotationWithAlternatives(
        string notation,
        CheckerColor color,
        List<int> availableDice)
    {
        // Get the primary parsing result
        var primaryMoves = ParseMoveNotation(notation, color, new List<int>(availableDice));

        var result = new List<List<Move>> { primaryMoves };

        // Look for two-dice abbreviated pairs and generate alternatives
        // Pattern: move1.To == move2.From with different dice values
        int direction = color == CheckerColor.Red ? 1 : -1;

        for (int i = 0; i < primaryMoves.Count - 1; i++)
        {
            var move1 = primaryMoves[i];
            var move2 = primaryMoves[i + 1];

            // Check if this is an abbreviated pair (same checker moved twice with different dice)
            if (move1.To == move2.From && move1.DieValue != move2.DieValue)
            {
                // Generate alternative ordering by swapping dice
                var altMoves = new List<Move>(primaryMoves);

                // Calculate alternative intermediate point
                int altIntermediate = move1.From + (direction * move2.DieValue);

                // Replace the pair with swapped ordering
                altMoves[i] = new Move(move1.From, altIntermediate, move2.DieValue);
                altMoves[i + 1] = new Move(altIntermediate, move2.To, move1.DieValue);

                result.Add(altMoves);

                // Only generate one alternative per abbreviated pair to avoid explosion
                // If there are multiple pairs, the bot will validate each sequence
                break;
            }
        }

        return result;
    }

    /// <summary>
    /// Expands an abbreviated move (e.g., 8/5 with 1,1,1 dice) into individual single-die moves.
    /// Tries both dice orderings for non-doubles since one intermediate might be blocked.
    /// </summary>
    private static bool ExpandAbbreviatedMove(
        int from,
        int to,
        int distance,
        int direction,
        List<int> remainingDice,
        List<Move> moves)
    {
        // Try to find dice that sum to the distance
        // For pairs, we need to try BOTH orderings (die1 first vs die2 first)
        // because one intermediate point might be blocked on the board
        // Start with larger die first (more likely to reach a safe point)
        var sortedDice = remainingDice.OrderByDescending(d => d).ToList();

        for (int i = 0; i < sortedDice.Count; i++)
        {
            for (int j = 0; j < sortedDice.Count; j++)
            {
                // Skip using same die twice unless we have duplicates
                if (i == j)
                {
                    continue;
                }

                var die1 = sortedDice[i];
                var die2 = sortedDice[j];

                if (die1 + die2 == distance)
                {
                    // Create intermediate point
                    int intermediate = from + (direction * die1);

                    // Add both moves
                    moves.Add(new Move(from, intermediate, die1));
                    moves.Add(new Move(intermediate, to, die2));

                    // Remove used dice from original list
                    remainingDice.Remove(die1);
                    remainingDice.Remove(die2);

                    return true;
                }
            }
        }

        // Also try using same die twice (for doubles)
        for (int i = 0; i < remainingDice.Count; i++)
        {
            var die = remainingDice[i];
            if (die + die == distance && remainingDice.Count(d => d == die) >= 2)
            {
                int intermediate = from + (direction * die);

                moves.Add(new Move(from, intermediate, die));
                moves.Add(new Move(intermediate, to, die));

                remainingDice.Remove(die);
                remainingDice.Remove(die);

                return true;
            }
        }

        // Try triples (for doubles like 8/5 with three 1s)
        for (int i = 0; i < remainingDice.Count; i++)
        {
            for (int j = i; j < remainingDice.Count; j++)
            {
                for (int k = j; k < remainingDice.Count; k++)
                {
                    if (remainingDice[i] + remainingDice[j] + remainingDice[k] == distance)
                    {
                        var die1 = remainingDice[i];
                        var die2 = remainingDice[j];
                        var die3 = remainingDice[k];

                        int inter1 = from + (direction * die1);
                        int inter2 = inter1 + (direction * die2);

                        moves.Add(new Move(from, inter1, die1));
                        moves.Add(new Move(inter1, inter2, die2));
                        moves.Add(new Move(inter2, to, die3));

                        // Remove used dice - sort indices descending to avoid index shifting
                        var indices = new[] { i, j, k }.Distinct().OrderByDescending(x => x).ToList();
                        foreach (var idx in indices)
                        {
                            // For doubles, might need to remove multiple at same index
                            int count = new[] { i, j, k }.Count(x => x == idx);
                            for (int c = 0; c < count && idx < remainingDice.Count; c++)
                            {
                                remainingDice.RemoveAt(idx);
                            }
                        }

                        return true;
                    }
                }
            }
        }

        // Try quads (for doubles like 8/4 with four 1s)
        if (remainingDice.Count >= 4)
        {
            for (int i = 0; i < remainingDice.Count; i++)
            {
                for (int j = i; j < remainingDice.Count; j++)
                {
                    for (int k = j; k < remainingDice.Count; k++)
                    {
                        for (int l = k; l < remainingDice.Count; l++)
                        {
                            if (remainingDice[i] + remainingDice[j] + remainingDice[k] + remainingDice[l] == distance)
                            {
                                var die1 = remainingDice[i];
                                var die2 = remainingDice[j];
                                var die3 = remainingDice[k];
                                var die4 = remainingDice[l];

                                int inter1 = from + (direction * die1);
                                int inter2 = inter1 + (direction * die2);
                                int inter3 = inter2 + (direction * die3);

                                moves.Add(new Move(from, inter1, die1));
                                moves.Add(new Move(inter1, inter2, die2));
                                moves.Add(new Move(inter2, inter3, die3));
                                moves.Add(new Move(inter3, to, die4));

                                // Remove 4 dice (for doubles, all same value)
                                for (int c = 0; c < 4 && remainingDice.Count > 0; c++)
                                {
                                    remainingDice.RemoveAt(0);
                                }

                                return true;
                            }
                        }
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Expands an abbreviated bar entry (e.g., bar/17 with dice [3,5] for Red).
    /// The first move is always from bar (0) to the entry point using one die,
    /// then subsequent moves continue to the final destination.
    /// Tries larger die first for entry since larger entry points are often safer.
    /// </summary>
    private static bool ExpandAbbreviatedBarEntry(
        int finalTo,
        int totalDistance,
        int direction,
        List<int> remainingDice,
        List<Move> moves)
    {
        // For bar entry, the first die determines the entry point
        // Red enters at points 1-6, White enters at points 19-24
        // direction: +1 for Red (ascending), -1 for White (descending)
        // Try larger dice first for entry (often safer entry points)
        var sortedDice = remainingDice.OrderByDescending(d => d).ToList();

        // Try pairs of different dice
        for (int i = 0; i < sortedDice.Count; i++)
        {
            for (int j = 0; j < sortedDice.Count; j++)
            {
                if (i == j)
                {
                    continue;
                }

                var die1 = sortedDice[i];
                var die2 = sortedDice[j];

                if (die1 + die2 == totalDistance)
                {
                    // For bar entry, die1 determines initial entry point
                    // Red: enters at point = die1, White: enters at point = 25 - die1
                    int entryPoint = direction > 0 ? die1 : 25 - die1;
                    int afterEntry = entryPoint + (direction * die2);

                    // Verify the final position matches
                    if (afterEntry == finalTo)
                    {
                        // First move: bar -> entry point
                        moves.Add(new Move(0, entryPoint, die1));

                        // Second move: entry point -> final destination
                        moves.Add(new Move(entryPoint, finalTo, die2));

                        // Remove used dice
                        remainingDice.Remove(die1);
                        remainingDice.Remove(die2);

                        return true;
                    }
                }
            }
        }

        // Try using same die twice (for doubles)
        for (int i = 0; i < remainingDice.Count; i++)
        {
            var die = remainingDice[i];
            if (die + die == totalDistance && remainingDice.Count(d => d == die) >= 2)
            {
                int entryPoint = direction > 0 ? die : 25 - die;
                int afterEntry = entryPoint + (direction * die);

                if (afterEntry == finalTo)
                {
                    moves.Add(new Move(0, entryPoint, die));
                    moves.Add(new Move(entryPoint, finalTo, die));

                    remainingDice.Remove(die);
                    remainingDice.Remove(die);

                    return true;
                }
            }
        }

        return false;
    }
}
