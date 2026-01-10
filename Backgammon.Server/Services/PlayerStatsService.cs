using Backgammon.Server.Models;
using Microsoft.Extensions.Logging;

namespace Backgammon.Server.Services;

/// <summary>
/// Service for managing player statistics after game completion
/// </summary>
public class PlayerStatsService : IPlayerStatsService
{
    private readonly IUserRepository _userRepository;
    private readonly IEloRatingService _eloRatingService;
    private readonly ILogger<PlayerStatsService> _logger;

    public PlayerStatsService(
        IUserRepository userRepository,
        IEloRatingService eloRatingService,
        ILogger<PlayerStatsService> logger)
    {
        _userRepository = userRepository;
        _eloRatingService = eloRatingService;
        _logger = logger;
    }

    /// <summary>
    /// Update user statistics after a game is completed
    /// </summary>
    public async Task UpdateStatsAfterGameCompletionAsync(Game game)
    {
        try
        {
            // Only update stats if game actually had two players
            if (game.Status == "WaitingForPlayer" ||
                string.IsNullOrEmpty(game.RedPlayerId) ||
                string.IsNullOrEmpty(game.WhitePlayerId))
            {
                _logger.LogInformation("Skipping stats update for game {GameId} - no opponent joined", game.GameId);
                return;
            }

            // Skip stats for AI games - AI opponents don't have ratings and games against them
            // shouldn't affect player rankings to prevent rating inflation/manipulation
            if (game.IsAiOpponent)
            {
                _logger.LogInformation("Skipping stats update for game {GameId} - AI opponent", game.GameId);
                return;
            }

            // Fetch both users
            User? whiteUser = null;
            User? redUser = null;

            if (!string.IsNullOrEmpty(game.WhiteUserId))
            {
                whiteUser = await _userRepository.GetByUserIdAsync(game.WhiteUserId);
            }

            if (!string.IsNullOrEmpty(game.RedUserId))
            {
                redUser = await _userRepository.GetByUserIdAsync(game.RedUserId);
            }

            // Update ratings only for rated games between two registered users
            // Unrated games and games with anonymous players don't affect ELO ratings
            // Defensive check: AI games should never be rated (enforced in GameEngineMapper)
            // TODO: Race condition risk - If a player completes two games simultaneously, rating updates
            // could be lost (read-modify-write pattern without locking). Consider implementing:
            // - Optimistic locking with DynamoDB conditional writes (version/timestamp field)
            // - Application-level distributed locking (Redis) per user during rating updates
            // - Queue-based processing to serialize rating updates per player
            if (game.IsRated && !game.IsAiOpponent && whiteUser != null && redUser != null)
            {
                try
                {
                    // Validate stakes match game outcome (data integrity check)
                    var expectedStakes = game.WinType switch
                    {
                        "Normal" => 1 * game.DoublingCubeValue,
                        "Gammon" => 2 * game.DoublingCubeValue,
                        "Backgammon" => 3 * game.DoublingCubeValue,
                        _ => throw new InvalidOperationException($"Unknown win type: {game.WinType}")
                    };

                    if (game.Stakes != expectedStakes)
                    {
                        _logger.LogWarning(
                            "Stakes mismatch in game {GameId}: expected {Expected} (WinType={WinType} × Cube={Cube}), got {Actual}. Using expected value.",
                            game.GameId,
                            expectedStakes,
                            game.WinType,
                            game.DoublingCubeValue,
                            game.Stakes);

                        // Use the expected value for rating calculation to prevent data integrity issues
                        game.Stakes = expectedStakes;
                    }

                    var whiteWon = game.Winner == "White";

                    // Store ratings before calculation
                    game.WhiteRatingBefore = whiteUser.Rating;
                    game.RedRatingBefore = redUser.Rating;

                    // Calculate new ratings, incorporating stakes
                    // Stakes = WinType multiplier (1=Normal, 2=Gammon, 3=Backgammon) * DoublingCube value
                    var (whiteNewRating, redNewRating) = _eloRatingService.CalculateNewRatings(
                        whiteUser.Rating,
                        redUser.Rating,
                        whiteUser.RatedGamesCount,
                        redUser.RatedGamesCount,
                        whiteWon,
                        game.Stakes);

                    // Apply rating floor and update white player rating
                    var whiteFlooredRating = Math.Max(User.MinimumRating, whiteNewRating);
                    whiteUser.Rating = whiteFlooredRating;
                    whiteUser.PeakRating = Math.Max(whiteUser.PeakRating, whiteFlooredRating);
                    whiteUser.RatingLastUpdatedAt = DateTime.UtcNow;
                    whiteUser.RatedGamesCount++;

                    // Apply rating floor and update red player rating
                    var redFlooredRating = Math.Max(User.MinimumRating, redNewRating);
                    redUser.Rating = redFlooredRating;
                    redUser.PeakRating = Math.Max(redUser.PeakRating, redFlooredRating);
                    redUser.RatingLastUpdatedAt = DateTime.UtcNow;
                    redUser.RatedGamesCount++;

                    // Store ratings after calculation (use floored values for consistency)
                    game.WhiteRatingAfter = whiteFlooredRating;
                    game.RedRatingAfter = redFlooredRating;

                    // Calculate deltas for logging
                    var whiteDelta = whiteFlooredRating - game.WhiteRatingBefore;
                    var redDelta = redFlooredRating - game.RedRatingBefore;

                    _logger.LogInformation(
                        "Updated ratings for game {GameId}: White {WhiteBefore}→{WhiteAfter} ({WhiteDelta:+#;-#;0}), Red {RedBefore}→{RedAfter} ({RedDelta:+#;-#;0})",
                        game.GameId,
                        game.WhiteRatingBefore,
                        game.WhiteRatingAfter,
                        whiteDelta,
                        game.RedRatingBefore,
                        game.RedRatingAfter,
                        redDelta);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update ratings for game {GameId}", game.GameId);
                    // Continue with stats updates even if rating update fails
                }
            }

            // Update white player stats if registered
            if (whiteUser != null)
            {
                var isWinner = game.Winner == "White";
                UpdateStats(whiteUser.Stats, isWinner, game.Stakes);
                await _userRepository.UpdateUserAsync(whiteUser);
            }

            // Update red player stats if registered
            if (redUser != null)
            {
                var isWinner = game.Winner == "Red";
                UpdateStats(redUser.Stats, isWinner, game.Stakes);
                await _userRepository.UpdateUserAsync(redUser);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user stats after game {GameId}", game.GameId);
        }
    }

    private void UpdateStats(UserStats stats, bool isWinner, int stakes)
    {
        stats.TotalGames++;

        if (isWinner)
        {
            stats.Wins++;
            stats.TotalStakes += stakes;
            stats.WinStreak++;

            if (stats.WinStreak > stats.BestWinStreak)
            {
                stats.BestWinStreak = stats.WinStreak;
            }

            // Track win types
            switch (stakes)
            {
                case 1:
                    stats.NormalWins++;
                    break;
                case 2:
                    stats.GammonWins++;
                    break;
                case 3:
                    stats.BackgammonWins++;
                    break;
            }
        }
        else
        {
            stats.Losses++;
            stats.WinStreak = 0; // Reset streak on loss
        }
    }
}
