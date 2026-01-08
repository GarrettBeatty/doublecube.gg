using Backgammon.Core;
using Backgammon.Server.Models;
using Microsoft.Extensions.Logging;
using Match = Backgammon.Server.Models.Match;
using ServerGame = Backgammon.Server.Models.Game;

namespace Backgammon.Server.Services;

public class MatchService : IMatchService
{
    private readonly IMatchRepository _matchRepository;
    private readonly IGameRepository _gameRepository;
    private readonly IGameSessionManager _gameSessionManager;
    private readonly IUserRepository _userRepository;
    private readonly IAiMoveService _aiMoveService;
    private readonly ILogger<MatchService> _logger;

    public MatchService(
        IMatchRepository matchRepository,
        IGameRepository gameRepository,
        IGameSessionManager gameSessionManager,
        IUserRepository userRepository,
        IAiMoveService aiMoveService,
        ILogger<MatchService> logger)
    {
        _matchRepository = matchRepository;
        _gameRepository = gameRepository;
        _gameSessionManager = gameSessionManager;
        _userRepository = userRepository;
        _aiMoveService = aiMoveService;
        _logger = logger;
    }

    public async Task<(Match Match, ServerGame FirstGame)> CreateMatchAsync(
        string player1Id,
        int targetScore,
        string opponentType,
        string? player1DisplayName = null,
        string? player2Id = null,
        TimeControlConfig? timeControl = null,
        bool isRated = true)
    {
        try
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(player1Id))
            {
                throw new ArgumentException("Player IDs cannot be null or empty");
            }

            if (targetScore <= 0 || targetScore > 25)
            {
                throw new ArgumentException("Target score must be between 1 and 25");
            }

            if (!new[] { "AI", "OpenLobby", "Friend" }.Contains(opponentType))
            {
                throw new ArgumentException("OpponentType must be 'AI', 'OpenLobby', or 'Friend'");
            }

            // Validate player2Id for Friend matches
            if (opponentType == "Friend" && string.IsNullOrWhiteSpace(player2Id))
            {
                throw new ArgumentException("Player IDs cannot be null or empty");
            }

            // Validate player IDs are not identical for Friend matches
            if (opponentType == "Friend" && player1Id == player2Id)
            {
                throw new ArgumentException("Player IDs cannot be identical");
            }

            // Get player 1 name
            var player1 = await _userRepository.GetByUserIdAsync(player1Id);

            // If displayName is just "Player" (guest), append player ID for uniqueness
            string player1Name;
            if (string.IsNullOrEmpty(player1DisplayName) || player1DisplayName == "Player")
            {
                player1Name = player1?.DisplayName ?? $"Player {player1Id.Substring(0, Math.Min(8, player1Id.Length))}";
            }
            else
            {
                player1Name = player1DisplayName;
            }

            // Create match (Status defaults to WaitingForPlayers from constructor)
            var match = new Match
            {
                MatchId = Guid.NewGuid().ToString(),
                TargetScore = targetScore,
                Player1Id = player1Id,
                Player1Name = player1Name,
                Player1DisplayName = player1DisplayName,
                OpponentType = opponentType,
                TimeControl = timeControl ?? new TimeControlConfig() // Default to None if not specified
            };

            // Handle opponent based on type
            if (opponentType == "AI")
            {
                // Generate AI player ID
                var aiPlayerId = _aiMoveService.GenerateAiPlayerId();
                match.Player2Id = aiPlayerId;
                match.Player2Name = "Computer";
                match.Status = "InProgress";  // AI matches start immediately
                match.IsOpenLobby = false;
            }
            else if (opponentType == "Friend" && !string.IsNullOrEmpty(player2Id))
            {
                // Set friend as player 2
                var player2 = await _userRepository.GetByUserIdAsync(player2Id);
                match.Player2Id = player2Id;
                // Use same formatting logic as player1 for consistency
                match.Player2Name = player2?.DisplayName ?? $"Player {player2Id.Substring(0, Math.Min(8, player2Id.Length))}";
                match.Status = "InProgress";  // Friend match with both players ready
                match.IsOpenLobby = false;
            }
            else
            {
                // OpenLobby: Player2Id remains null
                match.Status = "WaitingForPlayers";  // Waiting for join
                match.IsOpenLobby = true;
            }

