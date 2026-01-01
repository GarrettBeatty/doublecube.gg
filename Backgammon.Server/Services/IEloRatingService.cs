namespace Backgammon.Server.Services;

/// <summary>
/// Service for calculating ELO ratings for players
/// </summary>
public interface IEloRatingService
{
    /// <summary>
    /// Calculate new ratings for both players after a game
    /// </summary>
    /// <param name="whiteRating">White player's current rating</param>
    /// <param name="redRating">Red player's current rating</param>
    /// <param name="whiteRatedGames">Number of rated games White player has played</param>
    /// <param name="redRatedGames">Number of rated games Red player has played</param>
    /// <param name="whiteWon">True if White won, false if Red won</param>
    /// <returns>Tuple of (whiteNewRating, redNewRating)</returns>
    (int whiteNewRating, int redNewRating) CalculateNewRatings(
        int whiteRating,
        int redRating,
        int whiteRatedGames,
        int redRatedGames,
        bool whiteWon);

    /// <summary>
    /// Calculate expected score for a player based on rating difference
    /// </summary>
    /// <param name="playerRating">Player's rating</param>
    /// <param name="opponentRating">Opponent's rating</param>
    /// <returns>Expected score between 0.0 and 1.0</returns>
    double CalculateExpectedScore(int playerRating, int opponentRating);

    /// <summary>
    /// Get K-factor based on number of games played
    /// </summary>
    /// <param name="gamesPlayed">Number of rated games played</param>
    /// <returns>K-factor (32 for new players, 24 for established)</returns>
    int GetKFactor(int gamesPlayed);
}
