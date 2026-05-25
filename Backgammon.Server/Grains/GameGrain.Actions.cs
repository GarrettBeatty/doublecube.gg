using Backgammon.Core;
using Backgammon.Plugins.Models;
using Backgammon.Server.Extensions;
using Backgammon.Server.Grains.Interfaces;
using Backgammon.Server.Models;
using Backgammon.Server.Models.SignalR;
using Backgammon.Server.Services;
using Microsoft.Extensions.Logging;

namespace Backgammon.Server.Grains;

/// <summary>
/// Partial class for GameGrain: gameplay actions (roll, move, end turn, undo, abandon).
/// </summary>
public partial class GameGrain
{
    /// <inheritdoc/>
    public async Task<ActionResult> RollDiceAsync(string connectionId)
    {
        var gameId = this.GetPrimaryKeyString();
        _logger.LogDebug("RollDice: Game={GameId}, Connection={ConnectionId}", gameId, connectionId);

        if (_engine.Winner != null)
            return ActionResult.Error("Game is already completed");

        // Handle opening roll
        if (_engine.IsOpeningRoll)
        {
            var playerColor = GetPlayerColorInternal(connectionId);

            // AI player support (empty connectionId)
            if (playerColor == null && string.IsNullOrEmpty(connectionId))
            {
                if (_aiMoveService.IsAiPlayer(_whitePlayerId)) playerColor = CheckerColor.White;
                else if (_aiMoveService.IsAiPlayer(_redPlayerId)) playerColor = CheckerColor.Red;
            }

            if (playerColor == null)
                return ActionResult.Error("Not a player in this game");

            if (!_engine.IsOpeningRollTie)
            {
                if (playerColor == CheckerColor.White && _engine.WhiteOpeningRoll.HasValue)
                    return ActionResult.Error("You already rolled");
                if (playerColor == CheckerColor.Red && _engine.RedOpeningRoll.HasValue)
                    return ActionResult.Error("You already rolled");
            }

            int roll = _engine.RollOpening(playerColor.Value);
            _lastActivityAt = DateTime.UtcNow;

            if (roll != -1 && !_engine.IsOpeningRoll)
            {
                // Opening roll complete — start timers
                _engine.StartTurnTimer();
                StartTimeUpdates();

                // Update correspondence turn tracking. MatchGrain noops if the match
                // isn't actually correspondence — no need to gate here on the GameGrain
                // mirror of _isCorrespondence (which may not yet be populated on first
                // activation if the game was created post-Phase-4b but loaded from DB).
                var firstPlayerId = GetCurrentPlayerId();
                if (!string.IsNullOrEmpty(_matchId) && !string.IsNullOrEmpty(firstPlayerId))
                {
                    var capturedMatchId = _matchId;
                    var capturedPlayerId = firstPlayerId;
                    BackgroundTaskHelper.FireAndForget(async () =>
                    {
                        var matchGrain = GrainFactory.GetGrain<IMatchGrain>(capturedMatchId);
                        await matchGrain.HandleTurnCompletedAsync(capturedPlayerId);
                    }, _logger, $"CorrespondenceOpeningRoll-{gameId}");
                }

                // Check if AI won opening roll
                if (_aiMoveService.IsAiPlayer(firstPlayerId))
                {
                    await SaveGameAsync();
                    await BroadcastGameUpdateAsync();

                    var capturedFirstPlayerId = firstPlayerId;
                    var selfRef = GrainFactory.GetGrain<IGameGrain>(gameId);
                    BackgroundTaskHelper.FireAndForget(async () =>
                    {
                        await selfRef.TriggerAiTurnAsync(capturedFirstPlayerId!);
                    }, _logger, $"AiOpeningTurn-{gameId}");

                    return ActionResult.Ok();
                }
            }
            else if (_engine.IsOpeningRoll)
            {
                // Still in opening roll — trigger AI roll if needed
                if (_aiMoveService.IsAiPlayer(_whitePlayerId) && !_engine.WhiteOpeningRoll.HasValue)
                {
                    await SaveGameAsync();
                    await BroadcastGameUpdateAsync();
                    var selfRef = GrainFactory.GetGrain<IGameGrain>(gameId);
                    BackgroundTaskHelper.FireAndForget(async () =>
                    {
                        await Task.Delay(500);
                        await selfRef.RollDiceAsync(string.Empty);
                    }, _logger, $"AiOpeningRoll-White-{gameId}");
                    return ActionResult.Ok();
                }

                if (_aiMoveService.IsAiPlayer(_redPlayerId) && !_engine.RedOpeningRoll.HasValue)
                {
                    await SaveGameAsync();
                    await BroadcastGameUpdateAsync();
                    var selfRef = GrainFactory.GetGrain<IGameGrain>(gameId);
                    BackgroundTaskHelper.FireAndForget(async () =>
                    {
                        await Task.Delay(500);
                        await selfRef.RollDiceAsync(string.Empty);
                    }, _logger, $"AiOpeningRoll-Red-{gameId}");
                    return ActionResult.Ok();
                }
            }

            await SaveGameAsync();
            await BroadcastGameUpdateAsync();
            return ActionResult.Ok();
        }

        // Regular dice roll
        if (_hasPendingDoubleOffer)
            return ActionResult.Error("Cannot roll dice while waiting for double response");

        if (!IsPlayerTurnInternal(connectionId))
            return ActionResult.Error("Not your turn");

        if (_engine.RemainingMoves.Count > 0)
            return ActionResult.Error("Must complete current moves first");

        _engine.RollDice();
        _lastActivityAt = DateTime.UtcNow;

        await SaveGameAsync();
        await BroadcastGameUpdateAsync();
        return ActionResult.Ok();
    }