            await _matchRepository.SaveMatchAsync(match);

            _logger.LogInformation(
                "Created match {MatchId} for player {Player1Id} (type: {OpponentType}), target score: {TargetScore}",
                match.MatchId,
                player1Id,
                opponentType,
                targetScore);

            // Create first game immediately
            var game = new ServerGame
            {
                GameId = Guid.NewGuid().ToString(),
                WhitePlayerId = match.Player1Id,
                RedPlayerId = match.Player2Id, // Will be null for OpenLobby
                WhitePlayerName = match.Player1Name,
                RedPlayerName = match.Player2Name, // Will be empty for OpenLobby
                Status = "InProgress",
                IsMatchGame = true,
                MatchId = match.MatchId,
                IsCrawfordGame = false
            };

            await _gameRepository.SaveGameAsync(game);

            // Update match with game ID
            match.CurrentGameId = game.GameId;
            match.GameIds.Add(game.GameId);
            match.LastUpdatedAt = DateTime.UtcNow;
            await _matchRepository.UpdateMatchAsync(match);

            // Create game session
            var session = _gameSessionManager.CreateGame(game.GameId);
            session.WhitePlayerName = match.Player1Name;
            session.RedPlayerName = match.Player2Name ?? "Waiting...";
            session.MatchId = match.MatchId;
            session.IsMatchGame = true;
            session.TargetScore = match.TargetScore;
            session.Player1Score = match.Player1Score;
            session.Player2Score = match.Player2Score;
            session.IsCrawfordGame = match.IsCrawfordGame;

            // Set rated/unrated flag (AI matches are always unrated)
            session.IsRated = opponentType == "AI" ? false : isRated;

            // Configure time controls if enabled
            if (match.TimeControl != null && match.TimeControl.Type != TimeControlType.None)
            {
                session.TimeControl = match.TimeControl;

                // Calculate reserve times based on current match score
                var whiteReserve = match.TimeControl.CalculateReserveTime(
                    match.TargetScore, match.Player1Score, match.Player2Score);
                var redReserve = whiteReserve; // Same for both players

                session.Engine.InitializeTimeControl(match.TimeControl, whiteReserve, redReserve);

                _logger.LogInformation(
                    "Initialized time control for game {GameId}: {Type}, Reserve={Reserve}min",
                    game.GameId,
                    match.TimeControl.Type,
                    whiteReserve.TotalMinutes);
            }

            // For AI matches, add AI player to session
            if (opponentType == "AI")
            {
                session.AddPlayer(match.Player2Id!, string.Empty); // Empty connection for AI
                session.SetPlayerName(match.Player2Id!, "Computer");
            }

            _logger.LogInformation(
                "Created first game {GameId} for match {MatchId}",
                game.GameId,
                match.MatchId);

