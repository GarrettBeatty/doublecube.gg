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
    private readonly ILogger<MatchService> _logger;

    public MatchService(
        IMatchRepository matchRepository,
        IGameRepository gameRepository,
        IGameSessionManager gameSessionManager,
        IUserRepository userRepository,
        ILogger<MatchService> logger)
    {
        _matchRepository = matchRepository;
        _gameRepository = gameRepository;
        _gameSessionManager = gameSessionManager;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Match> CreateMatchAsync(string player1Id, string player2Id, int targetScore)
    {
        try
        {
            // Input validation
            if (string.IsNullOrWhiteSpace(player1Id) || string.IsNullOrWhiteSpace(player2Id))
            {
                throw new ArgumentException("Player IDs cannot be null or empty");
            }

            if (targetScore <= 0 || targetScore > 25)
            {
                throw new ArgumentException("Target score must be between 1 and 25");
            }

            if (player1Id == player2Id)
            {
                throw new ArgumentException("Cannot create a match against yourself");
            }

            // Get player names
            var player1 = await _userRepository.GetByUserIdAsync(player1Id);
            var player2 = await _userRepository.GetByUserIdAsync(player2Id);

            var match = new Match
            {
                MatchId = Guid.NewGuid().ToString(),
                TargetScore = targetScore,
                Player1Id = player1Id,
                Player2Id = player2Id,
                Player1Name = player1?.DisplayName ?? (player1Id.Length >= 8 ? $"Player {player1Id.Substring(0, 8)}" : $"Player {player1Id}"),
                Player2Name = player2?.DisplayName ?? (player2Id.Length >= 8 ? $"Player {player2Id.Substring(0, 8)}" : $"Player {player2Id}"),
                Status = "InProgress"
            };

            await _matchRepository.SaveMatchAsync(match);

            _logger.LogInformation(
                "Created match {MatchId} between {Player1} and {Player2}, target score: {TargetScore}",
                match.MatchId,
                match.Player1Name,
                match.Player2Name,
                targetScore);

            return match;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to create match between {Player1Id} and {Player2Id}",
                player1Id,
                player2Id);
            throw;
        }
    }

    public async Task<Match?> GetMatchAsync(string matchId)
    {
        return await _matchRepository.GetMatchByIdAsync(matchId);
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

            // Configure Crawford rule if applicable
            if (match.IsCrawfordGame)
            {
                session.Engine.IsCrawfordGame = true;
                session.Engine.MatchId = matchId;
            }

            _logger.LogInformation(
                "Started game {GameId} for match {MatchId} (Crawford: {IsCrawford})",
                gameId, matchId, match.IsCrawfordGame);

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
                    match.MatchId, match.WinnerId, match.Player1Score, match.Player2Score);
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

    public async Task AbandonMatchAsync(string matchId, string abandoningPlayerId)
    {
        try
        {
            var match = await _matchRepository.GetMatchByIdAsync(matchId);
            if (match == null || match.Status != "InProgress")
            {
                return;
            }

            match.Status = "Abandoned";
            match.CompletedAt = DateTime.UtcNow;
            match.DurationSeconds = (int)(match.CompletedAt.Value - match.CreatedAt).TotalSeconds;

            // The non-abandoning player wins by default
            match.WinnerId = match.Player1Id == abandoningPlayerId ? match.Player2Id : match.Player1Id;

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

    public async Task<Match> CreateMatchLobbyAsync(string player1Id, int targetScore, string opponentType, bool isOpenLobby, string? player1DisplayName = null, string? player2Id = null)
    {
        try
        {
            // Get player names
            var player1 = await _userRepository.GetByUserIdAsync(player1Id);
            string player1Name = player1DisplayName ?? player1?.DisplayName ?? $"Player {player1Id.Substring(0, 8)}";

            var match = new Match
            {
                MatchId = Guid.NewGuid().ToString(),
                TargetScore = targetScore,
                Player1Id = player1Id,
                Player1Name = player1Name,
                Player1DisplayName = player1DisplayName,
                OpponentType = opponentType,
                IsOpenLobby = isOpenLobby,
                LobbyStatus = "WaitingForOpponent",
                Status = "InProgress"
            };

            // If opponent is specified (Friend or AI), set player 2
            if (!string.IsNullOrEmpty(player2Id))
            {
                var player2 = await _userRepository.GetByUserIdAsync(player2Id);
                match.Player2Id = player2Id;
                match.Player2Name = player2?.DisplayName ?? player2Id;
                match.LobbyStatus = "Ready";
            }

            await _matchRepository.SaveMatchAsync(match);

            _logger.LogInformation(
                "Created match lobby {MatchId} for player {Player1Id} (type: {OpponentType})",
                match.MatchId, player1Id, opponentType);

            return match;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create match lobby");
            throw;
        }
    }

    public async Task<Match> JoinOpenLobbyAsync(string matchId, string player2Id, string? player2DisplayName = null)
    {
        try
        {
            var match = await _matchRepository.GetMatchByIdAsync(matchId);
            if (match == null)
            {
                throw new InvalidOperationException($"Match {matchId} not found");
            }

            if (!match.IsOpenLobby)
            {
                throw new InvalidOperationException($"Match {matchId} is not an open lobby");
            }

            if (!string.IsNullOrEmpty(match.Player2Id))
            {
                throw new InvalidOperationException($"Match {matchId} already has a second player");
            }

            // Get player 2 name
            var player2 = await _userRepository.GetByUserIdAsync(player2Id);
            string player2Name = player2DisplayName ?? player2?.DisplayName ?? $"Player {player2Id.Substring(0, 8)}";

            match.Player2Id = player2Id;
            match.Player2Name = player2Name;
            match.Player2DisplayName = player2DisplayName;
            match.LobbyStatus = "Ready";
            match.LastUpdatedAt = DateTime.UtcNow;

            await _matchRepository.UpdateMatchAsync(match);

            _logger.LogInformation("Player {Player2Id} joined match lobby {MatchId}", player2Id, matchId);

            return match;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to join open lobby {MatchId}", matchId);
            throw;
        }
    }

    public async Task<ServerGame> StartMatchFirstGameAsync(string matchId)
    {
        var match = await _matchRepository.GetMatchByIdAsync(matchId);
        if (match == null)
        {
            throw new InvalidOperationException($"Match {matchId} not found");
        }

        return await StartMatchFirstGameAsync(match);
    }

    public async Task<ServerGame> StartMatchFirstGameAsync(Match match)
    {
        try
        {
            if (string.IsNullOrEmpty(match.Player2Id))
            {
                throw new InvalidOperationException($"Match {match.MatchId} does not have a second player");
            }

            // Create the first game
            var game = new ServerGame
            {
                GameId = Guid.NewGuid().ToString(),
                WhitePlayerId = match.Player1Id,
                RedPlayerId = match.Player2Id,
                WhitePlayerName = match.Player1Name,
                RedPlayerName = match.Player2Name,
                Status = "InProgress",
                IsMatchGame = true,
                MatchId = match.MatchId,
                IsCrawfordGame = false
            };

            await _gameRepository.SaveGameAsync(game);

            // Update match
            match.CurrentGameId = game.GameId;
            match.GameIds.Add(game.GameId);
            match.LobbyStatus = "InGame";
            match.LastUpdatedAt = DateTime.UtcNow;
            await _matchRepository.UpdateMatchAsync(match);

            // Create game session
            var session = _gameSessionManager.CreateGame(game.GameId);
            session.WhitePlayerName = match.Player1Name;
            session.RedPlayerName = match.Player2Name;
            session.MatchId = match.MatchId;
            session.IsMatchGame = true;

            // Configure Crawford rule if applicable
            if (match.IsCrawfordGame)
            {
                session.Engine.IsCrawfordGame = true;
                session.Engine.MatchId = match.MatchId;
            }

            _logger.LogInformation("Started first game {GameId} for match {MatchId}", game.GameId, match.MatchId);

            return game;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start first game for match {MatchId}", match.MatchId);
            throw;
        }
    }

    public async Task LeaveMatchLobbyAsync(string matchId, string playerId)
    {
        try
        {
            var match = await _matchRepository.GetMatchByIdAsync(matchId);
            if (match == null || match.LobbyStatus == "InGame")
            {
                return;
            }

            // If creator leaves, delete the match
            if (match.Player1Id == playerId)
            {
                await _matchRepository.DeleteMatchAsync(matchId);
                _logger.LogInformation("Match lobby {MatchId} deleted as creator left", matchId);
            }

            // If joiner leaves, reset to waiting
            else if (match.Player2Id == playerId)
            {
                match.Player2Id = string.Empty;
                match.Player2Name = string.Empty;
                match.Player2DisplayName = null;
                match.LobbyStatus = "WaitingForOpponent";
                match.LastUpdatedAt = DateTime.UtcNow;
                await _matchRepository.UpdateMatchAsync(match);
                _logger.LogInformation("Player {PlayerId} left match lobby {MatchId}", playerId, matchId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to leave match lobby {MatchId}", matchId);
            throw;
        }
    }

    public async Task<Match?> GetMatchLobbyAsync(string matchId)
    {
        return await _matchRepository.GetMatchByIdAsync(matchId);
    }
}
