using Backgammon.Core;
using Backgammon.Server.Models;

namespace Backgammon.Server.Services;

/// <summary>
/// Represents an active game session between two players.
/// Wraps the GameEngine and manages player connection state.
/// </summary>
public class GameSession
{
    // Track spectator connections
    private readonly HashSet<string> _spectatorConnections = new();

    /// <summary>
    /// Read-only access to spectator connection IDs for broadcasting updates.
    /// </summary>
    public IReadOnlySet<string> SpectatorConnections => _spectatorConnections;

    public string Id { get; }
    public GameEngine Engine { get; }
    public string? WhitePlayerId { get; private set; }  // Persistent player ID
    public string? RedPlayerId { get; private set; }    // Persistent player ID
    public string? WhitePlayerName { get; set; }  // Display name for White player
    public string? RedPlayerName { get; set; }    // Display name for Red player
    public string? WhiteConnectionId { get; private set; }  // Current connection
    public string? RedConnectionId { get; private set; }    // Current connection
    public DateTime CreatedAt { get; }
    public DateTime LastActivityAt { get; private set; }
    
    public bool IsFull => WhitePlayerId != null && RedPlayerId != null;
    public bool IsStarted => IsFull && Engine.GameStarted;
    public bool IsAnalysisMode { get; set; } = false;
    public bool IsBotGame { get; set; } = false;

    public GameSession(string id)
    {
        Id = id;
        Engine = new GameEngine();
        // Initialize board with default checker positions so players can see them while waiting
        Engine.Board.SetupInitialPosition();
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
            // Set name if not already set (for existing games before this feature)
            if (string.IsNullOrEmpty(WhitePlayerName))
                WhitePlayerName = GenerateFriendlyName(playerId);
            return true;
        }
        
        if (RedPlayerId == playerId)
        {
            RedConnectionId = connectionId;
            // Set name if not already set (for existing games before this feature)
            if (string.IsNullOrEmpty(RedPlayerName))
                RedPlayerName = GenerateFriendlyName(playerId);
            return true;
        }
        
        // Add as new player
        if (WhitePlayerId == null)
        {
            WhitePlayerId = playerId;
            WhiteConnectionId = connectionId;
            WhitePlayerName = GenerateFriendlyName(playerId);
            return true;
        }
        
        if (RedPlayerId == null)
        {
            RedPlayerId = playerId;
            RedConnectionId = connectionId;
            RedPlayerName = GenerateFriendlyName(playerId);
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
    /// Set the Red player directly (used for analysis mode)
    /// </summary>
    public void SetRedPlayer(string playerId, string connectionId)
    {
        RedPlayerId = playerId;
        RedConnectionId = connectionId;
        if (string.IsNullOrEmpty(RedPlayerName))
        {
            RedPlayerName = GenerateFriendlyName(playerId);
        }
        LastActivityAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Add a spectator connection (not a player)
    /// </summary>
    public void AddSpectator(string connectionId)
    {
        _spectatorConnections.Add(connectionId);
        LastActivityAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Remove a spectator connection
    /// </summary>
    public void RemoveSpectator(string connectionId)
    {
        _spectatorConnections.Remove(connectionId);
        LastActivityAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Check if a connection is a spectator
    /// </summary>
    public bool IsSpectator(string connectionId)
    {
        return _spectatorConnections.Contains(connectionId);
    }
    
    /// <summary>
    /// Generate a friendly display name from a player ID
    /// </summary>
    private string GenerateFriendlyName(string playerId)
    {
        // Extract last 4 characters of the player ID for a short, readable name
        if (playerId.Length >= 4)
        {
            var suffix = playerId.Substring(playerId.Length - 4);
            return $"Player {suffix}";
        }
        return playerId;
    }

    /// <summary>
    /// Set the display name for a player
    /// </summary>
    public void SetPlayerName(string playerId, string displayName)
    {
        if (WhitePlayerId == playerId)
        {
            WhitePlayerName = displayName;
        }
        else if (RedPlayerId == playerId)
        {
            RedPlayerName = displayName;
        }
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
        // In analysis mode, player controls both sides - always their turn
        if (IsAnalysisMode)
            return true;

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
            WhitePlayerName = WhitePlayerName ?? WhitePlayerId ?? "Waiting...",
            RedPlayerName = RedPlayerName ?? RedPlayerId ?? "Waiting...",
            CurrentPlayer = Engine.CurrentPlayer?.Color ?? CheckerColor.White,
            YourColor = playerColor,
            IsYourTurn = IsAnalysisMode ? true : (playerColor.HasValue && Engine.CurrentPlayer?.Color == playerColor.Value),
            Dice = new[] { Engine.Dice.Die1, Engine.Dice.Die2 },
            RemainingMoves = Engine.RemainingMoves.ToArray(),
            MovesMadeThisTurn = Engine.MoveHistory.Count,
            Board = GetBoardState(),
            WhiteCheckersOnBar = Engine.WhitePlayer.CheckersOnBar,
            RedCheckersOnBar = Engine.RedPlayer.CheckersOnBar,
            WhiteBornOff = Engine.WhitePlayer.CheckersBornOff,
            RedBornOff = Engine.RedPlayer.CheckersBornOff,
            WhitePipCount = CalculatePipCount(CheckerColor.White),
            RedPipCount = CalculatePipCount(CheckerColor.Red),
            Status = IsFull ? (Engine.Winner != null ? GameStatus.Completed : GameStatus.InProgress) : GameStatus.WaitingForPlayer,
            Winner = Engine.Winner?.Color,
            DoublingCubeValue = Engine.DoublingCube.Value,
            DoublingCubeOwner = Engine.DoublingCube.Owner?.ToString(),
            IsAnalysisMode = this.IsAnalysisMode
        };

        // Get valid moves for current player
        // Always populate ValidMoves to ensure client has complete state
        var validMoves = Engine.GetValidMoves();
        state.ValidMoves = validMoves.Select(m => new MoveDto
        {
            From = m.From,
            To = m.To,
            DieValue = Math.Abs(m.To - m.From),
            IsHit = WillHit(m)
        }).ToList();
        
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
        // Bear-off moves (To = 0 or 25) cannot hit
        if (move.IsBearOff)
            return false;

        var targetPoint = Engine.Board.GetPoint(move.To);
        if (targetPoint.Color == null || targetPoint.Count == 0)
            return false;

        return targetPoint.Color != Engine.CurrentPlayer?.Color && targetPoint.Count == 1;
    }

    /// <summary>
    /// Calculate pip count for a specific color.
    /// Pip count is the total distance all checkers need to travel to bear off.
    /// </summary>
    private int CalculatePipCount(CheckerColor color)
    {
        int pips = 0;

        // Count pips from checkers on board points
        for (int pointNum = 1; pointNum <= 24; pointNum++)
        {
            var point = Engine.Board.GetPoint(pointNum);
            if (point.Color == color && point.Count > 0)
            {
                if (color == CheckerColor.White)
                {
                    // White moves 24→1, so distance is just the point number
                    pips += point.Count * pointNum;
                }
                else
                {
                    // Red moves 1→24, so distance is (25 - point number)
                    pips += point.Count * (25 - pointNum);
                }
            }
        }

        // Add pips for checkers on bar (25 pips each)
        if (color == CheckerColor.White)
        {
            pips += Engine.WhitePlayer.CheckersOnBar * 25;
        }
        else
        {
            pips += Engine.RedPlayer.CheckersOnBar * 25;
        }

        return pips;
    }

    public void UpdateActivity()
    {
        LastActivityAt = DateTime.UtcNow;
    }
}
