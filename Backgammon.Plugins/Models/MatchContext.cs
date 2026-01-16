namespace Backgammon.Plugins.Models;

/// <summary>
/// Represents the match context for position evaluation.
/// Contains match score information needed for accurate equity calculations.
/// </summary>
/// <param name="TargetScore">The target score to win the match (e.g., 5 for a 5-point match).</param>
/// <param name="Player1Score">Current score of player 1 (the player on roll for cube decisions).</param>
/// <param name="Player2Score">Current score of player 2 (the opponent).</param>
/// <param name="IsCrawfordGame">Whether this is a Crawford game (no doubling allowed).</param>
public record MatchContext(
    int TargetScore,
    int Player1Score,
    int Player2Score,
    bool IsCrawfordGame)
{
    /// <summary>
    /// Creates a money game context (no match score considerations).
    /// Use when match context is not available or not applicable.
    /// </summary>
    public static MatchContext MoneyGame => new(0, 0, 0, false);

    /// <summary>
    /// Returns true if this is a match game (target score > 0).
    /// </summary>
    public bool IsMatchGame => TargetScore > 0;
}
