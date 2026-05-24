namespace Backgammon.Core;

/// <summary>
/// Pure, stateless move validation logic extracted from <see cref="GameEngine"/>.
/// All methods take explicit state parameters and have no side effects.
/// </summary>
internal static class MoveValidator
{
    /// <summary>
    /// Returns all legal single-die moves for the current position.
    /// </summary>
    internal static List<Move> GetValidSingleDieMoves(Board board, Player currentPlayer, List<int> remainingMoves)
    {
        var validMoves = new List<Move>();

        // If checkers on bar, must enter first
        if (currentPlayer.CheckersOnBar > 0)
        {
            foreach (var die in remainingMoves.Distinct())
            {
                int entryPoint = currentPlayer.Color == CheckerColor.White ? 25 - die : die;
                var move = new Move(0, entryPoint, die);
                if (IsValidSingleMove(board, currentPlayer, remainingMoves, move))
                {
                    validMoves.Add(move);
                }
            }

            return validMoves;
        }

        // Check bearing off
        if (board.AreAllCheckersInHomeBoard(currentPlayer, currentPlayer.CheckersOnBar))
        {
            var (homeStart, homeEnd) = currentPlayer.GetHomeBoardRange();
            for (int pos = homeStart; pos <= homeEnd; pos++)
            {
                var point = board.GetPoint(pos);
                if (point.Color == currentPlayer.Color && point.Count > 0)
                {
                    foreach (var die in remainingMoves.Distinct())
                    {
                        var move = new Move(pos, 25, die);
                        if (IsValidSingleMove(board, currentPlayer, remainingMoves, move))
                        {
                            validMoves.Add(move);
                        }
                    }
                }
            }
        }

        // Normal moves
        for (int from = 1; from <= 24; from++)
        {
            var fromPoint = board.GetPoint(from);
            if (fromPoint.Color != currentPlayer.Color || fromPoint.Count == 0)
            {
                continue;
            }

            foreach (var die in remainingMoves.Distinct())
            {
                int to = from + (currentPlayer.GetDirection() * die);
                if (to >= 1 && to <= 24)
                {
                    var move = new Move(from, to, die);
                    if (IsValidSingleMove(board, currentPlayer, remainingMoves, move))
                    {
                        validMoves.Add(move);
                    }
                }
            }
        }

        return validMoves;
    }

    /// <summary>
    /// Validates a single-die move against current game state.
    /// </summary>
    internal static bool IsValidSingleMove(Board board, Player currentPlayer, List<int> remainingMoves, Move move)
    {
        if (!remainingMoves.Contains(move.DieValue))
        {
            return false;
        }

        // Bar priority: must enter from bar first
        if (currentPlayer.CheckersOnBar > 0 && move.From != 0)
        {
            return false;
        }

        // Entering from bar
        if (move.From == 0)
        {
            if (currentPlayer.CheckersOnBar == 0)
            {
                return false;
            }

            var destPoint = board.GetPoint(move.To);
            return destPoint.IsOpen(currentPlayer.Color);
        }

        // Bearing off
        if (move.IsBearOff)
        {
            return CanBearOff(board, currentPlayer, move.From, move.DieValue);
        }

        // Normal move
        var fromPoint = board.GetPoint(move.From);
        if (fromPoint.Color != currentPlayer.Color || fromPoint.Count == 0)
        {
            return false;
        }

        var toPoint = board.GetPoint(move.To);
        return toPoint.IsOpen(currentPlayer.Color);
    }

    /// <summary>
    /// Validates a combined multi-die move.
    /// </summary>
    internal static bool IsValidCombinedMove(Board board, Player currentPlayer, List<int> remainingMoves, Move combinedMove)
    {
        if (combinedMove.DiceUsed == null || combinedMove.DiceUsed.Length < 2)
        {
            return false;
        }

        // Check all required dice are available
        var availableDice = new List<int>(remainingMoves);
        foreach (var die in combinedMove.DiceUsed)
        {
            if (!availableDice.Contains(die))
            {
                return false;
            }

            availableDice.Remove(die);
        }

        // Validate by simulating execution (verifies path is valid)
        var calculator = new CombinedMoveCalculator(board, currentPlayer, remainingMoves);
        var singleDestinations = GetValidSingleDieMoves(board, currentPlayer, remainingMoves)
            .Where(m => m.From == combinedMove.From)
            .Select(m => m.To)
            .ToHashSet();

        var validCombinedMoves = calculator.Calculate(combinedMove.From, singleDestinations);
        return validCombinedMoves.Any(m => m.From == combinedMove.From && m.To == combinedMove.To);
    }

    /// <summary>
    /// Determines whether a checker can be borne off from <paramref name="from"/> using <paramref name="dieValue"/>.
    /// </summary>
    internal static bool CanBearOff(Board board, Player currentPlayer, int from, int dieValue)
    {
        if (!board.AreAllCheckersInHomeBoard(currentPlayer, currentPlayer.CheckersOnBar))
        {
            return false;
        }

        var fromPoint = board.GetPoint(from);
        if (fromPoint.Color != currentPlayer.Color || fromPoint.Count == 0)
        {
            return false;
        }

        var (homeStart, homeEnd) = currentPlayer.GetHomeBoardRange();

        if (currentPlayer.Color == CheckerColor.White)
        {
            if (from < homeStart || from > homeEnd)
            {
                return false;
            }

            if (from == dieValue)
            {
                return true;
            }

            if (dieValue > from)
            {
                int highestPoint = board.GetHighestPoint(currentPlayer.Color);
                return from == highestPoint;
            }
        }
        else
        {
            if (from < homeStart || from > homeEnd)
            {
                return false;
            }

            int normalizedPosition = 25 - from;

            if (normalizedPosition == dieValue)
            {
                return true;
            }

            if (dieValue > normalizedPosition)
            {
                int highestPoint = board.GetHighestPoint(currentPlayer.Color);
                return from == highestPoint;
            }
        }

        return false;
    }
}
