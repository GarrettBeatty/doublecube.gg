using Backgammon.Core;
using Backgammon.Server.Extensions;
using Backgammon.Server.Hubs;
using Backgammon.Server.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Backgammon.Server.Services;

/// <summary>
/// Orchestrates game actions with common patterns:
/// validation, execution, broadcasting, persistence, and AI turn triggering
/// </summary>
public class GameActionOrchestrator : IGameActionOrchestrator
{
    private readonly IGameRepository _gameRepository;
    private readonly IGameStateService _gameStateService;
    private readonly IAiMoveService _aiMoveService;
    private readonly IPlayerStatsService _playerStatsService;
    private readonly IMatchService _matchService;
    private readonly IHubContext<GameHub> _hubContext;
    private readonly ILogger<GameActionOrchestrator> _logger;

    public GameActionOrchestrator(
        IGameRepository gameRepository,
        IGameStateService gameStateService,
        IAiMoveService aiMoveService,
        IPlayerStatsService playerStatsService,
        IMatchService matchService,
        IHubContext<GameHub> hubContext,
        ILogger<GameActionOrchestrator> logger)
    {
        _gameRepository = gameRepository;
        _gameStateService = gameStateService;
        _aiMoveService = aiMoveService;
        _playerStatsService = playerStatsService;
        _matchService = matchService;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task<ActionResult> RollDiceAsync(GameSession session, string connectionId)
    {
        _logger.LogDebug("RollDice called by connection {ConnectionId}", connectionId);

        if (!session.IsPlayerTurn(connectionId))
        {
            _logger.LogWarning(
                "RollDice failed: Not player's turn. Connection={ConnectionId}, Game={GameId}",
                connectionId,
                session.Id);
            return ActionResult.Error("Not your turn");
        }

        if (session.Engine.RemainingMoves.Count > 0)
        {
            _logger.LogWarning(
                "RollDice failed: Remaining moves exist. Count={Count}, Connection={ConnectionId}",
                session.Engine.RemainingMoves.Count,
                connectionId);
            return ActionResult.Error("Must complete current moves first");
        }

        session.Engine.RollDice();
        session.UpdateActivity();

        _logger.LogInformation(
            "Player {ConnectionId} rolled dice in game {GameId}: [{Die1}, {Die2}]",
            connectionId,
            session.Id,
            session.Engine.Dice.Die1,
            session.Engine.Dice.Die2);

        // Broadcast and save
        await _gameStateService.BroadcastGameUpdateAsync(session);
        await SaveGameStateAsync(session);

        return ActionResult.Ok();
    }

    public async Task<ActionResult> MakeMoveAsync(GameSession session, string connectionId, int from, int to)
    {
        _logger.LogInformation("MakeMove called: from {From} to {To}", from, to);

        if (!session.IsPlayerTurn(connectionId))
        {
            _logger.LogWarning("Not player's turn");
            return ActionResult.Error("Not your turn");
        }

        _logger.LogInformation(
            "Current player: {Player}, Remaining moves: {Moves}",
            session.Engine.CurrentPlayer.Name,
            string.Join(",", session.Engine.RemainingMoves));

        // Find the correct die value from valid moves
        var validMoves = session.Engine.GetValidMoves();
        var matchingMove = validMoves.FirstOrDefault(m => m.From == from && m.To == to);

        if (matchingMove == null)
        {
            _logger.LogWarning("No valid move found from {From} to {To}", from, to);
            return ActionResult.Error("Invalid move - no matching valid move");
        }

        _logger.LogInformation("Found valid move with die value: {DieValue}", matchingMove.DieValue);

        var isValid = session.Engine.IsValidMove(matchingMove);
        _logger.LogInformation("Move validity check: {IsValid}", isValid);

        if (!session.Engine.ExecuteMove(matchingMove))
        {
            _logger.LogWarning("ExecuteMove returned false - invalid move");
            return ActionResult.Error("Invalid move");
        }

        _logger.LogInformation("Move executed successfully");

        session.UpdateActivity();

        // Broadcast and save
        await _gameStateService.BroadcastGameUpdateAsync(session);
        await SaveGameStateAsync(session);

        // Check if game is over
        if (session.Engine.Winner != null)
        {
            var stakes = session.Engine.GetGameResult();
            await _gameStateService.BroadcastGameOverAsync(session);

            // Handle match game completion if this is a match game
            await HandleMatchGameCompletion(session);

            // Update game status and stats in background
            BackgroundTaskHelper.FireAndForget(
                async () =>
                {
                    await _gameRepository.UpdateGameStatusAsync(session.Id, "Completed");

                    if (session.GameMode.ShouldTrackStats)
                    {
                        var game = GameEngineMapper.ToGame(session);
                        await _playerStatsService.UpdateStatsAfterGameCompletionAsync(game);
                    }
                    else
                    {
                        _logger.LogInformation("Skipping stats tracking for non-competitive game {GameId}", session.Id);
                    }

                    _logger.LogInformation("Updated game {GameId} to Completed status and user stats", session.Id);
                },
                _logger,
                $"UpdateGameCompletion-{session.Id}");

            return ActionResult.GameOver();
        }

        return ActionResult.Ok();
    }

    public async Task<ActionResult> EndTurnAsync(GameSession session, string connectionId)
    {
        if (!session.IsPlayerTurn(connectionId))
        {
            return ActionResult.Error("Not your turn");
        }

        // Validate that turn can be ended
        if (session.Engine.RemainingMoves.Count > 0)
        {
            var validMoves = session.Engine.GetValidMoves();
            if (validMoves.Count > 0)
            {
                return ActionResult.Error("You still have valid moves available");
            }
        }

        session.Engine.EndTurn();
        session.UpdateActivity();

        // Broadcast and save
        await _gameStateService.BroadcastGameUpdateAsync(session);
        await SaveGameStateAsync(session);

        _logger.LogInformation(
            "Turn ended in game {GameId}. Current player: {Player}",
            session.Id,
            session.Engine.CurrentPlayer?.Color.ToString() ?? "Unknown");

        // Check if next player is AI and trigger AI turn
        var nextPlayerId = GetCurrentPlayerId(session);
        if (_aiMoveService.IsAiPlayer(nextPlayerId))
        {
            _logger.LogInformation(
                "Triggering AI turn for player {AiPlayerId} in game {GameId}",
                nextPlayerId,
                session.Id);

            BackgroundTaskHelper.FireAndForget(
                async () =>
                {
                    await ExecuteAiTurnWithBroadcastAsync(session, nextPlayerId!);
                },
                _logger,
                $"AiTurn-{session.Id}");
        }

        return ActionResult.Ok();
    }

    public async Task<ActionResult> UndoLastMoveAsync(GameSession session, string connectionId)
    {
        if (!session.IsPlayerTurn(connectionId))
        {
            return ActionResult.Error("Not your turn");
        }

        if (session.Engine.MoveHistory.Count == 0)
        {
            return ActionResult.Error("No moves to undo");
        }

        if (!session.Engine.UndoLastMove())
        {
            return ActionResult.Error("Failed to undo move");
        }

        session.UpdateActivity();

        // Broadcast and save
        await _gameStateService.BroadcastGameUpdateAsync(session);
        await SaveGameStateAsync(session);

        _logger.LogInformation(
            "Player {ConnectionId} undid last move in game {GameId}",
            connectionId,
            session.Id);

        return ActionResult.Ok();
    }

    private async Task SaveGameStateAsync(GameSession session)
    {
        try
        {
            var game = GameEngineMapper.ToGame(session);
            await _gameRepository.SaveGameAsync(game);
            _logger.LogDebug("Saved game state for {GameId}, Status={Status}", session.Id, game.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save game state for {GameId}", session.Id);
        }
    }

    private string? GetCurrentPlayerId(GameSession session)
    {
        if (session.Engine.CurrentPlayer == null)
        {
            return null;
        }

        return session.Engine.CurrentPlayer.Color == CheckerColor.White
            ? session.WhitePlayerId
            : session.RedPlayerId;
    }

    private async Task ExecuteAiTurnWithBroadcastAsync(GameSession session, string aiPlayerId)
    {
        try
        {
            async Task BroadcastUpdate()
            {
                if (!string.IsNullOrEmpty(session.WhiteConnectionId))
                {
                    var whiteState = session.GetState(session.WhiteConnectionId);
                    await _hubContext.Clients.Client(session.WhiteConnectionId).SendAsync("GameUpdate", whiteState);
                }

                if (!string.IsNullOrEmpty(session.RedConnectionId))
                {
                    var redState = session.GetState(session.RedConnectionId);
                    await _hubContext.Clients.Client(session.RedConnectionId).SendAsync("GameUpdate", redState);
                }

                var spectatorState = session.GetState(null);
                foreach (var spectatorId in session.SpectatorConnections)
                {
                    await _hubContext.Clients.Client(spectatorId).SendAsync("GameUpdate", spectatorState);
                }
            }

            await _aiMoveService.ExecuteAiTurnAsync(session, aiPlayerId, BroadcastUpdate);
            await SaveGameStateAsync(session);

            if (session.Engine.Winner != null)
            {
                var stakes = session.Engine.GetGameResult();
                var finalState = session.GetState();
                await _hubContext.Clients.Group(session.Id).SendAsync("GameOver", finalState);

                _logger.LogInformation(
                    "AI game {GameId} completed. Winner: {Winner} (Stakes: {Stakes})",
                    session.Id,
                    session.Engine.Winner.Name,
                    stakes);

                BackgroundTaskHelper.FireAndForget(
                    async () =>
                    {
                        await _gameRepository.UpdateGameStatusAsync(session.Id, "Completed");
                        var game = GameEngineMapper.ToGame(session);
                        await _playerStatsService.UpdateStatsAfterGameCompletionAsync(game);
                    },
                    _logger,
                    $"AiGameCompletion-{session.Id}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing AI turn in game {GameId}", session.Id);
        }
    }

    private async Task HandleMatchGameCompletion(GameSession session)
    {
        if (!session.IsMatchGame || string.IsNullOrEmpty(session.MatchId))
        {
            return;
        }

        try
        {
            var winnerColor = session.Engine.Winner?.Color;
            if (winnerColor == null)
            {
                return;
            }

            var winnerId = winnerColor == CheckerColor.White ? session.WhitePlayerId : session.RedPlayerId;
            var winType = session.Engine.DetermineWinType();
            var stakes = session.Engine.GetGameResult();

            var result = new GameResult(winnerId ?? string.Empty, winType, session.Engine.DoublingCube.Value);

            await _matchService.CompleteGameAsync(session.Id, result);

            var match = await _matchService.GetMatchAsync(session.MatchId);
            if (match != null)
            {
                await _gameStateService.BroadcastMatchUpdateAsync(match, session.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling match game completion");
        }
    }
}