    /// <inheritdoc/>
    public async Task<ActionResult> MakeMoveAsync(string connectionId, int from, int to)
    {
        if (_engine.Winner != null) return ActionResult.Error("Game is already completed");
        if (_hasPendingDoubleOffer) return ActionResult.Error("Cannot move while waiting for double response");
        if (!IsPlayerTurnInternal(connectionId)) return ActionResult.Error("Not your turn");
        if (_engine.HasCurrentPlayerTimedOut()) return ActionResult.Error("You have run out of time");

        var validMoves = _engine.GetValidMoves(includeCombined: true);
        var matchingMove = validMoves.FirstOrDefault(m => m.From == from && m.To == to);
        if (matchingMove == null) return ActionResult.Error("Invalid move - no matching valid move");

        if (!_engine.ExecuteMove(matchingMove)) return ActionResult.Error("Invalid move");

        _lastActivityAt = DateTime.UtcNow;

        await SaveGameAsync();
        await BroadcastGameUpdateAsync();

        if (_engine.Winner != null)
        {
            await HandleGameCompletionAsync();
            return ActionResult.GameOver();
        }

        return ActionResult.Ok();
    }

    /// <inheritdoc/>
    public async Task<ActionResult> MakeCombinedMoveAsync(string connectionId, int from, int to, int[] intermediatePoints)
    {
        if (_engine.Winner != null) return ActionResult.Error("Game is already completed");
        if (_hasPendingDoubleOffer) return ActionResult.Error("Cannot move while waiting for double response");
        if (!IsPlayerTurnInternal(connectionId)) return ActionResult.Error("Not your turn");
        if (_engine.HasCurrentPlayerTimedOut()) return ActionResult.Error("You have run out of time");

        var fullPath = new List<int> { from };
        fullPath.AddRange(intermediatePoints);
        fullPath.Add(to);

        int movesExecuted = 0;
        for (int i = 0; i < fullPath.Count - 1; i++)
        {
            int stepFrom = fullPath[i], stepTo = fullPath[i + 1];
            var validMoves = _engine.GetValidMoves();
            var matchingMove = validMoves.FirstOrDefault(m => m.From == stepFrom && m.To == stepTo);

            if (matchingMove == null || !_engine.IsValidMove(matchingMove) || !_engine.ExecuteMove(matchingMove))
            {
                for (int j = 0; j < movesExecuted; j++) _engine.UndoLastMove();
                return ActionResult.Error($"Invalid combined move step {stepFrom}->{stepTo}");
            }

            movesExecuted++;
        }

        _lastActivityAt = DateTime.UtcNow;
        await SaveGameAsync();
        await BroadcastGameUpdateAsync();

        if (_engine.Winner != null)
        {
            await HandleGameCompletionAsync();
            return ActionResult.GameOver();
        }

        return ActionResult.Ok();
    }

