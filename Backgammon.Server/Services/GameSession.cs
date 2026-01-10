using Backgammon.Core;
using Backgammon.Server.Hubs.Interfaces;
using Backgammon.Server.Models;
using Backgammon.Server.Models.SignalR;
using Backgammon.Server.Services.GameModes;
using Microsoft.AspNetCore.SignalR;
using ServerGameStatus = Backgammon.Server.Models.GameStatus;

namespace Backgammon.Server.Services;

/// <summary>
/// Represents an active game session between two players.
/// Wraps the GameEngine and manages player connection state.
/// </summary>
public class GameSession
{
    private readonly HashSet<string> _spectatorConnections = new();
    private readonly HashSet<string> _whiteConnections = new();
    private readonly HashSet<string> _redConnections = new();
    private readonly object _timerLock = new();
    private readonly SemaphoreSlim _gameActionLock = new(1, 1);
    private System.Threading.Timer? _timeUpdateTimer;

    public GameSession(string id)
    {
        Id = id;
        Engine = new GameEngine();
        // Initialize board with default checker positions so players can see them while waiting
        Engine.Board.SetupInitialPosition();
        CreatedAt = DateTime.UtcNow;
        LastActivityAt = DateTime.UtcNow;
        GameMode = new MultiplayerMode();  // Default to multiplayer mode
    }

    public string Id { get; }

    public GameEngine Engine { get; }

    /// <summary>
    /// Lock for game actions that modify state (prevents race conditions with multi-tab access)
    /// </summary>
    public SemaphoreSlim GameActionLock => _gameActionLock;

    public string? WhitePlayerId { get; private set; }

    public string? RedPlayerId { get; private set; }

    public string? WhitePlayerName { get; set; }

    public string? RedPlayerName { get; set; }

    public int? WhiteRating { get; set; }

    public int? RedRating { get; set; }

    public int? WhiteRatingBefore { get; set; }

    public int? RedRatingBefore { get; set; }

    public bool IsRated { get; set; } = true; // Default to rated games

    // Legacy properties for backwards compatibility - return first connection
    public string? WhiteConnectionId => _whiteConnections.FirstOrDefault();

    public string? RedConnectionId => _redConnections.FirstOrDefault();

    // New properties to access all connections
    public IReadOnlySet<string> WhiteConnections => _whiteConnections;

    public IReadOnlySet<string> RedConnections => _redConnections;

    public DateTime CreatedAt { get; }

    public DateTime LastActivityAt { get; private set; }

    public bool IsFull => WhitePlayerId != null && RedPlayerId != null;

    public bool IsStarted => IsFull && Engine.GameStarted;

    public IGameMode GameMode { get; private set; }

    public bool IsAnalysisMode => GameMode is AnalysisMode;

    public bool IsBotGame { get; set; } = false;

    public string? MatchId { get; set; }

    public bool IsMatchGame { get; set; } = false;

    public int? TargetScore { get; set; }

    public int? Player1Score { get; set; }

    public int? Player2Score { get; set; }

    public bool? IsCrawfordGame { get; set; }

    public IReadOnlySet<string> SpectatorConnections => _spectatorConnections;

    public TimeControlConfig? TimeControl { get; set; }

