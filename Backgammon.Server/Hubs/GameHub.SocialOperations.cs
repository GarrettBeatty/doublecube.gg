using Backgammon.Server.Hubs.Interfaces;
using Backgammon.Server.Models;
using Backgammon.Server.Models.SignalR;
using Backgammon.Server.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Backgammon.Server.Hubs;

/// <summary>
/// GameHub partial class - Social Operations
/// Handles friends, profiles, chat, leaderboards, and online players
/// </summary>
public partial class GameHub
{
    public async Task SendChatMessage(string message)
    {
        try
        {
            await _chatService.SendChatMessageAsync(Context.ConnectionId, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending chat message");
            await Clients.Caller.Error("Failed to send message");
        }
    }

    /// <summary>
    /// Get a player's public profile
    /// </summary>
    public async Task<PlayerProfileDto?> GetPlayerProfile(string username)
    {
        try
        {
            var viewingUserId = GetAuthenticatedUserId();
            var (profile, error) = await _playerProfileService.GetPlayerProfileAsync(username, viewingUserId);

            if (error != null)
            {
                await Clients.Caller.Error(error);
                return null;
            }

            return profile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting player profile");
            await Clients.Caller.Error("Failed to load profile");
            return null;
        }
    }

    /// <summary>
    /// Get rating history for the current user
    /// </summary>
    public async Task<List<RatingHistoryEntryDto>> GetRatingHistory(int limit = 30)
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("GetRatingHistory called without authentication");
                return new List<RatingHistoryEntryDto>();
            }

