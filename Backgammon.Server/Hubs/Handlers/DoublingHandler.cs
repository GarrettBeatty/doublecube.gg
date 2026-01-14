using Backgammon.Core;
using Backgammon.Server.Extensions;
using Backgammon.Server.Hubs.Interfaces;
using Backgammon.Server.Services;
using Backgammon.Server.Services.Results;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Backgammon.Server.Hubs.Handlers;

/// <summary>
/// Handler for doubling cube operations.
/// </summary>
public class DoublingHandler : IDoublingHandler
{
    private readonly IGameSessionManager _sessionManager;
    private readonly IDoubleOfferService _doubleOfferService;
    private readonly IGameService _gameService;
    private readonly IGameRepository _gameRepository;
    private readonly IPlayerStatsService _playerStatsService;
    private readonly IAiMoveService _aiMoveService;
    private readonly IHubContext<GameHub, IGameHubClient> _hubContext;
    private readonly ILogger<DoublingHandler> _logger;

    public DoublingHandler(
        IGameSessionManager sessionManager,
        IDoubleOfferService doubleOfferService,
        IGameService gameService,
        IGameRepository gameRepository,
        IPlayerStatsService playerStatsService,
        IAiMoveService aiMoveService,
        IHubContext<GameHub, IGameHubClient> hubContext,
        ILogger<DoublingHandler> logger)
    {
        _sessionManager = sessionManager;
        _doubleOfferService = doubleOfferService;
        _gameService = gameService;
        _gameRepository = gameRepository;
        _playerStatsService = playerStatsService;
        _aiMoveService = aiMoveService;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task<Result> OfferDoubleAsync(string connectionId)
    {
        var session = _sessionManager.GetGameByPlayer(connectionId);
        if (session == null)
        {
            return Result.Failure(ErrorCodes.SessionNotFound, "Not in a game");
        }

        var (success, currentValue, newValue, error) = await _doubleOfferService.OfferDoubleAsync(session, connectionId);
        if (!success)
        {
            return Result.Failure(ErrorCodes.CannotDouble, error ?? "Failed to offer double");
        }

        // Notify opponent of the double offer
        var opponentConnections = session.GetPlayerColor(connectionId) == CheckerColor.White
            ? session.RedConnections
            : session.WhiteConnections;

        if (opponentConnections.Any())
        {
            await _gameService.BroadcastDoubleOfferAsync(session, connectionId, currentValue, newValue);
        }
        else
        {
            // Opponent might be an AI
            var opponentPlayerId = session.GetPlayerColor(connectionId) == CheckerColor.White
                ? session.RedPlayerId
                : session.WhitePlayerId;

            if (opponentPlayerId != null && _aiMoveService.IsAiPlayer(opponentPlayerId))
            {
                var (accepted, winner, stakes) = await _doubleOfferService.HandleAiDoubleResponseAsync(
                    session, opponentPlayerId, currentValue, newValue);

                if (accepted)
                {
                    // AI accepted - send updated state to human player
                    var state = session.GetState(connectionId);
                    await _hubContext.Clients.Client(connectionId).DoubleAccepted(state);

                    BackgroundTaskHelper.FireAndForget(
                        async () =>
                        {
                            var game = GameEngineMapper.ToGame(session);
                            await _gameRepository.SaveGameAsync(game);
                        },
                        _logger,
                        $"SaveGameState-{session.Id}");
                }
                else
                {
                    // AI declined - human wins
                    await _hubContext.Clients.Client(connectionId).Info("Computer declined the double. You win!");

                    await _gameRepository.UpdateGameStatusAsync(session.Id, "Completed");

                    if (session.GameMode.ShouldTrackStats)
                    {
                        var game = GameEngineMapper.ToGame(session);
                        await _playerStatsService.UpdateStatsAfterGameCompletionAsync(game);
                    }

                    var finalState = session.GetState(connectionId);
                    await _hubContext.Clients.Client(connectionId).GameOver(finalState);

                    _sessionManager.RemoveGame(session.Id);
                }
            }
        }

        return Result.Ok();
    }

    public async Task<Result> AcceptDoubleAsync(string connectionId)
    {
        var session = _sessionManager.GetGameByPlayer(connectionId);
        if (session == null)
        {
            return Result.Failure(ErrorCodes.SessionNotFound, "Not in a game");
        }

        await _doubleOfferService.AcceptDoubleAsync(session);
        await _gameService.BroadcastDoubleAcceptedAsync(session);

        BackgroundTaskHelper.FireAndForget(
            async () =>
            {
                var game = GameEngineMapper.ToGame(session);
                await _gameRepository.SaveGameAsync(game);
            },
            _logger,
            $"SaveGameState-{session.Id}");

        return Result.Ok();
    }

    public async Task<Result> DeclineDoubleAsync(string connectionId)
    {
        var session = _sessionManager.GetGameByPlayer(connectionId);
        if (session == null)
        {
            return Result.Failure(ErrorCodes.SessionNotFound, "Not in a game");
        }

        var (success, winner, stakes, error) = await _doubleOfferService.DeclineDoubleAsync(session, connectionId);
        if (!success)
        {
            return Result.Failure(ErrorCodes.NoDoubleOffered, error ?? "Failed to decline double");
        }

        await _gameRepository.UpdateGameStatusAsync(session.Id, "Completed");

        if (session.GameMode.ShouldTrackStats)
        {
            var game = GameEngineMapper.ToGame(session);
            await _playerStatsService.UpdateStatsAfterGameCompletionAsync(game);
        }

        await _gameService.BroadcastGameOverAsync(session);

        _sessionManager.RemoveGame(session.Id);
        _logger.LogInformation("Removed completed game {GameId} from memory (declined double)", session.Id);

        return Result.Ok();
    }
}
