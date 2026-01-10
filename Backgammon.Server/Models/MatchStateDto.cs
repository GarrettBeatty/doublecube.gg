namespace Backgammon.Server.Models;

/// <summary>
/// Data transfer object for match state.
/// Used to provide authoritative match scores from the server,
/// addressing the trust boundary issue where client-side state may be stale.
/// </summary>
public class MatchStateDto
{
    /// <summary>
    /// The match ID.
    /// </summary>
    public string MatchId { get; set; } = string.Empty;

    /// <summary>
    /// Player 1's current score.
    /// </summary>
    public int Player1Score { get; set; }

    /// <summary>
    /// Player 2's current score.
    /// </summary>
    public int Player2Score { get; set; }

    /// <summary>
    /// Target score to win the match.
    /// </summary>
    public int TargetScore { get; set; }

    /// <summary>
    /// Whether the current game is a Crawford game (doubling cube disabled).
    /// </summary>
    public bool IsCrawfordGame { get; set; }

    /// <summary>
    /// Whether the match is complete.
    /// </summary>
    public bool MatchComplete { get; set; }

    /// <summary>
    /// The winner's player ID if match is complete.
    /// </summary>
    public string? MatchWinner { get; set; }

    /// <summary>
    /// Player 1's display name.
    /// </summary>
    public string Player1Name { get; set; } = string.Empty;

    /// <summary>
    /// Player 2's display name.
    /// </summary>
    public string Player2Name { get; set; } = string.Empty;

    /// <summary>
    /// Current game ID in the match.
    /// </summary>
    public string? CurrentGameId { get; set; }

    /// <summary>
    /// Server timestamp for this state (UTC).
    /// Clients can use this to detect stale data.
    /// </summary>
    public DateTime LastUpdatedAt { get; set; }

    /// <summary>
    /// Creates a MatchStateDto from a Match domain object.
    /// </summary>
    public static MatchStateDto FromMatch(Match match)
    {
        return new MatchStateDto
        {
            MatchId = match.MatchId,
            Player1Score = match.Player1Score,
            Player2Score = match.Player2Score,
            TargetScore = match.TargetScore,
            IsCrawfordGame = match.IsCrawfordGame,
            MatchComplete = match.Status == "Completed",
            MatchWinner = match.WinnerId,
            Player1Name = match.Player1Name,
            Player2Name = match.Player2Name,
            CurrentGameId = match.CurrentGameId,
            LastUpdatedAt = match.LastUpdatedAt
        };
    }
}