    /// <inheritdoc/>
    public async Task<ActionResult> EndTurnAsync(string connectionId)
    {
        if (_engine.Winner != null) return ActionResult.Error("Game is already completed");
        if (_hasPendingDoubleOffer) return ActionResult.Error("Cannot end turn while waiting for double response");
        if (!IsPlayerTurnInternal(connectionId)) return ActionResult.Error("Not your turn");
        if (_engine.HasCurrentPlayerTimedOut()) return ActionResult.Error("You have run out of time");

        if (_engine.RemainingMoves.Count > 0 && _engine.GetValidMoves().Count > 0)
            return ActionResult.Error("You still have valid moves available");

        _engine.EndTurnTimer();
        _engine.EndTurn();
        _engine.StartTurnTimer();
        _lastActivityAt = DateTime.UtcNow;

        await SaveGameAsync();
        await BroadcastGameUpdateAsync();

        // Correspondence turn tracking. MatchGrain decides whether to act; we always
        // forward end-of-turn for match games and keep our local deadline mirror fresh
        // when the match is correspondence (so the client state DTO stays current
        // without waiting for the next round-trip from MatchGrain).
        if (!string.IsNullOrEmpty(_matchId))
        {
            var capturedMatchId = _matchId;
            var capturedNextPlayerId = GetCurrentPlayerId();
            if (!string.IsNullOrEmpty(capturedNextPlayerId))
            {
                if (_isCorrespondence)
                {
                    _turnDeadline = DateTime.UtcNow.AddDays(_timePerMoveDays ?? 3);
                }

                BackgroundTaskHelper.FireAndForget(async () =>
                {
                    var matchGrain = GrainFactory.GetGrain<IMatchGrain>(capturedMatchId);
                    await matchGrain.HandleTurnCompletedAsync(capturedNextPlayerId);
                }, _logger, $"CorrespondenceTurn-{this.GetPrimaryKeyString()}");
            }
        }

        // Trigger AI turn if next player is AI
        var nextPlayerId = GetCurrentPlayerId();
        if (_aiMoveService.IsAiPlayer(nextPlayerId))
        {
            var capturedPlayerId = nextPlayerId;
            var selfRef = GrainFactory.GetGrain<IGameGrain>(this.GetPrimaryKeyString());
            BackgroundTaskHelper.FireAndForget(async () =>
            {
                await selfRef.TriggerAiTurnAsync(capturedPlayerId!);
            }, _logger, $"AiTurn-{this.GetPrimaryKeyString()}");
        }

        return ActionResult.Ok();
    }

    /// <inheritdoc/>
    public async Task<ActionResult> UndoLastMoveAsync(string connectionId)
    {
        if (_engine.Winner != null) return ActionResult.Error("Game is already completed");
        if (!IsPlayerTurnInternal(connectionId)) return ActionResult.Error("Not your turn");
        if (_engine.MoveHistory.Count == 0) return ActionResult.Error("No moves to undo");
        if (!_engine.UndoLastMove()) return ActionResult.Error("Failed to undo move");

        _lastActivityAt = DateTime.UtcNow;
        await SaveGameAsync();
        await BroadcastGameUpdateAsync();
        return ActionResult.Ok();
    }

    /// <inheritdoc/>
    public async Task<ActionResult> AbandonAsync(string connectionId)
    {
        var gameId = this.GetPrimaryKeyString();
        var playerColor = GetPlayerColorInternal(connectionId);
        if (playerColor == null) return ActionResult.Error("Not a player in this game");
        if (_status == SessionStatus.Completed) return ActionResult.Error("Game is already completed");

        if (!_engine.GameStarted || _engine.IsOpeningRoll)
        {
            // Cancel without penalty
            _status = SessionStatus.Completed;
            if (_gameMode.ShouldPersist)
                await _gameRepository.UpdateGameStatusAsync(gameId, "Abandoned");

            await BroadcastGameOverAsync();
            return ActionResult.Ok();
        }

        // Forfeit — opponent wins
        var winnerColor = playerColor == CheckerColor.White ? CheckerColor.Red : CheckerColor.White;
        var winner = winnerColor == CheckerColor.White ? _engine.WhitePlayer : _engine.RedPlayer;
        _engine.ForfeitGame(winner);

        await HandleGameCompletionAsync();
        return ActionResult.Ok();
    }

