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
            _logger.LogInformation(
                "========== ContinueMatch Request ==========");
            _logger.LogInformation(
                "MatchId: {MatchId}, ConnectionId: {ConnectionId}",
                matchId,
                Context.ConnectionId);

            var match = await _matchService.GetMatchAsync(matchId);
            if (match == null)
            {
                _logger.LogWarning("ContinueMatch: Match {MatchId} not found", matchId);
                await Clients.Caller.Error("Match not found");
                return;
            }

            _logger.LogInformation(
                "ContinueMatch: Match {MatchId} - Status={Status}, Player1Id={P1}, Player2Id={P2}, CurrentGameId={GameId}, P1Score={P1Score}, P2Score={P2Score}",
                matchId,
                match.Status,
                match.Player1Id,
                match.Player2Id ?? "null",
                match.CurrentGameId ?? "null",
                match.Player1Score,
                match.Player2Score);

            // Check if match can continue to next game
            // Match must be InProgress AND have a completed game to continue from
            if (!match.CoreMatch.CanContinueToNextGame())
            {
                _logger.LogWarning(
                    "ContinueMatch: Cannot continue match {MatchId} - Status={Status}, MatchComplete={Complete}, CompletedGames={Games}",
                    matchId,
                    match.CoreMatch.Status,
                    match.CoreMatch.IsMatchComplete(),
                    match.CoreMatch.Games.Count(g => g.Status == Core.GameStatus.Completed));
                await Clients.Caller.Error("Cannot continue to next game");
                return;
            }

            var playerId = GetAuthenticatedUserId()!; // ! is safe - AuthenticationHubFilter ensures non-null
            var connectionId = Context.ConnectionId;

            _logger.LogInformation(
                "ContinueMatch: Player {PlayerId} from connection {ConnectionId}",
                playerId,
                connectionId);

            if (playerId != match.Player1Id && playerId != match.Player2Id)
            {
                _logger.LogWarning(
                    "ContinueMatch: Player {PlayerId} is not in match {MatchId} (P1={P1}, P2={P2})",
                    playerId,
                    matchId,
                    match.Player1Id,
                    match.Player2Id ?? "null");
                await Clients.Caller.Error("You are not a player in this match");
                return;
            }

            // Check if next game already exists (idempotency - handle both players clicking)
            if (!string.IsNullOrEmpty(match.CurrentGameId))
            {
                _logger.LogInformation(
                    "ContinueMatch: Checking for existing game {GameId}",
                    match.CurrentGameId);

                var existingSession = _sessionManager.GetSession(match.CurrentGameId);

                if (existingSession != null)
                {
                    _logger.LogInformation(
                        "ContinueMatch: Found existing session {GameId} - GameStarted={Started}, Winner={Winner}",
                        match.CurrentGameId,
                        existingSession.Engine.GameStarted,
                        existingSession.Engine.Winner?.Name ?? "null");
                }
                else
                {
                    _logger.LogInformation(
                        "ContinueMatch: No existing session found for {GameId}",
                        match.CurrentGameId);
                }

                if (existingSession != null &&
                    existingSession.Engine.GameStarted &&
                    existingSession.Engine.Winner == null)
                {
                    // Game already created, started, and still in progress - just join it
                    _logger.LogInformation(
                        "ContinueMatch: Player {PlayerId} joining existing in-progress game {GameId} for match {MatchId}",
                        playerId,
                        match.CurrentGameId,
                        matchId);

                    await JoinGame(match.CurrentGameId);
                    return;
                }
                else if (existingSession != null && existingSession.Engine.Winner != null)
                {
                    // Current game is completed/abandoned - need to create new game
                    _logger.LogInformation(
                        "ContinueMatch: Current game {GameId} is completed/abandoned (Winner={Winner}), creating new game for match {MatchId}",
                        match.CurrentGameId,
                        existingSession.Engine.Winner.Name,
                        matchId);
                }
                else if (existingSession != null && !existingSession.Engine.GameStarted)
                {
                    _logger.LogInformation(
                        "ContinueMatch: Current game {GameId} exists but not started, creating new game for match {MatchId}",
                        match.CurrentGameId,
                        matchId);
                }
            }
            else
            {
                _logger.LogInformation(
                    "ContinueMatch: No CurrentGameId set for match {MatchId}",
                    matchId);
            }

            // First player to click - create game atomically with all players
            _logger.LogInformation(
                "ContinueMatch: Player {PlayerId} creating next game for match {MatchId}",
                playerId,
                matchId);

            // Determine which player is calling
            bool isPlayer1 = playerId == match.Player1Id;
            var player1Connections = isPlayer1 ? new HashSet<string> { connectionId } : new HashSet<string>();
            var player2Connections = !isPlayer1 ? new HashSet<string> { connectionId } : new HashSet<string>();

            // Create and start the next game atomically
            var session = await _gameCompletionService.CreateAndStartNextMatchGameAsync(
                match,
                player1Connections,
                player2Connections);

            _logger.LogInformation(
                "Created and started next game {GameId} for match {MatchId}",
                session.Id,
                matchId);
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

            // Reuse GetMatchGames for consistent game fetching
            var games = await GetMatchGames(matchId);

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

            // Query games directly by status instead of matches
            var games = await _gameRepository.GetPlayerGamesAsync(playerId, "InProgress", limit);

            var activeGames = new List<object>();

            foreach (var game in games)
            {
                // Determine player's color based on their ID
                var isWhite = game.WhitePlayerId == playerId;
                var myColor = isWhite ? "White" : "Red";

                // Try to get current game state from active session
                var gameSession = _sessionManager.GetSession(game.GameId);

                var currentPlayer = gameSession?.Engine?.CurrentPlayer.ToString() ?? game.CurrentPlayer;
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
                else
                {
                    // Fall back to persisted game state
                    cubeValue = game.DoublingCubeValue;
                    cubeOwner = game.DoublingCubeOwner ?? "Center";
                }

                // Fetch match info for score and target
                string matchScore = "0-0";
                int matchLength = 1;
                bool isCrawford = game.IsCrawfordGame;

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
                    player1Rating = 0, // TODO: Fetch from user profile
                    player2Rating = 0, // TODO: Fetch from user profile
                    currentPlayer,
                    myColor,
                    isYourTurn,
                    matchScore,
                    matchLength,
                    timeControl = "Standard", // TODO: Add time control
                    cubeValue,
                    cubeOwner,
                    isCrawford,
                    viewers = 0, // TODO: Add spectator tracking
                    board = boardState,
                    whiteCheckersOnBar = whiteOnBar,
                    redCheckersOnBar = redOnBar,
                    whiteBornOff,
                    redBornOff,
                    dice = diceValues
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
    /// Get player's active matches (in-progress). Links to match summary page.
    /// </summary>
    public async Task<List<ActiveMatchDto>> GetActiveMatches(int limit = 10)
    {
        try
        {
            var playerId = GetAuthenticatedUserId()!;
            var matches = await _matchService.GetPlayerMatchesAsync(playerId, "InProgress");

            var activeMatches = matches.Take(limit).Select(m =>
            {
                var isPlayer1 = m.Player1Id == playerId;
                var opponentName = isPlayer1 ? m.Player2Name : m.Player1Name;

                return new ActiveMatchDto
                {
                    MatchId = m.MatchId,
                    OpponentName = opponentName ?? "Waiting...",
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

            return activeMatches;
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
            if (match == null)
            {
                _logger.LogWarning("Match {MatchId} not found when getting games", matchId);
                return new List<MatchGameDto>();
            }

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
