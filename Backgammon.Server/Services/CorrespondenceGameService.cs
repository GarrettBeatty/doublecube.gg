using Backgammon.Core;
using Backgammon.Server.Models;
using Microsoft.Extensions.Logging;
using Match = Backgammon.Server.Models.Match;
using ServerGame = Backgammon.Server.Models.Game;

namespace Backgammon.Server.Services;

public class CorrespondenceGameService : ICorrespondenceGameService
{
    private readonly IMatchRepository _matchRepository;
    private readonly IGameRepository _gameRepository;
    private readonly IGameSessionManager _gameSessionManager;
    private readonly IUserRepository _userRepository;
    private readonly IPlayerStatsService _playerStatsService;
    private readonly ILogger<CorrespondenceGameService> _logger;

    public CorrespondenceGameService(
        IMatchRepository matchRepository,
        IGameRepository gameRepository,
        IGameSessionManager gameSessionManager,
        IUserRepository userRepository,
        IPlayerStatsService playerStatsService,
        ILogger<CorrespondenceGameService> logger)
    {
        _matchRepository = matchRepository;
        _gameRepository = gameRepository;
        _gameSessionManager = gameSessionManager;
        _userRepository = userRepository;
        _playerStatsService = playerStatsService;
        _logger = logger;
    }

    public async Task<List<CorrespondenceGameDto>> GetMyTurnGamesAsync(string playerId)
    {
        var matches = await _matchRepository.GetCorrespondenceMatchesForTurnAsync(playerId);
        return await ConvertToGameDtos(matches, playerId, isYourTurn: true);
    }

    public async Task<List<CorrespondenceGameDto>> GetWaitingGamesAsync(string playerId)
    {
        var matches = await _matchRepository.GetCorrespondenceMatchesWaitingAsync(playerId);
        return await ConvertToGameDtos(matches, playerId, isYourTurn: false);
    }

    public async Task<CorrespondenceGamesResponse> GetAllCorrespondenceGamesAsync(string playerId)
    {
        var yourTurnMatches = await _matchRepository.GetCorrespondenceMatchesForTurnAsync(playerId);
        var waitingMatches = await _matchRepository.GetCorrespondenceMatchesWaitingAsync(playerId);

        // Get lobbies created by the player that are waiting for opponents
        var myLobbiesMatches = await _matchRepository.GetPlayerMatchesAsync(
            playerId,
            status: "WaitingForPlayers",
            limit: 50);

        // Filter to only correspondence lobbies where player is creator (Player1)
        var myCorrespondenceLobbies = myLobbiesMatches
            .Where(m => m.IsCorrespondence && m.Player1Id == playerId)
            .ToList();

        var yourTurnGames = await ConvertToGameDtos(yourTurnMatches, playerId, isYourTurn: true);
        var waitingGames = await ConvertToGameDtos(waitingMatches, playerId, isYourTurn: false);
        var myLobbies = await ConvertToGameDtos(myCorrespondenceLobbies, playerId, isYourTurn: false);

        return new CorrespondenceGamesResponse
        {
            YourTurnGames = yourTurnGames,
            WaitingGames = waitingGames,
            MyLobbies = myLobbies,
            TotalYourTurn = yourTurnGames.Count,
            TotalWaiting = waitingGames.Count,
            TotalMyLobbies = myLobbies.Count
        };
    }

    public async Task<(Match Match, ServerGame FirstGame)> CreateCorrespondenceMatchAsync(
        string player1Id,
        int targetScore,
        int timePerMoveDays,
        string opponentType,
        string? player1DisplayName = null,
        string? player2Id = null,
        bool isRated = true)
    {
        try
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(player1Id))
            {
                throw new ArgumentException("Player ID cannot be null or empty");
            }

            if (targetScore <= 0 || targetScore > 25)
            {
                throw new ArgumentException("Target score must be between 1 and 25");
            }

            if (timePerMoveDays <= 0 || timePerMoveDays > 30)
            {
                throw new ArgumentException("Time per move must be between 1 and 30 days");
            }

