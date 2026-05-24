using Backgammon.Server.Models;
using Microsoft.Extensions.Logging;
using Match = Backgammon.Server.Models.Match;
using ServerGame = Backgammon.Server.Models.Game;

namespace Backgammon.Server.Services;

public class CorrespondenceGameService : ICorrespondenceGameService
{
    private readonly IMatchRepository _matchRepository;
    private readonly IGameRepository _gameRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<CorrespondenceGameService> _logger;

    public CorrespondenceGameService(
        IMatchRepository matchRepository,
        IGameRepository gameRepository,
        IUserRepository userRepository,
        ILogger<CorrespondenceGameService> logger)
    {
        _matchRepository = matchRepository;
        _gameRepository = gameRepository;
        _userRepository = userRepository;
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

        var myLobbiesMatches = await _matchRepository.GetPlayerMatchesAsync(
            playerId,
            status: "WaitingForPlayers",
            limit: 50);

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
            TotalMyLobbies = myLobbies.Count,
        };
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

        var opponentIds = matches
            .Select(m => m.Player1Id == playerId ? m.Player2Id : m.Player1Id)
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
            .ToList();

        var opponentUsers = await _userRepository.GetUsersByIdsAsync(opponentIds!);
        var opponentRatings = opponentUsers.ToDictionary(
            u => u.UserId,
            u => u.Rating);

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

        var dtos = new List<CorrespondenceGameDto>();
        foreach (var match in matches)
        {
            var isPlayer1 = match.Player1Id == playerId;
            var opponentId = isPlayer1 ? match.Player2Id : match.Player1Id;
            var opponentName = isPlayer1 ? match.Player2Name : match.Player1Name;

            int opponentRating = 1500;
            if (!string.IsNullOrEmpty(opponentId) && opponentRatings.TryGetValue(opponentId, out var rating))
            {
                opponentRating = rating;
            }

            ServerGame? currentGame = null;
            if (!string.IsNullOrEmpty(match.CurrentGameId))
            {
                games.TryGetValue(match.CurrentGameId, out currentGame);
            }

            string? timeRemainingStr = null;
            if (match.TurnDeadline.HasValue)
            {
                var timeRemaining = match.TurnDeadline.Value - DateTime.UtcNow;
                timeRemainingStr = timeRemaining.ToString(@"d\.hh\:mm\:ss");
            }

            dtos.Add(new CorrespondenceGameDto
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
                LastUpdatedAt = match.LastUpdatedAt,
            });
        }

        return dtos;
    }
}
