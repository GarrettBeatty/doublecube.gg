using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Backgammon.Core;
using Backgammon.Server.Services;
using Backgammon.Server.Models;

namespace Backgammon.Server.Hubs;

/// <summary>
/// SignalR Hub for real-time Backgammon game communication.
/// Handles player connections, game actions, and state synchronization.
/// 
/// Client Methods (called FROM server TO clients):
/// - GameUpdate(GameState) - Sent when game state changes
/// - OpponentJoined(string) - Sent when second player joins
/// - OpponentLeft() - Sent when opponent disconnects
/// - Error(string) - Sent when an error occurs
/// - GameStart(GameState) - Sent when both players ready
/// 
/// Server Methods (called FROM clients TO server):
/// - JoinGame(string?) - Join or create game
/// - RollDice() - Request dice roll
/// - MakeMove(int, int) - Execute a move
/// - EndTurn() - Complete current turn
/// - LeaveGame() - Leave current game
/// </summary>
public class GameHub : Hub
{
    private readonly IGameSessionManager _sessionManager;
    private readonly IGameRepository _gameRepository;
    private readonly ILogger<GameHub> _logger;

    public GameHub(IGameSessionManager sessionManager, IGameRepository gameRepository, ILogger<GameHub> logger)
    {
        _sessionManager = sessionManager;
        _gameRepository = gameRepository;
        _logger = logger;
    }

