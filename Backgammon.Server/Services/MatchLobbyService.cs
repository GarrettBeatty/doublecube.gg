using Backgammon.Server.Models;
using Microsoft.Extensions.Logging;

namespace Backgammon.Server.Services;

/// <summary>
/// Implementation of match lobby operations
/// </summary>
public class MatchLobbyService : IMatchLobbyService
{
    private readonly IMatchService _matchService;
    private readonly IAiMoveService _aiMoveService;
    private readonly IGameSessionManager _sessionManager;
    private readonly ILogger<MatchLobbyService> _logger;

    public MatchLobbyService(
        IMatchService matchService,
        IAiMoveService aiMoveService,
        IGameSessionManager sessionManager,
        ILogger<MatchLobbyService> logger)
    {
        _matchService = matchService;
        _aiMoveService = aiMoveService;
        _sessionManager = sessionManager;
        _logger = logger;
    }

    public async Task<Match> CreateMatchLobbyAsync(string playerId, MatchConfig config, string? displayName)
    {
        // Determine if this is an open lobby
        bool isOpenLobby = config.OpponentType == "OpenLobby";
        string? opponentId = null;

        // For friend matches, set the opponent ID
        if (config.OpponentType == "Friend" && !string.IsNullOrEmpty(config.OpponentId))
        {
            opponentId = config.OpponentId;
        }

        // For AI matches, set the AI opponent ID
        else if (config.OpponentType == "AI" && !string.IsNullOrEmpty(config.OpponentId))
        {
            opponentId = config.OpponentId;
        }

        // Create match lobby
        var match = await _matchService.CreateMatchLobbyAsync(
            playerId,
            config.TargetScore,
            config.OpponentType,
            isOpenLobby,
            displayName,
            opponentId);

        _logger.LogInformation(
            "Match lobby {MatchId} created by {PlayerId} (type: {OpponentType})",
            match.MatchId,
            playerId,
            config.OpponentType);

        return match;
    }

    public async Task<(bool Success, Match? Match, string? Error)> JoinMatchLobbyAsync(
        string matchId,
        string playerId,
        string? displayName)
    {
        // Get match to check if it exists
        var existingMatch = await _matchService.GetMatchLobbyAsync(matchId);
        if (existingMatch == null)
        {
            return (false, null, "Match not found");
        }

        _logger.LogInformation(
            "Player {PlayerId} attempting to join match {MatchId}. Match state: IsOpenLobby={IsOpenLobby}, OpponentType={OpponentType}, Player1Id={Player1Id}, Player2Id={Player2Id}, LobbyStatus={LobbyStatus}",
            playerId,
            matchId,
            existingMatch.IsOpenLobby,
            existingMatch.OpponentType,
            existingMatch.Player1Id,
            existingMatch.Player2Id ?? "null",
            existingMatch.LobbyStatus);

        // If player is the creator, just return current state
        if (existingMatch.Player1Id == playerId)
        {
            return (true, existingMatch, null);
        }

        // Check if this is an open lobby with an empty slot
        if (existingMatch.IsOpenLobby && string.IsNullOrEmpty(existingMatch.Player2Id))
        {
            // New player joining open lobby
            var match = await _matchService.JoinOpenLobbyAsync(matchId, playerId, displayName);
            _logger.LogInformation("Player {PlayerId} joined match lobby {MatchId}", playerId, matchId);
            return (true, match, null);
        }
        else if (existingMatch.Player2Id == playerId)
        {
            // Player is already in this match (rejoining)
            _logger.LogInformation("Player {PlayerId} rejoined match lobby {MatchId}", playerId, matchId);
            return (true, existingMatch, null);
        }
        else
        {
            // Provide detailed error message
            string reason = existingMatch.IsOpenLobby
                ? "Match lobby is full"
                : "Match is not an open lobby (it's invite-only)";

            _logger.LogWarning(
                "Player {PlayerId} cannot join match {MatchId}. Reason: {Reason}. IsOpenLobby={IsOpenLobby}, Player2Id={Player2Id}",
                playerId,
                matchId,
                reason,
                existingMatch.IsOpenLobby,
                existingMatch.Player2Id);

            return (false, null, $"Cannot join this match: {reason}");
        }
    }

    public async Task<(bool Success, Game? Game, Match? Match, string? Error)> StartMatchGameAsync(
        string matchId,
        string playerId)
    {
        var match = await _matchService.GetMatchLobbyAsync(matchId);
        if (match == null)
        {
            return (false, null, null, "Match not found");
        }

        // Only the creator can start the match
        if (match.Player1Id != playerId)
        {
            return (false, null, null, "Only the match creator can start the game");
        }

        // Ensure both players are present
        if (string.IsNullOrEmpty(match.Player2Id))
        {
            return (false, null, null, "Waiting for opponent to join");
        }

        // Start the first game
        var game = await _matchService.StartMatchFirstGameAsync(matchId);

        // Refresh match data to get updated state
        var updatedMatch = await _matchService.GetMatchAsync(matchId);
        if (updatedMatch == null)
        {
            _logger.LogError("Failed to get match {MatchId} after starting first game", matchId);
            return (false, null, null, "Failed to load match data");
        }

        _logger.LogInformation(
            "Match {MatchId} first game {GameId} started by {PlayerId}",
            matchId,
            game.GameId,
            playerId);

        return (true, game, updatedMatch, null);
    }

    public async Task<bool> LeaveMatchLobbyAsync(string matchId, string playerId)
    {
        await _matchService.LeaveMatchLobbyAsync(matchId, playerId);
        _logger.LogInformation("Player {PlayerId} left match lobby {MatchId}", playerId, matchId);
        return true;
    }

    public async Task<Match?> GetMatchLobbyAsync(string matchId)
    {
        return await _matchService.GetMatchLobbyAsync(matchId);
    }

    public async Task<(Game Game, Match Match)?> StartMatchWithAiAsync(Match match)
    {
        var game = await _matchService.StartMatchFirstGameAsync(match.MatchId);

        // Get the game session and add AI player
        var session = _sessionManager.GetGame(game.GameId);
        if (session != null)
        {
            // Add AI player as Red (human player will be White when they join)
            var aiPlayerId = _aiMoveService.GenerateAiPlayerId();
            session.AddPlayer(aiPlayerId, string.Empty); // Empty connection ID for AI
            session.SetPlayerName(aiPlayerId, "Computer");

            _logger.LogInformation(
                "Added AI player {AiPlayerId} to match game {GameId}",
                aiPlayerId,
                game.GameId);
        }
        else
        {
            _logger.LogWarning("Could not find game session {GameId} to add AI player", game.GameId);
        }

        // Refresh match data
        var updatedMatch = await _matchService.GetMatchAsync(match.MatchId);
        if (updatedMatch == null)
        {
            _logger.LogError("Failed to refresh match {MatchId} after creating AI game", match.MatchId);
            return null;
        }

        _logger.LogInformation(
            "AI match {MatchId} created and started",
            match.MatchId);

        return (game, updatedMatch);
    }
}
