using System.Reflection;
using Backgammon.Core;
using Backgammon.Server.Extensions;
using Backgammon.Server.Grains.Interfaces;
using Backgammon.Server.Hubs;
using Backgammon.Server.Hubs.Interfaces;
using Backgammon.Server.Models;
using Backgammon.Server.Models.SignalR;
using Backgammon.Server.Services;
using Backgammon.Server.Services.GameModes;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using Match = Backgammon.Server.Models.Match;
using ServerGame = Backgammon.Server.Models.Game;
using ServerGameStatus = Backgammon.Server.Models.GameStatus;

namespace Backgammon.Server.Grains;

/// <summary>
/// Orleans grain for a single active game session.
/// Replaces GameSession + GameSessionManager + GameActionOrchestrator + GameBroadcastService + GameCompletionService.
/// Orleans guarantees single-threaded execution per grain — no SemaphoreSlim needed.
/// </summary>
public partial class GameGrain : Grain, IGameGrain
{
    // ===== Injected services =====
    private readonly IHubContext<GameHub, IGameHubClient> _hubContext;
    private readonly IGameRepository _gameRepository;
    private readonly IAiMoveService _aiMoveService;
    private readonly IAiPlayerManager _aiPlayerManager;
    private readonly IMatchService _matchService;
    private readonly IPlayerStatsService _playerStatsService;
    private readonly IChatService _chatService;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GameGrain> _logger;

    // ===== Orleans-persisted state =====
    // Owns the on-disk source of truth for this grain. Hydrated automatically on
    // activation by Orleans; written via _state.WriteStateAsync() after each move.
    // Instance fields below mirror this state during the grain's lifetime — they
    // are synced via CaptureToState()/ApplyFromState().
    private readonly IPersistentState<GameGrainState> _state;

    // ===== In-memory game state (what used to be GameSession fields) =====
    private GameEngine _engine = new();
    private string? _whitePlayerId;
    private string? _redPlayerId;
    private string? _whitePlayerName;
    private string? _redPlayerName;
    private int? _whiteRating;
    private int? _redRating;
    private int? _whiteRatingBefore;
    private int? _redRatingBefore;
    private readonly HashSet<string> _whiteConnections = new();
    private readonly HashSet<string> _redConnections = new();
    private readonly HashSet<string> _spectatorConnections = new();
    private SessionStatus _status = SessionStatus.WaitingForOpponent;
    private DateTime _createdAt = DateTime.UtcNow;
    private DateTime _lastActivityAt = DateTime.UtcNow;
    private bool _isRated = true;
    private bool _isBotGame;
    private string? _matchId;
    private int? _targetScore;
    private int? _player1Score;
    private int? _player2Score;
    private bool? _isCrawfordGame;
    private TimeControlConfig? _timeControl;
    private bool _isCorrespondence;
    private int? _timePerMoveDays;
    private DateTime? _turnDeadline;
    private bool _hasPendingDoubleOffer;
    private string? _pendingDoubleOfferedBy;
    private IGameMode _gameMode = new MultiplayerMode();