            return (match, game);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to create match for player {Player1Id}",
                player1Id);
            throw;
        }
    }

    public async Task<Match?> GetMatchAsync(string matchId)
    {
        return await _matchRepository.GetMatchByIdAsync(matchId);
    }

    public async Task UpdateMatchAsync(Match match)
    {
        await _matchRepository.UpdateMatchAsync(match);
    }

    public async Task<ServerGame> StartNextGameAsync(string matchId)
    {
        try
        {
            var match = await _matchRepository.GetMatchByIdAsync(matchId);
            if (match == null)
            {
                throw new InvalidOperationException($"Match {matchId} not found");
            }

            if (match.Status != "InProgress")
            {
                throw new InvalidOperationException($"Cannot start new game in completed match {matchId}");
            }

            // Create new game
            var gameId = Guid.NewGuid().ToString();
            var game = new ServerGame
            {
                Id = gameId,
                GameId = gameId,
                WhitePlayerId = match.Player1Id,
                RedPlayerId = match.Player2Id,
                WhitePlayerName = match.Player1Name,
                RedPlayerName = match.Player2Name,
                Status = "InProgress",
                MatchId = matchId,
                IsMatchGame = true,
                IsCrawfordGame = match.IsCrawfordGame
            };

            // Save game to database
            await _gameRepository.SaveGameAsync(game);

            // Update match with new game
            await _matchRepository.AddGameToMatchAsync(matchId, gameId);

            // Create game session
            var session = _gameSessionManager.CreateGame(gameId);
            session.WhitePlayerName = match.Player1Name;
            session.RedPlayerName = match.Player2Name;
            session.MatchId = matchId;
            session.IsMatchGame = true;
            session.TargetScore = match.TargetScore;
            session.Player1Score = match.Player1Score;
            session.Player2Score = match.Player2Score;
            session.IsCrawfordGame = match.IsCrawfordGame;

            // Configure Crawford rule if applicable
            if (match.IsCrawfordGame)
            {
                session.Engine.IsCrawfordGame = true;
                session.Engine.MatchId = matchId;
            }

            // Configure time controls if enabled
            if (match.TimeControl != null && match.TimeControl.Type != TimeControlType.None)
            {
                session.TimeControl = match.TimeControl;

                // Calculate reserve times based on current match score
                var whiteReserve = match.TimeControl.CalculateReserveTime(
                    match.TargetScore, match.Player1Score, match.Player2Score);
                var redReserve = whiteReserve; // Same for both players

                session.Engine.InitializeTimeControl(match.TimeControl, whiteReserve, redReserve);

                _logger.LogInformation(
                    "Initialized time control for game {GameId}: {Type}, Reserve={Reserve}min",
                    gameId,
                    match.TimeControl.Type,
                    whiteReserve.TotalMinutes);
            }

            _logger.LogInformation(
                "Started game {GameId} for match {MatchId} (Crawford: {IsCrawford})",
                gameId,
                matchId,
                match.IsCrawfordGame);

            return game;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start next game for match {MatchId}", matchId);
            throw;
        }
    }

    public async Task CompleteGameAsync(string gameId, GameResult result)
    {
        try
        {
            var game = await _gameRepository.GetGameByGameIdAsync(gameId);
            if (game == null || !game.IsMatchGame || string.IsNullOrEmpty(game.MatchId))
            {
                return;
            }

            var match = await _matchRepository.GetMatchByIdAsync(game.MatchId);
            if (match == null)
            {
                return;
            }

            // Create Core.Game from result
            var coreGame = new Core.Game(gameId)
            {
                Winner = result.WinnerColor,
                Stakes = result.PointsWon,
                WinType = result.WinType,
                MatchId = game.MatchId,
                IsMatchGame = true,
                IsCrawfordGame = match.IsCrawfordGame,
                MoveHistory = result.MoveHistory ?? new List<Move>(),
                Status = Core.GameStatus.Completed
            };

            // Add game to match and update scores (uses Core business logic)
            match.CoreMatch.AddGame(coreGame);

            // Track Crawford rule state before update
            bool wasCrawford = match.IsCrawfordGame;

            // Update scores - this handles Crawford rule logic automatically
            match.CoreMatch.UpdateScores(result.WinnerId, result.PointsWon);
            match.LastUpdatedAt = DateTime.UtcNow;

            // Log Crawford rule changes
            if (!wasCrawford && match.IsCrawfordGame)
            {
                _logger.LogInformation("Crawford rule activated for match {MatchId}", match.MatchId);
            }
            else if (wasCrawford && !match.IsCrawfordGame)
            {
                _logger.LogInformation("Crawford game completed for match {MatchId}", match.MatchId);
            }

            // Handle match completion
            if (match.CoreMatch.IsMatchComplete())
            {
                match.WinnerId = match.CoreMatch.GetWinnerId();
                match.DurationSeconds = (int)(DateTime.UtcNow - match.CreatedAt).TotalSeconds;

                _logger.LogInformation(
                    "Match {MatchId} completed. Winner: {WinnerId}, Score: {P1Score}-{P2Score}",
                    match.MatchId,
                    match.WinnerId,
                    match.Player1Score,
                    match.Player2Score);
            }

            await _matchRepository.UpdateMatchAsync(match);

            // Update game with win type
            game.WinType = result.WinType.ToString();
            await _gameRepository.SaveGameAsync(game);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete game {GameId}", gameId);
            throw;
        }
    }

    public async Task<bool> IsMatchCompleteAsync(string matchId)
    {
        var match = await _matchRepository.GetMatchByIdAsync(matchId);
        return match?.Status == "Completed";
    }

    public async Task<List<Match>> GetPlayerMatchesAsync(string playerId, string? status = null)
    {
        return await _matchRepository.GetPlayerMatchesAsync(playerId, status);
    }

    public async Task<List<Match>> GetActiveMatchesAsync()
    {
        return await _matchRepository.GetActiveMatchesAsync();
    }

    public async Task<List<Match>> GetOpenLobbiesAsync(int limit = 50)
    {
        return await _matchRepository.GetOpenLobbiesAsync(limit);
    }

    public async Task AbandonMatchAsync(string matchId, string abandoningPlayerId)
    {
        try
        {
            var match = await _matchRepository.GetMatchByIdAsync(matchId);
            if (match == null)
            {
                return;
            }

            // Allow abandoning both WaitingForPlayers and InProgress matches
            if (match.Status != "InProgress" && match.Status != "WaitingForPlayers")
            {
                return;  // Already completed or abandoned
            }

            match.Status = "Abandoned";
            match.CompletedAt = DateTime.UtcNow;
            match.DurationSeconds = (int)(match.CompletedAt.Value - match.CreatedAt).TotalSeconds;

            // Handle case where Player2 never joined
            if (string.IsNullOrEmpty(match.Player2Id))
            {
                match.WinnerId = null;  // No winner if match never started
            }
            else
            {
                // The non-abandoning player wins by default
                match.WinnerId = match.Player1Id == abandoningPlayerId ? match.Player2Id : match.Player1Id;
            }

            await _matchRepository.UpdateMatchAsync(match);

            _logger.LogInformation("Match {MatchId} abandoned by {PlayerId}", matchId, abandoningPlayerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to abandon match {MatchId}", matchId);
            throw;
        }
    }

    public async Task<MatchStats> GetPlayerMatchStatsAsync(string playerId)
    {
        return await _matchRepository.GetPlayerMatchStatsAsync(playerId);
    }

    public async Task<Match> JoinMatchAsync(string matchId, string player2Id, string? player2DisplayName = null)
    {
        try
        {
            var match = await _matchRepository.GetMatchByIdAsync(matchId);
            if (match == null)
            {
                throw new InvalidOperationException($"Match {matchId} not found");
            }

            if (match.OpponentType != "OpenLobby" && match.OpponentType != "Friend")
            {
                throw new InvalidOperationException($"Match {matchId} does not allow joining (OpponentType: {match.OpponentType})");
            }

            if (!string.IsNullOrEmpty(match.Player2Id))
            {
                throw new InvalidOperationException($"Match {matchId} already has a second player");
            }

            // Validate match is in correct state
            if (match.Status != "WaitingForPlayers")
            {
                throw new InvalidOperationException($"Match {matchId} is not accepting players (Status: {match.Status})");
            }

            // Get player 2 name
            var player2 = await _userRepository.GetByUserIdAsync(player2Id);

            // If displayName is just "Player" (guest), append player ID for uniqueness
            string player2Name;
            if (string.IsNullOrEmpty(player2DisplayName) || player2DisplayName == "Player")
            {
                player2Name = player2?.DisplayName ?? $"Player {player2Id.Substring(0, Math.Min(8, player2Id.Length))}";
            }
            else
            {
                player2Name = player2DisplayName;
            }

            match.Player2Id = player2Id;
            match.Player2Name = player2Name;
            match.Player2DisplayName = player2DisplayName;
            match.Status = "InProgress";  // Transition from WaitingForPlayers to InProgress
            match.LastUpdatedAt = DateTime.UtcNow;

            await _matchRepository.UpdateMatchAsync(match);

            _logger.LogInformation(
                "Player {Player2Id} joined match {MatchId}, Status: WaitingForPlayers → InProgress",
                player2Id,
                matchId);

            return match;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to join match {MatchId}", matchId);
            throw;
        }
    }
}