            if (!new[] { "OpenLobby", "Friend" }.Contains(opponentType))
            {
                throw new ArgumentException("Correspondence games support 'OpenLobby' or 'Friend' opponent types");
            }

            // Validate player2Id for Friend matches
            if (opponentType == "Friend" && string.IsNullOrWhiteSpace(player2Id))
            {
                throw new ArgumentException("Player 2 ID is required for Friend matches");
            }

            // Validate player IDs are not identical for Friend matches
            if (opponentType == "Friend" && player1Id == player2Id)
            {
                throw new ArgumentException("Player IDs cannot be identical");
            }

            // Get player 1 info - user guaranteed to exist (created in OnConnectedAsync)
            var player1 = await _userRepository.GetByUserIdAsync(player1Id);

            // Use player1DisplayName parameter if provided (from frontend), otherwise fall back to database
            var player1Name = !string.IsNullOrWhiteSpace(player1DisplayName)
                ? player1DisplayName
                : player1?.DisplayName ?? "Unknown";

            // Create match with correspondence fields
            var match = new Match
            {
                MatchId = Guid.NewGuid().ToString(),
                TargetScore = targetScore,
                Player1Id = player1Id,
                Player1Name = player1Name, // Use display name from parameter or database
                Player1DisplayName = player1DisplayName,
                OpponentType = opponentType,
                IsCorrespondence = true,
                TimePerMoveDays = timePerMoveDays,
                TimeControl = new TimeControlConfig { Type = TimeControlType.None }, // No real-time clock for correspondence
                IsRated = isRated // Set from parameter
            };

            // Handle opponent based on type
            if (opponentType == "Friend" && !string.IsNullOrEmpty(player2Id))
            {
                // Get player 2 info - user guaranteed to exist (created in OnConnectedAsync)
                var player2 = await _userRepository.GetByUserIdAsync(player2Id);

                match.Player2Id = player2Id;
                match.Player2Name = player2?.DisplayName ?? "Unknown"; // Fallback just in case
                match.Status = "InProgress";
                match.IsOpenLobby = false;

                // Set initial turn (White/Player1 goes first)
                match.CurrentTurnPlayerId = player1Id;
                match.TurnDeadline = DateTime.UtcNow.AddDays(timePerMoveDays);
            }
            else
            {
                // OpenLobby: Player2Id remains null
                match.Status = "WaitingForPlayers";
                match.IsOpenLobby = true;
            }

            await _matchRepository.SaveMatchAsync(match);

            _logger.LogInformation(
                "Created correspondence match {MatchId} for player {Player1Id} (type: {OpponentType}), target score: {TargetScore}, time per move: {TimePerMove} days",
                match.MatchId,
                player1Id,
                opponentType,
                targetScore,
                timePerMoveDays);

            // Create first game
            var game = new ServerGame
            {
                GameId = Guid.NewGuid().ToString(),
                WhitePlayerId = match.Player1Id,
                RedPlayerId = match.Player2Id,
                WhitePlayerName = match.Player1Name,
                RedPlayerName = match.Player2Name ?? string.Empty,
                Status = "InProgress",
                MatchId = match.MatchId,
                IsCrawfordGame = false,
                IsRated = isRated
            };

            await _gameRepository.SaveGameAsync(game);

            // Update match with game ID
            match.CurrentGameId = game.GameId;
            match.GameIds.Add(game.GameId);
            match.LastUpdatedAt = DateTime.UtcNow;
            await _matchRepository.UpdateMatchAsync(match);

            // Create game session (for state management, though players may not be connected yet)
            var session = _gameSessionManager.CreateGame(game.GameId);
            session.WhitePlayerName = match.Player1Name;
            session.RedPlayerName = match.Player2Name ?? "Waiting...";
            session.MatchId = match.MatchId;
            session.TargetScore = match.TargetScore;
            session.Player1Score = match.Player1Score;
            session.Player2Score = match.Player2Score;
            session.IsCrawfordGame = match.IsCrawfordGame;
            session.IsRated = isRated;
            session.IsCorrespondence = true;
            session.TimePerMoveDays = match.TimePerMoveDays;
            session.TurnDeadline = match.TurnDeadline;