    // ===== Orleans grain timer (replaces System.Threading.Timer) =====
    private IGrainTimer? _timeUpdateTimer;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameGrain"/> class.
    /// </summary>
    public GameGrain(
        [PersistentState("game", "Default")] IPersistentState<GameGrainState> state,
        IHubContext<GameHub, IGameHubClient> hubContext,
        IGameRepository gameRepository,
        IAiMoveService aiMoveService,
        IAiPlayerManager aiPlayerManager,
        IMatchService matchService,
        IPlayerStatsService playerStatsService,
        IChatService chatService,
        IUserRepository userRepository,
        ILogger<GameGrain> logger)
    {
        _state = state;
        _hubContext = hubContext;
        _gameRepository = gameRepository;
        _aiMoveService = aiMoveService;
        _aiPlayerManager = aiPlayerManager;
        _matchService = matchService;
        _playerStatsService = playerStatsService;
        _chatService = chatService;
        _userRepository = userRepository;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var gameId = this.GetPrimaryKeyString();
        _logger.LogDebug("Activating GameGrain {GameId}", gameId);

        // Prefer Orleans-persisted state (post-Phase-4a). Fall back to the games table for
        // grains created before this codepath existed — those rows get written to Orleans
        // storage on the next save, so the fallback is self-healing.
        if (_state.RecordExists && _state.State.WhitePlayerId != null)
        {
            ApplyFromState();
            _logger.LogInformation(
                "GameGrain {GameId} restored from Orleans state: White={White}, Red={Red}",
                gameId,
                _whitePlayerId,
                _redPlayerId);
        }
        else
        {
            try
            {
                var game = await _gameRepository.GetGameByGameIdAsync(gameId);
                if (game != null && game.Status == "InProgress")
                {
                    LoadFromGame(game);

                    // The Game record doesn't carry correspondence flags; fetch from the
                    // owning MatchGrain so the client state DTO and correspondence callbacks
                    // see the right values on the very first activation.
                    if (!string.IsNullOrEmpty(_matchId))
                    {
                        var info = await GrainFactory.GetGrain<IMatchGrain>(_matchId).GetCorrespondenceInfoAsync();
                        _isCorrespondence = info.IsCorrespondence;
                        _timePerMoveDays = info.TimePerMoveDays > 0 ? info.TimePerMoveDays : null;
                        _turnDeadline = info.TurnDeadline;
                    }

                    CaptureToState();
                    await _state.WriteStateAsync();
                    _logger.LogInformation(
                        "GameGrain {GameId} restored from DB (migrated to Orleans state): White={White}, Red={Red}, Correspondence={IsCorr}",
                        gameId,
                        _whitePlayerId,
                        _redPlayerId,
                        _isCorrespondence);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load game {GameId} from DB during activation", gameId);
            }
        }

        await base.OnActivateAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _timeUpdateTimer?.Dispose();
        _logger.LogDebug(
            "Deactivating GameGrain {GameId}, reason={Reason}",
            this.GetPrimaryKeyString(),
            reason.Description);
        return base.OnDeactivateAsync(reason, cancellationToken);
    }

    // ===== IGameGrain: Connection management =====

    /// <inheritdoc/>
    public async Task<GameState?> JoinAsync(string playerId, string connectionId, string? displayName)
    {
        var gameId = this.GetPrimaryKeyString();
        _logger.LogInformation(
            "JoinAsync: Game={GameId}, Player={PlayerId}, Connection={ConnectionId}",
            gameId,
            playerId,
            connectionId);

        // Track connection→game association in the presence registry
        var presence = GrainFactory.GetGrain<IPresenceGrain>(IPresenceGrain.Key);
        await presence.SetConnectionGameAsync(connectionId, gameId);

        // Track in MatchGrain if part of match
        if (!string.IsNullOrEmpty(_matchId))
        {
            var matchGrain = GrainFactory.GetGrain<IMatchGrain>(_matchId);
            await matchGrain.TrackPlayerConnectionAsync(playerId, connectionId);
        }

        _lastActivityAt = DateTime.UtcNow;

        // Check if player is already in this game (reconnect / multi-tab)
        if (_whitePlayerId == playerId)
        {
            _whiteConnections.Add(connectionId);
            await SetPlayerDisplayNameAndRatingAsync(playerId, displayName);
            TryStartGameIfReady();

            if (_engine.GameStarted)
            {
                await SendChatHistoryAsync(connectionId);
            }

            return GetStateInternal(connectionId);
        }

        if (_redPlayerId == playerId)
        {
            _redConnections.Add(connectionId);
            await SetPlayerDisplayNameAndRatingAsync(playerId, displayName);
            TryStartGameIfReady();

            if (_engine.GameStarted)
            {
                await SendChatHistoryAsync(connectionId);
            }

            return GetStateInternal(connectionId);
        }

        // Try to add as new player
        if (_whitePlayerId == null)
        {
            _whitePlayerId = playerId;
            _whiteConnections.Add(connectionId);
            _whitePlayerName = GenerateFriendlyName(playerId);
            await SetPlayerDisplayNameAndRatingAsync(playerId, displayName);
            TryStartGameIfReady();
        }
        else if (_redPlayerId == null)
        {
            _redPlayerId = playerId;
            _redConnections.Add(connectionId);
            _redPlayerName = GenerateFriendlyName(playerId);
            await SetPlayerDisplayNameAndRatingAsync(playerId, displayName);
            TryStartGameIfReady();
        }
        else
        {
            // Game is full — add as spectator
            _spectatorConnections.Add(connectionId);
            return GetStateInternal(null);
        }

        if (_engine.GameStarted)
        {
            await SendChatHistoryAsync(connectionId);
        }

        return GetStateInternal(connectionId);
    }

    /// <inheritdoc/>
    public async Task LeaveAsync(string connectionId)
    {
        var playerId = GetPlayerIdByConnectionInternal(connectionId);

        _whiteConnections.Remove(connectionId);
        _redConnections.Remove(connectionId);
        _spectatorConnections.Remove(connectionId);
        _lastActivityAt = DateTime.UtcNow;

        // Clear connection→game association in the presence registry
        if (!string.IsNullOrEmpty(playerId))
        {
            var presence = GrainFactory.GetGrain<IPresenceGrain>(IPresenceGrain.Key);
            await presence.ClearConnectionGameAsync(connectionId);
        }

        // Remove from MatchGrain if applicable
        if (!string.IsNullOrEmpty(_matchId) && !string.IsNullOrEmpty(playerId))
        {
            var matchGrain = GrainFactory.GetGrain<IMatchGrain>(_matchId);
            await matchGrain.RemovePlayerConnectionAsync(playerId, connectionId);
        }
    }

    /// <inheritdoc/>
    public Task<GameState> GetStateAsync(string? connectionId)
    {
        return Task.FromResult(GetStateInternal(connectionId));
    }

    /// <inheritdoc/>
    public Task<SessionStatus> GetStatusAsync() => Task.FromResult(_status);

    /// <inheritdoc/>
    public Task<bool> IsPlayerAsync(string playerId)
    {
        return Task.FromResult(_whitePlayerId == playerId || _redPlayerId == playerId);
    }

    /// <inheritdoc/>
    public Task<List<string>> GetAllConnectionsAsync()
    {
        var all = _whiteConnections.Concat(_redConnections).Concat(_spectatorConnections).ToList();
        return Task.FromResult(all);
    }

    // ===== IGameGrain: Match game initialization =====

    /// <inheritdoc/>
    public Task SetWhitePlayerAsync(string playerId, string? displayName = null, int? rating = null)
    {
        _whitePlayerId = playerId;
        _whitePlayerName = displayName ?? GenerateFriendlyName(playerId);
        if (rating.HasValue)
        {
            _whiteRating = rating;
            _whiteRatingBefore = rating;
        }

        _lastActivityAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SetRedPlayerAsync(string playerId, string? displayName = null, int? rating = null, string? connectionId = null)
    {
        _redPlayerId = playerId;
        _redPlayerName = displayName ?? GenerateFriendlyName(playerId);
        if (!string.IsNullOrEmpty(connectionId))
        {
            _redConnections.Add(connectionId);
        }

        if (rating.HasValue)
        {
            _redRating = rating;
            _redRatingBefore = rating;
        }

        _lastActivityAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SetMatchInfoAsync(string matchId, int? targetScore, int? p1Score, int? p2Score, bool? isCrawford)
    {
        _matchId = matchId;
        _targetScore = targetScore;
        _player1Score = p1Score;
        _player2Score = p2Score;
        _isCrawfordGame = isCrawford;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SetTimeControlAsync(TimeControlConfig? config)
    {
        _timeControl = config;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SetCorrespondenceAsync(bool isCorrespondence, int? timePerMoveDays = null)
    {
        _isCorrespondence = isCorrespondence;
        _timePerMoveDays = timePerMoveDays;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SetAiGameAsync(bool isBotGame)
    {
        _isBotGame = isBotGame;
        _isRated = false; // AI games are always unrated
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task MarkCompletedAsync()
    {
        _status = SessionStatus.Completed;
        return Task.CompletedTask;
    }

    // ===== Private helpers =====

    private bool IsFull => _whitePlayerId != null && _redPlayerId != null;

    private CheckerColor? GetPlayerColorInternal(string connectionId)
    {
        if (_whiteConnections.Contains(connectionId)) return CheckerColor.White;
        if (_redConnections.Contains(connectionId)) return CheckerColor.Red;
        return null;
    }

    private string? GetPlayerIdByConnectionInternal(string connectionId)
    {
        if (_whiteConnections.Contains(connectionId)) return _whitePlayerId;
        if (_redConnections.Contains(connectionId)) return _redPlayerId;
        return null;
    }

    private bool IsPlayerTurnInternal(string connectionId)
    {
        var playerColor = GetPlayerColorInternal(connectionId);
        if (playerColor == null) return false;
        // Analysis mode: the controlling player can move both sides.
        if (_gameMode is AnalysisMode) return true;
        // Multiplayer mode: only the player whose color matches the engine's CurrentPlayer.
        return _engine.CurrentPlayer?.Color == playerColor;
    }

    private static string GenerateFriendlyName(string playerId)
    {
        if (playerId.Length >= 4)
        {
            return $"Player {playerId.Substring(playerId.Length - 4)}";
        }

        return playerId;
    }

    private void TryStartGameIfReady()
    {
        if (_engine.GameStarted) return;
        if (string.IsNullOrEmpty(_whitePlayerId) || string.IsNullOrEmpty(_redPlayerId)) return;
        if (_whiteConnections.Count == 0 && _redConnections.Count == 0) return;

        _engine.StartNewGame();
        _status = SessionStatus.InProgress;
    }

    private async Task SetPlayerDisplayNameAndRatingAsync(string playerId, string? displayName)
    {
        try
        {
            var user = await _userRepository.GetByUserIdAsync(playerId);
            if (user != null)
            {
                var effectiveName = !string.IsNullOrEmpty(displayName) ? displayName : user.DisplayName;
                if (!string.IsNullOrEmpty(effectiveName))
                {
                    if (_whitePlayerId == playerId) _whitePlayerName = effectiveName;
                    else if (_redPlayerId == playerId) _redPlayerName = effectiveName;
                }

                if (_whitePlayerId == playerId)
                {
                    _whiteRating ??= user.Rating;
                    _whiteRatingBefore ??= user.Rating;
                }
                else if (_redPlayerId == playerId)
                {
                    _redRating ??= user.Rating;
                    _redRatingBefore ??= user.Rating;
                }
            }
            else if (!string.IsNullOrEmpty(displayName))
            {
                if (_whitePlayerId == playerId) _whitePlayerName = displayName;
                else if (_redPlayerId == playerId) _redPlayerName = displayName;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch user data for {PlayerId}", playerId);
        }
    }

    private async Task SendChatHistoryAsync(string connectionId)
    {
        if (string.IsNullOrEmpty(_matchId)) return;

        var chatHistory = _chatService.GetMatchChatHistory(_matchId);
        if (chatHistory.Count == 0) return;

        var historyDto = new ChatHistoryDto
        {
            Messages = chatHistory.Select(m => new ChatMessageDto
            {
                SenderName = m.SenderConnectionId == connectionId ? "You" : m.SenderName,
                Message = m.Message,
                Timestamp = m.Timestamp,
                IsOwn = m.SenderConnectionId == connectionId
            }).ToList()
        };

        await _hubContext.Clients.Client(connectionId).ReceiveChatHistory(historyDto);
    }

    // ===== Persistence =====
    // Orleans IPersistentState is the source of truth (Phase 4a). The IGameRepository
    // dual-write keeps the queryable Games table fresh for HTTP endpoints like
    // /api/games and /api/player/{id}/active-games which read from Postgres directly.

    private async Task SaveGameAsync()
    {
        if (!_gameMode.ShouldPersist)
        {
            _logger.LogDebug("Skipping save for analysis game {GameId}", this.GetPrimaryKeyString());
            return;
        }

        CaptureToState();
        await _state.WriteStateAsync();

        var game = BuildGameModel();
        await _gameRepository.SaveGameAsync(game);
        _logger.LogDebug("Saved game {GameId}, Status={Status}", this.GetPrimaryKeyString(), game.Status);
    }

    private void CaptureToState()
    {
        var s = _state.State;
        s.WhitePlayerId = _whitePlayerId;
        s.RedPlayerId = _redPlayerId;
        s.WhitePlayerName = _whitePlayerName;
        s.RedPlayerName = _redPlayerName;
        s.WhiteRating = _whiteRating;
        s.RedRating = _redRating;
        s.WhiteRatingBefore = _whiteRatingBefore;
        s.RedRatingBefore = _redRatingBefore;

        s.Status = _status;
        s.CreatedAt = _createdAt;
        s.LastActivityAt = _lastActivityAt;
        s.IsRated = _isRated;
        s.IsBotGame = _isBotGame;

        s.MatchId = _matchId;
        s.TargetScore = _targetScore;
        s.Player1Score = _player1Score;
        s.Player2Score = _player2Score;
        s.IsCrawfordGame = _isCrawfordGame;

        s.TimeControl = _timeControl;
        s.IsCorrespondence = _isCorrespondence;
        s.TimePerMoveDays = _timePerMoveDays;
        s.TurnDeadline = _turnDeadline;

        s.HasPendingDoubleOffer = _hasPendingDoubleOffer;
        s.PendingDoubleOfferedBy = _pendingDoubleOfferedBy;

        s.BoardState = SerializeBoardState(_engine.Board);
        s.WhiteCheckersOnBar = _engine.WhitePlayer.CheckersOnBar;
        s.RedCheckersOnBar = _engine.RedPlayer.CheckersOnBar;
        s.WhiteBornOff = _engine.WhitePlayer.CheckersBornOff;
        s.RedBornOff = _engine.RedPlayer.CheckersBornOff;
        s.CurrentPlayer = _engine.CurrentPlayer?.Color.ToString() ?? "White";
        s.Die1 = _engine.Dice.Die1;
        s.Die2 = _engine.Dice.Die2;
        s.RemainingMoves = new List<int>(_engine.RemainingMoves);
        s.DoublingCubeValue = _engine.DoublingCube.Value;
        s.DoublingCubeOwner = _engine.DoublingCube.Owner?.ToString();
        s.GameStarted = _engine.GameStarted;
        s.Winner = _engine.Winner?.Color.ToString();
        s.GameSgf = _engine.GameSgf ?? string.Empty;
    }

    private void ApplyFromState()
    {
        var s = _state.State;
        _whitePlayerId = s.WhitePlayerId;
        _redPlayerId = s.RedPlayerId;
        _whitePlayerName = s.WhitePlayerName;
        _redPlayerName = s.RedPlayerName;
        _whiteRating = s.WhiteRating;
        _redRating = s.RedRating;
        _whiteRatingBefore = s.WhiteRatingBefore;
        _redRatingBefore = s.RedRatingBefore;

        _status = s.Status;
        _createdAt = s.CreatedAt;
        _lastActivityAt = s.LastActivityAt;
        _isRated = s.IsRated;
        _isBotGame = s.IsBotGame;

        _matchId = s.MatchId;
        _targetScore = s.TargetScore;
        _player1Score = s.Player1Score;
        _player2Score = s.Player2Score;
        _isCrawfordGame = s.IsCrawfordGame;

        _timeControl = s.TimeControl;
        _isCorrespondence = s.IsCorrespondence;
        _timePerMoveDays = s.TimePerMoveDays;
        _turnDeadline = s.TurnDeadline;

        _hasPendingDoubleOffer = s.HasPendingDoubleOffer;
        _pendingDoubleOfferedBy = s.PendingDoubleOfferedBy;

        _engine = new GameEngine();
        _engine.Board.SetupInitialPosition();
        RestoreBoardState(_engine.Board, s.BoardState);
        _engine.WhitePlayer.CheckersOnBar = s.WhiteCheckersOnBar;
        _engine.RedPlayer.CheckersOnBar = s.RedCheckersOnBar;
        _engine.WhitePlayer.CheckersBornOff = s.WhiteBornOff;
        _engine.RedPlayer.CheckersBornOff = s.RedBornOff;

        _engine.Dice.SetDice(s.Die1, s.Die2);
        _engine.RemainingMoves.Clear();
        _engine.RemainingMoves.AddRange(s.RemainingMoves);

        if (s.DoublingCubeValue > 1)
        {
            SetPrivateField(_engine.DoublingCube, "<Value>k__BackingField", s.DoublingCubeValue);
            if (s.DoublingCubeOwner != null)
            {
                var owner = Enum.Parse<CheckerColor>(s.DoublingCubeOwner);
                SetPrivateField(_engine.DoublingCube, "<Owner>k__BackingField", (CheckerColor?)owner);
            }
        }

        var currentPlayer = s.CurrentPlayer == "White" ? _engine.WhitePlayer : _engine.RedPlayer;
        SetPrivateField(_engine, "<CurrentPlayer>k__BackingField", currentPlayer);

        if (s.GameStarted)
        {
            SetPrivateField(_engine, "<GameStarted>k__BackingField", true);
        }

        if (!string.IsNullOrEmpty(s.Winner))
        {
            var winnerColor = Enum.Parse<CheckerColor>(s.Winner);
            _engine.Winner = winnerColor == CheckerColor.White ? _engine.WhitePlayer : _engine.RedPlayer;
        }
    }

    private ServerGame BuildGameModel()
    {
        var gameId = this.GetPrimaryKeyString();
        var engine = _engine;

        var whiteUserId = IsRegisteredUserId(_whitePlayerId) ? _whitePlayerId : null;
        var redUserId = IsRegisteredUserId(_redPlayerId) ? _redPlayerId : null;

        var game = new ServerGame
        {
            GameId = gameId,
            WhitePlayerId = _whitePlayerId,
            RedPlayerId = _redPlayerId,
            WhiteUserId = whiteUserId,
            RedUserId = redUserId,
            WhitePlayerName = _whitePlayerName,
            RedPlayerName = _redPlayerName,
            Status = engine.Winner != null ? "Completed" : "InProgress",
            GameStarted = engine.GameStarted,
            BoardState = SerializeBoardState(engine.Board),
            WhiteCheckersOnBar = engine.WhitePlayer.CheckersOnBar,
            RedCheckersOnBar = engine.RedPlayer.CheckersOnBar,
            WhiteBornOff = engine.WhitePlayer.CheckersBornOff,
            RedBornOff = engine.RedPlayer.CheckersBornOff,
            CurrentPlayer = engine.CurrentPlayer?.Color.ToString() ?? "White",
            Die1 = engine.Dice.Die1,
            Die2 = engine.Dice.Die2,
            RemainingMoves = new List<int>(engine.RemainingMoves),
            DoublingCubeValue = engine.DoublingCube.Value,
            DoublingCubeOwner = engine.DoublingCube.Owner?.ToString(),
            Moves = engine.MoveHistory.Select(m => m.ToNotation()).ToList(),
            MoveCount = engine.MoveHistory.Count,
            GameSgf = engine.GameSgf,
            CreatedAt = _createdAt,
            LastUpdatedAt = DateTime.UtcNow,
            IsAiOpponent = GameEngineMapper.IsAiPlayer(_whitePlayerId) || GameEngineMapper.IsAiPlayer(_redPlayerId),
            MatchId = _matchId,
            IsCrawfordGame = _isCrawfordGame ?? false,
        };

        game.IsRated = game.IsAiOpponent ? false : _isRated;

        if (engine.Winner != null)
        {
            game.Winner = engine.Winner.Color.ToString();
            game.WinType = engine.DetermineWinType().ToString();
            game.Stakes = engine.GetGameResult();
            game.CompletedAt = DateTime.UtcNow;
            game.DurationSeconds = (int)(DateTime.UtcNow - _createdAt).TotalSeconds;
        }

        return game;
    }

    private void LoadFromGame(ServerGame game)
    {
        _createdAt = game.CreatedAt;
        _whitePlayerId = game.WhitePlayerId;
        _redPlayerId = game.RedPlayerId;
        _whitePlayerName = game.WhitePlayerName;
        _redPlayerName = game.RedPlayerName;
        _matchId = game.MatchId;
        _isCrawfordGame = game.IsCrawfordGame;
        _isRated = game.IsRated;
        _isBotGame = game.IsAiOpponent;

        _engine = new GameEngine();
        _engine.Board.SetupInitialPosition();

        RestoreBoardState(_engine.Board, game.BoardState);
        _engine.WhitePlayer.CheckersOnBar = game.WhiteCheckersOnBar;
        _engine.RedPlayer.CheckersOnBar = game.RedCheckersOnBar;
        _engine.WhitePlayer.CheckersBornOff = game.WhiteBornOff;
        _engine.RedPlayer.CheckersBornOff = game.RedBornOff;

        _engine.Dice.SetDice(game.Die1, game.Die2);
        _engine.RemainingMoves.Clear();
        _engine.RemainingMoves.AddRange(game.RemainingMoves);

        if (game.DoublingCubeValue > 1)
        {
            SetPrivateField(_engine.DoublingCube, "<Value>k__BackingField", game.DoublingCubeValue);
            if (game.DoublingCubeOwner != null)
            {
                var owner = Enum.Parse<CheckerColor>(game.DoublingCubeOwner);
                SetPrivateField(_engine.DoublingCube, "<Owner>k__BackingField", (CheckerColor?)owner);
            }
        }

        var currentPlayer = game.CurrentPlayer == "White" ? _engine.WhitePlayer : _engine.RedPlayer;
        SetPrivateField(_engine, "<CurrentPlayer>k__BackingField", currentPlayer);

        if (game.GameStarted)
        {
            SetPrivateField(_engine, "<GameStarted>k__BackingField", true);
            _status = SessionStatus.InProgress;
        }

        if (!string.IsNullOrEmpty(game.Winner))
        {
            var winnerColor = Enum.Parse<CheckerColor>(game.Winner);
            _engine.Winner = winnerColor == CheckerColor.White ? _engine.WhitePlayer : _engine.RedPlayer;
            _status = SessionStatus.Completed;
        }
    }

    private static void SetPrivateField(object obj, string fieldName, object? value)
    {
        var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        field?.SetValue(obj, value);
    }

    private static List<PointStateDto> SerializeBoardState(Board board)
    {
        var states = new List<PointStateDto>();
        for (int i = 1; i <= 24; i++)
        {
            var point = board.GetPoint(i);
            states.Add(new PointStateDto { Position = i, Color = point.Color?.ToString(), Count = point.Count });
        }

        return states;
    }

    private static void RestoreBoardState(Board board, List<PointStateDto> states)
    {
        for (int i = 1; i <= 24; i++) board.GetPoint(i).Checkers.Clear();
        foreach (var state in states)
        {
            if (state.Color != null && state.Count > 0)
            {
                var color = Enum.Parse<CheckerColor>(state.Color);
                var point = board.GetPoint(state.Position);
                for (int i = 0; i < state.Count; i++) point.AddChecker(color);
            }
        }
    }

    private static bool IsRegisteredUserId(string? playerId)
    {
        return !string.IsNullOrEmpty(playerId) && Guid.TryParse(playerId, out _);
    }
}
