using Backgammon.Core;
using Backgammon.Server.Extensions;
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
    public async Task ContinueMatch(string matchId)
    {
        try
        {
            var match = await _matchService.GetMatchAsync(matchId);
            if (match == null)
            {
                await Clients.Caller.Error("Match not found");
                return;
            }

            if (match.Status != "InProgress")
            {
                await Clients.Caller.Error("Match is not in progress");
                return;
            }

            var playerId = GetAuthenticatedUserId()!; // ! is safe - AuthenticationHubFilter ensures non-null

            if (playerId != match.Player1Id && playerId != match.Player2Id)
            {
                await Clients.Caller.Error("You are not a player in this match");
                return;
            }

            // Start next game in the match
            var nextGame = await _matchService.StartNextGameAsync(matchId);

            // Create game session
            var session = _sessionManager.GetSession(nextGame.GameId);
            if (session != null)
            {
                session.MatchId = match.MatchId;

                // For AI matches, add AI player before human joins
                if (match.OpponentType == "AI")
                {
                    var aiPlayerId = _aiMoveService.GenerateAiPlayerId();
                    session.AddPlayer(aiPlayerId, string.Empty); // Empty connection ID for AI
                    session.SetPlayerName(aiPlayerId, "Computer");

                    _logger.LogInformation(
                        "Added AI player {AiPlayerId} to next match game {GameId}",
                        aiPlayerId,
                        nextGame.GameId);
                }
            }

            // Send match status update
            await Clients.Caller.MatchContinued(new MatchContinuedDto
            {
                MatchId = match.MatchId,
                GameId = nextGame.GameId,
                Player1Score = match.Player1Score,
                Player2Score = match.Player2Score,
                TargetScore = match.TargetScore,
                IsCrawfordGame = match.IsCrawfordGame
            });

            // Join the new game
            await JoinGame(nextGame.GameId);

            _logger.LogInformation(
                "Continued match {MatchId} with game {GameId}",
                matchId,
                nextGame.GameId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error continuing match");
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
            var playerId = GetAuthenticatedUserId()!; // ! is safe - AuthenticationHubFilter ensures non-null
            var match = await _matchService.GetMatchAsync(matchId);
            if (match == null)
            {
                await Clients.Caller.Error("Match not found");
                return;
            }

            // Authorization: only participants can view match status
            if (match.Player1Id != playerId && match.Player2Id != playerId)
            {
                await Clients.Caller.Error("Access denied");
                _logger.LogWarning(
                    "Player {PlayerId} attempted to access match {MatchId} without authorization",
                    playerId,
                    matchId);
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
    /// Returns match scores with a timestamp for staleness detection.
    /// </summary>
    /// <param name="matchId">The match ID to fetch state for</param>
    /// <returns>MatchStateDto with current scores and timestamp</returns>
    public async Task<MatchStateDto?> GetMatchState(string matchId)
    {
        try
        {
            var playerId = GetAuthenticatedUserId()!; // ! is safe - AuthenticationHubFilter ensures non-null
            var match = await _matchService.GetMatchAsync(matchId);

            if (match == null)
            {
                _logger.LogWarning("GetMatchState: Match {MatchId} not found", matchId);
                await Clients.Caller.Error("Match not found");
                return null;
            }

            // Authorization: only participants can view match state
            if (match.Player1Id != playerId && match.Player2Id != playerId)
            {
                _logger.LogWarning(
                    "GetMatchState: Player {PlayerId} attempted to access match {MatchId} without authorization",
                    playerId,
                    matchId);
                await Clients.Caller.Error("Access denied");
                return null;
            }

            var matchState = MatchStateDto.FromMatch(match);

            _logger.LogDebug(
                "GetMatchState: Returning state for match {MatchId} - P1: {P1Score}, P2: {P2Score}, Updated: {UpdatedAt}",
                matchId,
                matchState.Player1Score,
                matchState.Player2Score,
                matchState.LastUpdatedAt);

            return matchState;
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
            var playerId = GetAuthenticatedUserId()!; // ! is safe - AuthenticationHubFilter ensures non-null
            var match = await _matchService.GetMatchAsync(matchId);

            if (match == null)
            {
                _logger.LogWarning("GetMatchResults: Match {MatchId} not found", matchId);
                await Clients.Caller.Error("Match not found");
                return null;
            }

            // Authorization: only participants can view match results
            if (match.Player1Id != playerId && match.Player2Id != playerId)
            {
                _logger.LogWarning(
                    "GetMatchResults: Player {PlayerId} attempted to access match {MatchId} without authorization",
                    playerId,
                    matchId);
                await Clients.Caller.Error("Access denied");
                return null;
            }

            // Build game history from match
            var games = match.CoreMatch.Games.Select((g, index) => new MatchGameDto
            {
                GameId = g.GameId,
                GameNumber = index + 1,
                Winner = g.Winner,
                Points = g.Stakes,
                IsGamemon = g.WinType == WinType.Gammon,
                IsBackgammon = g.WinType == WinType.Backgammon,
                IsCrawfordGame = g.IsCrawfordGame,
                CompletedAt = null // Core.Game doesn't track this, could add if needed
            }).ToList();

            var winnerId = match.CoreMatch.GetWinnerId();

            var results = new MatchResultsDto
            {
                MatchId = match.MatchId,
                WinnerUserId = match.WinnerId,
                WinnerUsername = winnerId == match.Player1Id
                    ? match.Player1Name
                    : match.Player2Name,
                LoserUsername = winnerId == match.Player1Id
                    ? match.Player2Name
                    : match.Player1Name,
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

            _logger.LogInformation(
                "GetMatchResults: Returning results for match {MatchId} - Winner: {Winner}, Games: {TotalGames}",
                matchId,
                results.WinnerUsername,
                results.TotalGames);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting match results for {MatchId}", matchId);
            await Clients.Caller.Error("Failed to get match results");
            return null;
        }
    }

    /// <summary>
    /// Get player's active matches
    /// </summary>
    public async Task GetMyMatches(string? status = null)
    {
        try
        {
            var playerId = GetAuthenticatedUserId()!; // ! is safe - AuthenticationHubFilter ensures non-null
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
    /// <param name="lobbyType">Filter by type: "regular", "correspondence", or null for all</param>
    public async Task<List<object>> GetMatchLobbies(string? lobbyType = null)
    {
        try
        {
            // Parse lobby type filter
            bool? isCorrespondence = lobbyType?.ToLower() switch
            {
                "correspondence" => true,
                "regular" => false,
                _ => null
            };

            var lobbies = await _matchService.GetOpenLobbiesAsync(isCorrespondence: isCorrespondence);

            var lobbyList = lobbies.Select(m => new
            {
                matchId = m.MatchId,
                creatorPlayerId = m.Player1Id,
                creatorUsername = m.Player1Name,
                opponentType = m.OpponentType,
                targetScore = m.TargetScore,
                status = m.Status,  // Will be "WaitingForPlayers"
                opponentPlayerId = m.Player2Id,
                opponentUsername = m.Player2Name,
                createdAt = m.CreatedAt.ToString("O"),
                isOpenLobby = m.IsOpenLobby,
                isCorrespondence = m.IsCorrespondence,
                timePerMoveDays = m.TimePerMoveDays
            }).ToList<object>();

            _logger.LogDebug(
                "Retrieved {Count} match lobbies (filter: {LobbyType})",
                lobbyList.Count,
                lobbyType ?? "all");

            return lobbyList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting match lobbies");
            throw;
        }
    }

    public async Task<List<object>> GetRecentGames(int limit = 10)
    {
        try
        {
            var playerId = GetAuthenticatedUserId()!; // ! is safe - AuthenticationHubFilter ensures non-null
            var matches = await _matchService.GetPlayerMatchesAsync(playerId, "Completed");

            var recentGames = matches.Take(limit).Select(m =>
            {
                var isPlayer1 = m.Player1Id == playerId;
                var opponentId = isPlayer1 ? m.Player2Id : m.Player1Id;
                var opponentName = isPlayer1 ? m.Player2Name : m.Player1Name;
                var myScore = isPlayer1 ? m.Player1Score : m.Player2Score;
                var opponentScore = isPlayer1 ? m.Player2Score : m.Player1Score;
                var didWin = myScore > opponentScore;

                return new
                {
                    matchId = m.MatchId,
                    opponentId = opponentId,
                    opponentName = opponentName ?? "Unknown",
                    opponentRating = 0, // TODO: Fetch from user profile when available
                    result = didWin ? "win" : "loss",
                    myScore = myScore,
                    opponentScore = opponentScore,
                    matchScore = $"{myScore}-{opponentScore}",
                    targetScore = m.TargetScore,
                    matchLength = $"{m.TargetScore}-point",
                    timeControl = "Standard", // TODO: Add time control to Match model
                    ratingChange = 0, // TODO: Calculate rating change when rating system is implemented
                    completedAt = m.CompletedAt,
                    createdAt = m.CreatedAt
                };
            }).ToList<object>();

            return recentGames;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent games for player");
            throw;
        }
    }

    public async Task<List<object>> GetActiveGames(int limit = 10)
    {
        try
        {
            var playerId = GetAuthenticatedUserId()!; // ! is safe - AuthenticationHubFilter ensures non-null
            var matches = await _matchService.GetPlayerMatchesAsync(playerId, "InProgress");

            var activeGames = matches.Take(limit).Select(m =>
            {
                var isPlayer1 = m.Player1Id == playerId;
                var opponentId = isPlayer1 ? m.Player2Id : m.Player1Id;
                var opponentName = isPlayer1 ? m.Player2Name : m.Player1Name;
                var myColor = isPlayer1 ? "White" : "Red";

                // Try to get current game state if there's an active game session
                var currentGameId = m.CurrentGameId;
                var gameSession = currentGameId != null ? _sessionManager.GetSession(currentGameId) : null;

                var currentPlayer = gameSession?.Engine?.CurrentPlayer.ToString() ?? "White";
                var isYourTurn = currentPlayer == myColor;

                // Get board state if session exists
                object[]? boardState = null;
                int whiteOnBar = 0;
                int redOnBar = 0;
                int whiteBornOff = 0;
                int redBornOff = 0;

                int[]? diceValues = null;
                int cubeValue = 1;
                string cubeOwner = "Center";

                if (gameSession?.Engine != null)
                {
                    var board = new List<object>();
                    for (int i = 1; i <= 24; i++)
                    {
                        var point = gameSession.Engine.Board.GetPoint(i);
                        board.Add(new
                        {
                            position = i,
                            color = point.Color?.ToString(),
                            count = point.Count
                        });
                    }

                    boardState = board.ToArray();
                    whiteOnBar = gameSession.Engine.WhitePlayer.CheckersOnBar;
                    redOnBar = gameSession.Engine.RedPlayer.CheckersOnBar;
                    whiteBornOff = gameSession.Engine.WhitePlayer.CheckersBornOff;
                    redBornOff = gameSession.Engine.RedPlayer.CheckersBornOff;

                    // Get dice values if rolled
                    if (gameSession.Engine.Dice?.Die1 > 0 && gameSession.Engine.Dice?.Die2 > 0)
                    {
                        diceValues = new[] { gameSession.Engine.Dice.Die1, gameSession.Engine.Dice.Die2 };
                    }

                    // Get cube info
                    cubeValue = gameSession.Engine.DoublingCube?.Value ?? 1;
                    cubeOwner = gameSession.Engine.DoublingCube?.Owner?.ToString() ?? "Center";
                }

                return new
                {
                    matchId = m.MatchId,
                    gameId = currentGameId,
                    player1Name = m.Player1Name ?? "Player 1",
                    player2Name = m.Player2Name ?? "Player 2",
                    player1Rating = 0, // TODO: Fetch from user profile
                    player2Rating = 0, // TODO: Fetch from user profile
                    currentPlayer,
                    myColor,
                    isYourTurn,
                    matchScore = $"{m.Player1Score}-{m.Player2Score}",
                    matchLength = m.TargetScore,
                    timeControl = "Standard", // TODO: Add time control to Match model
                    cubeValue,
                    cubeOwner,
                    isCrawford = m.IsCrawfordGame,
                    viewers = 0, // TODO: Add spectator tracking
                    board = boardState,
                    whiteCheckersOnBar = whiteOnBar,
                    redCheckersOnBar = redOnBar,
                    whiteBornOff,
                    redBornOff,
                    dice = diceValues
                };
            }).ToList<object>();

            return activeGames;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active games for player");
            throw;
        }
    }

    /// <summary>
    /// Get recent opponents for the current player with head-to-head records
    /// </summary>
    /// <param name="limit">Maximum number of opponents to return</param>
    /// <param name="includeAi">Whether to include AI opponents</param>
    /// <returns>List of recent opponents with their statistics</returns>
    public async Task<List<RecentOpponentDto>> GetRecentOpponents(int limit = 10, bool includeAi = false)
    {
        try
        {
            var playerId = GetAuthenticatedUserId()!; // ! is safe - AuthenticationHubFilter ensures non-null

            // Get all completed matches for the player
            var matches = await _matchService.GetPlayerMatchesAsync(playerId, "Completed");

            // Group matches by opponent and aggregate statistics
            var opponentStats = new Dictionary<string, (string Name, int Wins, int Losses, DateTime LastPlayed, bool IsAi)>();

            foreach (var match in matches)
            {
                var isPlayer1 = match.Player1Id == playerId;
                var opponentId = isPlayer1 ? match.Player2Id : match.Player1Id;
                var opponentName = isPlayer1 ? match.Player2Name : match.Player1Name;
                var myScore = isPlayer1 ? match.Player1Score : match.Player2Score;
                var opponentScore = isPlayer1 ? match.Player2Score : match.Player1Score;
                var isAi = match.OpponentType == "AI";

                // Skip if opponent ID is missing
                if (string.IsNullOrEmpty(opponentId))
                {
                    continue;
                }

                // Skip AI opponents if not requested
                if (isAi && !includeAi)
                {
                    continue;
                }

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
                    opponentStats[opponentId] = (
                        opponentName ?? "Unknown",
                        didWin ? 1 : 0,
                        didWin ? 0 : 1,
                        matchTime,
                        isAi);
                }
            }

            // Sort by most recent and take the limit
            var recentOpponents = opponentStats
                .OrderByDescending(kvp => kvp.Value.LastPlayed)
                .Take(limit)
                .ToList();

            // Get opponent ratings from user profiles (non-AI only)
            var nonAiOpponentIds = recentOpponents
                .Where(kvp => !kvp.Value.IsAi)
                .Select(kvp => kvp.Key)
                .ToList();

            var opponentUsers = nonAiOpponentIds.Count > 0
                ? await _userRepository.GetUsersByIdsAsync(nonAiOpponentIds)
                : new List<User>();

            var userRatings = opponentUsers.ToDictionary(
                u => u.UserId,
                u => u?.Rating ?? 1500);

            // Build the result DTOs
            var result = recentOpponents.Select(kvp => new RecentOpponentDto
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

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent opponents for player");
            throw;
        }
    }

    /// <summary>
    /// Create a new match with configuration (lobby-based)
    /// </summary>
    /// <summary>
    /// Create a new match and immediately create the first game
    /// </summary>
    public async Task CreateMatch(MatchConfig config)
    {
        try
        {
            var playerId = GetAuthenticatedUserId()!; // ! is safe - AuthenticationHubFilter ensures non-null

            // Parse time control type
            Core.TimeControlConfig? timeControl = null;
            if (!string.IsNullOrEmpty(config.TimeControlType) && config.TimeControlType != "None")
            {
                if (Enum.TryParse<Core.TimeControlType>(config.TimeControlType, out var timeControlType))
                {
                    timeControl = new Core.TimeControlConfig
                    {
                        Type = timeControlType,
                        DelaySeconds = timeControlType == Core.TimeControlType.ChicagoPoint ? 12 : 0
                    };
                }
            }

            // Create match and first game immediately
            var (match, firstGame) = await _matchService.CreateMatchAsync(
                playerId,
                config.TargetScore,
                config.OpponentType,
                config.DisplayName,
                config.OpponentId,
                timeControl,
                config.IsRated,
                config.AiType);

            // Send MatchCreated event with game ID
            await Clients.Caller.MatchCreated(new MatchCreatedDto
            {
                MatchId = match.MatchId,
                GameId = firstGame.GameId,
                TargetScore = match.TargetScore,
                OpponentType = match.OpponentType ?? string.Empty,
                Player1Id = match.Player1Id,
                Player2Id = match.Player2Id,
                Player1Name = match.Player1Name ?? string.Empty,
                Player2Name = match.Player2Name
            });

            _logger.LogInformation(
                "Match {MatchId} created for player {PlayerId} (type: {OpponentType}), first game: {GameId}",
                match.MatchId,
                playerId,
                config.OpponentType,
                firstGame.GameId);

            // For OpenLobby, broadcast to all clients that a new lobby is available
            if (config.OpponentType == "OpenLobby")
            {
                await Clients.All.LobbyCreated(new LobbyCreatedDto
                {
                    MatchId = match.MatchId,
                    GameId = firstGame.GameId,
                    CreatorName = match.Player1Name ?? string.Empty,
                    TargetScore = match.TargetScore,
                    IsRated = match.IsRated
                });

                _logger.LogInformation(
                    "Broadcast LobbyCreated event for match {MatchId} (isRated: {IsRated})",
                    match.MatchId,
                    match.IsRated);
            }

            // For friend matches, notify the friend if they're online
            if (config.OpponentType == "Friend" && !string.IsNullOrEmpty(config.OpponentId))
            {
                if (_sessionManager.IsPlayerOnline(config.OpponentId))
                {
                    var opponentConnection = GetPlayerConnection(config.OpponentId);
                    if (!string.IsNullOrEmpty(opponentConnection))
                    {
                        await Clients.Client(opponentConnection).MatchInvite(new MatchInviteDto
                        {
                            MatchId = match.MatchId,
                            GameId = firstGame.GameId,
                            TargetScore = match.TargetScore,
                            ChallengerName = match.Player1Name ?? string.Empty,
                            ChallengerId = match.Player1Id
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating match");
            await Clients.Caller.Error(ex.Message);
        }
    }

    // Legacy method for backwards compatibility - redirects to CreateMatch
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
            var playerId = GetAuthenticatedUserId()!; // ! is safe - AuthenticationHubFilter ensures non-null
            var displayName = GetAuthenticatedDisplayName();

            // Track this player's connection
            _playerConnectionService.AddConnection(playerId, Context.ConnectionId);

            // Join the match
            var match = await _matchService.JoinMatchAsync(matchId, playerId, displayName);

            // Send MatchCreated to joiner (navigates to game page)
            await Clients.Caller.MatchCreated(new MatchCreatedDto
            {
                MatchId = match.MatchId,
                GameId = match.CurrentGameId ?? string.Empty,
                TargetScore = match.TargetScore,
                OpponentType = match.OpponentType ?? string.Empty,
                Player1Id = match.Player1Id,
                Player2Id = match.Player2Id,
                Player1Name = match.Player1Name ?? string.Empty,
                Player2Name = match.Player2Name
            });

            // Notify creator that opponent joined
            var creatorConnection = GetPlayerConnection(match.Player1Id);
            if (!string.IsNullOrEmpty(creatorConnection))
            {
                await Clients.Client(creatorConnection).OpponentJoinedMatch(new OpponentJoinedMatchDto
                {
                    MatchId = match.MatchId,
                    Player2Id = match.Player2Id ?? string.Empty,
                    Player2Name = match.Player2Name ?? string.Empty
                });
            }

            // For correspondence matches, notify Player1 it's their turn
            if (match.IsCorrespondence && _sessionManager.IsPlayerOnline(match.Player1Id))
            {
                var player1Connection = GetPlayerConnection(match.Player1Id);
                if (!string.IsNullOrEmpty(player1Connection))
                {
                    await Clients.Client(player1Connection).CorrespondenceTurnNotification(
                        new CorrespondenceTurnNotificationDto
                        {
                            MatchId = match.MatchId,
                            GameId = match.CurrentGameId,
                            Message = "Opponent joined! It's your turn."
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

    /// <summary>
    /// Analyze the current position and return evaluation
    /// </summary>
    /// <param name="gameId">The game ID to analyze</param>
}
