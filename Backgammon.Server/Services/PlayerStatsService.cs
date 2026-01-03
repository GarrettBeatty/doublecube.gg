using Backgammon.Server.Models;
using Microsoft.Extensions.Logging;

namespace Backgammon.Server.Services;

/// <summary>
/// Service for managing player statistics after game completion
/// </summary>
public class PlayerStatsService : IPlayerStatsService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<PlayerStatsService> _logger;

    public PlayerStatsService(
        IUserRepository userRepository,
        ILogger<PlayerStatsService> logger)
    {
        _userRepository = userRepository;
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

            // Skip stats for AI games
            if (game.IsAiOpponent)
            {
                _logger.LogInformation("Skipping stats update for game {GameId} - AI opponent", game.GameId);
                return;
            }

            // Update white player stats if registered
            if (!string.IsNullOrEmpty(game.WhiteUserId))
            {
                var user = await _userRepository.GetByUserIdAsync(game.WhiteUserId);
                if (user != null)
                {
                    var isWinner = game.Winner == "White";
                    UpdateStats(user.Stats, isWinner, game.Stakes);
                    await _userRepository.UpdateStatsAsync(user.UserId, user.Stats);
                }
            }

            // Update red player stats if registered
            if (!string.IsNullOrEmpty(game.RedUserId))
            {
                var user = await _userRepository.GetByUserIdAsync(game.RedUserId);
                if (user != null)
                {
                    var isWinner = game.Winner == "Red";
                    UpdateStats(user.Stats, isWinner, game.Stakes);
                    await _userRepository.UpdateStatsAsync(user.UserId, user.Stats);
                }
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
