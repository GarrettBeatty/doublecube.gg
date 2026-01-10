using Backgammon.Core;
using Backgammon.Server.Models;
using Microsoft.Extensions.Logging;

namespace Backgammon.Server.Services;

/// <summary>
/// Handles queries for valid moves and destinations
/// </summary>
public class MoveQueryService : IMoveQueryService
{
    private readonly IGameSessionManager _sessionManager;
    private readonly ILogger<MoveQueryService> _logger;

    public MoveQueryService(
        IGameSessionManager sessionManager,
        ILogger<MoveQueryService> logger)
    {
        _sessionManager = sessionManager;
        _logger = logger;
    }

    public List<int> GetValidSources(string connectionId)
    {
        try
        {
            var session = _sessionManager.GetGameByPlayer(connectionId);
            if (session == null)
            {
                return new List<int>();
            }

            if (!session.IsPlayerTurn(connectionId))
            {
                return new List<int>();
            }

            if (session.Engine.RemainingMoves.Count == 0)
            {
                return new List<int>();
            }

            var validMoves = session.Engine.GetValidMoves();
            var sources = validMoves.Select(m => m.From).Distinct().ToList();
            return sources;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting valid sources");
            return new List<int>();
        }
    }

    public List<MoveDto> GetValidDestinations(string connectionId, int fromPoint)
    {
        try
        {
            _logger.LogInformation("GetValidDestinations called for point {FromPoint}", fromPoint);
            var session = _sessionManager.GetGameByPlayer(connectionId);
            if (session == null)
            {
                _logger.LogWarning("No session found for player");
                return new List<MoveDto>();
            }

            if (!session.IsPlayerTurn(connectionId))
            {
                _logger.LogWarning("Not player's turn");
                return new List<MoveDto>();
            }

            if (session.Engine.RemainingMoves.Count == 0)
            {
                _logger.LogWarning("No remaining moves");
                return new List<MoveDto>();
            }

            var allValidMoves = session.Engine.GetValidMoves();
            _logger.LogInformation(
                "Total valid moves: {Count}",
                allValidMoves.Count);
            foreach (var m in allValidMoves)
            {
                _logger.LogInformation(
                    "  Valid move: {From} -> {To} (die: {Die})",
                    m.From,
                    m.To,
                    m.DieValue);
            }

            var validMoves = allValidMoves
                .Where(m => m.From == fromPoint)
                .Select(m => new MoveDto
                {
                    From = m.From,
                    To = m.To,
                    DieValue = m.DieValue,
                    IsHit = WillHit(session.Engine, m)
                })
                .ToList();

            _logger.LogInformation(
                "Filtered moves from point {FromPoint}: {Count}",
                fromPoint,
                validMoves.Count);
            return validMoves;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting valid destinations");
            return new List<MoveDto>();
        }
    }

    /// <summary>
    /// Calculates all combined moves (using 2+ dice) from a given source point.
    /// </summary>
    /// <param name="engine">The game engine with current board state.</param>
    /// <param name="sourcePoint">The starting point for the move.</param>
    /// <param name="singleMoveDestinations">Set of destinations reachable with single die (to exclude).</param>
    /// <returns>List of combined move DTOs.</returns>
    public List<MoveDto> CalculateCombinedMoves(
        GameEngine engine,
        int sourcePoint,
        HashSet<int> singleMoveDestinations)
    {
        var results = new List<MoveDto>();

        if (engine.CurrentPlayer == null)
        {
            return results;
        }

        // Don't calculate combined moves if checker is on bar (must enter one at a time)
        if (sourcePoint == 0)
        {
            return results;
        }

        var remainingDice = new List<int>(engine.RemainingMoves);
        var direction = engine.CurrentPlayer.GetDirection();
        var playerColor = engine.CurrentPlayer.Color;

        // Track visited destinations to avoid duplicates
        var visitedDestinations = new HashSet<int>();

        // Recursive exploration
        FindCombinedPaths(
            engine,
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
        GameEngine engine,
        int originalSource,
        int currentPoint,
        List<int> remainingDice,
        List<int> pathSoFar,
        List<int> diceUsedSoFar,
        int direction,
        CheckerColor playerColor,
        HashSet<int> singleMoveDestinations,
        HashSet<int> visitedDestinations,
        List<MoveDto> results)
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
                var point = engine.Board.GetPoint(nextPoint);
                isValidLanding = point.IsOpen(playerColor);
            }
            else if (playerColor == CheckerColor.White && nextPoint <= 0)
            {
                // White bearing off (moving toward 0)
                isBearOff = true;
                // For combined moves during bearing off, we need all checkers in home board
                // and the move must be valid according to bearing off rules
                isValidLanding = engine.Board.AreAllCheckersInHomeBoard(
                    engine.CurrentPlayer!,
                    engine.CurrentPlayer.CheckersOnBar);
                nextPoint = 0; // Normalize to bear-off point
            }
            else if (playerColor == CheckerColor.Red && nextPoint >= 25)
            {
                // Red bearing off (moving toward 25)
                isBearOff = true;
                isValidLanding = engine.Board.AreAllCheckersInHomeBoard(
                    engine.CurrentPlayer!,
                    engine.CurrentPlayer.CheckersOnBar);
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
                        var destPoint = engine.Board.GetPoint(finalDestination);
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

                    results.Add(new MoveDto
                    {
                        From = originalSource,
                        To = finalDestination,
                        DieValue = newDiceUsed.Sum(),
                        IsHit = isHit,
                        IsCombinedMove = true,
                        DiceUsed = newDiceUsed.ToArray(),
                        IntermediatePoints = intermediatePoints.Length > 0 ? intermediatePoints : null
                    });
                }
            }

            // Continue exploring if more dice remain (for doubles)
            if (newRemainingDice.Count > 0 && !isBearOff)
            {
                FindCombinedPaths(
                    engine,
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

    private bool WillHit(GameEngine engine, Move move)
    {
        // Bear-off moves (To = 0 or 25) cannot hit
        if (move.IsBearOff)
        {
            return false;
        }

        var targetPoint = engine.Board.GetPoint(move.To);
        if (targetPoint.Color == null || targetPoint.Count == 0)
        {
            return false;
        }

        return targetPoint.Color != engine.CurrentPlayer?.Color && targetPoint.Count == 1;
    }
}
