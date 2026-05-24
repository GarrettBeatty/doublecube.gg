using Backgammon.Core;
using Backgammon.Server.Grains;
using Backgammon.Server.Grains.Interfaces;
using Backgammon.Server.Hubs.Interfaces;
using Backgammon.Server.Models;
using Backgammon.Server.Models.SignalR;
using Backgammon.Server.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Backgammon.Server.Hubs;

/// <summary>
/// GameHub partial class - Match Operations
/// Handles match creation, joining, status, and management
/// </summary>
public partial class GameHub
{
    /// <summary>
    /// Continue to the next game in a match after the current game is complete.
    /// Race coordination across concurrent calls lives inside <see cref="IMatchGrain.EnsureNextGameAsync"/>;
    /// the grain's single-threaded execution makes an external lock unnecessary.
    /// </summary>
    public async Task ContinueMatch(string matchId)
    {
        try
        {
            _logger.LogInformation("ContinueMatch: MatchId={MatchId}, ConnectionId={ConnectionId}", matchId, Context.ConnectionId);

            var playerId = GetAuthenticatedUserId()!;
            var matchGrain = _grainFactory.GetGrain<IMatchGrain>(matchId);
            var result = await matchGrain.EnsureNextGameAsync(playerId);

            if (!string.IsNullOrEmpty(result.Error) || string.IsNullOrEmpty(result.GameId))
            {
                await Clients.Caller.Error(result.Error ?? "Cannot continue to next game");
                return;
            }

            await JoinGame(result.GameId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error continuing match {MatchId}", matchId);
            await Clients.Caller.Error(ex.Message);
        }
    }

    /// <summary>
    /// Get match status
    /// </summary>
    public async Task GetMatchStatus(string matchId)
    {
        try
        {
            var playerId = GetAuthenticatedUserId()!;
            var match = await _matchService.GetMatchAsync(matchId);
            if (match == null)
            {
                await Clients.Caller.Error("Match not found");
                return;
            }

            if (match.Player1Id != playerId && match.Player2Id != playerId)
            {
                await Clients.Caller.Error("Access denied");
                return;
            }

            await Clients.Caller.MatchStatus(new MatchStatusDto
            {
                MatchId = match.MatchId,
                TargetScore = match.TargetScore,
                Player1Name = match.Player1Name ?? string.Empty,
                Player2Name = match.Player2Name ?? string.Empty,
                Player1Score = match.Player1Score,
                Player2Score = match.Player2Score,
                IsCrawfordGame = match.IsCrawfordGame,
                HasCrawfordGameBeenPlayed = match.HasCrawfordGameBeenPlayed,
                Status = match.Status,
                WinnerId = match.WinnerId,
                TotalGames = match.GameIds.Count,
                CurrentGameId = match.CurrentGameId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting match status");
            await Clients.Caller.Error(ex.Message);
        }
    }

    /// <summary>
    /// Get authoritative match state from server.
    /// Used to sync client state on reconnection and detect stale data.
    /// </summary>
    public async Task<MatchStateDto?> GetMatchState(string matchId)
    {
        try
        {
            var playerId = GetAuthenticatedUserId()!;
            var match = await _matchService.GetMatchAsync(matchId);

            if (match == null)
            {
                await Clients.Caller.Error("Match not found");
                return null;
            }

            if (match.Player1Id != playerId && match.Player2Id != playerId)
            {
                await Clients.Caller.Error("Access denied");
                return null;
            }

            return MatchStateDto.FromMatch(match);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting match state for {MatchId}", matchId);
            await Clients.Caller.Error("Failed to get match state");
            return null;
        }
    }

    /// <summary>
    /// Get complete match results including all games
    /// </summary>
    public async Task<MatchResultsDto?> GetMatchResults(string matchId)
    {
        try
        {
            var playerId = GetAuthenticatedUserId()!;
            var match = await _matchService.GetMatchAsync(matchId);

            if (match == null)
            {
                await Clients.Caller.Error("Match not found");
                return null;
            }

            if (match.Player1Id != playerId && match.Player2Id != playerId)
            {
                await Clients.Caller.Error("Access denied");
                return null;
            }

            var games = await GetMatchGames(matchId);
            var winnerId = match.CoreMatch.GetWinnerId();

            return new MatchResultsDto
            {
                MatchId = match.MatchId,
                WinnerUserId = match.WinnerId,
                WinnerUsername = winnerId == match.Player1Id ? match.Player1Name : match.Player2Name,
                LoserUsername = winnerId == match.Player1Id ? match.Player2Name : match.Player1Name,
                FinalScore = new MatchScoreDto
                {
                    Player1 = match.Player1Score,
                    Player2 = match.Player2Score
                },
                TargetScore = match.TargetScore,
                Games = games,
                TotalGames = games.Count,
                Duration = match.CompletedAt.HasValue
                    ? (match.CompletedAt.Value - match.CreatedAt).ToString()
                    : "N/A",
                CompletedAt = match.CompletedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting match results for {MatchId}", matchId);
            await Clients.Caller.Error("Failed to get match results");
            return null;
        }
    }

    /// <summary>
    /// Get player's matches (optionally filtered by status)
    /// </summary>
    public async Task GetMyMatches(string? status = null)
    {
        try
        {
            var playerId = GetAuthenticatedUserId()!;
            var matches = await _matchService.GetPlayerMatchesAsync(playerId, status);

            var matchList = matches.Select(m => new MatchSummaryDto
            {
                MatchId = m.MatchId,
                TargetScore = m.TargetScore,
                OpponentId = m.Player1Id == playerId ? m.Player2Id : m.Player1Id,
                OpponentName = m.Player1Id == playerId ? m.Player2Name : m.Player1Name,
                MyScore = m.Player1Id == playerId ? m.Player1Score : m.Player2Score,
                OpponentScore = m.Player1Id == playerId ? m.Player2Score : m.Player1Score,
                Status = m.Status,
                CreatedAt = m.CreatedAt,
                TotalGames = m.GameIds.Count
            }).ToList();

            await Clients.Caller.MyMatches(matchList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting player matches");
            await Clients.Caller.Error(ex.Message);
        }
    }

    /// <summary>
    /// Get match lobbies, optionally filtered by type
    /// </summary>
    public async Task<List<object>> GetMatchLobbies(string? lobbyType = null)
    {
        try
        {
            bool? isCorrespondence = lobbyType?.ToLower() switch
            {
                "correspondence" => true,
                "regular" => false,
                _ => null
            };

            var lobbies = await _matchService.GetOpenLobbiesAsync(isCorrespondence: isCorrespondence);

            return lobbies.Select(m => new
            {
                matchId = m.MatchId,
                creatorPlayerId = m.Player1Id,
                creatorUsername = m.Player1Name,
                opponentType = m.OpponentType,
                targetScore = m.TargetScore,
                status = m.Status,
                opponentPlayerId = m.Player2Id,
                opponentUsername = m.Player2Name,
                createdAt = m.CreatedAt.ToString("O"),
                isOpenLobby = m.IsOpenLobby,
                isCorrespondence = m.IsCorrespondence,
                timePerMoveDays = m.TimePerMoveDays
            }).ToList<object>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting match lobbies");
            throw;
        }
    }

    /// <summary>
    /// Get recent completed matches for the current player.
    /// </summary>
    public async Task<List<object>> GetRecentGames(int limit = 10)
    {
        try
        {
            var playerId = GetAuthenticatedUserId()!;
            var matches = await _matchService.GetPlayerMatchesAsync(playerId, "Completed");

            return matches.Take(limit).Select(m =>
            {
                var isPlayer1 = m.Player1Id == playerId;
                var myScore = isPlayer1 ? m.Player1Score : m.Player2Score;
                var opponentScore = isPlayer1 ? m.Player2Score : m.Player1Score;
                var didWin = myScore > opponentScore;

                return new
                {
                    matchId = m.MatchId,
                    opponentId = isPlayer1 ? m.Player2Id : m.Player1Id,
                    opponentName = (isPlayer1 ? m.Player2Name : m.Player1Name) ?? "Unknown",
                    opponentRating = 0,
                    result = didWin ? "win" : "loss",
                    myScore,
                    opponentScore,
                    matchScore = $"{myScore}-{opponentScore}",
                    targetScore = m.TargetScore,
                    matchLength = $"{m.TargetScore}-point",
                    timeControl = "Standard",
                    ratingChange = 0,
                    completedAt = m.CompletedAt,
                    createdAt = m.CreatedAt
                } as object;
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent games for player");
            throw;
        }
    }

    /// <summary>
    /// Get active in-progress games for the current player (DB-based).
    /// </summary>
    public async Task<List<object>> GetActiveGames(int limit = 10)
    {
        try
        {
            var playerId = GetAuthenticatedUserId()!;
            var games = await _gameRepository.GetPlayerGamesAsync(playerId, "InProgress", limit);

            var activeGames = new List<object>();

            foreach (var game in games)
            {
                var isWhite = game.WhitePlayerId == playerId;
                var myColor = isWhite ? "White" : "Red";
                var currentPlayer = game.CurrentPlayer;
                var isYourTurn = currentPlayer == myColor;

                var cubeValue = game.DoublingCubeValue;
                var cubeOwner = game.DoublingCubeOwner ?? "Center";

                string matchScore = "0-0";
                int matchLength = 1;
                var isCrawford = game.IsCrawfordGame;

                if (!string.IsNullOrEmpty(game.MatchId))
                {
                    var match = await _matchService.GetMatchAsync(game.MatchId);
                    if (match != null)
                    {
                        matchScore = $"{match.Player1Score}-{match.Player2Score}";
                        matchLength = match.TargetScore;
                        isCrawford = match.IsCrawfordGame;
                    }
                }

                activeGames.Add(new
                {
                    matchId = game.MatchId ?? string.Empty,
                    gameId = game.GameId,
                    player1Name = game.WhitePlayerName ?? "Player 1",
                    player2Name = game.RedPlayerName ?? "Player 2",
                    player1Rating = 0,
                    player2Rating = 0,
                    currentPlayer,
                    myColor,
                    isYourTurn,
                    matchScore,
                    matchLength,
                    timeControl = "Standard",
                    cubeValue,
                    cubeOwner,
                    isCrawford,
                    viewers = 0,
                    board = (object[]?)null,
                    whiteCheckersOnBar = 0,
                    redCheckersOnBar = 0,
                    whiteBornOff = 0,
                    redBornOff = 0,
                    dice = (int[]?)null
                });
            }

            return activeGames;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active games for player");
            throw;
        }
    }

    /// <summary>
    /// Get player's active matches (in-progress).
    /// </summary>
    public async Task<List<ActiveMatchDto>> GetActiveMatches(int limit = 10)
    {
        try
        {
            var playerId = GetAuthenticatedUserId()!;
            var matches = await _matchService.GetPlayerMatchesAsync(playerId, "InProgress");

            return matches.Take(limit).Select(m =>
            {
                var isPlayer1 = m.Player1Id == playerId;
                return new ActiveMatchDto
                {
                    MatchId = m.MatchId,
                    OpponentName = (isPlayer1 ? m.Player2Name : m.Player1Name) ?? "Waiting...",
                    MyScore = isPlayer1 ? m.Player1Score : m.Player2Score,
                    OpponentScore = isPlayer1 ? m.Player2Score : m.Player1Score,
                    TargetScore = m.TargetScore,
                    CurrentGameId = m.CurrentGameId,
                    GamesPlayed = m.TotalGamesPlayed,
                    IsCrawford = m.IsCrawfordGame,
                    IsCorrespondence = m.IsCorrespondence,
                    CreatedAt = m.CreatedAt
                };
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active matches for player");
            throw;
        }
    }

    /// <summary>
    /// Get all games for a specific match
    /// </summary>
    public async Task<List<MatchGameDto>> GetMatchGames(string matchId)
    {
        try
        {
            var match = await _matchService.GetMatchAsync(matchId);
            if (match == null) return new List<MatchGameDto>();

            var games = new List<MatchGameDto>();
            var gameNumber = 1;

            foreach (var gameId in match.GameIds)
            {
                var game = await _gameRepository.GetGameByGameIdAsync(gameId);
                if (game != null)
                {
                    games.Add(new MatchGameDto
                    {
                        GameId = game.GameId,
                        GameNumber = gameNumber,
                        Status = game.Status,
                        Winner = game.Winner,
                        WinType = game.WinType,
                        PointsScored = game.Stakes,
                        IsCrawford = game.IsCrawfordGame,
                        CompletedAt = game.CompletedAt
                    });
                    gameNumber++;
                }
            }

            return games;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting games for match {MatchId}", matchId);
            throw;
        }
    }

    /// <summary>
    /// Get recent opponents for the current player with head-to-head records.
    /// </summary>
    public async Task<List<RecentOpponentDto>> GetRecentOpponents(int limit = 10, bool includeAi = false)
    {
        try
        {
            var playerId = GetAuthenticatedUserId()!;
            var matches = await _matchService.GetPlayerMatchesAsync(playerId, "Completed");

            var opponentStats = new Dictionary<string, (string Name, int Wins, int Losses, DateTime LastPlayed, bool IsAi)>();

            foreach (var match in matches)
            {
                var isPlayer1 = match.Player1Id == playerId;
                var opponentId = isPlayer1 ? match.Player2Id : match.Player1Id;
                var opponentName = isPlayer1 ? match.Player2Name : match.Player1Name;
                var myScore = isPlayer1 ? match.Player1Score : match.Player2Score;
                var opponentScore = isPlayer1 ? match.Player2Score : match.Player1Score;
                var isAi = match.OpponentType == "AI";

                if (string.IsNullOrEmpty(opponentId)) continue;
                if (isAi && !includeAi) continue;

                var matchTime = match.CompletedAt ?? match.CreatedAt;
                var didWin = myScore > opponentScore;

                if (opponentStats.TryGetValue(opponentId, out var existing))
                {
                    opponentStats[opponentId] = (
                        existing.Name,
                        existing.Wins + (didWin ? 1 : 0),
                        existing.Losses + (didWin ? 0 : 1),
                        matchTime > existing.LastPlayed ? matchTime : existing.LastPlayed,
                        isAi);
                }
                else
                {
                    opponentStats[opponentId] = (opponentName ?? "Unknown", didWin ? 1 : 0, didWin ? 0 : 1, matchTime, isAi);
                }
            }

            var recentOpponents = opponentStats
                .OrderByDescending(kvp => kvp.Value.LastPlayed)
                .Take(limit)
                .ToList();

            var nonAiIds = recentOpponents.Where(kvp => !kvp.Value.IsAi).Select(kvp => kvp.Key).ToList();
            var opponentUsers = nonAiIds.Count > 0
                ? await _userRepository.GetUsersByIdsAsync(nonAiIds)
                : new List<User>();
            var userRatings = opponentUsers.ToDictionary(u => u.UserId, u => u?.Rating ?? 1500);

            return recentOpponents.Select(kvp => new RecentOpponentDto
            {
                OpponentId = kvp.Key,
                OpponentName = kvp.Value.Name,
                OpponentRating = kvp.Value.IsAi ? 0 : (userRatings.TryGetValue(kvp.Key, out var rating) ? rating : 0),
                TotalMatches = kvp.Value.Wins + kvp.Value.Losses,
                Wins = kvp.Value.Wins,
                Losses = kvp.Value.Losses,
                LastPlayedAt = kvp.Value.LastPlayed,
                IsAi = kvp.Value.IsAi
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent opponents for player");
            throw;
        }
    }

    /// <summary>
    /// Create a new match and immediately create the first game
    /// </summary>
    public async Task CreateMatch(MatchConfig config)
    {
        try
        {
            var playerId = GetAuthenticatedUserId()!;

            Core.TimeControlConfig timeControl;
            if (!string.IsNullOrEmpty(config.TimeControlType)
                && config.TimeControlType != "None"
                && Enum.TryParse<Core.TimeControlType>(config.TimeControlType, out var timeControlType))
            {
                timeControl = new Core.TimeControlConfig
                {
                    Type = timeControlType,
                    DelaySeconds = timeControlType == Core.TimeControlType.ChicagoPoint ? 12 : 0
                };
            }
            else
            {
                timeControl = new Core.TimeControlConfig { Type = Core.TimeControlType.None };
            }

            var matchId = Guid.NewGuid().ToString();
            var matchGrain = _grainFactory.GetGrain<IMatchGrain>(matchId);
            var result = await matchGrain.CreateMatchAsync(new MatchCreationRequest
            {
                Player1Id = playerId,
                TargetScore = config.TargetScore,
                OpponentType = config.OpponentType,
                Player1DisplayName = config.DisplayName,
                Player2Id = config.OpponentId,
                TimeControl = timeControl,
                IsRated = config.IsRated,
                AiType = config.AiType,
            });

            await Clients.Caller.MatchCreated(new MatchCreatedDto
            {
                MatchId = result.MatchId,
                GameId = result.FirstGameId,
                TargetScore = result.TargetScore,
                OpponentType = result.OpponentType,
                Player1Id = result.Player1Id,
                Player2Id = result.Player2Id,
                Player1Name = result.Player1Name,
                Player2Name = result.Player2Name,
            });

            _logger.LogInformation(
                "Match {MatchId} created for player {PlayerId} (type: {OpponentType}), first game: {GameId}",
                result.MatchId,
                playerId,
                config.OpponentType,
                result.FirstGameId);

            if (config.OpponentType == "OpenLobby")
            {
                await Clients.All.LobbyCreated(new LobbyCreatedDto
                {
                    MatchId = result.MatchId,
                    GameId = result.FirstGameId,
                    CreatorName = result.Player1Name,
                    TargetScore = result.TargetScore,
                    IsRated = result.IsRated,
                });
            }

            if (config.OpponentType == "Friend" && !string.IsNullOrEmpty(config.OpponentId))
            {
                var opponentConnection = await GetPlayerConnectionAsync(config.OpponentId);
                if (!string.IsNullOrEmpty(opponentConnection))
                {
                    await Clients.Client(opponentConnection).MatchInvite(new MatchInviteDto
                    {
                        MatchId = result.MatchId,
                        GameId = result.FirstGameId,
                        TargetScore = result.TargetScore,
                        ChallengerName = result.Player1Name,
                        ChallengerId = result.Player1Id,
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating match");
            await Clients.Caller.Error(ex.Message);
        }
    }

    /// <summary>
    /// Legacy method for backwards compatibility — redirects to CreateMatch.
    /// </summary>
    public async Task CreateMatchWithConfig(MatchConfig config)
    {
        await CreateMatch(config);
    }

    /// <summary>
    /// Join an existing match as player 2
    /// </summary>
    public async Task JoinMatch(string matchId)
    {
        try
        {
            var playerId = GetAuthenticatedUserId()!;
            var displayName = GetAuthenticatedDisplayName();

            // Presence is registered in OnConnectedAsync; no extra tracking needed here.
            var matchGrain = _grainFactory.GetGrain<IMatchGrain>(matchId);
            var result = await matchGrain.JoinAsync(playerId, displayName);

            await Clients.Caller.MatchCreated(new MatchCreatedDto
            {
                MatchId = result.MatchId,
                GameId = result.CurrentGameId ?? string.Empty,
                TargetScore = result.TargetScore,
                OpponentType = result.OpponentType,
                Player1Id = result.Player1Id,
                Player2Id = result.Player2Id,
                Player1Name = result.Player1Name,
                Player2Name = result.Player2Name,
            });

            // Notify creator that opponent joined
            var creatorConnection = await GetPlayerConnectionAsync(result.Player1Id);
            if (!string.IsNullOrEmpty(creatorConnection))
            {
                await Clients.Client(creatorConnection).OpponentJoinedMatch(new OpponentJoinedMatchDto
                {
                    MatchId = result.MatchId,
                    Player2Id = result.Player2Id ?? string.Empty,
                    Player2Name = result.Player2Name,
                });
            }

            // For correspondence matches, notify Player1 it's their turn
            if (result.IsCorrespondence)
            {
                var player1Connection = await GetPlayerConnectionAsync(result.Player1Id);
                if (!string.IsNullOrEmpty(player1Connection))
                {
                    await Clients.Client(player1Connection).CorrespondenceTurnNotification(
                        new CorrespondenceTurnNotificationDto
                        {
                            MatchId = result.MatchId,
                            GameId = result.CurrentGameId,
                            Message = "Opponent joined! It's your turn.",
                        });
                }
            }

            _logger.LogInformation("Player {PlayerId} joined match {MatchId}", playerId, matchId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining match {MatchId}", matchId);
            await Clients.Caller.Error(ex.Message);
        }
    }
}
