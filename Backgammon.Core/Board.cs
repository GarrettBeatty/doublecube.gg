namespace Backgammon.Core;

/// <summary>
/// Represents the backgammon board with 24 points
/// </summary>
public class Board
{
    private readonly Point[] _points;

    public Board()
    {
        _points = new Point[25]; // Points 1-24, index 0 unused
        for (int i = 1; i <= 24; i++)
        {
            _points[i] = new Point(i);
        }
    }

    /// <summary>
    /// Get a point by position (1-24)
    /// </summary>
    public Point GetPoint(int position)
    {
        if (position < 1 || position > 24)
        {
            throw new ArgumentOutOfRangeException(nameof(position));
        }

        return _points[position];
    }

    /// <summary>
    /// Initialize the board with standard starting position
    /// </summary>
    public void SetupInitialPosition()
    {
        // Clear all points
        for (int i = 1; i <= 24; i++)
        {
            _points[i].Checkers.Clear();
        }

        // White's initial position (moves from high to low: 24->1)
        AddCheckers(24, CheckerColor.White, 2);  // Far from home
        AddCheckers(13, CheckerColor.White, 5);  // Mid-board
        AddCheckers(8, CheckerColor.White, 3);   // Getting closer
        AddCheckers(6, CheckerColor.White, 5);   // Just outside home

        // Red's initial position (moves from low to high: 1->24)
        AddCheckers(1, CheckerColor.Red, 2);     // Far from home
        AddCheckers(12, CheckerColor.Red, 5);    // Mid-board
        AddCheckers(17, CheckerColor.Red, 3);    // Getting closer
        AddCheckers(19, CheckerColor.Red, 5);    // Just outside home
    }

    /// <summary>
    /// Add multiple checkers to a point
    /// </summary>
    private void AddCheckers(int position, CheckerColor color, int count)
    {
        for (int i = 0; i < count; i++)
        {
            _points[position].AddChecker(color);
        }
    }

    /// <summary>
    /// Check if all checkers of a color are in the home board
    /// </summary>
    public bool AreAllCheckersInHomeBoard(Player player, int checkersOnBar)
    {
        if (checkersOnBar > 0)
        {
            return false;
        }

        var (homeStart, homeEnd) = player.GetHomeBoardRange();

        for (int i = 1; i <= 24; i++)
        {
            // Skip home board range
            if (player.Color == CheckerColor.White && i >= homeStart && i <= homeEnd)
            {
                continue;
            }

            if (player.Color == CheckerColor.Red && i >= homeStart && i <= homeEnd)
            {
                continue;
            }

            // Check if any checkers outside home board
            var point = _points[i];
            if (point.Color == player.Color && point.Count > 0)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Get the highest point with a checker for bearing off
    /// </summary>
    public int GetHighestPoint(CheckerColor color)
    {
        if (color == CheckerColor.White)
        {
            for (int i = 6; i >= 1; i--)
            {
                if (_points[i].Color == color && _points[i].Count > 0)
                {
                    return i;
                }
            }
        }
        else
        {
            for (int i = 19; i <= 24; i++)
            {
                if (_points[i].Color == color && _points[i].Count > 0)
                {
                    return i;
                }
            }
        }

        return 0;
    }

    /// <summary>
    /// Count checkers of a specific color on the board
    /// </summary>
    public int CountCheckers(CheckerColor color)
    {
        int count = 0;
        for (int i = 1; i <= 24; i++)
        {
            if (_points[i].Color == color)
            {
                count += _points[i].Count;
            }
        }

        return count;
    }
}