            _logger.LogInformation(
                "Created first game {GameId} for correspondence match {MatchId}",
                game.GameId,
                match.MatchId);

            return (match, game);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to create correspondence match for player {Player1Id}",
                player1Id);
            throw;
        }
    }

    public async Task HandleTurnCompletedAsync(string matchId, string nextPlayerId)
    {
        try
        {
            var match = await _matchRepository.GetMatchByIdAsync(matchId);
            if (match == null || !match.IsCorrespondence)
            {
                _logger.LogWarning("Cannot handle turn completion: match {MatchId} not found or not correspondence", matchId);
                return;
            }

            var newDeadline = DateTime.UtcNow.AddDays(match.TimePerMoveDays);
            await _matchRepository.UpdateCorrespondenceTurnAsync(matchId, nextPlayerId, newDeadline);

            _logger.LogInformation(
                "Turn completed in correspondence match {MatchId}. Next player: {PlayerId}, Deadline: {Deadline}",
                matchId,
                nextPlayerId,
                newDeadline);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle turn completion for match {MatchId}", matchId);
            throw;
        }
    }

    public async Task HandleTimeoutAsync(string matchId)
    {
        try
        {
            var match = await _matchRepository.GetMatchByIdAsync(matchId);
            if (match == null || !match.IsCorrespondence)
            {
                _logger.LogWarning("Cannot handle timeout: match {MatchId} not found or not correspondence", matchId);
                return;
            }

            if (match.TurnDeadline == null || DateTime.UtcNow < match.TurnDeadline)
            {
                _logger.LogWarning("Timeout check for match {MatchId}: deadline not expired", matchId);
                return;
            }

            // The player who timed out loses
            var timedOutPlayerId = match.CurrentTurnPlayerId;
            if (timedOutPlayerId == null)
            {
                _logger.LogWarning("Cannot handle timeout for match {MatchId}: CurrentTurnPlayerId is null", matchId);
                return;
            }

            var winnerId = timedOutPlayerId == match.Player1Id ? match.Player2Id : match.Player1Id;

            // Complete the match
            match.Status = "Completed";
            match.WinnerId = winnerId;
            match.CompletedAt = DateTime.UtcNow;
            match.DurationSeconds = (int)(DateTime.UtcNow - match.CreatedAt).TotalSeconds;

            // Clear the turn tracking since match is over
            match.CurrentTurnPlayerId = null;
            match.TurnDeadline = null;

            await _matchRepository.UpdateMatchAsync(match);

            // Update player stats and ELO ratings for rated games
            if (!string.IsNullOrEmpty(match.CurrentGameId))
            {
                var currentGame = await _gameRepository.GetGameByGameIdAsync(match.CurrentGameId);
                if (currentGame != null && currentGame.IsRated)
                {
                    // Mark game as completed (status remains as is, match winner determines outcome)
                    currentGame.Status = "Completed";
                    await _gameRepository.SaveGameAsync(currentGame);

                    // Update stats (includes ELO rating updates)
                    await _playerStatsService.UpdateStatsAfterGameCompletionAsync(currentGame);

                    _logger.LogInformation(
                        "Updated stats and ELO ratings for timeout in match {MatchId}",
                        matchId);
                }
            }

            _logger.LogInformation(
                "Correspondence match {MatchId} ended by timeout. Winner: {WinnerId}, Timed out: {TimedOutId}",
                matchId,
                winnerId,
                timedOutPlayerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle timeout for match {MatchId}", matchId);
            throw;
        }
    }

    public async Task InitializeTurnTrackingAsync(string matchId)
    {
        try
        {
            var match = await _matchRepository.GetMatchByIdAsync(matchId);
            if (match == null || !match.IsCorrespondence)
            {
                _logger.LogWarning("Cannot initialize turn tracking: match {MatchId} not found or not correspondence", matchId);
                return;
            }

            // For correspondence games, we don't set CurrentTurnPlayerId during initialization
            // because both players need to roll opening dice. The turn tracking will be set
            // properly after the opening roll completes in GameActionOrchestrator.
            // We just set the deadline for the opening roll phase.

            var turnDeadline = DateTime.UtcNow.AddDays(match.TimePerMoveDays);
            match.TurnDeadline = turnDeadline;
            match.LastUpdatedAt = DateTime.UtcNow;
            // Leave CurrentTurnPlayerId as null to indicate opening roll phase
            await _matchRepository.UpdateMatchAsync(match);

            _logger.LogInformation(
                "Initialized turn tracking for correspondence match {MatchId}. Opening roll phase, Deadline: {Deadline}",
                matchId,
                turnDeadline);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize turn tracking for match {MatchId}", matchId);
            throw;
        }
    }

    private async Task<List<CorrespondenceGameDto>> ConvertToGameDtos(
        List<Match> matches,
        string playerId,
        bool isYourTurn)
    {
        if (matches.Count == 0)
        {
            return new List<CorrespondenceGameDto>();
        }

        // Batch fetch all opponent user data to avoid N+1 queries
        var opponentIds = matches
            .Select(m => m.Player1Id == playerId ? m.Player2Id : m.Player1Id)
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
            .ToList();

        var opponentUsers = await _userRepository.GetUsersByIdsAsync(opponentIds!);
        var opponentRatings = opponentUsers.ToDictionary(
            u => u.UserId,
            u => u.Rating);

        // Batch fetch all current game data to avoid N+1 queries
        var gameIds = matches
            .Select(m => m.CurrentGameId)
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
            .ToList();

        var games = new Dictionary<string, ServerGame>();
        foreach (var gameId in gameIds)
        {
            var game = await _gameRepository.GetGameByGameIdAsync(gameId!);
            if (game != null)
            {
                games[gameId!] = game;
            }
        }

        // Build DTOs with pre-fetched data
        var dtos = new List<CorrespondenceGameDto>();
        foreach (var match in matches)
        {
            // Determine opponent info
            var isPlayer1 = match.Player1Id == playerId;
            var opponentId = isPlayer1 ? match.Player2Id : match.Player1Id;
            var opponentName = isPlayer1 ? match.Player2Name : match.Player1Name;

            // Get opponent rating from pre-fetched data
            int opponentRating = 1500; // Default
            if (!string.IsNullOrEmpty(opponentId) && opponentRatings.TryGetValue(opponentId, out var rating))
            {
                opponentRating = rating;
            }

            // Get current game info from pre-fetched data
            ServerGame? currentGame = null;
            if (!string.IsNullOrEmpty(match.CurrentGameId))
            {
                games.TryGetValue(match.CurrentGameId, out currentGame);
            }

            // Calculate time remaining and format as string for frontend compatibility
            string? timeRemainingStr = null;
            if (match.TurnDeadline.HasValue)
            {
                var timeRemaining = match.TurnDeadline.Value - DateTime.UtcNow;
                // Format as "d.hh:mm:ss" to match .NET TimeSpan.ToString() format
                timeRemainingStr = timeRemaining.ToString(@"d\.hh\:mm\:ss");
            }

            var dto = new CorrespondenceGameDto
            {
                MatchId = match.MatchId,
                GameId = match.CurrentGameId ?? string.Empty,
                OpponentId = opponentId ?? string.Empty,
                OpponentName = opponentName ?? "Waiting for opponent",
                OpponentRating = opponentRating,
                IsYourTurn = isYourTurn,
                TimePerMoveDays = match.TimePerMoveDays,
                TurnDeadline = match.TurnDeadline,
                TimeRemaining = timeRemainingStr,
                MoveCount = currentGame?.MoveCount ?? 0,
                MatchScore = $"{match.Player1Score}-{match.Player2Score}",
                TargetScore = match.TargetScore,
                IsRated = currentGame?.IsRated ?? false,
                LastUpdatedAt = match.LastUpdatedAt
            };

            dtos.Add(dto);
        }

        return dtos;
    }
}
