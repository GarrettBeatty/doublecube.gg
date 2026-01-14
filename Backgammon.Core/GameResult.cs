namespace Backgammon.Core;

public class GameResult
{
    public GameResult()
    {
        CubeValue = 1;
        WinType = WinType.Normal;
    }

    public GameResult(string winnerId, WinType winType, int cubeValue)
        : this()
    {
        WinnerId = winnerId;
        WinType = winType;
        CubeValue = cubeValue;
        CalculatePoints();
    }

    public string WinnerId { get; set; } = string.Empty;

    public CheckerColor? WinnerColor { get; set; }

    public int PointsWon { get; set; }

    public WinType WinType { get; set; }

    public int CubeValue { get; set; }

    public List<Move> MoveHistory { get; set; } = new();

    /// <summary>
    /// Flag to indicate game was abandoned (no points should be awarded to winner).
    /// Used to differentiate Abandon (0 points) from Forfeit (points awarded).
    /// </summary>
    public bool IsAbandoned { get; set; }

    public void SetWinType(WinType winType)
    {
        WinType = winType;
        CalculatePoints();
    }

    public void SetCubeValue(int cubeValue)
    {
        CubeValue = cubeValue;
        CalculatePoints();
    }

    private void CalculatePoints()
    {
        PointsWon = WinType switch
        {
            WinType.Normal => 1 * CubeValue,
            WinType.Gammon => 2 * CubeValue,
            WinType.Backgammon => 3 * CubeValue,
            _ => 1 * CubeValue
        };
    }
}
