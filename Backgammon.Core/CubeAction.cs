namespace Backgammon.Core;

/// <summary>
/// Cube actions in SGF format for backgammon games
/// </summary>
public enum CubeAction
{
    /// <summary>
    /// Player offers a double
    /// </summary>
    Double,

    /// <summary>
    /// Player accepts a double
    /// </summary>
    Take,

    /// <summary>
    /// Player declines a double (ends the game)
    /// </summary>
    Drop,

    /// <summary>
    /// Player resigns
    /// </summary>
    Resign
}
