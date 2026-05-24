using System.Security.Claims;
using Backgammon.Core;
using Backgammon.Server.Grains.Interfaces;
using Backgammon.Server.Hubs.Interfaces;
using Backgammon.Server.Models;
using Backgammon.Server.Models.SignalR;
using Backgammon.Server.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Backgammon.Server.Hubs;

/// <summary>
/// GameHub partial class - Game Operations
/// Handles core game play, analysis mode, and doubling cube operations
/// </summary>
public partial class GameHub
{
    /// <summary>
    /// Join or reconnect to a game by ID.
    /// </summary>
    public async Task JoinGame(string? gameId = null)
    {
        try
        {
            var connectionId = Context.ConnectionId;
            var playerId = GetAuthenticatedUserId()!;
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

            // Add to SignalR group so this connection receives group broadcasts
            await Groups.AddToGroupAsync(connectionId, gameId);

            var grain = _grainFactory.GetGrain<IGameGrain>(gameId);
            var state = await grain.JoinAsync(playerId, connectionId, displayName);

            if (state != null)
            {
                await Clients.Caller.GameUpdate(state);
            }
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

                session.Engine.StartTurnWithDice(die1, die2);
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
            _logger.LogError(ex, "Error setting dice");
            await Clients.Caller.Error(ex.Message);
        }
    }

