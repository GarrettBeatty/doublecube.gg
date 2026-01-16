using Backgammon.Core;
using Backgammon.Server.Models;
using Backgammon.Server.Models.SignalR;
using ServerGameStatus = Backgammon.Server.Models.GameStatus;

namespace Backgammon.Server.Services;

/// <summary>
/// Represents an analysis session for a single user.
/// Unlike GameSession, this is a lightweight session for position analysis
/// where one user controls both sides of the board.
/// </summary>
public class AnalysisSession : IGameSession
{
    private readonly HashSet<string> _connections = new();
    private readonly SemaphoreSlim _gameActionLock = new(1, 1);

    /// <summary>
    /// Creates a new analysis session.
    /// </summary>
    /// <param name="id">Unique session identifier.</param>
    /// <param name="userId">The user who owns this analysis session.</param>
    public AnalysisSession(string id, string userId)
    {
        Id = id;
        UserId = userId;
        Engine = new GameEngine();
        Engine.StartNewGame();
        // Skip opening roll - analysis starts ready to play
        SkipOpeningRoll();
        CreatedAt = DateTime.UtcNow;
        LastActivityAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Unique session identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// The game engine managing board state and rules.
    /// </summary>
    public GameEngine Engine { get; }

    /// <summary>
    /// The user who owns this analysis session.
    /// </summary>
    public string UserId { get; }

    /// <summary>
    /// Analysis sessions are always in progress (no waiting for opponent).
    /// </summary>
    public SessionStatus Status { get; set; } = SessionStatus.InProgress;

    /// <summary>
    /// Lock for game actions that modify state (prevents race conditions with multi-tab access).
    /// </summary>
    public SemaphoreSlim GameActionLock => _gameActionLock;

    /// <summary>
    /// When this session was created.
    /// </summary>
    public DateTime CreatedAt { get; }

    /// <summary>
    /// Last time there was activity on this session.
    /// </summary>
    public DateTime LastActivityAt { get; private set; }

    /// <summary>
    /// All connections (tabs) for this session.
    /// </summary>
    public IReadOnlySet<string> Connections => _connections;

    /// <summary>
    /// Whether this session has any active connections.
    /// </summary>
    public bool HasConnections => _connections.Count > 0;

    /// <summary>
    /// Add a connection to this session.
    /// </summary>
    public void AddConnection(string connectionId)
    {
        _connections.Add(connectionId);
        LastActivityAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Remove a connection from this session.
    /// </summary>
    public void RemoveConnection(string connectionId)
    {
        _connections.Remove(connectionId);
        LastActivityAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update the last activity timestamp.
    /// </summary>
    public void UpdateActivity()
    {
        LastActivityAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Check if a connection belongs to this session.
    /// </summary>
    public bool HasConnection(string connectionId)
    {
        return _connections.Contains(connectionId);
    }

    /// <summary>
    /// Convert the current engine state to a DTO for clients.
    /// Analysis mode shows full state - user controls both sides.
    /// </summary>
    /// <param name="forConnectionId">Optional connection ID (ignored for analysis - user sees everything).</param>
    public GameState GetState(string? forConnectionId = null)
    {
        var state = new GameState
        {
            GameId = Id,
            WhitePlayerId = UserId,
            RedPlayerId = UserId,
            WhitePlayerName = "White",
            RedPlayerName = "Red",
            WhiteRating = null,
            RedRating = null,
            WhiteRatingChange = null,
            RedRatingChange = null,
            CurrentPlayer = Engine.CurrentPlayer?.Color ?? CheckerColor.White,
            YourColor = Engine.CurrentPlayer?.Color, // User controls current player
            IsYourTurn = true, // Always user's turn in analysis mode
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
            Status = ServerGameStatus.InProgress,
            Winner = Engine.Winner?.Color,
            DoublingCubeValue = Engine.DoublingCube.Value,
            DoublingCubeOwner = Engine.DoublingCube.Owner?.ToString(),
            CanDouble = false, // No doubling in analysis mode
            IsAnalysisMode = true,
            IsRated = false,
            MatchId = null,
            TargetScore = null,
            Player1Score = null,
            Player2Score = null,
            IsCrawfordGame = null,
            IsOpeningRoll = false, // Analysis skips opening roll
            WhiteOpeningRoll = null,
            RedOpeningRoll = null,
            IsOpeningRollTie = false,
            LeaveGameAction = "Leave", // Analysis sessions are just abandoned
            TimeControlType = null,
            DelaySeconds = null,
            WhiteReserveSeconds = null,
            RedReserveSeconds = null,
            WhiteIsInDelay = null,
            RedIsInDelay = null,
            WhiteDelayRemaining = null,
            RedDelayRemaining = null
        };

        // Get valid moves for current player
        var validMoves = Engine.GetValidMoves(includeCombined: true);
        state.ValidMoves = validMoves.Select(m => new MoveDto
        {
            From = m.From,
            To = m.To,
            DieValue = m.DieValue,
            IsHit = m.IsHit || WillHit(m),
            IsCombinedMove = m.IsCombined,
            DiceUsed = m.DiceUsed,
            IntermediatePoints = m.IntermediatePoints
        }).ToList();

        state.HasValidMoves = validMoves.Count > 0;

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

    /// <summary>
    /// Skip the opening roll phase so analysis can start immediately.
    /// </summary>
    private void SkipOpeningRoll()
    {
        // Use reflection to set IsOpeningRoll = false since there's no public setter
        var isOpeningRollField = typeof(GameEngine).GetProperty("IsOpeningRoll");
        if (isOpeningRollField != null && isOpeningRollField.CanWrite)
        {
            isOpeningRollField.SetValue(Engine, false);
        }
        else
        {
            // Fallback: Try the backing field
            var bindingFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var field = typeof(GameEngine).GetField("<IsOpeningRoll>k__BackingField", bindingFlags);
            field?.SetValue(Engine, false);
        }
    }

    private PointState[] GetBoardState()
    {
        var points = new List<PointState>();

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
        if (move.IsBearOff)
        {
            return false;
        }

        var targetPoint = Engine.Board.GetPoint(move.To);
        if (targetPoint.Color == null || targetPoint.Count == 0)
        {
            return false;
        }

        return targetPoint.Color != Engine.CurrentPlayer?.Color && targetPoint.Count == 1;
    }

    private int CalculatePipCount(CheckerColor color)
    {
        int pips = 0;

        for (int pointNum = 1; pointNum <= 24; pointNum++)
        {
            var point = Engine.Board.GetPoint(pointNum);
            if (point.Color == color && point.Count > 0)
            {
                if (color == CheckerColor.White)
                {
                    pips += point.Count * pointNum;
                }
                else
                {
                    pips += point.Count * (25 - pointNum);
                }
            }
        }

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
}