    /// <summary>
    /// Add a player to the game session
    /// </summary>
    public bool AddPlayer(string playerId, string connectionId)
    {
        LastActivityAt = DateTime.UtcNow;

        // Check if player is already in this game (reconnection/multi-tab)
        if (WhitePlayerId == playerId)
        {
            _whiteConnections.Add(connectionId);
            // Set name if not already set (for existing games before this feature)
            if (string.IsNullOrEmpty(WhitePlayerName))
            {
                WhitePlayerName = GenerateFriendlyName(playerId);
            }

            return true;
        }

        if (RedPlayerId == playerId)
        {
            _redConnections.Add(connectionId);
            // Set name if not already set (for existing games before this feature)
            if (string.IsNullOrEmpty(RedPlayerName))
            {
                RedPlayerName = GenerateFriendlyName(playerId);
            }

            return true;
        }

        // Add as new player
        if (WhitePlayerId == null)
        {
            WhitePlayerId = playerId;
            _whiteConnections.Add(connectionId);
            WhitePlayerName = GenerateFriendlyName(playerId);
            return true;
        }

        if (RedPlayerId == null)
        {
            RedPlayerId = playerId;
            _redConnections.Add(connectionId);
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
        _redConnections.Add(connectionId);
        if (string.IsNullOrEmpty(RedPlayerName))
        {
            RedPlayerName = GenerateFriendlyName(playerId);
        }

        LastActivityAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Enable analysis mode for this game session
    /// </summary>
    public void EnableAnalysisMode(string playerId)
    {
        GameMode = new AnalysisMode(playerId);
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
    /// Now adds the new connection rather than replacing
    /// </summary>
    public bool UpdatePlayerConnection(string playerId, string newConnectionId)
    {
        if (WhitePlayerId == playerId)
        {
            _whiteConnections.Add(newConnectionId);
            LastActivityAt = DateTime.UtcNow;
            return true;
        }

        if (RedPlayerId == playerId)
        {
            _redConnections.Add(newConnectionId);
            LastActivityAt = DateTime.UtcNow;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Remove a player connection from the game session
    /// Only removes this specific connection, not the player entirely
    /// </summary>
    public void RemovePlayer(string connectionId)
    {
        _whiteConnections.Remove(connectionId);
        _redConnections.Remove(connectionId);
        LastActivityAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Get the color assigned to a specific player connection
    /// </summary>
    public CheckerColor? GetPlayerColor(string connectionId)
    {
        if (_whiteConnections.Contains(connectionId))
        {
            return CheckerColor.White;
        }

        if (_redConnections.Contains(connectionId))
        {
            return CheckerColor.Red;
        }

        return null;
    }

    /// <summary>
    /// Check if it's a specific player's turn
    /// </summary>
    public bool IsPlayerTurn(string connectionId)
    {
        return GameMode.IsPlayerTurn(connectionId, this);
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

        var features = GameMode.GetFeatures();

        var state = new GameState
        {
            GameId = Id,
            WhitePlayerId = WhitePlayerId ?? string.Empty,
            RedPlayerId = RedPlayerId ?? string.Empty,
            WhitePlayerName = WhitePlayerName ?? WhitePlayerId ?? "Waiting...",
            RedPlayerName = RedPlayerName ?? RedPlayerId ?? "Waiting...",
            WhiteRating = WhiteRating,
            RedRating = RedRating,
            WhiteRatingChange = WhiteRating.HasValue && WhiteRatingBefore.HasValue ? WhiteRating - WhiteRatingBefore : null,
            RedRatingChange = RedRating.HasValue && RedRatingBefore.HasValue ? RedRating - RedRatingBefore : null,
            CurrentPlayer = Engine.CurrentPlayer?.Color ?? CheckerColor.White,
            YourColor = playerColor,
            IsYourTurn = forConnectionId != null && GameMode.IsPlayerTurn(forConnectionId, this),
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
            Status = IsFull ? (Engine.Winner != null ? ServerGameStatus.Completed : ServerGameStatus.InProgress) : ServerGameStatus.WaitingForPlayer,
            Winner = Engine.Winner?.Color,
            DoublingCubeValue = Engine.DoublingCube.Value,
            DoublingCubeOwner = Engine.DoublingCube.Owner?.ToString(),
            CanDouble = playerColor.HasValue
                && IsFull
                && !Engine.IsOpeningRoll
                && !(IsCrawfordGame ?? false)
                && Engine.CurrentPlayer?.Color == playerColor.Value
                && Engine.DoublingCube.CanDouble(playerColor.Value),
            IsAnalysisMode = features.ShowAnalysisBadge,
            IsRated = IsRated,
            MatchId = MatchId,
            IsMatchGame = IsMatchGame,
            TargetScore = TargetScore,
            Player1Score = Player1Score,
            Player2Score = Player2Score,
            IsCrawfordGame = IsCrawfordGame,
            IsOpeningRoll = Engine.IsOpeningRoll,
            WhiteOpeningRoll = Engine.WhiteOpeningRoll,
            RedOpeningRoll = Engine.RedOpeningRoll,
            IsOpeningRollTie = Engine.IsOpeningRollTie,
            TimeControlType = TimeControl?.Type.ToString(),
            DelaySeconds = TimeControl?.DelaySeconds,
            WhiteReserveSeconds = Engine.WhiteTimeState?.GetRemainingTime(TimeControl?.DelaySeconds ?? 0).TotalSeconds,
            RedReserveSeconds = Engine.RedTimeState?.GetRemainingTime(TimeControl?.DelaySeconds ?? 0).TotalSeconds,
            // Only show delay for current player (and only after opening roll is complete and turn has started)
            WhiteIsInDelay = !Engine.IsOpeningRoll && Engine.CurrentPlayer?.Color == CheckerColor.White && Engine.WhiteTimeState != null && Engine.WhiteTimeState.TurnStartTime != null && Engine.WhiteTimeState.CalculateIsInDelay(TimeControl?.DelaySeconds ?? 0),
            RedIsInDelay = !Engine.IsOpeningRoll && Engine.CurrentPlayer?.Color == CheckerColor.Red && Engine.RedTimeState != null && Engine.RedTimeState.TurnStartTime != null && Engine.RedTimeState.CalculateIsInDelay(TimeControl?.DelaySeconds ?? 0),
            WhiteDelayRemaining = !Engine.IsOpeningRoll && Engine.CurrentPlayer?.Color == CheckerColor.White && Engine.WhiteTimeState?.TurnStartTime != null ? Engine.WhiteTimeState.GetDelayRemaining(TimeControl?.DelaySeconds ?? 0).TotalSeconds : 0,
            RedDelayRemaining = !Engine.IsOpeningRoll && Engine.CurrentPlayer?.Color == CheckerColor.Red && Engine.RedTimeState?.TurnStartTime != null ? Engine.RedTimeState.GetDelayRemaining(TimeControl?.DelaySeconds ?? 0).TotalSeconds : 0
        };

        // Log time state calculations
        if (TimeControl != null)
        {
            Console.WriteLine($"[TIME DEBUG] GetState for game {Id}");
            Console.WriteLine($"[TIME DEBUG]   IsOpeningRoll: {Engine.IsOpeningRoll}");
            Console.WriteLine($"[TIME DEBUG]   CurrentPlayer: {Engine.CurrentPlayer?.Color}");
            Console.WriteLine($"[TIME DEBUG]   WhiteTimeState.TurnStartTime: {Engine.WhiteTimeState?.TurnStartTime}");
            Console.WriteLine($"[TIME DEBUG]   RedTimeState.TurnStartTime: {Engine.RedTimeState?.TurnStartTime}");
            Console.WriteLine($"[TIME DEBUG]   WhiteIsInDelay: {state.WhiteIsInDelay}");
            Console.WriteLine($"[TIME DEBUG]   RedIsInDelay: {state.RedIsInDelay}");
            Console.WriteLine($"[TIME DEBUG]   WhiteDelayRemaining: {state.WhiteDelayRemaining}");
            Console.WriteLine($"[TIME DEBUG]   RedDelayRemaining: {state.RedDelayRemaining}");
        }

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

    public void UpdateActivity()
    {
        LastActivityAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Start broadcasting time updates every second
    /// </summary>
    public void StartTimeUpdates(IHubContext<Hubs.GameHub, IGameHubClient> hubContext)
    {
        if (TimeControl == null || TimeControl.Type == TimeControlType.None)
        {
            return;
        }

        Console.WriteLine($"[TIME DEBUG] StartTimeUpdates called for game {Id}");
        Console.WriteLine($"[TIME DEBUG]   IsOpeningRoll: {Engine.IsOpeningRoll}");
        Console.WriteLine($"[TIME DEBUG]   CurrentPlayer: {Engine.CurrentPlayer?.Color}");
        Console.WriteLine($"[TIME DEBUG]   WhiteTimeState.TurnStartTime: {Engine.WhiteTimeState?.TurnStartTime}");
        Console.WriteLine($"[TIME DEBUG]   RedTimeState.TurnStartTime: {Engine.RedTimeState?.TurnStartTime}");

        lock (_timerLock)
        {
            _timeUpdateTimer?.Dispose();
            _timeUpdateTimer = new System.Threading.Timer(
                async _ => await BroadcastTimeUpdate(hubContext),
                null,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1));
        }
    }

    /// <summary>
    /// Stop time updates
    /// </summary>
    public void StopTimeUpdates()
    {
        lock (_timerLock)
        {
            _timeUpdateTimer?.Dispose();
            _timeUpdateTimer = null;
        }
    }

    private string GenerateFriendlyName(string playerId)
    {
        if (playerId.Length >= 4)
        {
            var suffix = playerId.Substring(playerId.Length - 4);
            return $"Player {suffix}";
        }

        return playerId;
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

    /// <summary>
    /// Broadcast time update to all connections
    /// </summary>
    private async Task BroadcastTimeUpdate(IHubContext<Hubs.GameHub, IGameHubClient> hubContext)
    {
        if (Engine.WhiteTimeState == null || Engine.RedTimeState == null || TimeControl == null)
        {
            return;
        }

        Console.WriteLine($"[TIME DEBUG] BroadcastTimeUpdate for game {Id}");
        Console.WriteLine($"[TIME DEBUG]   IsOpeningRoll: {Engine.IsOpeningRoll}");
        Console.WriteLine($"[TIME DEBUG]   CurrentPlayer: {Engine.CurrentPlayer?.Color}");
        Console.WriteLine($"[TIME DEBUG]   WhiteTimeState.TurnStartTime: {Engine.WhiteTimeState.TurnStartTime}");
        Console.WriteLine($"[TIME DEBUG]   RedTimeState.TurnStartTime: {Engine.RedTimeState.TurnStartTime}");

        var whiteIsInDelay = !Engine.IsOpeningRoll && Engine.CurrentPlayer?.Color == CheckerColor.White && Engine.WhiteTimeState.TurnStartTime != null && Engine.WhiteTimeState.CalculateIsInDelay(TimeControl.DelaySeconds);
        var redIsInDelay = !Engine.IsOpeningRoll && Engine.CurrentPlayer?.Color == CheckerColor.Red && Engine.RedTimeState.TurnStartTime != null && Engine.RedTimeState.CalculateIsInDelay(TimeControl.DelaySeconds);
        var whiteDelayRemaining = !Engine.IsOpeningRoll && Engine.CurrentPlayer?.Color == CheckerColor.White && Engine.WhiteTimeState.TurnStartTime != null ? Engine.WhiteTimeState.GetDelayRemaining(TimeControl.DelaySeconds).TotalSeconds : 0;
        var redDelayRemaining = !Engine.IsOpeningRoll && Engine.CurrentPlayer?.Color == CheckerColor.Red && Engine.RedTimeState.TurnStartTime != null ? Engine.RedTimeState.GetDelayRemaining(TimeControl.DelaySeconds).TotalSeconds : 0;

        Console.WriteLine($"[TIME DEBUG]   whiteIsInDelay: {whiteIsInDelay}");
        Console.WriteLine($"[TIME DEBUG]   redIsInDelay: {redIsInDelay}");
        Console.WriteLine($"[TIME DEBUG]   whiteDelayRemaining: {whiteDelayRemaining}");
        Console.WriteLine($"[TIME DEBUG]   redDelayRemaining: {redDelayRemaining}");

        var timeUpdate = new TimeUpdateDto
        {
            GameId = Id,
            WhiteReserveSeconds = Engine.WhiteTimeState.GetRemainingTime(TimeControl.DelaySeconds).TotalSeconds,
            RedReserveSeconds = Engine.RedTimeState.GetRemainingTime(TimeControl.DelaySeconds).TotalSeconds,
            // Only show delay for current player (and only after opening roll is complete and turn has started)
            WhiteIsInDelay = whiteIsInDelay,
            RedIsInDelay = redIsInDelay,
            WhiteDelayRemaining = whiteDelayRemaining,
            RedDelayRemaining = redDelayRemaining
        };

        Console.WriteLine($"[TIME DEBUG]   Broadcasting whiteReserve: {timeUpdate.WhiteReserveSeconds}, redReserve: {timeUpdate.RedReserveSeconds}");

        await hubContext.Clients.Group(Id).TimeUpdate(timeUpdate);

        // Check for timeout
        if (Engine.HasCurrentPlayerTimedOut())
        {
            StopTimeUpdates();
            await HandleTimeout(hubContext);
        }
    }

    /// <summary>
    /// Handle player timeout
    /// </summary>
    private async Task HandleTimeout(IHubContext<Hubs.GameHub, IGameHubClient> hubContext)
    {
        var losingPlayer = Engine.CurrentPlayer;
        var winningPlayer = Engine.GetOpponent();

        if (losingPlayer == null || winningPlayer == null)
        {
            Console.WriteLine("[TIME DEBUG] ERROR: Cannot handle timeout - player is null");
            return;
        }

        Console.WriteLine($"[TIME DEBUG] TIMEOUT! {losingPlayer.Color} ran out of time. {winningPlayer.Color} wins!");

        // Mark game as over using ForfeitGame
        Engine.ForfeitGame(winningPlayer);

        // Broadcast timeout event
        var timeoutEvent = new PlayerTimedOutDto
        {
            GameId = Id,
            TimedOutPlayer = losingPlayer.Color.ToString(),
            Winner = winningPlayer.Color.ToString()
        };

        await hubContext.Clients.Group(Id).PlayerTimedOut(timeoutEvent);

        // Broadcast final game state to all connections so UI updates with winner
        foreach (var connectionId in _whiteConnections.Concat(_redConnections).Concat(_spectatorConnections))
        {
            var state = GetState(connectionId);
            await hubContext.Clients.Client(connectionId).GameUpdate(state);
        }
    }
}