    /// <summary>
    /// Create a new game against an AI opponent.
    /// Delegates to CreateMatch with opponentType "AI".
    /// </summary>
    public async Task CreateAiGame()
    {
        try
        {
            var config = new MatchConfig
            {
                TargetScore = 1,
                OpponentType = "AI",
                DisplayName = GetAuthenticatedDisplayName(),
                TimeControlType = "None",
                IsRated = false
            };

            await CreateMatch(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating AI game");
            await Clients.Caller.Error(ex.Message);
        }
    }

    /// <summary>
    /// Get list of points that have checkers that can be moved.
    /// </summary>
    public async Task<List<int>> GetValidSources()
    {
        try
        {
            var grain = await GetGameGrainForCallerAsync();
            if (grain == null) return new List<int>();

            var state = await grain.GetStateAsync(Context.ConnectionId);
            if (!state.IsYourTurn || state.RemainingMoves.Length == 0) return new List<int>();

            return state.ValidMoves.Select(m => m.From).Distinct().ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting valid sources");
            return new List<int>();
        }
    }

    /// <summary>
    /// Get list of valid destinations from a specific source point.
    /// </summary>
    public async Task<List<MoveDto>> GetValidDestinations(int fromPoint)
    {
        try
        {
            _logger.LogInformation("GetValidDestinations called for point {FromPoint}", fromPoint);

            var grain = await GetGameGrainForCallerAsync();
            if (grain == null)
            {
                _logger.LogWarning("No grain found for caller");
                return new List<MoveDto>();
            }

            var state = await grain.GetStateAsync(Context.ConnectionId);
            if (!state.IsYourTurn || state.RemainingMoves.Length == 0)
            {
                _logger.LogWarning("Not player's turn or no remaining moves");
                return new List<MoveDto>();
            }

            var moves = state.ValidMoves
                .Where(m => m.From == fromPoint)
                .Select(m => new MoveDto
                {
                    From = m.From,
                    To = m.To,
                    DieValue = m.DieValue,
                    IsHit = m.IsHit
                })
                .ToList();

            _logger.LogInformation("Filtered moves from point {FromPoint}: {Count}", fromPoint, moves.Count);
            return moves;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting valid destinations");
            return new List<MoveDto>();
        }
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

            var grain = await GetGameGrainForCallerAsync();
            if (grain == null)
            {
                await Clients.Caller.Error("Not in a game");
                return;
            }

            var result = await grain.RollDiceAsync(Context.ConnectionId);
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

            var grain = await GetGameGrainForCallerAsync();
            if (grain == null)
            {
                await Clients.Caller.Error("Not in a game");
                return;
            }

            var result = await grain.MakeMoveAsync(Context.ConnectionId, from, to);
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

            var grain = await GetGameGrainForCallerAsync();
            if (grain == null)
            {
                await Clients.Caller.Error("Not in a game");
                return;
            }

            var result = await grain.MakeCombinedMoveAsync(Context.ConnectionId, from, to, intermediatePoints);
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

            var grain = await GetGameGrainForCallerAsync();
            if (grain == null)
            {
                await Clients.Caller.Error("Not in a game");
                return;
            }

            var result = await grain.EndTurnAsync(Context.ConnectionId);
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

            var grain = await GetGameGrainForCallerAsync();
            if (grain == null)
            {
                await Clients.Caller.Error("Not in a game");
                return;
            }

            var result = await grain.UndoLastMoveAsync(Context.ConnectionId);
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
            var grain = await GetGameGrainForCallerAsync();
            if (grain == null)
            {
                await Clients.Caller.Error("Not in a game");
                return;
            }

            var result = await grain.OfferDoubleAsync(Context.ConnectionId);
            if (!result.Success)
            {
                await Clients.Caller.Error(result.ErrorMessage ?? "Failed to offer double");
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
            var grain = await GetGameGrainForCallerAsync();
            if (grain == null)
            {
                await Clients.Caller.Error("Not in a game");
                return;
            }

            var result = await grain.AcceptDoubleAsync(Context.ConnectionId);
            if (!result.Success)
            {
                await Clients.Caller.Error(result.ErrorMessage ?? "Failed to accept double");
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
            var grain = await GetGameGrainForCallerAsync();
            if (grain == null)
            {
                await Clients.Caller.Error("Not in a game");
                return;
            }

            var result = await grain.DeclineDoubleAsync(Context.ConnectionId);
            if (!result.Success)
            {
                await Clients.Caller.Error(result.ErrorMessage ?? "Failed to decline double");
            }
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
            var grain = await GetGameGrainForCallerAsync();
            if (grain == null)
            {
                await Clients.Caller.Error("Not in a game");
                return;
            }

            var result = await grain.AbandonAsync(Context.ConnectionId);
            if (!result.Success)
            {
                await Clients.Caller.Error(result.ErrorMessage ?? "Failed to abandon game");
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
            var grain = await GetGameGrainForCallerAsync();
            if (grain == null)
            {
                await Clients.Caller.Error("Not in a game");
                return;
            }

            var state = await grain.GetStateAsync(Context.ConnectionId);
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
        await HandleDisconnectionAsync(Context.ConnectionId);
    }

    /// <summary>
    /// Handle player disconnection
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Remove from presence registry
        await _grainFactory.GetGrain<IPresenceGrain>(IPresenceGrain.Key)
            .SetOfflineAsync(Context.ConnectionId);

        // Clean up chat rate limit history
        _chatService.CleanupConnection(Context.ConnectionId);

        await HandleDisconnectionAsync(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Export the current position (base64-encoded SGF - used for URLs).
    /// Works for both analysis sessions and game sessions.
    /// </summary>
    public async Task<string> ExportPosition()
    {
        // Check analysis session first
        var analysisSession = _analysisSessionManager.GetSessionByConnection(Context.ConnectionId);
        if (analysisSession != null)
        {
            var sgf = SgfSerializer.ExportPosition(analysisSession.Engine);
            var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(sgf));
            return base64;
        }

        // Check game grain
        var grain = await GetGameGrainForCallerAsync();
        if (grain != null)
        {
            var exported = await grain.ExportPositionAsync();
            return exported ?? string.Empty;
        }

        return string.Empty;
    }

    /// <summary>
    /// Export full game SGF with move history.
    /// Works for both analysis sessions and game sessions.
    /// </summary>
    public async Task<string> ExportGameSgf()
    {
        // Check analysis session first
        var analysisSession = _analysisSessionManager.GetSessionByConnection(Context.ConnectionId);
        if (analysisSession != null)
        {
            return analysisSession.Engine.GameSgf;
        }

        // Check game grain
        var grain = await GetGameGrainForCallerAsync();
        if (grain != null)
        {
            return await grain.ExportGameSgfAsync() ?? string.Empty;
        }

        return string.Empty;
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

    /// <summary>
    /// Evaluate the current position for an analysis session.
    /// </summary>
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

    // ==================== Private helpers ====================

    private async Task HandleDisconnectionAsync(string connectionId)
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            if (string.IsNullOrEmpty(userId)) return;

            var presence = _grainFactory.GetGrain<IPresenceGrain>(IPresenceGrain.Key);
            var gameId = await presence.GetGameIdForConnectionAsync(connectionId);

            if (!string.IsNullOrEmpty(gameId))
            {
                await Groups.RemoveFromGroupAsync(connectionId, gameId);
                var grain = _grainFactory.GetGrain<IGameGrain>(gameId);
                await grain.LeaveAsync(connectionId);
                _logger.LogInformation("Player {PlayerId} left game {GameId} (connection {ConnectionId})", userId, gameId, connectionId);
            }

            // Clean up analysis session if present
            _analysisSessionManager.RemoveConnection(connectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling disconnection for connection {ConnectionId}", connectionId);
        }
    }

    // ==================== Correspondence Game Methods ====================
}
