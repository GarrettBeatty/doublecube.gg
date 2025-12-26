using Backgammon.Core;
using Backgammon.Web.Models;

namespace Backgammon.Web.Services;

/// <summary>
/// Represents an active game session between two players.
/// Wraps the GameEngine and manages player connection state.
/// </summary>
public class GameSession
{
    public string Id { get; }
    public GameEngine Engine { get; }
    public string? WhitePlayerId { get; private set; }  // Persistent player ID
    public string? RedPlayerId { get; private set; }    // Persistent player ID
    public string? WhiteConnectionId { get; private set; }  // Current connection
    public string? RedConnectionId { get; private set; }    // Current connection
    public DateTime CreatedAt { get; }
    public DateTime LastActivityAt { get; private set; }
    
    public bool IsFull => WhitePlayerId != null && RedPlayerId != null;
    public bool IsStarted => IsFull && Engine.GameStarted;
    
    public GameSession(string id)
    {
        Id = id;
        Engine = new GameEngine();
        CreatedAt = DateTime.UtcNow;
        LastActivityAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Add a player to the game session
    /// </summary>
    public bool AddPlayer(string playerId, string connectionId)
    {
        LastActivityAt = DateTime.UtcNow;
        
        // Check if player is already in this game (reconnection)
        if (WhitePlayerId == playerId)
        {
            WhiteConnectionId = connectionId;
            return true;
        }
        
        if (RedPlayerId == playerId)
        {
            RedConnectionId = connectionId;
            return true;
        }
        
        // Add as new player
        if (WhitePlayerId == null)
        {
            WhitePlayerId = playerId;
            WhiteConnectionId = connectionId;
            return true;
        }
        
        if (RedPlayerId == null)
        {
            RedPlayerId = playerId;
            RedConnectionId = connectionId;
            // Start game when both players joined (only if not already started)
            if (!Engine.GameStarted)
            {
                Engine.StartNewGame();
            }
            return true;
        }
        
        return false; // Game is full
    }
    
    /// <summary>
    /// Update player's connection ID (for reconnection scenarios)
    /// </summary>
    public bool UpdatePlayerConnection(string playerId, string newConnectionId)
    {
        if (WhitePlayerId == playerId)
        {
            WhiteConnectionId = newConnectionId;
            LastActivityAt = DateTime.UtcNow;
            return true;
        }
        
        if (RedPlayerId == playerId)
        {
            RedConnectionId = newConnectionId;
            LastActivityAt = DateTime.UtcNow;
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Remove a player from the game session
    /// </summary>
    public void RemovePlayer(string connectionId)
    {
        if (WhiteConnectionId == connectionId)
            WhiteConnectionId = null;
        if (RedConnectionId == connectionId)
            RedConnectionId = null;
            
        LastActivityAt = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Get the color assigned to a specific player connection
    /// </summary>
    public CheckerColor? GetPlayerColor(string connectionId)
    {
        if (connectionId == WhiteConnectionId)
            return CheckerColor.White;
        if (connectionId == RedConnectionId)
            return CheckerColor.Red;
        return null;
    }
    
    /// <summary>
    /// Check if it's a specific player's turn
    /// </summary>
    public bool IsPlayerTurn(string connectionId)
    {
        var playerColor = GetPlayerColor(connectionId);
        return playerColor.HasValue && Engine.CurrentPlayer?.Color == playerColor.Value;
    }
    
    /// <summary>
    /// Convert the current engine state to a DTO for clients
    /// </summary>
    /// <param name="forConnectionId">Optional connection ID to provide player-specific information</param>
    public GameState GetState(string? forConnectionId = null)
    {
        CheckerColor? playerColor = null;
        if (forConnectionId != null)
        {
            playerColor = GetPlayerColor(forConnectionId);
        }
        
        var state = new GameState
        {
            GameId = Id,
            WhitePlayerId = WhitePlayerId ?? "",
            RedPlayerId = RedPlayerId ?? "",
            CurrentPlayer = Engine.CurrentPlayer?.Color ?? CheckerColor.White,
            YourColor = playerColor,
            IsYourTurn = playerColor.HasValue && Engine.CurrentPlayer?.Color == playerColor.Value,
            Dice = new[] { Engine.Dice.Die1, Engine.Dice.Die2 },
            RemainingMoves = Engine.RemainingMoves.ToArray(),
            MovesMadeThisTurn = Engine.MoveHistory.Count,
            Board = GetBoardState(),
            WhiteCheckersOnBar = Engine.WhitePlayer.CheckersOnBar,
            RedCheckersOnBar = Engine.RedPlayer.CheckersOnBar,
            WhiteBornOff = Engine.WhitePlayer.CheckersBornOff,
            RedBornOff = Engine.RedPlayer.CheckersBornOff,
            Status = IsFull ? (Engine.Winner != null ? GameStatus.Completed : GameStatus.InProgress) : GameStatus.WaitingForPlayer,
            Winner = Engine.Winner?.Color
        };
        
        // Get valid moves for current player
        if (Engine.RemainingMoves.Count > 0)
        {
            var validMoves = Engine.GetValidMoves();
            state.ValidMoves = validMoves.Select(m => new MoveDto
            {
                From = m.From,
                To = m.To,
                DieValue = Math.Abs(m.To - m.From),
                IsHit = WillHit(m)
            }).ToList();
        }
        
        if (Engine.Winner != null)
        {
            var stakes = Engine.GetGameResult();
            var multiplier = stakes / Engine.DoublingCube.Value;
            state.WinType = multiplier switch
            {
                3 => "Backgammon",
                2 => "Gammon",
                _ => "Normal"
            };
        }
        
        return state;
    }
    
    private PointState[] GetBoardState()
    {
        var points = new List<PointState>();
        
        // Points 1-24 on the board
        for (int i = 1; i <= 24; i++)
        {
            var point = Engine.Board.GetPoint(i);
            points.Add(new PointState
            {
                Position = i,
                Color = point.Color,
                Count = point.Count
            });
        }
        
        return points.ToArray();
    }
    
    private bool WillHit(Move move)
    {
        var targetPoint = Engine.Board.GetPoint(move.To);
        if (targetPoint.Color == null || targetPoint.Count == 0)
            return false;
            
        return targetPoint.Color != Engine.CurrentPlayer?.Color && targetPoint.Count == 1;
    }
    
    public void UpdateActivity()
    {
        LastActivityAt = DateTime.UtcNow;
    }
}
