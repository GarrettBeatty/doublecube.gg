using Backgammon.Core;
using Backgammon.Server.Hubs.Interfaces;
using Backgammon.Server.Models;
using Backgammon.Server.Models.SignalR;
using Backgammon.Server.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Backgammon.Server.Hubs;

/// <summary>
/// GameHub partial class - Miscellaneous Operations
/// Handles puzzles and correspondence games
/// </summary>
public partial class GameHub
{
    // ==================== Correspondence Game Methods ====================

    /// <summary>
    /// Get all correspondence games for the current user
    /// </summary>
    public async Task<CorrespondenceGamesResponse> GetCorrespondenceGames()
    {
        try
        {
            var playerId = GetAuthenticatedUserId()!; // ! is safe - AuthenticationHubFilter ensures non-null
            var response = await _correspondenceGameService.GetAllCorrespondenceGamesAsync(playerId);

            _logger.LogInformation(
                "Retrieved correspondence games for player {PlayerId}: {YourTurn} your turn, {Waiting} waiting",
                playerId,
                response.TotalYourTurn,
                response.TotalWaiting);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting correspondence games");
            throw new HubException("Failed to retrieve correspondence games");
        }
    }

    /// <summary>
    /// Create a new correspondence match
    /// </summary>
    public async Task CreateCorrespondenceMatch(MatchConfig config)
    {
        try
        {
            var playerId = GetAuthenticatedUserId()!; // ! is safe - AuthenticationHubFilter ensures non-null

            // Validate correspondence-specific settings
            if (!config.IsCorrespondence)
            {
                throw new ArgumentException("IsCorrespondence must be true for correspondence matches");
            }

            if (config.TimePerMoveDays <= 0 || config.TimePerMoveDays > 30)
            {
                throw new ArgumentException("TimePerMoveDays must be between 1 and 30");
            }

            // Create correspondence match
            var (match, firstGame) = await _correspondenceGameService.CreateCorrespondenceMatchAsync(
                playerId,
                config.TargetScore,
                config.TimePerMoveDays,
                config.OpponentType,
                config.DisplayName,
                config.OpponentId,
                config.IsRated);

            // Send MatchCreated event with game ID
            await Clients.Caller.MatchCreated(new MatchCreatedDto
            {
                MatchId = match.MatchId,
                GameId = firstGame.GameId,
                TargetScore = match.TargetScore,
                OpponentType = match.OpponentType ?? string.Empty,
                Player1Id = match.Player1Id,
                Player2Id = match.Player2Id,
                Player1Name = match.Player1Name ?? string.Empty,
                Player2Name = match.Player2Name,
                IsCorrespondence = true,
                TimePerMoveDays = match.TimePerMoveDays,
                TurnDeadline = match.TurnDeadline
            });

            _logger.LogInformation(
                "Correspondence match {MatchId} created for player {PlayerId}, time per move: {TimePerMove} days",
                match.MatchId,
                playerId,
                match.TimePerMoveDays);

            // For OpenLobby, broadcast to all clients that a new lobby is available
            if (config.OpponentType == "OpenLobby")
            {
                await Clients.All.CorrespondenceLobbyCreated(new CorrespondenceLobbyCreatedDto
                {
                    MatchId = match.MatchId,
                    GameId = firstGame.GameId,
                    CreatorPlayerId = match.Player1Id,
                    CreatorUsername = match.Player1Name ?? string.Empty,
                    TargetScore = match.TargetScore,
                    TimePerMoveDays = match.TimePerMoveDays,
                    IsRated = match.IsRated
                });

                _logger.LogInformation(
                    "Broadcast CorrespondenceLobbyCreated event for match {MatchId} (isRated: {IsRated})",
                    match.MatchId,
                    match.IsRated);
            }

            // For friend matches, notify the friend if they're online
            if (config.OpponentType == "Friend"
                && !string.IsNullOrEmpty(config.OpponentId)
                && _sessionManager.IsPlayerOnline(config.OpponentId))
            {
                var opponentConnection = GetPlayerConnection(config.OpponentId);
                if (!string.IsNullOrEmpty(opponentConnection))
                {
                    await Clients.Client(opponentConnection).CorrespondenceMatchInvite(
                        new CorrespondenceMatchInviteDto
                        {
                            MatchId = match.MatchId,
                            GameId = firstGame.GameId,
                            TargetScore = match.TargetScore,
                            ChallengerName = match.Player1Name ?? string.Empty,
                            ChallengerId = match.Player1Id,
                            TimePerMoveDays = match.TimePerMoveDays
                        });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating correspondence match");
            await Clients.Caller.Error(ex.Message);
        }
    }

    /// <summary>
    /// Notify that a turn has been completed in a correspondence game.
    /// This hub method should be called explicitly by the client after EndTurn in correspondence matches
    /// to inform the server that the turn has been completed and to trigger notification of the next player.
    /// </summary>
    public async Task NotifyCorrespondenceTurnComplete(string matchId, string nextPlayerId)
    {
        try
        {
            await _correspondenceGameService.HandleTurnCompletedAsync(matchId, nextPlayerId);

            // Notify next player if they're online
            if (_sessionManager.IsPlayerOnline(nextPlayerId))
            {
                var nextPlayerConnection = GetPlayerConnection(nextPlayerId);
                if (!string.IsNullOrEmpty(nextPlayerConnection))
                {
                    await Clients.Client(nextPlayerConnection).CorrespondenceTurnNotification(
                        new CorrespondenceTurnNotificationDto
                        {
                            MatchId = matchId,
                            Message = "It's your turn!"
                        });
                }
            }

            _logger.LogInformation(
                "Correspondence turn completed for match {MatchId}, next player: {NextPlayerId}",
                matchId,
                nextPlayerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying correspondence turn completion for match {MatchId}", matchId);
            await Clients.Caller.Error(ex.Message);
        }
    }

    // ==================== Daily Puzzle Methods ====================

    /// <summary>
    /// Get today's daily puzzle.
    /// </summary>
    /// <returns>The daily puzzle DTO, or null if no puzzle exists for today.</returns>
    public async Task<DailyPuzzleDto?> GetDailyPuzzle()
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            return await _dailyPuzzleService.GetTodaysPuzzleAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily puzzle");
            throw new HubException("Failed to get daily puzzle");
        }
    }

    /// <summary>
    /// Submit an answer to today's puzzle.
    /// </summary>
    /// <param name="moves">The moves the user played.</param>
    /// <returns>The result of the puzzle submission.</returns>
    public async Task<PuzzleResultDto> SubmitPuzzleAnswer(List<MoveDto> moves)
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            if (string.IsNullOrEmpty(userId))
            {
                throw new HubException("Authentication required");
            }

            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            return await _dailyPuzzleService.SubmitAnswerAsync(userId, today, moves);
        }
        catch (HubException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting puzzle answer");
            throw new HubException("Failed to submit puzzle answer");
        }
    }

    /// <summary>
    /// Give up on today's puzzle and reveal the answer.
    /// </summary>
    /// <returns>The result with the best moves revealed.</returns>
    public async Task<PuzzleResultDto> GiveUpPuzzle()
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            if (string.IsNullOrEmpty(userId))
            {
                throw new HubException("Authentication required");
            }

            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            return await _dailyPuzzleService.GiveUpPuzzleAsync(userId, today);
        }
        catch (HubException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error giving up on puzzle");
            throw new HubException("Failed to give up on puzzle");
        }
    }

    /// <summary>
    /// Get user's puzzle streak information.
    /// </summary>
    /// <returns>The user's puzzle streak info.</returns>
    public async Task<PuzzleStreakInfo> GetPuzzleStreak()
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return new PuzzleStreakInfo();
            }

            return await _dailyPuzzleService.GetStreakInfoAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting puzzle streak");
            throw new HubException("Failed to get puzzle streak");
        }
    }

    /// <summary>
    /// Get a historical puzzle by date.
    /// </summary>
    /// <param name="date">Date in yyyy-MM-dd format.</param>
    /// <returns>The puzzle for the specified date, or null if not found.</returns>
    public async Task<DailyPuzzleDto?> GetHistoricalPuzzle(string date)
    {
        try
        {
            var userId = GetAuthenticatedUserId();
            return await _dailyPuzzleService.GetPuzzleByDateAsync(date, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting historical puzzle for date {Date}", date);
            throw new HubException("Failed to get historical puzzle");
        }
    }

    /// <summary>
    /// Get valid moves for a puzzle position with pending moves applied.
    /// Creates a temporary GameEngine, applies the position and pending moves,
    /// then returns valid moves using the engine's rules.
    /// </summary>
    /// <param name="request">The puzzle position with pending moves.</param>
    /// <returns>List of valid moves from the current position.</returns>
    public Task<List<MoveDto>> GetPuzzleValidMoves(PuzzleValidMovesRequest request)
    {
        try
        {
            var engine = new GameEngine();

            // Clear board first
            for (int i = 1; i <= 24; i++)
            {
                engine.Board.GetPoint(i).Checkers.Clear();
            }

            engine.WhitePlayer.CheckersOnBar = 0;
            engine.WhitePlayer.CheckersBornOff = 0;
            engine.RedPlayer.CheckersOnBar = 0;
            engine.RedPlayer.CheckersBornOff = 0;

            // Apply board state
            foreach (var pointState in request.BoardState)
            {
                if (pointState.Count > 0 && !string.IsNullOrEmpty(pointState.Color))
                {
                    var color = pointState.Color.Equals("White", StringComparison.OrdinalIgnoreCase)
                        ? CheckerColor.White
                        : CheckerColor.Red;
                    var point = engine.Board.GetPoint(pointState.Position);
                    for (int i = 0; i < pointState.Count; i++)
                    {
                        point.AddChecker(color);
                    }
                }
            }

            // Apply bar and bear-off counts
            engine.WhitePlayer.CheckersOnBar = request.WhiteCheckersOnBar;
            engine.RedPlayer.CheckersOnBar = request.RedCheckersOnBar;
            engine.WhitePlayer.CheckersBornOff = request.WhiteBornOff;
            engine.RedPlayer.CheckersBornOff = request.RedBornOff;

            // Set current player
            var currentPlayer = request.CurrentPlayer.Equals("White", StringComparison.OrdinalIgnoreCase)
                ? CheckerColor.White
                : CheckerColor.Red;
            engine.SetCurrentPlayer(currentPlayer);

            // Set dice and remaining moves
            if (request.Dice.Length >= 2)
            {
                engine.Dice.SetDice(request.Dice[0], request.Dice[1]);
            }

            // Build remaining moves: start with full dice values
            var remainingDice = new List<int>(engine.Dice.GetMoves());

            // Remove dice used by pending moves
            foreach (var pendingMove in request.PendingMoves)
            {
                // Remove the die value used
                var dieIndex = remainingDice.IndexOf(pendingMove.DieValue);
                if (dieIndex >= 0)
                {
                    remainingDice.RemoveAt(dieIndex);
                }
            }

            engine.RemainingMoves.Clear();
            engine.RemainingMoves.AddRange(remainingDice);

            // Mark game as started
            engine.SetGameStarted(true);

            // Apply pending moves to board state
            foreach (var pendingMove in request.PendingMoves)
            {
                ApplyPendingMoveToBoard(engine, pendingMove, currentPlayer);
            }

            // Get valid moves from the engine
            var validMoves = engine.GetValidMoves();

            // Convert to DTOs
            var moveDtos = validMoves.Select(m => new MoveDto
            {
                From = m.From,
                To = m.To,
                DieValue = m.DieValue,
                IsHit = m.IsHit
            }).ToList();

            return Task.FromResult(moveDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting puzzle valid moves");
            throw new HubException("Failed to get puzzle valid moves");
        }
    }

    // ==================== Players Page Methods ====================

    /// <summary>
    /// Get the leaderboard with top players by rating.
    /// </summary>
    /// <param name="limit">Maximum number of players to return (default 50).</param>
    /// <returns>List of leaderboard entries.</returns>
}
