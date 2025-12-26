using Microsoft.AspNetCore.SignalR;
using Backgammon.Core;
using Backgammon.Web.Services;
using Backgammon.Web.Models;

namespace Backgammon.Web.Hubs;

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
    private readonly ILogger<GameHub> _logger;

    public GameHub(IGameSessionManager sessionManager, ILogger<GameHub> logger)
    {
        _sessionManager = sessionManager;
        _logger = logger;
    }

    /// <summary>
    /// Join an existing game by ID or create/join via matchmaking
    /// </summary>
    /// <param name="gameId">Optional game ID. If null, uses matchmaking.</param>
    public async Task JoinGame(string? gameId = null)
    {
        try
        {
            var connectionId = Context.ConnectionId;
            var session = _sessionManager.JoinOrCreate(connectionId, gameId);
            
            // Add connection to SignalR group for this game
            await Groups.AddToGroupAsync(connectionId, session.Id);
            
            _logger.LogInformation("Player {ConnectionId} joined game {GameId}", connectionId, session.Id);
            
            if (session.IsFull)
            {
                // Game is ready to start
                var state = session.GetState();
                await Clients.Group(session.Id).SendAsync("GameStart", state);
                _logger.LogInformation("Game {GameId} started with both players", session.Id);
            }
            else
            {
                // Waiting for opponent
                var state = session.GetState();
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
            
            var state = session.GetState();
            await Clients.Group(session.Id).SendAsync("DiceRolled", state);
            
            _logger.LogInformation("Player {ConnectionId} rolled dice in game {GameId}: {Dice}", 
                Context.ConnectionId, session.Id, string.Join(",", state.Dice));
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
            var state = session.GetState();
            
            await Clients.Group(session.Id).SendAsync("MoveMade", state);
            
            // Check if game is over
            if (session.Engine.Winner != null)
            {
                var stakes = session.Engine.GetGameResult();
                await Clients.Group(session.Id).SendAsync("GameOver", state);
                _logger.LogInformation("Game {GameId} completed. Winner: {Winner} (Stakes: {Stakes})", 
                    session.Id, session.Engine.Winner.Name, stakes);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making move");
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

            session.Engine.EndTurn();
            session.UpdateActivity();
            
            var state = session.GetState();
            await Clients.Group(session.Id).SendAsync("TurnEnded", state);
            
            _logger.LogInformation("Turn ended in game {GameId}. Current player: {Player}", 
                session.Id, state.CurrentPlayer);
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

            var state = session.GetState();
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
