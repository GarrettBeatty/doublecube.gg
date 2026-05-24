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

            // Backfill match membership for players who land on a game URL without
            // first calling JoinMatch (direct URL, refresh, shared link). No-op for
            // spectators (IsPlayerAsync false), creators (already Player1), and
            // matches that don't accept new players.
            if (!string.IsNullOrEmpty(state?.MatchId) && await grain.IsPlayerAsync(playerId))
            {
                var matchGrain = _grainFactory.GetGrain<IMatchGrain>(state.MatchId);
                var joinResult = await matchGrain.EnsurePlayerJoinedAsync(playerId, displayName);
                if (joinResult != null)
                {
                    await BroadcastMatchJoinAsync(joinResult);
                    _logger.LogInformation(
                        "Player {PlayerId} auto-promoted to Player2 of match {MatchId} via JoinGame",
                        playerId,
                        state.MatchId);
                }
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
            var dispatched = await TryDispatchAnalysisAsync((g, s) => g.SetDiceAsync(s, die1, die2));
            if (!dispatched)
            {
                await Clients.Caller.Error("Not in an analysis session");
            }
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
            if (await TryDispatchAnalysisAsync((g, s) => g.RollDiceAsync(s))) return;

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
            if (await TryDispatchAnalysisAsync((g, s) => g.MakeMoveAsync(s, from, to))) return;

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
            if (await TryDispatchAnalysisAsync((g, s) => g.MakeCombinedMoveAsync(s, from, to, intermediatePoints))) return;

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
            if (await TryDispatchAnalysisAsync((g, s) => g.EndTurnAsync(s))) return;

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
            if (await TryDispatchAnalysisAsync((g, s) => g.UndoLastMoveAsync(s))) return;

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
        // Remove from presence registry. (Chat rate-limit state lives inside each
        // IMatchChatGrain; entries naturally expire after 1 minute and the grain
        // deactivates when the match ends, so no per-disconnect cleanup is needed.)
        await _grainFactory.GetGrain<IPresenceGrain>(IPresenceGrain.Key)
            .SetOfflineAsync(Context.ConnectionId);

        await HandleDisconnectionAsync(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Export the current position (base64-encoded SGF - used for URLs).
    /// Works for both analysis sessions and game sessions.
    /// </summary>
    public async Task<string> ExportPosition()
    {
        var analysisGrain = GetAnalysisGrain();
        var sessionId = await analysisGrain.GetSessionIdForConnectionAsync(Context.ConnectionId);
        if (sessionId != null)
        {
            return await analysisGrain.ExportPositionAsync(sessionId);
        }

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
        var analysisGrain = GetAnalysisGrain();
        var sessionId = await analysisGrain.GetSessionIdForConnectionAsync(Context.ConnectionId);
        if (sessionId != null)
        {
            return await analysisGrain.ExportGameSgfAsync(sessionId);
        }

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
        var dispatched = await TryDispatchAnalysisAsync((g, s) => g.ImportPositionAsync(s, positionData));
        if (!dispatched)
        {
            await Clients.Caller.Error("Not in an analysis session");
        }
    }

    /// <summary>
    /// Move a checker directly from one point to another in analysis mode (bypasses game rules)
    /// </summary>
    public async Task MoveCheckerDirectly(int from, int to)
    {
        var dispatched = await TryDispatchAnalysisAsync((g, s) => g.MoveCheckerDirectlyAsync(s, from, to));
        if (!dispatched)
        {
            await Clients.Caller.Error("Not in an analysis session");
        }
    }

    /// <summary>
    /// Set the current player in analysis mode
    /// </summary>
    public async Task SetCurrentPlayer(CheckerColor color)
    {
        var dispatched = await TryDispatchAnalysisAsync((g, s) => g.SetCurrentPlayerAsync(s, color));
        if (!dispatched)
        {
            await Clients.Caller.Error("Not in an analysis session");
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
        var result = await GetAnalysisGrain().AnalyzePositionAsync(sessionId, evaluatorType);
        if (result == null)
        {
            throw new HubException("Analysis session not found");
        }

        return result;
    }

    /// <summary>
    /// Find the best moves for the current position
    /// </summary>
    /// <param name="sessionId">The analysis session ID to analyze</param>
    /// <param name="evaluatorType">Optional evaluator type ("Heuristic" or "Gnubg"). If null, uses default from settings.</param>
    public async Task<BestMovesAnalysisDto> FindBestMoves(string sessionId, string? evaluatorType)
    {
        var result = await GetAnalysisGrain().FindBestMovesAsync(sessionId, evaluatorType);
        if (result == null)
        {
            throw new HubException("Analysis session not found or no dice rolled");
        }

        return result;
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
            await GetAnalysisGrain().LeaveSessionAsync(connectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling disconnection for connection {ConnectionId}", connectionId);
        }
    }

    // ==================== Correspondence Game Methods ====================
}
