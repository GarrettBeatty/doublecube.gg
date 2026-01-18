using System.Security.Claims;
using Backgammon.Core;
using Backgammon.Server.Extensions;
using Backgammon.Server.Hubs.Interfaces;
using Backgammon.Server.Models;
using Backgammon.Server.Models.SignalR;
using Backgammon.Server.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using ServerGameStatus = Backgammon.Server.Models.GameStatus;

namespace Backgammon.Server.Hubs;

/// <summary>
/// GameHub partial class - Game Operations
/// Handles core game play, analysis mode, and doubling cube operations
/// </summary>
public partial class GameHub
{
    public async Task JoinGame(string? gameId = null)
    {
        try
        {
            var connectionId = Context.ConnectionId;
            var playerId = GetAuthenticatedUserId()!; // ! is safe - AuthenticationHubFilter ensures non-null
            var displayName = GetEffectiveDisplayNameAsync(playerId);

            _logger.LogInformation("========== JoinGame Request ==========");
            _logger.LogInformation("Connection ID: {ConnectionId}", connectionId);
            _logger.LogInformation("Player ID: {PlayerId}", playerId);
            _logger.LogInformation("Display Name (resolved): {DisplayName}", displayName ?? "null");
            _logger.LogInformation("Game ID: {GameId}", gameId ?? "null");
            _logger.LogInformation("======================================");

            if (string.IsNullOrEmpty(gameId))
            {
                await Clients.Caller.Error("Game ID is required");
                return;
            }

            await _gameService.JoinGameAsync(connectionId, playerId, displayName, gameId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining game");
            await Clients.Caller.Error(ex.Message);
        }
    }

    /// <summary>
    /// Set dice values manually (analysis mode only)
    /// </summary>
    public async Task SetDice(int die1, int die2)
    {
        try
        {
            var session = _analysisSessionManager.GetSessionByConnection(Context.ConnectionId);
            if (session == null)
            {
                await Clients.Caller.Error("Not in an analysis session");
                return;
            }

            // Validate dice values early (before acquiring lock)
            if (die1 < 1 || die1 > 6 || die2 < 1 || die2 > 6)
            {
                await Clients.Caller.Error("Dice values must be between 1 and 6");
                return;
            }

            // Acquire lock to prevent race conditions with multi-tab access
            await session.GameActionLock.WaitAsync();
            try
            {
                // Get initial dice count to detect if moves were made
                var initialDiceCount = session.Engine.Dice.GetMoves().Count;
                var currentRemainingCount = session.Engine.RemainingMoves.Count;

                // Allow setting dice if:
                // 1. No remaining moves (turn ended), OR
                // 2. All moves still available (no moves made yet)
                var noMovesLeft = currentRemainingCount == 0;
                var noMovesMadeYet = currentRemainingCount == initialDiceCount;

                if (!noMovesLeft && !noMovesMadeYet)
                {
                    await Clients.Caller.Error("End your turn or undo moves before setting new dice");
                    return;
                }

                // Start a turn with the dice (creates turn snapshot for history tracking)
                _logger.LogInformation(
                    "[SetDice] Before StartTurnWithDice - History.Turns.Count: {TurnCount}",
                    session.Engine.History.Turns.Count);

                session.Engine.StartTurnWithDice(die1, die2);
                session.UpdateActivity();

                _logger.LogInformation(
                    "[SetDice] After StartTurnWithDice - History.Turns.Count: {TurnCount}, Dice: [{Die1}, {Die2}]",
                    session.Engine.History.Turns.Count,
                    die1,
                    die2);
            }
            finally
            {
                session.GameActionLock.Release();
            }

            // Broadcast update to all connections
            await BroadcastAnalysisSessionUpdate(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting dice");
            await Clients.Caller.Error(ex.Message);
        }
    }

    /// <summary>
    /// Create a new game against an AI opponent.
    /// The human player is always White (moves first).
    /// </summary>
    public async Task CreateAiGame()
    {
        try
        {
            var connectionId = Context.ConnectionId;
            var playerId = GetAuthenticatedUserId()!; // ! is safe - AuthenticationHubFilter ensures non-null
            var displayName = GetAuthenticatedDisplayName();

            await _gameService.CreateAiGameAsync(connectionId, playerId, displayName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating AI game");
            await Clients.Caller.Error(ex.Message);
        }
    }

    /// <summary>
    /// Get list of points that have checkers that can be moved
    /// </summary>
    public async Task<List<int>> GetValidSources()
    {
        return _moveQueryService.GetValidSources(Context.ConnectionId);
    }

    /// <summary>
    /// Get list of valid destinations from a specific source point
    /// </summary>
    public async Task<List<MoveDto>> GetValidDestinations(int fromPoint)
    {
        return _moveQueryService.GetValidDestinations(Context.ConnectionId, fromPoint);
    }

    /// <summary>
    /// Roll dice to start turn (only valid when no remaining moves)
    /// </summary>
    public async Task RollDice()
    {
        try
        {
            // Check if in analysis session first
            var analysisSession = _analysisSessionManager.GetSessionByConnection(Context.ConnectionId);
            if (analysisSession != null)
            {
                await RollDiceForAnalysisSession(analysisSession);
                return;
            }

            var session = _sessionManager.GetGameByPlayer(Context.ConnectionId);
            if (session == null)
            {
                await Clients.Caller.Error("Not in a game");
                return;
            }

            var result = await _gameActionOrchestrator.RollDiceAsync(session, Context.ConnectionId);
            if (!result.Success)
            {
                await Clients.Caller.Error(result.ErrorMessage ?? "An error occurred");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rolling dice");
            await Clients.Caller.Error(ex.Message);
        }
    }

    /// <summary>
    /// Execute a move from one point to another
    /// </summary>
    public async Task MakeMove(int from, int to)
    {
        try
        {
            // Check if in analysis session first
            var analysisSession = _analysisSessionManager.GetSessionByConnection(Context.ConnectionId);
            if (analysisSession != null)
            {
                await MakeMoveForAnalysisSession(analysisSession, from, to);
                return;
            }

            var session = _sessionManager.GetGameByPlayer(Context.ConnectionId);
            if (session == null)
            {
                await Clients.Caller.Error("Not in a game");
                return;
            }

            var result = await _gameActionOrchestrator.MakeMoveAsync(session, Context.ConnectionId, from, to);
            if (!result.Success)
            {
                await Clients.Caller.Error(result.ErrorMessage ?? "An error occurred");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making move");
            await Clients.Caller.Error(ex.Message);
        }
    }

    /// <summary>
    /// Execute a combined move (using 2+ dice) atomically through intermediate points.
    /// Either all moves succeed or none are applied.
    /// </summary>
    /// <param name="from">Starting point</param>
    /// <param name="to">Final destination point</param>
    /// <param name="intermediatePoints">Points the checker passes through</param>
    public async Task MakeCombinedMove(int from, int to, int[] intermediatePoints)
    {
        try
        {
            // Check if in analysis session first
            var analysisSession = _analysisSessionManager.GetSessionByConnection(Context.ConnectionId);
            if (analysisSession != null)
            {
                await MakeCombinedMoveForAnalysisSession(analysisSession, from, to, intermediatePoints);
                return;
            }

            var session = _sessionManager.GetGameByPlayer(Context.ConnectionId);
            if (session == null)
            {
                await Clients.Caller.Error("Not in a game");
                return;
            }

            var result = await _gameActionOrchestrator.MakeCombinedMoveAsync(
                session,
                Context.ConnectionId,
                from,
                to,
                intermediatePoints);

            if (!result.Success)
            {
                await Clients.Caller.Error(result.ErrorMessage ?? "An error occurred");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making combined move");
            await Clients.Caller.Error(ex.Message);
        }
    }

    /// <summary>
    /// End current turn and switch to opponent
    /// </summary>
    public async Task EndTurn()
    {
        try
        {
            // Check if in analysis session first
            var analysisSession = _analysisSessionManager.GetSessionByConnection(Context.ConnectionId);
            if (analysisSession != null)
            {
                await EndTurnForAnalysisSession(analysisSession);
                return;
            }

            var session = _sessionManager.GetGameByPlayer(Context.ConnectionId);
            if (session == null)
            {
                await Clients.Caller.Error("Not in a game");
                return;
            }

            var result = await _gameActionOrchestrator.EndTurnAsync(session, Context.ConnectionId);
            if (!result.Success)
            {
                await Clients.Caller.Error(result.ErrorMessage ?? "An error occurred");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending turn");
            await Clients.Caller.Error(ex.Message);
        }
    }

    /// <summary>
    /// Undo the last move made during the current turn
    /// </summary>
    public async Task UndoLastMove()
    {
        try
        {
            // Check if in analysis session first
            var analysisSession = _analysisSessionManager.GetSessionByConnection(Context.ConnectionId);
            if (analysisSession != null)
            {
                await UndoLastMoveForAnalysisSession(analysisSession);
                return;
            }

            var session = _sessionManager.GetGameByPlayer(Context.ConnectionId);
            if (session == null)
            {
                await Clients.Caller.Error("Not in a game");
                return;
            }

            var result = await _gameActionOrchestrator.UndoLastMoveAsync(session, Context.ConnectionId);
            if (!result.Success)
            {
                await Clients.Caller.Error(result.ErrorMessage ?? "An error occurred");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error undoing move");
            await Clients.Caller.Error(ex.Message);
        }
    }

    /// <summary>
    /// Offer to double the stakes to the opponent
    /// </summary>
    public async Task OfferDouble()
    {
        try
        {
            var session = _sessionManager.GetGameByPlayer(Context.ConnectionId);
            if (session == null)
            {
                await Clients.Caller.Error("Not in a game");
                return;
            }

            var (success, currentValue, newValue, error) = await _doubleOfferService.OfferDoubleAsync(session, Context.ConnectionId);
            if (!success)
            {
                await Clients.Caller.Error(error ?? "Failed to offer double");
                return;
            }

            // Notify opponent of the double offer
            var opponentConnections = session.GetPlayerColor(Context.ConnectionId) == CheckerColor.White
                ? session.RedConnections
                : session.WhiteConnections;

            if (opponentConnections.Any(c => !string.IsNullOrEmpty(c)))
            {
                await _gameService.BroadcastDoubleOfferAsync(session, Context.ConnectionId, currentValue, newValue);
                // Send full game state to both players so modals display correctly
                await _gameService.BroadcastGameUpdateAsync(session);
            }
            else
            {
                // Opponent might be an AI (empty connection ID)
                var opponentPlayerId = session.GetPlayerColor(Context.ConnectionId) == CheckerColor.White
                    ? session.RedPlayerId
                    : session.WhitePlayerId;

                if (opponentPlayerId != null && _aiMoveService.IsAiPlayer(opponentPlayerId))
                {
                    var (accepted, winner, stakes) = await _doubleOfferService.HandleAiDoubleResponseAsync(
                        session, opponentPlayerId, currentValue, newValue);

                    if (accepted)
                    {
                        // AI accepted - send updated state to human player
                        if (!string.IsNullOrEmpty(Context.ConnectionId))
                        {
                            var state = session.GetState(Context.ConnectionId);
                            await Clients.Caller.DoubleAccepted(state);
                        }

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
                        await Clients.Caller.Info("Computer declined the double. You win!");

                        // Update database and stats BEFORE broadcasting GameOver (prevents race condition)
                        await _gameRepository.UpdateGameStatusAsync(session.Id, "Completed");

                        if (session.GameMode.ShouldTrackStats)
                        {
                            var game = GameEngineMapper.ToGame(session);
                            await _playerStatsService.UpdateStatsAfterGameCompletionAsync(game);
                        }

                        _logger.LogInformation("Updated game {GameId} to Completed status and user stats", session.Id);

                        // Broadcast GameOver AFTER database is updated
                        if (!string.IsNullOrEmpty(Context.ConnectionId))
                        {
                            var finalState = session.GetState(Context.ConnectionId);
                            await Clients.Caller.GameOver(finalState);
                        }

                        _sessionManager.RemoveGame(session.Id);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error offering double");
            await Clients.Caller.Error(ex.Message);
        }
    }

    /// <summary>
    /// Accept a double offer from the opponent
    /// </summary>
    public async Task AcceptDouble()
    {
        try
        {
            var session = _sessionManager.GetGameByPlayer(Context.ConnectionId);
            if (session == null)
            {
                await Clients.Caller.Error("Not in a game");
                return;
            }

            // Determine who offered the double (the current player - the one whose turn it is)
            var doublingPlayerId = session.Engine.CurrentPlayer?.Color == CheckerColor.White
                ? session.WhitePlayerId
                : session.RedPlayerId;

            // Accept the double
            await _doubleOfferService.AcceptDoubleAsync(session);

            // Broadcast double accepted to both players
            await _gameService.BroadcastDoubleAcceptedAsync(session);

            // Save game state
            BackgroundTaskHelper.FireAndForget(
                async () =>
                {
                    var game = GameEngineMapper.ToGame(session);
                    await _gameRepository.SaveGameAsync(game);
                },
                _logger,
                $"SaveGameState-{session.Id}");

            // If the doubling player was an AI, resume their turn (they need to roll and move)
            if (_aiMoveService.IsAiPlayer(doublingPlayerId))
            {
                _logger.LogInformation(
                    "Human accepted AI double in game {GameId} - resuming AI turn",
                    session.Id);

                // Execute AI turn in background (roll dice and make moves)
                BackgroundTaskHelper.FireAndForget(
                    async () => await _gameActionOrchestrator.ExecuteAiTurnWithBroadcastAsync(session, doublingPlayerId!),
                    _logger,
                    $"ResumeAiTurn-{session.Id}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting double");
            await Clients.Caller.Error(ex.Message);
        }
    }

    /// <summary>
    /// Decline a double offer (opponent wins at current stakes)
    /// </summary>
    public async Task DeclineDouble()
    {
        try
        {
            var session = _sessionManager.GetGameByPlayer(Context.ConnectionId);
            if (session == null)
            {
                await Clients.Caller.Error("Not in a game");
                return;
            }

            var (success, winner, stakes, error) = await _doubleOfferService.DeclineDoubleAsync(session, Context.ConnectionId);
            if (!success)
            {
                await Clients.Caller.Error(error ?? "Failed to decline double");
                return;
            }

            // Update database and stats BEFORE broadcasting GameOver (prevents race condition)
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

            // Broadcast game over AFTER database is updated
            await _gameService.BroadcastGameOverAsync(session);

            // Remove from memory to prevent memory leak
            _sessionManager.RemoveGame(session.Id);
            _logger.LogInformation("Removed completed game {GameId} from memory (declined double)", session.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error declining double");
            await Clients.Caller.Error(ex.Message);
        }
    }

    /// <summary>
    /// Abandon the current game. The opponent wins automatically.
    /// </summary>
    public async Task AbandonGame()
    {
        try
        {
            var session = _sessionManager.GetGameByPlayer(Context.ConnectionId);
            if (session == null)
            {
                await Clients.Caller.Error("Not in a game");
                return;
            }

            // Determine abandoning player and opponent
            var abandoningColor = session.GetPlayerColor(Context.ConnectionId);
            if (abandoningColor == null)
            {
                await Clients.Caller.Error("You are not a player in this game");
                return;
            }

            // Check if game is still waiting for opponent
            var currentState = session.GetState();
            var isWaitingForPlayer = currentState.Status == ServerGameStatus.WaitingForPlayer;

            if (isWaitingForPlayer)
            {
                // No opponent yet - just cancel the game
                var gameId = session.Id;

                // Remove player from group
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId);

                // Remove game completely from session manager (no DB update needed - game was never persisted)
                _sessionManager.RemoveGame(gameId);

                _logger.LogInformation("Game {GameId} cancelled by player while waiting for opponent (removed from memory)", gameId);

                return;
            }

            var abandoningPlayer = abandoningColor == CheckerColor.White
                ? session.Engine.WhitePlayer
                : session.Engine.RedPlayer;
            var opponentPlayer = abandoningColor == CheckerColor.White
                ? session.Engine.RedPlayer
                : session.Engine.WhitePlayer;

            // Check if game is already over
            if (session.Engine.GameOver)
            {
                _logger.LogWarning("Game {GameId} is already over, cannot forfeit", session.Id);
                await Clients.Caller.Error("Game is already finished");
                return;
            }

            // Forfeit the game - opponent wins
            session.Engine.ForfeitGame(opponentPlayer);

            // Get stakes from doubling cube and determine win type
            var stakes = session.Engine.GetGameResult();

            _logger.LogInformation(
                "Game {GameId} forfeited by {Player}. Winner: {Winner} (Stakes: {Stakes})",
                session.Id,
                abandoningPlayer.Name,
                opponentPlayer.Name,
                stakes);

            // Save complete game state (including Winner, WinType, Stakes)
            var completedGame = GameEngineMapper.ToGame(session);
            await _gameRepository.SaveGameAsync(completedGame);

            // Update match scores if this is a match game
            if (!string.IsNullOrEmpty(session.MatchId))
            {
                var winnerPlayerId = abandoningColor == CheckerColor.White ? session.RedPlayerId : session.WhitePlayerId;
                var winnerColor = abandoningColor == CheckerColor.White ? CheckerColor.Red : CheckerColor.White;

                var gameResult = new GameResult(winnerPlayerId!, session.Engine.DetermineWinType(), session.Engine.DoublingCube.Value)
                {
                    WinnerColor = winnerColor,
                    MoveHistory = session.Engine.MoveHistory.ToList()
                };

                await _matchService.CompleteGameAsync(session.Id, gameResult);
                _logger.LogInformation(
                    "Updated match {MatchId} scores after game {GameId} was forfeited",
                    session.MatchId,
                    session.Id);
            }

            // Skip stats update for non-competitive games
            if (session.GameMode.ShouldTrackStats)
            {
                var game = GameEngineMapper.ToGame(session);
                await _playerStatsService.UpdateStatsAfterGameCompletionAsync(game);
            }
            else
            {
                _logger.LogInformation("Skipping stats tracking for analysis game {GameId}", session.Id);
            }

            // Mark session as completed so status is correctly set in GetState()
            // Note: ForfeitGame already sets Engine.Winner and Engine.GameOver
            session.MarkCompleted();

            _logger.LogInformation("Game {GameId} completed (forfeited)", session.Id);

            // Broadcast game over AFTER database is updated
            var finalState = session.GetState();
            await Clients.Group(session.Id).GameOver(finalState);

            // Handle match continuation (if this is a match game)
            if (!string.IsNullOrEmpty(session.MatchId))
            {
                var match = await _matchService.GetMatchAsync(session.MatchId);
                if (match != null)
                {
                    // Broadcast match score update - next game will be created when players click "Continue"
                    await Clients.Group(session.Id).MatchUpdate(new MatchUpdateDto
                    {
                        MatchId = match.MatchId,
                        Player1Score = match.Player1Score,
                        Player2Score = match.Player2Score,
                        TargetScore = match.TargetScore,
                        IsCrawfordGame = match.IsCrawfordGame,
                        MatchComplete = match.Status == "Completed",
                        MatchWinner = match.WinnerId,
                        NextGameId = match.CurrentGameId // Current game for reference
                    });

                    if (!match.CoreMatch.IsMatchComplete())
                    {
                        _logger.LogInformation(
                            "Match {MatchId} continues after abandoned game. Score: {P1Score}-{P2Score}. Waiting for players to continue.",
                            match.MatchId,
                            match.Player1Score,
                            match.Player2Score);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Match {MatchId} complete after abandoned game. Winner: {WinnerId}",
                            match.MatchId,
                            match.WinnerId);
                    }

                    _logger.LogInformation(
                        "Broadcasted match update for match {MatchId}: {P1Score}-{P2Score}",
                        match.MatchId,
                        match.Player1Score,
                        match.Player2Score);

                    // Keep completed match game in memory for continuation (will be cleaned up when next game starts)
                    _logger.LogInformation(
                        "Keeping abandoned match game {GameId} in memory for continuation",
                        session.Id);
                }
            }
            else
            {
                // Remove from memory to prevent memory leak (only for non-match games)
                _sessionManager.RemoveGame(session.Id);
                _logger.LogInformation("Removed abandoned game {GameId} from memory", session.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error abandoning game");
            await Clients.Caller.Error(ex.Message);
        }
    }

    /// <summary>
    /// Get current game state
    /// </summary>
    public async Task GetGameState()
    {
        try
        {
            var session = _sessionManager.GetGameByPlayer(Context.ConnectionId);
            if (session == null)
            {
                await Clients.Caller.Error("Not in a game");
                return;
            }

            var state = session.GetState(Context.ConnectionId);
            await Clients.Caller.GameUpdate(state);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting game state");
            await Clients.Caller.Error(ex.Message);
        }
    }

    /// <summary>
    /// Leave current game
    /// </summary>
    public async Task LeaveGame()
    {
        await HandleDisconnection(Context.ConnectionId);
    }

    /// <summary>
    /// Handle player disconnection
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Remove from player connections tracking
        // Note: User might disconnect before authenticating, so null check is needed here
        var playerId = GetAuthenticatedUserId();
        if (!string.IsNullOrEmpty(playerId))
        {
            _playerConnectionService.RemoveConnection(playerId);
        }

        // Clean up chat rate limit history
        _chatService.CleanupConnection(Context.ConnectionId);

        await HandleDisconnection(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Export the current position (base64-encoded SGF - used for URLs).
    /// Works for both analysis sessions and game sessions.
    /// </summary>
    public Task<string> ExportPosition()
    {
        // Check analysis session first
        var analysisSession = _analysisSessionManager.GetSessionByConnection(Context.ConnectionId);
        if (analysisSession != null)
        {
            var sgf = SgfSerializer.ExportPosition(analysisSession.Engine);
            var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(sgf));
            return Task.FromResult(base64);
        }

        // Check game session
        var gameSession = _sessionManager.GetGameByPlayer(Context.ConnectionId);
        if (gameSession != null)
        {
            var sgf = SgfSerializer.ExportPosition(gameSession.Engine);
            var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(sgf));
            return Task.FromResult(base64);
        }

        return Task.FromResult(string.Empty);
    }

    /// <summary>
    /// Export full game SGF with move history.
    /// Works for both analysis sessions and game sessions.
    /// </summary>
    public Task<string> ExportGameSgf()
    {
        // Check analysis session first
        var analysisSession = _analysisSessionManager.GetSessionByConnection(Context.ConnectionId);
        if (analysisSession != null)
        {
            return Task.FromResult(analysisSession.Engine.GameSgf);
        }

        // Check game session
        var gameSession = _sessionManager.GetGameByPlayer(Context.ConnectionId);
        if (gameSession != null)
        {
            return Task.FromResult(gameSession.Engine.GameSgf);
        }

        return Task.FromResult(string.Empty);
    }

    /// <summary>
    /// Import a position (auto-detects raw SGF or base64-encoded SGF)
    /// </summary>
    public async Task ImportPosition(string positionData)
    {
        var session = _analysisSessionManager.GetSessionByConnection(Context.ConnectionId);
        if (session == null)
        {
            await Clients.Caller.Error("Not in an analysis session");
            return;
        }

        try
        {
            await session.GameActionLock.WaitAsync();
            try
            {
                // Auto-detect format: base64-encoded or raw SGF
                string sgf;
                if (positionData.StartsWith("(;"))
                {
                    sgf = positionData;
                }
                else
                {
                    // Try base64 decode
                    var bytes = Convert.FromBase64String(positionData);
                    sgf = System.Text.Encoding.UTF8.GetString(bytes);
                }

                // Apply the position to the engine
                SgfSerializer.ImportPosition(session.Engine, sgf);
                session.UpdateActivity();
            }
            finally
            {
                session.GameActionLock.Release();
            }

            await BroadcastAnalysisSessionUpdate(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing position");
            await Clients.Caller.Error("Failed to import position: invalid format");
        }
    }

    /// <summary>
    /// Move a checker directly from one point to another in analysis mode (bypasses game rules)
    /// </summary>
    public async Task MoveCheckerDirectly(int from, int to)
    {
        var session = _analysisSessionManager.GetSessionByConnection(Context.ConnectionId);
        if (session == null)
        {
            await Clients.Caller.Error("Not in an analysis session");
            return;
        }

        // Validate basic constraints
        if (!IsValidDirectMove(session.Engine, from, to))
        {
            await Clients.Caller.Error("Invalid move: check piece placement rules");
            return;
        }

        try
        {
            await session.GameActionLock.WaitAsync();
            try
            {
                // Execute move bypassing game rules
                ExecuteDirectMove(session.Engine, from, to);
                session.UpdateActivity();
            }
            finally
            {
                session.GameActionLock.Release();
            }

            // Broadcast update to all connections
            await BroadcastAnalysisSessionUpdate(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing direct move in analysis mode");
            await Clients.Caller.Error("Failed to move checker");
        }
    }

    /// <summary>
    /// Set the current player in analysis mode
    /// </summary>
    public async Task SetCurrentPlayer(CheckerColor color)
    {
        var session = _analysisSessionManager.GetSessionByConnection(Context.ConnectionId);
        if (session == null)
        {
            await Clients.Caller.Error("Not in an analysis session");
            return;
        }

        try
        {
            await session.GameActionLock.WaitAsync();
            try
            {
                // Update current player
                session.Engine.SetCurrentPlayer(color);

                // Clear remaining moves (reset turn state)
                session.Engine.RemainingMoves.Clear();
                session.UpdateActivity();
            }
            finally
            {
                session.GameActionLock.Release();
            }

            await BroadcastAnalysisSessionUpdate(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting current player in analysis mode");
            await Clients.Caller.Error("Failed to set current player");
        }
    }

    // ============================================
    // Analysis Operations
    // ============================================

    public async Task<PositionEvaluationDto> AnalyzePosition(string sessionId, string? evaluatorType)
    {
        var session = _analysisSessionManager.GetSession(sessionId);
        if (session == null)
        {
            throw new HubException("Analysis session not found");
        }

        return await _analysisService.EvaluatePositionAsync(session.Engine, evaluatorType);
    }

    /// <summary>
    /// Find the best moves for the current position
    /// </summary>
    /// <param name="sessionId">The analysis session ID to analyze</param>
    /// <param name="evaluatorType">Optional evaluator type ("Heuristic" or "Gnubg"). If null, uses default from settings.</param>
    public async Task<BestMovesAnalysisDto> FindBestMoves(string sessionId, string? evaluatorType)
    {
        var session = _analysisSessionManager.GetSession(sessionId);
        if (session == null)
        {
            throw new HubException("Analysis session not found");
        }

        if (session.Engine.RemainingMoves.Count == 0)
        {
            throw new HubException("No dice rolled - cannot analyze moves");
        }

        return await _analysisService.FindBestMovesAsync(session.Engine, evaluatorType);
    }

    /// <summary>
    /// Get turn-by-turn history for a completed game for analysis board replay
    /// </summary>
    /// <param name="gameId">The game ID to retrieve history for</param>
    /// <returns>Game history with turn snapshots, or null if game not found</returns>
    public async Task<GameHistoryDto?> GetGameHistory(string gameId)
    {
        var game = await _gameRepository.GetGameByGameIdAsync(gameId);
        if (game == null)
        {
            return null;
        }

        // Parse turns from SGF game record
        var turnHistory = new List<TurnSnapshotDto>();
        if (!string.IsNullOrEmpty(game.GameSgf))
        {
            var gameRecord = SgfSerializer.ParseGameSgf(game.GameSgf);
            turnHistory = gameRecord.Turns.Select(TurnSnapshotDto.FromCore).ToList();
        }

        return new GameHistoryDto
        {
            GameId = game.GameId,
            MatchId = game.MatchId,
            TurnHistory = turnHistory,
            WhitePlayerName = game.WhitePlayerName,
            RedPlayerName = game.RedPlayerName,
            Winner = game.Winner,
            WinType = game.WinType,
            CreatedAt = game.CreatedAt,
            CompletedAt = game.CompletedAt,
            DoublingCubeValue = game.DoublingCubeValue
        };
    }

    /// <summary>
    /// Parse a full game SGF into turn history for replay.
    /// This allows analyzing games from SGF strings without needing a gameId.
    /// </summary>
    /// <param name="sgf">The full game SGF string</param>
    /// <returns>Parsed game history with turn snapshots, or null if invalid</returns>
    public Task<GameHistoryDto?> ParseGameSgf(string sgf)
    {
        if (string.IsNullOrWhiteSpace(sgf))
        {
            return Task.FromResult<GameHistoryDto?>(null);
        }

        try
        {
            var gameRecord = SgfSerializer.ParseGameSgf(sgf);

            var turnHistory = gameRecord.Turns
                .Select(TurnSnapshotDto.FromCore)
                .ToList();

            var result = new GameHistoryDto
            {
                GameId = string.Empty,
                TurnHistory = turnHistory,
                WhitePlayerName = gameRecord.WhitePlayer,
                RedPlayerName = gameRecord.BlackPlayer,
                Winner = gameRecord.Winner?.ToString(),
                WinType = gameRecord.WinType?.ToString(),
                CreatedAt = DateTime.UtcNow,
                DoublingCubeValue = 1
            };

            return Task.FromResult<GameHistoryDto?>(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse game SGF");
            return Task.FromResult<GameHistoryDto?>(null);
        }
    }

    // ==================== Correspondence Game Methods ====================

    /// <summary>
    /// Get all correspondence games for the current user
}
