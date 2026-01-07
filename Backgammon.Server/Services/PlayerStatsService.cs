using Backgammon.Server.Models;
using Microsoft.Extensions.Logging;

namespace Backgammon.Server.Services;

/// <summary>
/// Service for managing player statistics after game completion
/// </summary>
public class PlayerStatsService : IPlayerStatsService
{
    private const int MinimumRating = 100;

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
            // TODO: Race condition risk - If a player completes two games simultaneously, rating updates
            // could be lost (read-modify-write pattern without locking). Consider implementing:
            // - Optimistic locking with DynamoDB conditional writes (version/timestamp field)
            // - Application-level distributed locking (Redis) per user during rating updates
            // - Queue-based processing to serialize rating updates per player
            if (game.IsRated && whiteUser != null && redUser != null)
            {
                try
                {
                    var whiteWon = game.Winner == "White";

                    // Store ratings before calculation
                    game.WhiteRatingBefore = whiteUser.Rating;
                    game.RedRatingBefore = redUser.Rating;

                    // Calculate new ratings
                    var (whiteNewRating, redNewRating) = _eloRatingService.CalculateNewRatings(
                        whiteUser.Rating,
                        redUser.Rating,
                        whiteUser.RatedGamesCount,
                        redUser.RatedGamesCount,
                        whiteWon);

                    // Update white player rating (enforce minimum rating floor for defense in depth)
                    whiteUser.Rating = Math.Max(MinimumRating, whiteNewRating);
                    whiteUser.PeakRating = Math.Max(whiteUser.PeakRating, whiteNewRating);
                    whiteUser.RatingLastUpdatedAt = DateTime.UtcNow;
                    whiteUser.RatedGamesCount++;

                    // Update red player rating (enforce minimum rating floor for defense in depth)
                    redUser.Rating = Math.Max(MinimumRating, redNewRating);
                    redUser.PeakRating = Math.Max(redUser.PeakRating, redNewRating);
                    redUser.RatingLastUpdatedAt = DateTime.UtcNow;
                    redUser.RatedGamesCount++;

                    // Store ratings after calculation
                    game.WhiteRatingAfter = whiteNewRating;
                    game.RedRatingAfter = redNewRating;

                    _logger.LogInformation(
                        "Updated ratings for game {GameId}: White {WhiteBefore}->{WhiteAfter}, Red {RedBefore}->{RedAfter}",
                        game.GameId,
                        game.WhiteRatingBefore,
                        game.WhiteRatingAfter,
                        game.RedRatingBefore,
                        game.RedRatingAfter);
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
