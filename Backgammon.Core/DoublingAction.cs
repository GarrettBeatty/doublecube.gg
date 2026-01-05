namespace Backgammon.Core;

/// <summary>
/// Represents a doubling cube action that occurred during a turn
/// </summary>
public enum DoublingAction
{
    /// <summary>
    /// Player offered to double the stakes
    /// </summary>
    Offered,

    /// <summary>
    /// Player accepted the opponent's double
    /// </summary>
    Accepted,

    /// <summary>
    /// Player declined the opponent's double (resigned)
    /// </summary>
    Declined
}