    /// <summary>
    /// Join an existing game by ID or create/join via matchmaking
    /// </summary>
    /// <param name="playerId">Persistent player ID</param>
    /// <param name="gameId">Optional game ID. If null, uses matchmaking.</param>
    public async Task JoinGame(string playerId, string? gameId = null)
    {
        try
        {
            var connectionId = Context.ConnectionId;
            var session = _sessionManager.JoinOrCreate(playerId, connectionId, gameId);
            await Groups.AddToGroupAsync(connectionId, session.Id);
            _logger.LogInformation("Player {ConnectionId} joined game {GameId}", connectionId, session.Id);

            // Try to add as player; if full, add as spectator
            if (!session.AddPlayer(playerId, connectionId))
            {
                session.AddSpectator(connectionId);
                var spectatorState = session.GetState(null); // No color for spectators
                await Clients.Caller.SendAsync("SpectatorJoined", spectatorState);
                _logger.LogInformation("Spectator {ConnectionId} joined game {GameId}", connectionId, session.Id);
                return;
            }

            if (session.IsFull)
            {
                // Game is ready to start - send personalized state to each player
                if (session.WhiteConnectionId != null)
                {
                    var whiteState = session.GetState(session.WhiteConnectionId);
                    await Clients.Client(session.WhiteConnectionId).SendAsync("GameStart", whiteState);
                }
                if (session.RedConnectionId != null)
                {
                    var redState = session.GetState(session.RedConnectionId);
                    await Clients.Client(session.RedConnectionId).SendAsync("GameStart", redState);
                }
                _logger.LogInformation("Game {GameId} started with both players", session.Id);
            }
            else
            {
                // Waiting for opponent
                var state = session.GetState(connectionId);
                await Clients.Caller.SendAsync("GameUpdate", state);
                await Clients.Caller.SendAsync("WaitingForOpponent", session.Id);
                _logger.LogInformation("Player {ConnectionId} waiting in game {GameId}", connectionId, session.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining game");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    /// <summary>
    /// Get list of points that have checkers that can be moved
    /// </summary>
    public async Task<List<int>> GetValidSources()
    {
        try
        {
            var session = _sessionManager.GetGameByPlayer(Context.ConnectionId);
            if (session == null)
                return new List<int>();

            if (!session.IsPlayerTurn(Context.ConnectionId))
                return new List<int>();

            if (session.Engine.RemainingMoves.Count == 0)
                return new List<int>();

            var validMoves = session.Engine.GetValidMoves();
            var sources = validMoves.Select(m => m.From).Distinct().ToList();
            return sources;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting valid sources");
            return new List<int>();
        }
    }

    /// <summary>
    /// Get list of valid destinations from a specific source point
    /// </summary>
    public async Task<List<MoveDto>> GetValidDestinations(int fromPoint)
    {
        try
        {
            var session = _sessionManager.GetGameByPlayer(Context.ConnectionId);
            if (session == null)
                return new List<MoveDto>();

            if (!session.IsPlayerTurn(Context.ConnectionId))
                return new List<MoveDto>();

            if (session.Engine.RemainingMoves.Count == 0)
                return new List<MoveDto>();

            var validMoves = session.Engine.GetValidMoves()
                .Where(m => m.From == fromPoint)
                .Select(m => new MoveDto
                {
                    From = m.From,
                    To = m.To,
                    DieValue = Math.Abs(m.To - m.From),
                    IsHit = WillHit(session.Engine, m)
                })
                .ToList();
            
            return validMoves;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting valid destinations");
            return new List<MoveDto>();
        }
    }

    private bool WillHit(GameEngine engine, Move move)
    {
        var targetPoint = engine.Board.GetPoint(move.To);
        if (targetPoint.Color == null || targetPoint.Count == 0)
            return false;
            
        return targetPoint.Color != engine.CurrentPlayer?.Color && targetPoint.Count == 1;
    }

    /// <summary>
    /// Roll dice to start turn (only valid when no remaining moves)
    /// </summary>
    public async Task RollDice()
    {
        try
        {
            var session = _sessionManager.GetGameByPlayer(Context.ConnectionId);
            if (session == null)
            {
                await Clients.Caller.SendAsync("Error", "Not in a game");
                return;
            }

            if (!session.IsPlayerTurn(Context.ConnectionId))
            {
                await Clients.Caller.SendAsync("Error", "Not your turn");
                return;
            }

            if (session.Engine.RemainingMoves.Count > 0)
            {
                await Clients.Caller.SendAsync("Error", "Must complete current moves first");
                return;
            }

            session.Engine.RollDice();
            session.UpdateActivity();
            
            // Send personalized state to each player
            if (session.WhiteConnectionId != null)
            {
                var whiteState = session.GetState(session.WhiteConnectionId);
                await Clients.Client(session.WhiteConnectionId).SendAsync("GameUpdate", whiteState);
            }
            if (session.RedConnectionId != null)
            {
                var redState = session.GetState(session.RedConnectionId);
                await Clients.Client(session.RedConnectionId).SendAsync("GameUpdate", redState);
            }
            
            _logger.LogInformation("Player {ConnectionId} rolled dice in game {GameId}", 
                Context.ConnectionId, session.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rolling dice");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    /// <summary>
    /// Execute a move from one point to another
    /// </summary>
    public async Task MakeMove(int from, int to)
    {
        try
        {
            var session = _sessionManager.GetGameByPlayer(Context.ConnectionId);
            if (session == null)
            {
                await Clients.Caller.SendAsync("Error", "Not in a game");
                return;
            }

            if (!session.IsPlayerTurn(Context.ConnectionId))
            {
                await Clients.Caller.SendAsync("Error", "Not your turn");
                return;
            }

            var dieValue = Math.Abs(to - from);
            var move = new Move(from, to, dieValue);
            
            if (!session.Engine.ExecuteMove(move))
            {
                await Clients.Caller.SendAsync("Error", "Invalid move");
                return;
            }

            session.UpdateActivity();
            
            // Send personalized state to each player
            if (session.WhiteConnectionId != null)
            {
                var whiteState = session.GetState(session.WhiteConnectionId);
                await Clients.Client(session.WhiteConnectionId).SendAsync("GameUpdate", whiteState);
            }
            if (session.RedConnectionId != null)
            {
                var redState = session.GetState(session.RedConnectionId);
                await Clients.Client(session.RedConnectionId).SendAsync("GameUpdate", redState);
            }
            
            // Check if game is over
            if (session.Engine.Winner != null)
            {
                var stakes = session.Engine.GetGameResult();
                // Use group send for game over since it's the same for both players
                var finalState = session.GetState();
                await Clients.Group(session.Id).SendAsync("GameOver", finalState);
                _logger.LogInformation("Game {GameId} completed. Winner: {Winner} (Stakes: {Stakes})", 
                    session.Id, session.Engine.Winner.Name, stakes);
                
                // Persist completed game to database
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var completedGame = new CompletedGame
                        {
                            GameId = session.Id,
                            WhitePlayerId = session.WhitePlayerId ?? "unknown",
                            RedPlayerId = session.RedPlayerId ?? "unknown",
                            Moves = session.Engine.MoveHistory
                                .Select(m => m.IsBearOff ? $"{m.From}/off" : 
                                             m.From == 0 ? $"bar/{m.To}" : 
                                             $"{m.From}/{m.To}")
                                .ToList(),
                            DiceRolls = new List<string>(), // TODO: Track dice rolls per turn
                            Winner = session.Engine.Winner.Color.ToString(),
                            Stakes = stakes,
                            TurnCount = 0, // TODO: Track turn count
                            MoveCount = session.Engine.MoveHistory.Count,
                            CreatedAt = session.CreatedAt,
                            CompletedAt = DateTime.UtcNow,
                            DurationSeconds = (int)(DateTime.UtcNow - session.CreatedAt).TotalSeconds
                        };
                        
                        await _gameRepository.SaveCompletedGameAsync(completedGame);
                        _logger.LogInformation("Persisted game {GameId} to database", session.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to persist game {GameId}", session.Id);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making move");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    /// <summary>
    /// Undo the last move made during this turn
    /// </summary>
    public async Task UndoMove()
    {
        try
        {
            var session = _sessionManager.GetGameByPlayer(Context.ConnectionId);
            if (session == null)
            {
                await Clients.Caller.SendAsync("Error", "Not in a game");
                return;
            }

            if (!session.IsPlayerTurn(Context.ConnectionId))
            {
                await Clients.Caller.SendAsync("Error", "Not your turn");
                return;
            }

            if (!session.Engine.UndoLastMove())
            {
                await Clients.Caller.SendAsync("Error", "No moves to undo");
                return;
            }

            session.UpdateActivity();
            
            // Send personalized state to each player
            if (session.WhiteConnectionId != null)
            {
                var whiteState = session.GetState(session.WhiteConnectionId);
                await Clients.Client(session.WhiteConnectionId).SendAsync("GameUpdate", whiteState);
            }
            if (session.RedConnectionId != null)
            {
                var redState = session.GetState(session.RedConnectionId);
                await Clients.Client(session.RedConnectionId).SendAsync("GameUpdate", redState);
            }
            
            _logger.LogInformation("Player {ConnectionId} undid last move in game {GameId}", 
                Context.ConnectionId, session.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error undoing move");
            await Clients.Caller.SendAsync("Error", ex.Message);
        }
    }

    /// <summary>
    /// End current turn and switch to opponent
    /// </summary>
    public async Task EndTurn()
    {
        try
        {
            var session = _sessionManager.GetGameByPlayer(Context.ConnectionId);
            if (session == null)
            {
                await Clients.Caller.SendAsync("Error", "Not in a game");
                return;
            }

            if (!session.IsPlayerTurn(Context.ConnectionId))
            {
                await Clients.Caller.SendAsync("Error", "Not your turn");
                return;
            }

            // Validate that turn can be ended
            // Only allow ending turn if:
            // 1. No remaining moves, OR
            // 2. No valid moves are possible
            if (session.Engine.RemainingMoves.Count > 0)
            {
                var validMoves = session.Engine.GetValidMoves();
                if (validMoves.Count > 0)
                {
                    await Clients.Caller.SendAsync("Error", "You still have valid moves available");
                    return;
                }
            }

            session.Engine.EndTurn();
            session.UpdateActivity();
            
            // Send personalized state to each player
            if (session.WhiteConnectionId != null)
            {
                var whiteState = session.GetState(session.WhiteConnectionId);
                await Clients.Client(session.WhiteConnectionId).SendAsync("GameUpdate", whiteState);
            }
            if (session.RedConnectionId != null)
            {
                var redState = session.GetState(session.RedConnectionId);
                await Clients.Client(session.RedConnectionId).SendAsync("GameUpdate", redState);
            }
            
            _logger.LogInformation("Turn ended in game {GameId}. Current player: {Player}", 
                session.Id, session.Engine.CurrentPlayer?.Color.ToString() ?? "Unknown");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending turn");
            await Clients.Caller.SendAsync("Error", ex.Message);
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
                await Clients.Caller.SendAsync("Error", "Not in a game");
                return;
            }

            var state = session.GetState(Context.ConnectionId);
            await Clients.Caller.SendAsync("GameUpdate", state);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting game state");
            await Clients.Caller.SendAsync("Error", ex.Message);
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
        await HandleDisconnection(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    private async Task HandleDisconnection(string connectionId)
    {
        try
        {
            var session = _sessionManager.GetGameByPlayer(connectionId);
            if (session != null)
            {
                await Groups.RemoveFromGroupAsync(connectionId, session.Id);
                
                // Notify opponent
                await Clients.Group(session.Id).SendAsync("OpponentLeft");
                
                _sessionManager.RemovePlayer(connectionId);
                
                _logger.LogInformation("Player {ConnectionId} left game {GameId}", connectionId, session.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling disconnection");
        }
    }
}
