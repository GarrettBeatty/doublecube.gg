using Backgammon.Core;
using Microsoft.Extensions.Logging;

namespace Backgammon.Server.Services;

/// <summary>
/// Generates random valid backgammon positions suitable for daily puzzles.
/// Positions are designed to be interesting and have valid moves.
/// </summary>
public class RandomPositionGenerator
{
    private readonly ILogger<RandomPositionGenerator> _logger;

    public RandomPositionGenerator(ILogger<RandomPositionGenerator> logger)
    {
        _logger = logger;
    }

    private enum GamePhase
    {
        Opening,
        Midgame,
        BearingOff,
        Contact,
    }

    /// <summary>
    /// Generate a valid random backgammon position with dice rolled.
    /// The position will have valid moves available.
    /// </summary>
    /// <param name="maxAttempts">Maximum attempts before giving up (default 100).</param>
    /// <returns>A GameEngine with a valid position and dice rolled.</returns>
    public GameEngine GenerateRandomPosition(int maxAttempts = 100)
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var game = CreateRandomPosition();

            if (game != null && ValidatePosition(game))
            {
                _logger.LogDebug("Generated valid position on attempt {Attempt}", attempt + 1);
                return game;
            }
        }

        // Fallback: return a simple known-good position
        _logger.LogWarning(
            "Failed to generate random position after {MaxAttempts} attempts, using fallback",
            maxAttempts);
        return CreateFallbackPosition();
    }

    private GameEngine? CreateRandomPosition()
    {
        try
        {
            var game = new GameEngine("White", "Red");
            game.StartNewGame();

            // Clear the standard starting position
            ClearBoard(game);

            // Select random game phase with weighted distribution
            var phase = SelectRandomPhase();

            // Generate position based on phase
            switch (phase)
            {
                case GamePhase.Opening:
                    GenerateOpeningPosition(game);
                    break;
                case GamePhase.Midgame:
                    GenerateMidgamePosition(game);
                    break;
                case GamePhase.BearingOff:
                    GenerateBearingOffPosition(game);
                    break;
                case GamePhase.Contact:
                    GenerateContactPosition(game);
                    break;
            }

            // Validate checker counts
            if (!ValidateCheckerCounts(game))
            {
                return null;
            }

            // Roll dice and set up remaining moves
            RollRandomDice(game);

            // Set current player
            var currentColor = Random.Shared.Next(2) == 0 ? CheckerColor.White : CheckerColor.Red;
            game.SetCurrentPlayer(currentColor);

            return game;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to create random position");
            return null;
        }
    }

    private GamePhase SelectRandomPhase()
    {
        // Weighted distribution: Midgame most common, then Contact, then BearingOff, Opening rare
        var roll = Random.Shared.Next(100);
        return roll switch
        {
            < 10 => GamePhase.Opening, // 10%
            < 45 => GamePhase.Midgame, // 35%
            < 70 => GamePhase.Contact, // 25%
            _ => GamePhase.BearingOff // 30%
        };
    }

    private void ClearBoard(GameEngine game)
    {
        for (int i = 1; i <= 24; i++)
        {
            game.Board.GetPoint(i).Checkers.Clear();
        }

        game.WhitePlayer.CheckersOnBar = 0;
        game.RedPlayer.CheckersOnBar = 0;
        game.WhitePlayer.CheckersBornOff = 0;
        game.RedPlayer.CheckersBornOff = 0;
    }

    private void GenerateOpeningPosition(GameEngine game)
    {
        // Near-starting positions with some development
        // White: mostly on 24, 13, 8, 6 with some moves made
        // Red: mostly on 1, 12, 17, 19 with some moves made

        // Place White checkers (home is 1-6, starting from 24 side)
        PlaceCheckersWithVariation(game, CheckerColor.White, new[]
        {
            (24, 2, 1), // 1-2 on point 24
            (13, 4, 2), // 3-5 on point 13
            (8, 2, 2), // 1-3 on point 8
            (6, 4, 2) // 3-5 on point 6
        });

        // Place Red checkers (home is 19-24, starting from 1 side)
        PlaceCheckersWithVariation(game, CheckerColor.Red, new[]
        {
            (1, 2, 1), // 1-2 on point 1
            (12, 4, 2), // 3-5 on point 12
            (17, 2, 2), // 1-3 on point 17
            (19, 4, 2) // 3-5 on point 19
        });

        // Add some variation by moving 1-3 checkers to random points
        MakeRandomAdjustments(game, CheckerColor.White, 2);
        MakeRandomAdjustments(game, CheckerColor.Red, 2);
    }

    private void GenerateMidgamePosition(GameEngine game)
    {
        // Midgame: checkers spread across the board, some contact
        // White moving toward home (1-6), Red moving toward home (19-24)

        // White: spread from midfield toward home
        PlaceRandomCheckers(game, CheckerColor.White, 15, 1, 18);

        // Red: spread from midfield toward their home
        PlaceRandomCheckers(game, CheckerColor.Red, 15, 7, 24);

        // Possibly add checkers on bar (25% chance)
        if (Random.Shared.Next(4) == 0)
        {
            var barCount = Random.Shared.Next(1, 3);
            game.WhitePlayer.CheckersOnBar = barCount;
            // Remove from board to keep count at 15
            RemoveRandomCheckers(game, CheckerColor.White, barCount);
        }
    }

    private void GenerateBearingOffPosition(GameEngine game)
    {
        // Both players have all checkers in their home boards
        // Some checkers may be borne off

        // White home board: points 1-6
        int whiteBornOff = Random.Shared.Next(0, 8); // 0-7 borne off
        game.WhitePlayer.CheckersBornOff = whiteBornOff;
        PlaceRandomCheckers(game, CheckerColor.White, 15 - whiteBornOff, 1, 6);

        // Red home board: points 19-24
        int redBornOff = Random.Shared.Next(0, 8);
        game.RedPlayer.CheckersBornOff = redBornOff;
        PlaceRandomCheckers(game, CheckerColor.Red, 15 - redBornOff, 19, 24);
    }

    private void GenerateContactPosition(GameEngine game)
    {
        // Both players have anchors in opponent's home, primes, etc.
        // More complex tactical positions

        // White: some checkers in red's home (19-24) as anchors, some in own home
        PlaceCheckersWithDistribution(game, CheckerColor.White, new[]
        {
            (1, 6, 6), // 6 checkers in white home
            (7, 12, 4), // 4 in midfield
            (19, 24, 5) // 5 as anchors in red's home
        });

        // Red: some checkers in white's home (1-6) as anchors
        PlaceCheckersWithDistribution(game, CheckerColor.Red, new[]
        {
            (19, 24, 6), // 6 in red home
            (13, 18, 4), // 4 in midfield
            (1, 6, 5) // 5 as anchors in white's home
        });

        // Maybe put someone on the bar
        if (Random.Shared.Next(3) == 0)
        {
            game.WhitePlayer.CheckersOnBar = 1;
            RemoveRandomCheckers(game, CheckerColor.White, 1);
        }
    }

    /// <summary>
    /// Place checkers with variation from base amounts.
    /// </summary>
    private void PlaceCheckersWithVariation(
        GameEngine game,
        CheckerColor color,
        (int Point, int BaseCount, int Variation)[] placements)
    {
        foreach (var (point, baseCount, variation) in placements)
        {
            var count = Math.Max(0, baseCount + Random.Shared.Next(-variation, variation + 1));
            for (int i = 0; i < count; i++)
            {
                var p = game.Board.GetPoint(point);
                if (p.Color == null || p.Color == color)
                {
                    p.AddChecker(color);
                }
            }
        }
    }

    /// <summary>
    /// Place checkers randomly within a range of points.
    /// </summary>
    private void PlaceRandomCheckers(GameEngine game, CheckerColor color, int count, int minPoint, int maxPoint)
    {
        int placed = 0;
        int maxPerPoint = 6;
        int attempts = 0;
        int maxAttempts = count * 10;

        while (placed < count && attempts < maxAttempts)
        {
            attempts++;
            var point = Random.Shared.Next(minPoint, maxPoint + 1);
            var p = game.Board.GetPoint(point);

            // Only place if empty or same color, and not too stacked
            if ((p.Color == null || p.Color == color) && p.Count < maxPerPoint)
            {
                p.AddChecker(color);
                placed++;
            }
        }
    }

    /// <summary>
    /// Place checkers with specified distribution across ranges.
    /// </summary>
    private void PlaceCheckersWithDistribution(
        GameEngine game,
        CheckerColor color,
        (int MinPoint, int MaxPoint, int Count)[] distributions)
    {
        foreach (var (minPoint, maxPoint, count) in distributions)
        {
            PlaceRandomCheckers(game, color, count, minPoint, maxPoint);
        }
    }

    /// <summary>
    /// Make small random adjustments to existing positions.
    /// </summary>
    private void MakeRandomAdjustments(GameEngine game, CheckerColor color, int adjustments)
    {
        for (int i = 0; i < adjustments; i++)
        {
            // Find a point with this color's checkers
            var sourcePoint = FindRandomPointWithColor(game, color);
            if (sourcePoint == null)
            {
                continue;
            }

            // Find a nearby valid destination
            var destPointNum = Random.Shared.Next(1, 25);
            var destPoint = game.Board.GetPoint(destPointNum);

            // Move if valid (empty or same color) and source has checkers
            if ((destPoint.Color == null || destPoint.Color == color) && sourcePoint.Count > 0)
            {
                sourcePoint.RemoveChecker();
                destPoint.AddChecker(color);
            }
        }
    }

    private Point? FindRandomPointWithColor(GameEngine game, CheckerColor color)
    {
        var points = new List<Point>();
        for (int i = 1; i <= 24; i++)
        {
            var p = game.Board.GetPoint(i);
            // More than 1 to avoid stranding
            if (p.Color == color && p.Count > 1)
            {
                points.Add(p);
            }
        }

        return points.Count > 0 ? points[Random.Shared.Next(points.Count)] : null;
    }

    private void RemoveRandomCheckers(GameEngine game, CheckerColor color, int count)
    {
        int removed = 0;
        int attempts = 0;

        while (removed < count && attempts < 100)
        {
            attempts++;
            var point = Random.Shared.Next(1, 25);
            var p = game.Board.GetPoint(point);

            if (p.Color == color && p.Count > 0)
            {
                p.RemoveChecker();
                removed++;
            }
        }
    }

    private void RollRandomDice(GameEngine game)
    {
        var die1 = Random.Shared.Next(1, 7);
        var die2 = Random.Shared.Next(1, 7);

        game.Dice.SetDice(die1, die2);
        game.RemainingMoves.Clear();
        game.RemainingMoves.AddRange(game.Dice.GetMoves());
    }

    private bool ValidateCheckerCounts(GameEngine game)
    {
        // Count all white checkers
        int whiteCount = game.WhitePlayer.CheckersOnBar + game.WhitePlayer.CheckersBornOff;
        for (int i = 1; i <= 24; i++)
        {
            var p = game.Board.GetPoint(i);
            if (p.Color == CheckerColor.White)
            {
                whiteCount += p.Count;
            }
        }

        // Count all red checkers
        int redCount = game.RedPlayer.CheckersOnBar + game.RedPlayer.CheckersBornOff;
        for (int i = 1; i <= 24; i++)
        {
            var p = game.Board.GetPoint(i);
            if (p.Color == CheckerColor.Red)
            {
                redCount += p.Count;
            }
        }

        // Both must have exactly 15 checkers
        if (whiteCount != 15 || redCount != 15)
        {
            _logger.LogDebug(
                "Invalid checker counts: White={WhiteCount}, Red={RedCount}",
                whiteCount,
                redCount);

            // Try to fix by adjusting
            AdjustCheckerCount(game, CheckerColor.White, 15 - whiteCount);
            AdjustCheckerCount(game, CheckerColor.Red, 15 - redCount);

            // Re-validate
            return ValidateCheckerCountsStrict(game);
        }

        return true;
    }

    private bool ValidateCheckerCountsStrict(GameEngine game)
    {
        int whiteCount = game.WhitePlayer.CheckersOnBar + game.WhitePlayer.CheckersBornOff;
        int redCount = game.RedPlayer.CheckersOnBar + game.RedPlayer.CheckersBornOff;

        for (int i = 1; i <= 24; i++)
        {
            var p = game.Board.GetPoint(i);
            if (p.Color == CheckerColor.White)
            {
                whiteCount += p.Count;
            }

            if (p.Color == CheckerColor.Red)
            {
                redCount += p.Count;
            }
        }

        return whiteCount == 15 && redCount == 15;
    }

    private void AdjustCheckerCount(GameEngine game, CheckerColor color, int adjustment)
    {
        if (adjustment > 0)
        {
            // Need to add checkers
            PlaceRandomCheckers(game, color, adjustment, 1, 24);
        }
        else if (adjustment < 0)
        {
            // Need to remove checkers
            RemoveRandomCheckers(game, color, -adjustment);
        }
    }

    private bool ValidatePosition(GameEngine game)
    {
        // Must have exactly 15 checkers per side
        if (!ValidateCheckerCountsStrict(game))
        {
            return false;
        }

        // Must have valid moves available
        var validMoves = game.GetValidMoves();
        if (validMoves.Count == 0)
        {
            _logger.LogDebug("Position has no valid moves");
            return false;
        }

        return true;
    }

    private GameEngine CreateFallbackPosition()
    {
        // A known-good position that always has valid moves
        var game = new GameEngine("White", "Red");
        game.StartNewGame();

        ClearBoard(game);

        // Simple midgame position
        // White: 2 on 24, 5 on 13, 3 on 8, 5 on 6
        for (int i = 0; i < 2; i++)
        {
            game.Board.GetPoint(24).AddChecker(CheckerColor.White);
        }

        for (int i = 0; i < 5; i++)
        {
            game.Board.GetPoint(13).AddChecker(CheckerColor.White);
        }

        for (int i = 0; i < 3; i++)
        {
            game.Board.GetPoint(8).AddChecker(CheckerColor.White);
        }

        for (int i = 0; i < 5; i++)
        {
            game.Board.GetPoint(6).AddChecker(CheckerColor.White);
        }

        // Red: 2 on 1, 5 on 12, 3 on 17, 5 on 19
        for (int i = 0; i < 2; i++)
        {
            game.Board.GetPoint(1).AddChecker(CheckerColor.Red);
        }

        for (int i = 0; i < 5; i++)
        {
            game.Board.GetPoint(12).AddChecker(CheckerColor.Red);
        }

        for (int i = 0; i < 3; i++)
        {
            game.Board.GetPoint(17).AddChecker(CheckerColor.Red);
        }

        for (int i = 0; i < 5; i++)
        {
            game.Board.GetPoint(19).AddChecker(CheckerColor.Red);
        }

        // Roll 6-4 - always has good moves
        game.Dice.SetDice(6, 4);
        game.RemainingMoves.Clear();
        game.RemainingMoves.AddRange(game.Dice.GetMoves());

        game.SetCurrentPlayer(CheckerColor.White);

        return game;
    }
}
