using Backgammon.Core;
using Backgammon.Server.Extensions;
using Backgammon.Server.Hubs;
using Backgammon.Server.Hubs.Interfaces;
using Backgammon.Server.Models;
using Backgammon.Server.Models.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Match = Backgammon.Server.Models.Match;

namespace Backgammon.Server.Services;

/// <summary>
/// Orchestrates game actions with common patterns:
/// validation, execution, broadcasting, persistence, and AI turn triggering
/// </summary>
public class GameActionOrchestrator : IGameActionOrchestrator
{
    private readonly IGameRepository _gameRepository;
    private readonly IAiMoveService _aiMoveService;
    private readonly IGameSessionManager _sessionManager;
    private readonly IGameBroadcastService _broadcastService;
    private readonly IGameCompletionService _completionService;
    private readonly IMatchRepository _matchRepository;
    private readonly ICorrespondenceGameService _correspondenceGameService;
    private readonly IHubContext<GameHub, IGameHubClient> _hubContext;
    private readonly ILogger<GameActionOrchestrator> _logger;

    public GameActionOrchestrator(
        IGameRepository gameRepository,
        IAiMoveService aiMoveService,
        IGameSessionManager sessionManager,
        IGameBroadcastService broadcastService,
        IGameCompletionService completionService,
        IMatchRepository matchRepository,
        ICorrespondenceGameService correspondenceGameService,
        IHubContext<GameHub, IGameHubClient> hubContext,
        ILogger<GameActionOrchestrator> logger)
    {
        _gameRepository = gameRepository;
        _aiMoveService = aiMoveService;
        _sessionManager = sessionManager;
        _broadcastService = broadcastService;
        _completionService = completionService;
        _matchRepository = matchRepository;
        _correspondenceGameService = correspondenceGameService;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task<ActionResult> RollDiceAsync(GameSession session, string connectionId)
    {
        _logger.LogDebug("RollDice called by connection {ConnectionId}", connectionId);

        // Prevent actions on completed games
        if (session.Engine.Winner != null)
        {
            return ActionResult.Error("Game is already completed");
        }

        // Handle opening roll
        if (session.Engine.IsOpeningRoll)
        {
            var playerColor = session.GetPlayerColor(connectionId);

            // For AI players, determine color by checking which player ID is an AI
            if (playerColor == null && string.IsNullOrEmpty(connectionId))
            {
                if (_aiMoveService.IsAiPlayer(session.WhitePlayerId))
                {
                    playerColor = CheckerColor.White;
                }
                else if (_aiMoveService.IsAiPlayer(session.RedPlayerId))
                {
                    playerColor = CheckerColor.Red;
                }
            }

            if (playerColor == null)
            {
                return ActionResult.Error("Not a player in this game");
            }

            // Check if this player already rolled (unless there's a tie - then allow re-roll)
            if (!session.Engine.IsOpeningRollTie)
            {
                if (playerColor == CheckerColor.White && session.Engine.WhiteOpeningRoll.HasValue)
                {
                    return ActionResult.Error("You already rolled");
                }

                if (playerColor == CheckerColor.Red && session.Engine.RedOpeningRoll.HasValue)
                {
                    return ActionResult.Error("You already rolled");
                }
            }

            int roll = session.Engine.RollOpening(playerColor.Value);
            session.UpdateActivity();

            if (roll == -1)
            {
                // Tie - need to re-roll
                _logger.LogInformation(
                    "Opening roll tie in game {GameId}. Both players must re-roll.",
                    session.Id);
            }
            else
            {
                _logger.LogInformation(
                    "{Player} rolled {Roll} for opening in game {GameId}",
                    playerColor == CheckerColor.White ? "White" : "Red",
                    roll,
                    session.Id);

                // For correspondence games, after first player rolls, update turn to the other player
                if (!string.IsNullOrEmpty(session.MatchId))
                {
                    BackgroundTaskHelper.FireAndForget(
                        async () =>
                        {
                            var match = await _matchRepository.GetMatchByIdAsync(session.MatchId);
                            if (match?.IsCorrespondence == true && match.CurrentTurnPlayerId == null)
                            {
                                // Determine which player needs to roll next
                                var nextPlayerId = playerColor == CheckerColor.White ? session.RedPlayerId : session.WhitePlayerId;
                                if (!string.IsNullOrEmpty(nextPlayerId))
                                {
                                    await _correspondenceGameService.HandleTurnCompletedAsync(session.MatchId, nextPlayerId);
                                    _logger.LogInformation(
                                        "Updated correspondence turn tracking after first opening roll for match {MatchId}, waiting for: {PlayerId}",
                                        session.MatchId,
                                        nextPlayerId);
                                }
                            }
                        },
                        _logger,
                        $"CorrespondenceFirstRoll-{session.Id}");
                }

                // Check if opening roll is complete
                if (!session.Engine.IsOpeningRoll)
                {
                    _logger.LogInformation(
                        "Opening roll complete in game {GameId}. {Winner} goes first with dice [{Die1}, {Die2}]",
                        session.Id,
                        session.Engine.CurrentPlayer.Name,
                        session.Engine.Dice.Die1,
                        session.Engine.Dice.Die2);

                    // Start timer for first player's turn
                    session.Engine.StartTurnTimer();

                    // Start broadcasting time updates (only after opening roll completes)
                    session.StartTimeUpdates(_hubContext);

                    // For correspondence games, update turn tracking with the actual first player
                    var firstPlayerId = GetCurrentPlayerId(session);
                    if (!string.IsNullOrEmpty(session.MatchId) && !string.IsNullOrEmpty(firstPlayerId))
                    {
                        BackgroundTaskHelper.FireAndForget(
                            async () =>
                            {
                                var match = await _matchRepository.GetMatchByIdAsync(session.MatchId);
                                if (match?.IsCorrespondence == true)
                                {
                                    await _correspondenceGameService.HandleTurnCompletedAsync(session.MatchId, firstPlayerId);
                                    _logger.LogInformation(
                                        "Updated correspondence turn tracking after opening roll for match {MatchId}, first player: {PlayerId}",
                                        session.MatchId,
                                        firstPlayerId);
                                }
                            },
                            _logger,
                            $"CorrespondenceOpeningRoll-{session.Id}");
                    }

                    // Check if the winner is AI and should start playing
                    if (_aiMoveService.IsAiPlayer(firstPlayerId))
                    {
                        _logger.LogInformation(
                            "AI won opening roll. Triggering AI turn for player {AiPlayerId} in game {GameId}",
                            firstPlayerId,
                            session.Id);

                        // Save first, then broadcast (ensures DB has state before client sees it)
                        await SaveGameStateAsync(session);
                        await _broadcastService.BroadcastGameUpdateAsync(session);

                        BackgroundTaskHelper.FireAndForget(
                            async () =>
                            {
                                await ExecuteAiTurnWithBroadcastAsync(session, firstPlayerId!);
                            },
                            _logger,
                            $"AiOpeningTurn-{session.Id}");

                        return ActionResult.Ok();
                    }
                    else
                    {
                        // Human won opening roll - save first, then broadcast
                        await SaveGameStateAsync(session);
                        await _broadcastService.BroadcastGameUpdateAsync(session);
                    }
                }
                else if (session.Engine.IsOpeningRoll)
                {
                    // Still in opening roll - check if AI needs to roll
                    var whitePlayerId = session.WhitePlayerId;
                    var redPlayerId = session.RedPlayerId;

                    // Check if AI hasn't rolled yet
                    if (_aiMoveService.IsAiPlayer(whitePlayerId) && !session.Engine.WhiteOpeningRoll.HasValue)
                    {
                        _logger.LogInformation("AI (White) needs to roll opening die in game {GameId}", session.Id);

                        // Save first, then broadcast
                        await SaveGameStateAsync(session);
                        await _broadcastService.BroadcastGameUpdateAsync(session);

                        // Trigger AI opening roll
                        BackgroundTaskHelper.FireAndForget(
                            async () =>
                            {
                                await Task.Delay(500); // Small delay for visual effect
                                await RollDiceAsync(session, string.Empty); // Empty connection for AI
                            },
                            _logger,
                            $"AiOpeningRoll-White-{session.Id}");

                        return ActionResult.Ok();
                    }

                    if (_aiMoveService.IsAiPlayer(redPlayerId) && !session.Engine.RedOpeningRoll.HasValue)
                    {
                        _logger.LogInformation("AI (Red) needs to roll opening die in game {GameId}", session.Id);

                        // Save first, then broadcast
                        await SaveGameStateAsync(session);
                        await _broadcastService.BroadcastGameUpdateAsync(session);

                        // Trigger AI opening roll
                        BackgroundTaskHelper.FireAndForget(
                            async () =>
                            {
                                await Task.Delay(500); // Small delay for visual effect
                                await RollDiceAsync(session, string.Empty); // Empty connection for AI
                            },
                            _logger,
                            $"AiOpeningRoll-Red-{session.Id}");

                        return ActionResult.Ok();
                    }
                }
            }

            // Save first, then broadcast
            await SaveGameStateAsync(session);
            await _broadcastService.BroadcastGameUpdateAsync(session);

            return ActionResult.Ok();
        }

        // Regular dice roll
        if (session.HasPendingDoubleOffer)
        {
            _logger.LogWarning(
                "RollDice failed: Double offer pending. Connection={ConnectionId}, Game={GameId}",
                connectionId,
                session.Id);
            return ActionResult.Error("Cannot roll dice while waiting for double response");
        }

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

        // Save first, then broadcast
        await SaveGameStateAsync(session);
        await _broadcastService.BroadcastGameUpdateAsync(session);

        return ActionResult.Ok();
    }

    public async Task<ActionResult> MakeMoveAsync(GameSession session, string connectionId, int from, int to)
    {
        _logger.LogInformation(
            "MakeMove request: Game={GameId}, Connection={ConnectionId}, From={From}, To={To}",
            session.Id,
            connectionId,
            from,
            to);

        // Prevent moves on completed games
        if (session.Engine.Winner != null)
        {
            _logger.LogWarning(
                "Move rejected: Game already completed. Game={GameId}, Winner={Winner}",
                session.Id,
                session.Engine.Winner.Color);
            return ActionResult.Error("Game is already completed");
        }

        // Prevent moves while double is pending
        if (session.HasPendingDoubleOffer)
        {
            _logger.LogWarning(
                "Move rejected: Double offer pending. Game={GameId}",
                session.Id);
            return ActionResult.Error("Cannot move while waiting for double response");
        }

        if (!session.IsPlayerTurn(connectionId))
        {
            _logger.LogWarning(
                "Move rejected: Not player's turn. Game={GameId}, Connection={ConnectionId}",
                session.Id,
                connectionId);
            return ActionResult.Error("Not your turn");
        }

        // Check for timeout before allowing move
        if (session.Engine.HasCurrentPlayerTimedOut())
        {
            return ActionResult.Error("You have run out of time");
        }

        // Acquire lock to prevent race conditions with multi-tab access
        await session.GameActionLock.WaitAsync();
        try
        {
            _logger.LogDebug(
                "Current player: {Player}, Remaining moves: {Moves}, Dice: [{Die1},{Die2}]",
                session.Engine.CurrentPlayer.Name,
                string.Join(",", session.Engine.RemainingMoves),
                session.Engine.Dice.Die1,
                session.Engine.Dice.Die2);

            // Find the correct move from valid moves (includes both single and combined moves)
            var validMoves = session.Engine.GetValidMoves(includeCombined: true);
            var matchingMove = validMoves.FirstOrDefault(m => m.From == from && m.To == to);

            if (matchingMove == null)
            {
                // Security: Log detailed info about rejected move attempt
                _logger.LogWarning(
                    "Move rejected: No valid move. Game={GameId}, From={From}, To={To}, ValidMoves=[{ValidMoves}]",
                    session.Id,
                    from,
                    to,
                    string.Join("; ", validMoves.Select(m => $"{m.From}->{m.To}{(m.IsCombined ? "*" : string.Empty)}")));
                return ActionResult.Error("Invalid move - no matching valid move");
            }

            if (matchingMove.IsCombined)
            {
                _logger.LogDebug(
                    "Found combined move: {From}->{To}, DiceUsed=[{DiceUsed}]",
                    from,
                    to,
                    string.Join(",", matchingMove.DiceUsed ?? Array.Empty<int>()));
            }
            else
            {
                _logger.LogDebug("Found valid move with die value: {DieValue}", matchingMove.DieValue);
            }

            if (!session.Engine.ExecuteMove(matchingMove))
            {
                _logger.LogWarning(
                    "Move rejected: ExecuteMove returned false. Game={GameId}, Move={From}->{To}",
                    session.Id,
                    from,
                    to);
                return ActionResult.Error("Invalid move");
            }

            _logger.LogInformation(
                "Move executed: Game={GameId}, Move={From}->{To}, DieUsed={Die}{Combined}",
                session.Id,
                from,
                to,
                matchingMove.DieValue,
                matchingMove.IsCombined ? " (combined)" : string.Empty);

            session.UpdateActivity();
        }
        finally
        {
            session.GameActionLock.Release();
        }

        // Save first, then broadcast
        await SaveGameStateAsync(session);
        await _broadcastService.BroadcastGameUpdateAsync(session);

        // Check if game is over
        if (session.Engine.Winner != null)
        {
            await _completionService.HandleGameCompletionAsync(session);
            return ActionResult.GameOver();
        }

        return ActionResult.Ok();
    }

    public async Task<ActionResult> MakeCombinedMoveAsync(
        GameSession session,
        string connectionId,
        int from,
        int to,
        int[] intermediatePoints)
    {
        _logger.LogInformation(
            "MakeCombinedMove request: Game={GameId}, Connection={ConnectionId}, From={From}, To={To}, Intermediates=[{Intermediates}]",
            session.Id,
            connectionId,
            from,
            to,
            string.Join(",", intermediatePoints));

        // Prevent moves on completed games
        if (session.Engine.Winner != null)
        {
            _logger.LogWarning(
                "Combined move rejected: Game already completed. Game={GameId}",
                session.Id);
            return ActionResult.Error("Game is already completed");
        }

        // Prevent moves while double is pending
        if (session.HasPendingDoubleOffer)
        {
            _logger.LogWarning(
                "Combined move rejected: Double offer pending. Game={GameId}",
                session.Id);
            return ActionResult.Error("Cannot move while waiting for double response");
        }

        if (!session.IsPlayerTurn(connectionId))
        {
            _logger.LogWarning(
                "Combined move rejected: Not player's turn. Game={GameId}",
                session.Id);
            return ActionResult.Error("Not your turn");
        }

        if (session.Engine.HasCurrentPlayerTimedOut())
        {
            return ActionResult.Error("You have run out of time");
        }

        // Build full path: from -> intermediate[0] -> intermediate[1] -> ... -> to
        var fullPath = new List<int> { from };
        fullPath.AddRange(intermediatePoints);
        fullPath.Add(to);

        // Acquire lock for the entire combined move operation
        await session.GameActionLock.WaitAsync();
        int movesExecuted = 0;

        try
        {
            // Execute each step in the path
            for (int i = 0; i < fullPath.Count - 1; i++)
            {
                int stepFrom = fullPath[i];
                int stepTo = fullPath[i + 1];

                var validMoves = session.Engine.GetValidMoves();
                var matchingMove = validMoves.FirstOrDefault(m => m.From == stepFrom && m.To == stepTo);

                if (matchingMove == null)
                {
                    _logger.LogWarning(
                        "Combined move failed at step {Step}: No valid move {From}->{To}. Game={GameId}",
                        i + 1,
                        stepFrom,
                        stepTo,
                        session.Id);

                    // Rollback all previously executed moves
                    for (int j = 0; j < movesExecuted; j++)
                    {
                        session.Engine.UndoLastMove();
                    }

                    return ActionResult.Error($"Invalid combined move - step {stepFrom}->{stepTo} not valid");
                }

                if (!session.Engine.IsValidMove(matchingMove))
                {
                    _logger.LogWarning(
                        "Combined move failed at step {Step}: IsValidMove returned false. Game={GameId}",
                        i + 1,
                        session.Id);

                    // Rollback
                    for (int j = 0; j < movesExecuted; j++)
                    {
                        session.Engine.UndoLastMove();
                    }

                    return ActionResult.Error($"Invalid combined move - step {stepFrom}->{stepTo} validation failed");
                }

                if (!session.Engine.ExecuteMove(matchingMove))
                {
                    _logger.LogWarning(
                        "Combined move failed at step {Step}: ExecuteMove returned false. Game={GameId}",
                        i + 1,
                        session.Id);

                    // Rollback
                    for (int j = 0; j < movesExecuted; j++)
                    {
                        session.Engine.UndoLastMove();
                    }

                    return ActionResult.Error($"Invalid combined move - step {stepFrom}->{stepTo} execution failed");
                }

                movesExecuted++;
                _logger.LogDebug(
                    "Combined move step {Step} executed: {From}->{To}, DieUsed={Die}",
                    i + 1,
                    stepFrom,
                    stepTo,
                    matchingMove.DieValue);
            }

            _logger.LogInformation(
                "Combined move completed: Game={GameId}, Path=[{Path}], TotalMoves={Moves}",
                session.Id,
                string.Join("->", fullPath),
                movesExecuted);

            session.UpdateActivity();
        }
        finally
        {
            session.GameActionLock.Release();
        }

        // Save first, then broadcast
        await SaveGameStateAsync(session);
        await _broadcastService.BroadcastGameUpdateAsync(session);

        // Check if game is over (same logic as MakeMoveAsync)
        if (session.Engine.Winner != null)
        {
            await _completionService.HandleGameCompletionAsync(session);
            return ActionResult.GameOver();
        }

        return ActionResult.Ok();
    }

    public async Task<ActionResult> EndTurnAsync(GameSession session, string connectionId)
    {
        // Prevent actions on completed games
        if (session.Engine.Winner != null)
        {
            return ActionResult.Error("Game is already completed");
        }

        // Prevent end turn while double is pending
        if (session.HasPendingDoubleOffer)
        {
            return ActionResult.Error("Cannot end turn while waiting for double response");
        }

        if (!session.IsPlayerTurn(connectionId))
        {
            return ActionResult.Error("Not your turn");
        }

        // Check for timeout
        if (session.Engine.HasCurrentPlayerTimedOut())
        {
            return ActionResult.Error("You have run out of time");
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

        // End turn timer before switching turns
        session.Engine.EndTurnTimer();

        session.Engine.EndTurn();

        // Start timer for next player's turn
        session.Engine.StartTurnTimer();

        session.UpdateActivity();

        // Save first, then broadcast
        await SaveGameStateAsync(session);
        await _broadcastService.BroadcastGameUpdateAsync(session);

        _logger.LogInformation(
            "Turn ended in game {GameId}. Current player: {Player}",
            session.Id,
            session.Engine.CurrentPlayer?.Color.ToString() ?? "Unknown");

        // Handle correspondence game turn tracking
        if (!string.IsNullOrEmpty(session.MatchId))
        {
            BackgroundTaskHelper.FireAndForget(
                async () =>
                {
                    var match = await _matchRepository.GetMatchByIdAsync(session.MatchId);
                    if (match?.IsCorrespondence == true)
                    {
                        var nextPlayerId = GetCurrentPlayerId(session);
                        if (!string.IsNullOrEmpty(nextPlayerId))
                        {
                            await _correspondenceGameService.HandleTurnCompletedAsync(session.MatchId, nextPlayerId);
                            _logger.LogInformation(
                                "Updated correspondence turn deadline for match {MatchId}, next player: {PlayerId}",
                                session.MatchId,
                                nextPlayerId);
                        }
                    }
                },
                _logger,
                $"CorrespondenceTurn-{session.Id}");
        }

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
        // Prevent actions on completed games
        if (session.Engine.Winner != null)
        {
            return ActionResult.Error("Game is already completed");
        }

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

        // Save first, then broadcast
        await SaveGameStateAsync(session);
        await _broadcastService.BroadcastGameUpdateAsync(session);

        _logger.LogInformation(
            "Player {ConnectionId} undid last move in game {GameId}",
            connectionId,
            session.Id);

        return ActionResult.Ok();
    }

    public async Task ExecuteAiTurnWithBroadcastAsync(GameSession session, string aiPlayerId)
    {
        try
        {
            // Use broadcast service for updates
            async Task BroadcastUpdate() => await _broadcastService.BroadcastGameUpdateAsync(session);

            // Callback to notify human opponent when AI offers a double
            async Task NotifyDoubleOffered(int currentValue, int newValue)
            {
                // Set pending double offer on the session so game state reflects it
                session.SetPendingDoubleOffer(aiPlayerId);

                // Determine human opponent's connections (the non-AI player)
                var humanConnections = session.WhitePlayerId == aiPlayerId
                    ? session.RedConnections
                    : session.WhiteConnections;

                foreach (var connectionId in humanConnections)
                {
                    await _hubContext.Clients.Client(connectionId).DoubleOffered(
                        new Models.SignalR.DoubleOfferDto
                        {
                            CurrentStakes = currentValue,
                            NewStakes = newValue
                        });
                }

                // Send full game state so modals display correctly (matches human double flow)
                await BroadcastUpdate();

                _logger.LogInformation(
                    "AI {AiPlayerId} offered double to {HumanConnectionCount} human connection(s) in game {GameId}",
                    aiPlayerId,
                    humanConnections.Count,
                    session.Id);
            }

            var aiOfferedDouble = await _aiMoveService.ExecuteAiTurnAsync(
                session,
                aiPlayerId,
                BroadcastUpdate,
                NotifyDoubleOffered);

            await SaveGameStateAsync(session);

            // If AI offered a double, don't check for completion yet - wait for human response
            if (aiOfferedDouble)
            {
                _logger.LogInformation(
                    "AI turn paused in game {GameId} - waiting for human response to double",
                    session.Id);
                return;
            }

            // Handle game completion if there's a winner
            if (session.Engine.Winner != null)
            {
                await _completionService.HandleGameCompletionAsync(session);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing AI turn in game {GameId}", session.Id);
        }
    }

    private async Task SaveGameStateAsync(GameSession session)
    {
        // Skip saving for analysis mode
        if (!session.GameMode.ShouldPersist)
        {
            _logger.LogDebug("Skipping save for analysis game {GameId}", session.Id);
            return;
        }

        // Let exceptions propagate - callers should handle failures
        var game = GameEngineMapper.ToGame(session);
        await _gameRepository.SaveGameAsync(game);
        _logger.LogDebug("Saved game state for {GameId}, Status={Status}", session.Id, game.Status);
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
}
