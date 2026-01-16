using Backgammon.Core;

namespace Backgammon.Analysis.Gnubg;

/// <summary>
/// Converts game positions to gnubg's native format.
/// </summary>
/// <remarks>
/// gnubg uses 'set board simple' format which is 26 space-separated integers:
/// - Points 1-24 from gnubg's perspective (point 1 = player O's ace point)
/// - Index 24: opponent's checkers on bar
/// - Index 25: current player's checkers on bar
///
/// Sign convention:
/// - Positive = current player's checkers
/// - Negative = opponent's checkers
///
/// Player naming:
/// - "O" = player who moves 24→1 (White in our system)
/// - "X" = player who moves 1→24 (Red in our system)
/// </remarks>
public static class GnubgPositionConverter
{
    /// <summary>
    /// Build gnubg 'set board simple' position string from GameEngine.
    /// </summary>
    /// <param name="engine">The game engine with current position.</param>
    /// <returns>Space-separated position string for gnubg.</returns>
    public static string ToSimpleBoardString(GameEngine engine)
    {
        var values = new int[26];
        var currentColor = engine.CurrentPlayer.Color;

        // gnubg's point numbering:
        // - For player O (White): point 1 is their ace point (our point 1), point 24 is far point (our point 24)
        // - For player X (Red): point 1 is their ace point (our point 24), point 24 is far point (our point 1)
        //
        // When White is on roll, we use White's perspective directly.
        // When Red is on roll, we need to reverse the point numbering.

        if (currentColor == CheckerColor.White)
        {
            // White's perspective - point numbers match directly
            for (int i = 0; i < 24; i++)
            {
                var point = engine.Board.GetPoint(i + 1);
                if (point.Count > 0 && point.Color != null)
                {
                    var sign = point.Color == CheckerColor.White ? 1 : -1;
                    values[i] = point.Count * sign;
                }
            }

            // Bar: index 24 = opponent (Red) on bar, index 25 = own (White) on bar
            values[24] = -engine.RedPlayer.CheckersOnBar;
            values[25] = engine.WhitePlayer.CheckersOnBar;
        }
        else
        {
            // Red's perspective - point numbers are reversed
            // gnubg point 1 for Red = our point 24
            // gnubg point 24 for Red = our point 1
            for (int i = 0; i < 24; i++)
            {
                var ourPoint = 24 - i; // gnubg point i+1 = our point 24-i
                var point = engine.Board.GetPoint(ourPoint);
                if (point.Count > 0 && point.Color != null)
                {
                    var sign = point.Color == CheckerColor.Red ? 1 : -1;
                    values[i] = point.Count * sign;
                }
            }

            // Bar: index 24 = opponent (White) on bar, index 25 = own (Red) on bar
            values[24] = -engine.WhitePlayer.CheckersOnBar;
            values[25] = engine.RedPlayer.CheckersOnBar;
        }

        return string.Join(" ", values);
    }

    /// <summary>
    /// Get the gnubg player identifier for the current player.
    /// </summary>
    /// <param name="engine">The game engine.</param>
    /// <returns>"O" for White (moves 24→1), "X" for Red (moves 1→24).</returns>
    public static string GetPlayerString(GameEngine engine)
    {
        // In gnubg: O = player moving from 24 to 1, X = player moving from 1 to 24
        // In our system: White moves 24→1, Red moves 1→24
        return engine.CurrentPlayer.Color == CheckerColor.White ? "O" : "X";
    }

    /// <summary>
    /// Get the dice string for gnubg.
    /// </summary>
    /// <param name="engine">The game engine with dice rolled.</param>
    /// <returns>Dice values as "die1 die2" string.</returns>
    public static string GetDiceString(GameEngine engine)
    {
        return $"{engine.Dice.Die1} {engine.Dice.Die2}";
    }

    /// <summary>
    /// Convert game position to gnubg Position ID format.
    /// </summary>
    /// <remarks>
    /// Position ID is a Base64 encoded representation of the board state.
    /// It correctly handles checkers on the bar, unlike 'set board simple'.
    /// Format: 80 bits encoded as 14 Base64 characters.
    /// Encoding uses unary for each point: n 1-bits followed by 0-bit.
    /// Order: Player O (White) checkers on points 1-24 + bar,
    ///        then Player X (Red) checkers on points 24-1 + bar.
    /// </remarks>
    /// <param name="engine">The game engine with current position.</param>
    /// <returns>Position ID string for use with 'set board' command.</returns>
    public static string ToPositionId(GameEngine engine)
    {
        // gnubg Position ID always uses O's (White's) perspective
        // O = player moving 24→1 (our White)
        // X = player moving 1→24 (our Red)

        var bits = new List<bool>();

        // Player O (White) checkers: points 1-24, then bar
        for (int pointNum = 1; pointNum <= 24; pointNum++)
        {
            var point = engine.Board.GetPoint(pointNum);
            int count = (point.Color == CheckerColor.White) ? point.Count : 0;
            AppendUnary(bits, count);
        }

        // White's bar
        AppendUnary(bits, engine.WhitePlayer.CheckersOnBar);

        // Player X (Red) checkers: points 24-1 (reversed), then bar
        for (int pointNum = 24; pointNum >= 1; pointNum--)
        {
            var point = engine.Board.GetPoint(pointNum);
            int count = (point.Color == CheckerColor.Red) ? point.Count : 0;
            AppendUnary(bits, count);
        }

        // Red's bar
        AppendUnary(bits, engine.RedPlayer.CheckersOnBar);

        // Pad to 80 bits
        while (bits.Count < 80)
        {
            bits.Add(false);
        }

        // Convert to bytes (little-endian bit order within each byte)
        var bytes = new byte[10];
        for (int i = 0; i < 80; i++)
        {
            if (bits[i])
            {
                bytes[i / 8] |= (byte)(1 << (i % 8));
            }
        }

        // Base64 encode and remove trailing padding
        return Convert.ToBase64String(bytes).TrimEnd('=');
    }

    /// <summary>
    /// Append unary encoding for a checker count.
    /// </summary>
    /// <param name="bits">The bit list to append to.</param>
    /// <param name="count">Number of checkers (0-15).</param>
    private static void AppendUnary(List<bool> bits, int count)
    {
        // Unary encoding: n 1-bits followed by one 0-bit
        for (int i = 0; i < count; i++)
        {
            bits.Add(true);
        }

        bits.Add(false);
    }
}
