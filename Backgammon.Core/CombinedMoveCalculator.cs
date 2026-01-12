namespace Backgammon.Core;

/// <summary>
/// Calculates combined moves (using 2+ dice) for a player.
/// </summary>
internal class CombinedMoveCalculator
{
    private readonly Board _board;
    private readonly Player _currentPlayer;
    private readonly List<int> _remainingMoves;

    /// <summary>
    /// Initializes a new instance of the <see cref="CombinedMoveCalculator"/> class.
    /// </summary>
    /// <param name="board">The game board.</param>
    /// <param name="currentPlayer">The current player.</param>
    /// <param name="remainingMoves">The remaining dice values available.</param>
    public CombinedMoveCalculator(Board board, Player currentPlayer, List<int> remainingMoves)
    {
        _board = board;
        _currentPlayer = currentPlayer;
        _remainingMoves = remainingMoves;
    }

    /// <summary>
    /// Calculates all combined moves from a given source point.
    /// </summary>
    /// <param name="sourcePoint">The source point to calculate from.</param>
    /// <param name="singleMoveDestinations">Destinations already reachable with a single die (to exclude).</param>
    /// <returns>A list of combined moves.</returns>
    public List<Move> Calculate(int sourcePoint, HashSet<int> singleMoveDestinations)
    {
        var results = new List<Move>();

        // Don't calculate combined moves if checker is on bar (must enter one at a time)
        if (sourcePoint == 0)
        {
            return results;
        }

        var remainingDice = new List<int>(_remainingMoves);
        var direction = _currentPlayer.GetDirection();
        var playerColor = _currentPlayer.Color;

        // Track visited destinations to avoid duplicates
        var visitedDestinations = new HashSet<int>();

        // Recursive exploration
        FindCombinedPaths(
            sourcePoint,
            sourcePoint,
            remainingDice,
            new List<int>(),
            new List<int>(),
            direction,
            playerColor,
            singleMoveDestinations,
            visitedDestinations,
            results);

        return results;
    }

    private void FindCombinedPaths(
        int originalSource,
        int currentPoint,
        List<int> remainingDice,
        List<int> pathSoFar,
        List<int> diceUsedSoFar,
        int direction,
        CheckerColor playerColor,
        HashSet<int> singleMoveDestinations,
        HashSet<int> visitedDestinations,
        List<Move> results)
    {
        // Try each distinct die value
        foreach (var die in remainingDice.Distinct().ToList())
        {
            int nextPoint = currentPoint + (direction * die);

            // Check if this is a valid landing spot
            bool isValidLanding = false;
            bool isBearOff = false;

            if (nextPoint >= 1 && nextPoint <= 24)
            {
                // Normal board point - check if open
                var point = _board.GetPoint(nextPoint);
                isValidLanding = point.IsOpen(playerColor);
            }
            else if (playerColor == CheckerColor.White && nextPoint <= 0)
            {
                // White bearing off (moving toward 0)
                isBearOff = true;
                isValidLanding = _board.AreAllCheckersInHomeBoard(
                    _currentPlayer,
                    _currentPlayer.CheckersOnBar);

                // Additional check: if die > currentPoint, must be bearing off from highest point
                // Account for the checker having moved from originalSource
                if (isValidLanding && die > currentPoint)
                {
                    int effectiveHighest = GetEffectiveHighestPoint(playerColor, originalSource);
                    isValidLanding = currentPoint == effectiveHighest;
                }

                nextPoint = 25; // Normalize to bear-off point
            }
            else if (playerColor == CheckerColor.Red && nextPoint >= 25)
            {
                // Red bearing off (moving toward 25)
                isBearOff = true;
                isValidLanding = _board.AreAllCheckersInHomeBoard(
                    _currentPlayer,
                    _currentPlayer.CheckersOnBar);

                // Additional check: if die > normalized point (25 - currentPoint), must be bearing off from highest
                // Account for the checker having moved from originalSource
                int normalizedPoint = 25 - currentPoint;
                if (isValidLanding && die > normalizedPoint)
                {
                    int effectiveHighest = GetEffectiveHighestPoint(playerColor, originalSource);
                    isValidLanding = currentPoint == effectiveHighest;
                }

                nextPoint = 25; // Normalize to bear-off point
            }

            if (!isValidLanding)
            {
                continue;
            }

            // Create new path and dice lists
            var newPath = new List<int>(pathSoFar);
            if (!isBearOff)
            {
                newPath.Add(nextPoint);
            }

            var newDiceUsed = new List<int>(diceUsedSoFar) { die };

            // Remove used die from remaining
            var newRemainingDice = new List<int>(remainingDice);
            newRemainingDice.Remove(die);

            // If we've used 2+ dice, this is a combined move
            if (newDiceUsed.Count >= 2)
            {
                int finalDestination = isBearOff ? nextPoint : newPath.Last();

                // Only add if not already reachable with single die and not already visited
                if (!singleMoveDestinations.Contains(finalDestination) &&
                    !visitedDestinations.Contains(finalDestination))
                {
                    visitedDestinations.Add(finalDestination);

                    // Check if final destination is a capture
                    bool isHit = false;
                    if (!isBearOff && finalDestination >= 1 && finalDestination <= 24)
                    {
                        var destPoint = _board.GetPoint(finalDestination);
                        isHit = destPoint.Color != null &&
                                destPoint.Color != playerColor &&
                                destPoint.Count == 1;
                    }

                    // Build intermediate points (all points except final)
                    var intermediatePoints = newPath.Take(newPath.Count - 1).ToArray();
                    if (isBearOff)
                    {
                        intermediatePoints = newPath.ToArray();
                    }

                    results.Add(new Move(
                        from: originalSource,
                        to: finalDestination,
                        diceUsed: newDiceUsed.ToArray(),
                        intermediatePoints: intermediatePoints.Length > 0 ? intermediatePoints : null,
                        isHit: isHit));
                }
            }

            // Continue exploring if more dice remain (for doubles)
            if (newRemainingDice.Count > 0 && !isBearOff)
            {
                FindCombinedPaths(
                    originalSource,
                    nextPoint,
                    newRemainingDice,
                    newPath,
                    newDiceUsed,
                    direction,
                    playerColor,
                    singleMoveDestinations,
                    visitedDestinations,
                    results);
            }
        }
    }

    /// <summary>
    /// Gets the effective highest point for bearing off, accounting for a checker
    /// having moved from the original source (which may now be empty).
    /// </summary>
    private int GetEffectiveHighestPoint(CheckerColor playerColor, int originalSource)
    {
        var sourcePoint = _board.GetPoint(originalSource);

        // If the original source has more than 1 checker, it's still occupied after the move
        if (sourcePoint.Color == playerColor && sourcePoint.Count > 1)
        {
            return _board.GetHighestPoint(playerColor);
        }

        // Original source will be empty (or already empty), find highest excluding it
        if (playerColor == CheckerColor.White)
        {
            for (int i = 6; i >= 1; i--)
            {
                if (i == originalSource)
                {
                    continue;
                }

                var point = _board.GetPoint(i);
                if (point.Color == playerColor && point.Count > 0)
                {
                    return i;
                }
            }
        }
        else
        {
            for (int i = 24; i >= 19; i--)
            {
                if (i == originalSource)
                {
                    continue;
                }

                var point = _board.GetPoint(i);
                if (point.Color == playerColor && point.Count > 0)
                {
                    return i;
                }
            }
        }

        // No other checkers on higher points, the current point is effectively the highest
        return originalSource;
    }
}