            var entries = await _userRepository.GetRatingHistoryAsync(userId, limit);
            return entries.Select(RatingHistoryEntryDto.FromEntity).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rating history");
            await Clients.Caller.Error("Failed to load rating history");
            return new List<RatingHistoryEntryDto>();
        }
    }

    // ==================== Friends Methods ====================

    /// <summary>
    /// Get the current user's friends list with online status
    /// </summary>
    public async Task<List<FriendDto>> GetFriends()
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("GetFriends called without authentication");
                return new List<FriendDto>();
            }

            var friends = await _friendService.GetFriendsAsync(userId);
            return friends;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting friends list");
            await Clients.Caller.Error("Failed to load friends");
            return new List<FriendDto>();
        }
    }

    /// <summary>
    /// Get pending friend requests for the current user
    /// </summary>
    public async Task<List<FriendDto>> GetFriendRequests()
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("GetFriendRequests called without authentication");
                return new List<FriendDto>();
            }

            var requests = await _friendService.GetPendingRequestsAsync(userId);
            return requests;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting friend requests");
            await Clients.Caller.Error("Failed to load friend requests");
            return new List<FriendDto>();
        }
    }

    /// <summary>
    /// Search for players by username
    /// </summary>
    public async Task<List<PlayerSearchResultDto>> SearchPlayers(string query)
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("SearchPlayers called without authentication");
                return new List<PlayerSearchResultDto>();
            }

            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                return new List<PlayerSearchResultDto>();
            }

            var users = await _userRepository.SearchUsersAsync(query);
            var onlinePlayerIds = _playerConnectionService.GetAllConnectedPlayerIds().ToHashSet();

            // Exclude the current user from search results
            var results = users
                .Where(u => u.UserId != userId)
                .Select(u => new PlayerSearchResultDto
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    DisplayName = u.DisplayName ?? u.Username,
                    Rating = u.Rating,
                    IsOnline = onlinePlayerIds.Contains(u.UserId),
                    TotalGames = u.Stats?.TotalGames ?? 0
                })
                .ToList();

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching players");
            await Clients.Caller.Error("Failed to search players");
            return new List<PlayerSearchResultDto>();
        }
    }

    /// <summary>
    /// Get all registered players sorted by rating
    /// </summary>
    public async Task<List<PlayerSearchResultDto>> GetAllPlayers(int limit = 50)
    {
        try
        {
            var userId = GetAuthenticatedUserId();

            var users = await _userRepository.GetAllPlayersAsync(limit);
            var onlinePlayerIds = _playerConnectionService.GetAllConnectedPlayerIds().ToHashSet();

            // Exclude the current user from results
            var results = users
                .Where(u => u.UserId != userId)
                .Select(u => new PlayerSearchResultDto
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    DisplayName = u.DisplayName ?? u.Username,
                    Rating = u.Rating,
                    IsOnline = onlinePlayerIds.Contains(u.UserId),
                    TotalGames = u.Stats?.TotalGames ?? 0
                })
                .ToList();

            _logger.LogDebug("Retrieved {Count} players for all players list", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all players");
            await Clients.Caller.Error("Failed to get players");
            return new List<PlayerSearchResultDto>();
        }
    }

    /// <summary>
    /// Send a friend request to another user
    /// </summary>
    public async Task<bool> SendFriendRequest(string toUserId)
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            if (string.IsNullOrEmpty(userId))
            {
                await Clients.Caller.Error("Must be logged in to send friend requests");
                return false;
            }

            var (success, error) = await _friendService.SendFriendRequestAsync(userId, toUserId);

            if (!success)
            {
                await Clients.Caller.Error(error ?? "Failed to send friend request");
                return false;
            }

            _logger.LogInformation("User {UserId} sent friend request to {ToUserId}", userId, toUserId);

            // Notify the recipient if they're online
            var recipientConnection = GetPlayerConnection(toUserId);
            if (!string.IsNullOrEmpty(recipientConnection))
            {
                await Clients.Client(recipientConnection).FriendRequestReceived();
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending friend request");
            await Clients.Caller.Error("Failed to send friend request");
            return false;
        }
    }

    /// <summary>
    /// Accept a friend request
    /// </summary>
    public async Task<bool> AcceptFriendRequest(string friendUserId)
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            if (string.IsNullOrEmpty(userId))
            {
                await Clients.Caller.Error("Must be logged in to accept friend requests");
                return false;
            }

            var (success, error) = await _friendService.AcceptFriendRequestAsync(userId, friendUserId);

            if (!success)
            {
                await Clients.Caller.Error(error ?? "Failed to accept friend request");
                return false;
            }

            _logger.LogInformation("User {UserId} accepted friend request from {FriendUserId}", userId, friendUserId);

            // Notify the requester if they're online
            var requesterConnection = GetPlayerConnection(friendUserId);
            if (!string.IsNullOrEmpty(requesterConnection))
            {
                await Clients.Client(requesterConnection).FriendRequestAccepted();
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting friend request");
            await Clients.Caller.Error("Failed to accept friend request");
            return false;
        }
    }

    /// <summary>
    /// Decline a friend request
    /// </summary>
    public async Task<bool> DeclineFriendRequest(string friendUserId)
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            if (string.IsNullOrEmpty(userId))
            {
                await Clients.Caller.Error("Must be logged in to decline friend requests");
                return false;
            }

            var (success, error) = await _friendService.DeclineFriendRequestAsync(userId, friendUserId);

            if (!success)
            {
                await Clients.Caller.Error(error ?? "Failed to decline friend request");
                return false;
            }

            _logger.LogInformation("User {UserId} declined friend request from {FriendUserId}", userId, friendUserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error declining friend request");
            await Clients.Caller.Error("Failed to decline friend request");
            return false;
        }
    }

    /// <summary>
    /// Remove a friend
    /// </summary>
    public async Task<bool> RemoveFriend(string friendUserId)
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            if (string.IsNullOrEmpty(userId))
            {
                await Clients.Caller.Error("Must be logged in to remove friends");
                return false;
            }

            var (success, error) = await _friendService.RemoveFriendAsync(userId, friendUserId);

            if (!success)
            {
                await Clients.Caller.Error(error ?? "Failed to remove friend");
                return false;
            }

            _logger.LogInformation("User {UserId} removed friend {FriendUserId}", userId, friendUserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing friend");
            await Clients.Caller.Error("Failed to remove friend");
            return false;
        }
    }

    // ============================================
    // Leaderboard & Statistics
    // ============================================

    public async Task<List<LeaderboardEntryDto>> GetLeaderboard(int limit = 50)
    {
        try
        {
            var topPlayers = await _userRepository.GetTopPlayersByRatingAsync(limit);
            var onlinePlayerIds = _playerConnectionService.GetAllConnectedPlayerIds().ToHashSet();

            var leaderboard = topPlayers.Select((user, index) => new LeaderboardEntryDto
            {
                Rank = index + 1,
                UserId = user.UserId,
                Username = user.Username,
                DisplayName = user.DisplayName,
                Rating = user.Rating,
                TotalGames = user.Stats.TotalGames,
                Wins = user.Stats.Wins,
                Losses = user.Stats.Losses,
                WinRate = user.Stats.TotalGames > 0
                    ? Math.Round((double)user.Stats.Wins / user.Stats.TotalGames * 100, 1)
                    : 0,
                IsOnline = onlinePlayerIds.Contains(user.UserId)
            }).ToList();

            _logger.LogDebug("Retrieved leaderboard with {Count} players", leaderboard.Count);
            return leaderboard;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting leaderboard");
            throw new HubException("Failed to get leaderboard");
        }
    }

    /// <summary>
    /// Get list of currently online players.
    /// </summary>
    /// <returns>List of online players.</returns>
    public async Task<List<OnlinePlayerDto>> GetOnlinePlayers()
    {
        try
        {
            var currentUserId = GetAuthenticatedUserId();
            var onlinePlayerIds = _playerConnectionService.GetAllConnectedPlayerIds().ToList();

            if (!onlinePlayerIds.Any())
            {
                return new List<OnlinePlayerDto>();
            }

            var users = await _userRepository.GetUsersByIdsAsync(onlinePlayerIds);

            // Get friends list for the current user to mark friends
            var friends = new HashSet<string>();
            if (!string.IsNullOrEmpty(currentUserId))
            {
                var friendsList = await _friendService.GetFriendsAsync(currentUserId);
                friends = friendsList.Select(f => f.UserId).ToHashSet();
            }

            var onlinePlayers = users
                .Where(u => !u.IsAnonymous && u.UserId != currentUserId)
                .Select(user =>
                {
                    // Check if user is in a game
                    var playerGames = _sessionManager.GetPlayerGames(user.UserId);
                    var activeGame = playerGames.FirstOrDefault(g => !g.Engine.GameOver);

                    return new OnlinePlayerDto
                    {
                        UserId = user.UserId,
                        Username = user.Username,
                        DisplayName = user.DisplayName,
                        Rating = user.Rating,
                        Status = activeGame != null ? OnlinePlayerStatus.InGame : OnlinePlayerStatus.Available,
                        CurrentGameId = activeGame?.Id,
                        IsFriend = friends.Contains(user.UserId)
                    };
                })
                .OrderByDescending(p => p.IsFriend)
                .ThenByDescending(p => p.Rating)
                .ToList();

            _logger.LogDebug("Retrieved {Count} online players", onlinePlayers.Count);
            return onlinePlayers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting online players");
            throw new HubException("Failed to get online players");
        }
    }

    /// <summary>
    /// Get rating distribution statistics.
    /// </summary>
    /// <returns>Rating distribution data.</returns>
    public async Task<RatingDistributionDto> GetRatingDistribution()
    {
        try
        {
            var currentUserId = GetAuthenticatedUserId();
            var allRatings = await _userRepository.GetAllRatingsAsync();

            if (!allRatings.Any())
            {
                return new RatingDistributionDto
                {
                    Buckets = new List<RatingBucketDto>(),
                    TotalPlayers = 0,
                    AverageRating = 0,
                    MedianRating = 0
                };
            }

            var sortedRatings = allRatings.OrderBy(r => r).ToList();
            var totalPlayers = sortedRatings.Count;
            var averageRating = sortedRatings.Average();
            var medianRating = sortedRatings[totalPlayers / 2];

            // Get current user's rating if authenticated
            int? userRating = null;
            double? userPercentile = null;
            if (!string.IsNullOrEmpty(currentUserId))
            {
                var user = await _userRepository.GetByUserIdAsync(currentUserId);
                if (user != null && user.RatedGamesCount > 0)
                {
                    userRating = user.Rating;
                    var playersBelow = sortedRatings.Count(r => r < user.Rating);
                    userPercentile = Math.Round((double)playersBelow / totalPlayers * 100, 1);
                }
            }

            // Create buckets (100-point ranges)
            var minRating = (sortedRatings.Min() / 100) * 100;
            var maxRating = ((sortedRatings.Max() / 100) + 1) * 100;
            var buckets = new List<RatingBucketDto>();

            for (int bucketStart = minRating; bucketStart < maxRating; bucketStart += 100)
            {
                var bucketEnd = bucketStart + 100;
                var count = sortedRatings.Count(r => r >= bucketStart && r < bucketEnd);
                var isUserBucket = userRating.HasValue && userRating >= bucketStart && userRating < bucketEnd;

                buckets.Add(new RatingBucketDto
                {
                    MinRating = bucketStart,
                    MaxRating = bucketEnd,
                    Label = $"{bucketStart}-{bucketEnd - 1}",
                    Count = count,
                    Percentage = Math.Round((double)count / totalPlayers * 100, 1),
                    IsUserBucket = isUserBucket
                });
            }

            return new RatingDistributionDto
            {
                Buckets = buckets,
                UserRating = userRating,
                UserPercentile = userPercentile,
                TotalPlayers = totalPlayers,
                AverageRating = Math.Round(averageRating, 1),
                MedianRating = medianRating
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting rating distribution");
            throw new HubException("Failed to get rating distribution");
        }
    }

    /// <summary>
    /// Get list of available AI bots.
    /// </summary>
    /// <returns>List of available bots.</returns>
    public Task<List<BotInfoDto>> GetAvailableBots()
    {
        var bots = new List<BotInfoDto>
        {
            new BotInfoDto
            {
                Id = "random",
                Name = "Random Bot",
                Description = "Makes completely random moves. Great for beginners learning the game.",
                Difficulty = 1,
                IsAvailable = true,
                Icon = "dice"
            },
            new BotInfoDto
            {
                Id = "greedy",
                Name = "Greedy Bot",
                Description = "Prioritizes hitting blots and bearing off. A solid intermediate challenge.",
                Difficulty = 3,
                IsAvailable = true,
                Icon = "target"
            },
            new BotInfoDto
            {
                Id = "gnubg_easy",
                Name = "Easy Bot (GNUBG)",
                Description = "Uses GNU Backgammon with 0-ply evaluation. Quick and approachable.",
                Difficulty = 2,
                IsAvailable = true,
                Icon = "brain"
            },
            new BotInfoDto
            {
                Id = "gnubg_medium",
                Name = "Medium Bot (GNUBG)",
                Description = "Uses GNU Backgammon with 1-ply evaluation. A balanced challenge.",
                Difficulty = 3,
                IsAvailable = true,
                Icon = "brain"
            },
            new BotInfoDto
            {
                Id = "gnubg_hard",
                Name = "Hard Bot (GNUBG)",
                Description = "Uses GNU Backgammon with 2-ply evaluation. Plays strong positional backgammon.",
                Difficulty = 4,
                IsAvailable = true,
                Icon = "brain"
            },
            new BotInfoDto
            {
                Id = "gnubg_expert",
                Name = "Expert Bot (GNUBG)",
                Description = "Uses GNU Backgammon with 3-ply evaluation. Near world-class play.",
                Difficulty = 5,
                IsAvailable = true,
                Icon = "brain"
            }
        };

        _logger.LogDebug("Retrieved {Count} available bots", bots.Count);
        return Task.FromResult(bots);
    }

    /// <summary>
    /// Generate a consistent anonymous display name from a player ID.
    /// </summary>
    /// <param name="playerId">The player ID to generate a name from.</param>
}
