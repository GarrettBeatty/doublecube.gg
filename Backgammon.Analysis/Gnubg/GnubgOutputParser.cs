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
    public static List<Move> ParseMoveNotation(string notation, CheckerColor color, List<int> availableDice)
    {
        var moves = new List<Move>();

        try
        {
            // Split notation by spaces: "24/20 13/9" -> ["24/20", "13/9"]
            var parts = notation.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // Track which dice we've used
            var remainingDice = new List<int>(availableDice);

            foreach (var part in parts)
            {
                // Handle bar entry: "bar/20"
                if (part.Contains("bar", StringComparison.OrdinalIgnoreCase))
                {
                    var moveParts = part.Split('/');
                    if (moveParts.Length == 2 && moveParts[0].Contains("bar", StringComparison.OrdinalIgnoreCase))
                    {
                        var to = int.Parse(moveParts[1]);
                        var dieValue = 25 - to;

                        // Use the appropriate die
                        if (remainingDice.Contains(dieValue))
                        {
                            moves.Add(new Move(0, to, dieValue));
                            remainingDice.Remove(dieValue);
                        }
                    }

                    continue;
                }

                // Handle bearing off: "6/off"
                if (part.Contains("off", StringComparison.OrdinalIgnoreCase))
                {
                    var moveParts = part.Split('/');
                    if (moveParts.Length == 2)
                    {
                        var from = int.Parse(moveParts[0]);
                        var dieValue = from;

                        // For bearing off, might need exact die or higher
                        if (remainingDice.Contains(dieValue))
                        {
                            moves.Add(new Move(from, 25, dieValue));
                            remainingDice.Remove(dieValue);
                        }
                        else
                        {
                            // Use any higher die for bearing off
                            var higherDie = remainingDice.FirstOrDefault(d => d >= dieValue);
                            if (higherDie > 0)
                            {
                                moves.Add(new Move(from, 25, higherDie));
                                remainingDice.Remove(higherDie);
                            }
                        }
                    }

                    continue;
                }

                // Parse regular move: "24/20" or abbreviated "12/5"
                var regularMoveParts = part.Split('/');
                if (regularMoveParts.Length == 2)
                {
                    var from = int.Parse(regularMoveParts[0]);
                    var to = int.Parse(regularMoveParts[1]);
                    var distance = Math.Abs(from - to);

                    // Check if this is a single-die move
                    if (remainingDice.Contains(distance))
                    {
                        moves.Add(new Move(from, to, distance));
                        remainingDice.Remove(distance);
                    }
                    else
                    {
                        // This is an abbreviated move - need to expand it
                        // Try to find two dice that sum to the distance
                        bool expanded = false;

                        for (int i = 0; i < remainingDice.Count && !expanded; i++)
                        {
                            for (int j = i; j < remainingDice.Count && !expanded; j++)
                            {
                                if (remainingDice[i] + remainingDice[j] == distance)
                                {
                                    // Found two dice that work
                                    var die1 = remainingDice[i];
                                    var die2 = remainingDice[j];

                                    // Create intermediate point
                                    // For white moving down (24->1), intermediate is from - die1
                                    // For red moving up (1->24), intermediate is from + die1
                                    int intermediate = from > to ? from - die1 : from + die1;

                                    // Add both moves
                                    moves.Add(new Move(from, intermediate, die1));
                                    moves.Add(new Move(intermediate, to, die2));

                                    // Remove used dice (remove by index to handle doubles correctly)
                                    if (i == j)
                                    {
                                        // Same die used twice (doubles)
                                        remainingDice.RemoveAt(i);
                                        remainingDice.RemoveAt(i); // Remove again at same index
                                    }
                                    else
                                    {
                                        // Different dice - remove larger index first
                                        remainingDice.RemoveAt(Math.Max(i, j));
                                        remainingDice.RemoveAt(Math.Min(i, j));
                                    }

                                    expanded = true;
                                }
                            }
                        }

                        if (!expanded)
                        {
                            // Couldn't expand - this shouldn't happen with valid gnubg output
                            throw new Exception($"Could not expand move {part} with remaining dice: {string.Join(",", remainingDice)}");
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
}