    private async Task HandleGameCompletionAsync()
    {
        var gameId = this.GetPrimaryKeyString();

        if (_engine.Winner == null)
        {
            _logger.LogWarning("HandleGameCompletion called but no winner for game {GameId}", gameId);
            return;
        }

        _status = SessionStatus.Completed;
        StopTimeUpdates();

        if (_gameMode.ShouldPersist)
            await _gameRepository.UpdateGameStatusAsync(gameId, "Completed");

        if (_gameMode.ShouldTrackStats)
        {
            var game = BuildGameModel();
            BackgroundTaskHelper.FireAndForget(async () =>
            {
                await _playerStatsService.UpdateStatsAfterGameCompletionAsync(game);
            }, _logger, $"Stats-{gameId}");
        }

        await BroadcastGameOverAsync();

        if (!string.IsNullOrEmpty(_matchId))
        {
            await HandleMatchGameCompletionAsync();
        }
    }

    private async Task HandleMatchGameCompletionAsync()
    {
        var winnerColor = _engine.Winner?.Color;
        if (winnerColor == null) return;

        var winnerId = winnerColor == CheckerColor.White ? _whitePlayerId : _redPlayerId;
        var winType = _engine.DetermineWinType();
        var coreResult = new GameResult(winnerId ?? string.Empty, winType, _engine.DoublingCube.Value);

        var completionInfo = new GameCompletionInfo
        {
            WinnerId = coreResult.WinnerId,
            WinnerColor = winnerColor,
            PointsWon = coreResult.PointsWon,
            WinType = coreResult.WinType,
        };

        var matchGrain = GrainFactory.GetGrain<IMatchGrain>(_matchId!);
        await matchGrain.CompleteGameAsync(this.GetPrimaryKeyString(), completionInfo);

        var match = await _matchService.GetMatchAsync(_matchId!);
        if (match != null)
        {
            await BroadcastMatchUpdateAsync(match);
        }
    }

    /// <inheritdoc/>
    public async Task TriggerAiTurnAsync(string aiPlayerId)
    {
        try
        {
            async Task BroadcastUpdate() => await BroadcastGameUpdateAsync();

            async Task NotifyDoubleOffered(int currentValue, int newValue)
            {
                _hasPendingDoubleOffer = true;
                _pendingDoubleOfferedBy = aiPlayerId;

                var humanConnections = _whitePlayerId == aiPlayerId ? _redConnections : _whiteConnections;
                foreach (var connId in humanConnections)
                {
                    await _hubContext.Clients.Client(connId).DoubleOffered(
                        new DoubleOfferDto
                        {
                            CurrentStakes = currentValue,
                            NewStakes = newValue
                        });
                }

                await BroadcastUpdate();
            }

            var matchContext = CreateMatchContext();
            var aiOfferedDouble = await _aiMoveService.ExecuteAiTurnAsync(
                _engine,
                this.GetPrimaryKeyString(),
                aiPlayerId,
                matchContext,
                BroadcastUpdate,
                NotifyDoubleOffered);

            await SaveGameAsync();

            if (!aiOfferedDouble && _engine.Winner != null)
            {
                await HandleGameCompletionAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing AI turn in game {GameId}", this.GetPrimaryKeyString());
        }
    }

    private MatchContext? CreateMatchContext()
    {
        if (!_targetScore.HasValue || _targetScore.Value <= 0)
        {
            return null; // No match context — treat as money game
        }

        return new MatchContext(
            TargetScore: _targetScore.Value,
            Player1Score: _player1Score ?? 0,
            Player2Score: _player2Score ?? 0,
            IsCrawfordGame: _isCrawfordGame ?? false);
    }

    private string? GetCurrentPlayerId()
    {
        if (_engine.CurrentPlayer == null) return null;
        return _engine.CurrentPlayer.Color == CheckerColor.White ? _whitePlayerId : _redPlayerId;
    }
}
