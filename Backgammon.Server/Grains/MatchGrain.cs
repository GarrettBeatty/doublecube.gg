using Backgammon.Core;
using Backgammon.Server.Grains.Interfaces;
using Backgammon.Server.Models;
using Backgammon.Server.Services;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using Match = Backgammon.Server.Models.Match;
using ServerGame = Backgammon.Server.Models.Game;

namespace Backgammon.Server.Grains;

/// <summary>
/// Per-match aggregate grain. Owns Orleans-persisted match state and the in-memory
/// connection map. Replaces the mutating surface of MatchService and the full surface
/// of CorrespondenceGameService; queries that scan many matches still go through
/// MatchService against the Postgres index tables (kept fresh by the grain's dual-write).
/// </summary>
public class MatchGrain : Grain, IMatchGrain
{
    // ===== Injected services =====
    private readonly IMatchRepository _matchRepository;
    private readonly IGameRepository _gameRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAiPlayerManager _aiPlayerManager;
    private readonly IPlayerStatsService _playerStatsService;
    // Chat lives on IMatchChatGrain (Phase 6B); we resolve it from GrainFactory rather than DI.
    private readonly ILogger<MatchGrain> _logger;

    // ===== Orleans-persisted state =====
    private readonly IPersistentState<MatchGrainState> _state;

    // ===== Ephemeral connection tracking =====
    private readonly Dictionary<string, HashSet<string>> _playerConnections = new();

    public MatchGrain(
        [PersistentState("match", "Default")] IPersistentState<MatchGrainState> state,
        IMatchRepository matchRepository,
        IGameRepository gameRepository,
        IUserRepository userRepository,
        IAiPlayerManager aiPlayerManager,
        IPlayerStatsService playerStatsService,
        ILogger<MatchGrain> logger)
    {
        _state = state;
        _matchRepository = matchRepository;
        _gameRepository = gameRepository;
        _userRepository = userRepository;
        _aiPlayerManager = aiPlayerManager;
        _playerStatsService = playerStatsService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var matchId = this.GetPrimaryKeyString();
        _logger.LogDebug("Activating MatchGrain {MatchId}", matchId);

        // Prefer Orleans-persisted state. Fall back to MatchRepository for matches
        // created before Phase 4b — those will be migrated to Orleans state on the
        // next save (self-healing, same pattern as Phase 4a).
        if (!_state.RecordExists || !_state.State.IsInitialized)
        {
            try
            {
                var match = await _matchRepository.GetMatchByIdAsync(matchId);
                if (match != null)
                {
                    CaptureMatchToState(match);
                    await _state.WriteStateAsync();
                    _logger.LogInformation(
                        "MatchGrain {MatchId} restored from DB (migrated to Orleans state)",
                        matchId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load match {MatchId} from DB during activation", matchId);
            }
        }

        await base.OnActivateAsync(cancellationToken);
    }

    // ==================== Lifecycle ====================

    /// <inheritdoc/>
    public async Task<MatchCreationResult> CreateMatchAsync(MatchCreationRequest request)
    {
        var matchId = this.GetPrimaryKeyString();

        ValidateMatchCreationCommon(request.Player1Id, request.TargetScore);
        if (!new[] { "AI", "OpenLobby", "Friend" }.Contains(request.OpponentType))
        {
            throw new ArgumentException("OpponentType must be 'AI', 'OpenLobby', or 'Friend'");
        }

        if (request.OpponentType == "Friend" && string.IsNullOrWhiteSpace(request.Player2Id))
        {
            throw new ArgumentException("Player IDs cannot be null or empty");
        }

        if (request.OpponentType == "Friend" && request.Player1Id == request.Player2Id)
        {
            throw new ArgumentException("Player IDs cannot be identical");
        }

        var player1 = await _userRepository.GetByUserIdAsync(request.Player1Id);
        var player1Name = player1?.DisplayName ?? "Unknown";

        var s = _state.State;
        s.MatchId = matchId;
        s.TargetScore = request.TargetScore;
        s.Player1Id = request.Player1Id;
        s.Player1Name = player1Name;
        s.Player1DisplayName = request.Player1DisplayName;
        s.OpponentType = request.OpponentType;
        s.TimeControl = request.TimeControl ?? new TimeControlConfig
        {
            Type = TimeControlType.ChicagoPoint,
            DelaySeconds = 12,
        };
        s.IsRated = request.IsRated;
        s.CreatedAt = DateTime.UtcNow;
        s.LastUpdatedAt = DateTime.UtcNow;

        switch (request.OpponentType)
        {
            case "AI":
                var aiPlayerId = _aiPlayerManager.GetOrCreateAiForMatch(matchId, request.AiType);
                s.Player2Id = aiPlayerId;
                s.Player2Name = _aiPlayerManager.GetAiNameForMatch(matchId, request.AiType);
                s.Status = MatchStatus.InProgress;
                s.IsOpenLobby = false;
                s.IsRated = false; // AI matches always unrated
                break;

            case "Friend":
                var player2 = await _userRepository.GetByUserIdAsync(request.Player2Id!);
                s.Player2Id = request.Player2Id!;
                s.Player2Name = player2?.DisplayName ?? "Unknown";
                s.Status = MatchStatus.InProgress;
                s.IsOpenLobby = false;
                break;

            case "OpenLobby":
                s.Status = MatchStatus.WaitingForPlayers;
                s.IsOpenLobby = true;
                break;
        }

        // Create the first game record
        var gameId = Guid.NewGuid().ToString();
        var game = BuildFirstGame(gameId, s, isCrawford: false);
        s.CurrentGameId = gameId;
        s.GamesSummary.Add(new MatchGameSummary { GameId = gameId });

        await _gameRepository.SaveGameAsync(game);

        // Persist match — dual-write to Orleans + MatchRepository
        var match = BuildMatchFromState();
        await _matchRepository.SaveMatchAsync(match);
        await _matchRepository.AddGameToMatchAsync(matchId, gameId);
        await _state.WriteStateAsync();

        _logger.LogInformation(
            "Created match {MatchId} (type: {OpponentType}), first game: {GameId}",
            matchId,
            request.OpponentType,
            gameId);

        return new MatchCreationResult
        {
            MatchId = matchId,
            FirstGameId = gameId,
            Player1Id = s.Player1Id,
            Player2Id = string.IsNullOrEmpty(s.Player2Id) ? null : s.Player2Id,
            Player1Name = s.Player1Name,
            Player2Name = string.IsNullOrEmpty(s.Player2Name) ? null : s.Player2Name,
            TargetScore = s.TargetScore,
            OpponentType = s.OpponentType,
            IsRated = s.IsRated,
            IsCorrespondence = false,
        };
    }

    /// <inheritdoc/>
    public async Task<MatchCreationResult> CreateCorrespondenceMatchAsync(CorrespondenceMatchCreationRequest request)
    {
        var matchId = this.GetPrimaryKeyString();

        ValidateMatchCreationCommon(request.Player1Id, request.TargetScore);
        if (request.TimePerMoveDays <= 0 || request.TimePerMoveDays > 30)
        {
            throw new ArgumentException("Time per move must be between 1 and 30 days");
        }

        if (!new[] { "OpenLobby", "Friend" }.Contains(request.OpponentType))
        {
            throw new ArgumentException("Correspondence games support 'OpenLobby' or 'Friend' opponent types");
        }

        if (request.OpponentType == "Friend" && string.IsNullOrWhiteSpace(request.Player2Id))
        {
            throw new ArgumentException("Player 2 ID is required for Friend matches");
        }

        if (request.OpponentType == "Friend" && request.Player1Id == request.Player2Id)
        {
            throw new ArgumentException("Player IDs cannot be identical");
        }

        var player1 = await _userRepository.GetByUserIdAsync(request.Player1Id);
        var player1Name = !string.IsNullOrWhiteSpace(request.Player1DisplayName)
            ? request.Player1DisplayName
            : player1?.DisplayName ?? "Unknown";

        var s = _state.State;
        s.MatchId = matchId;
        s.TargetScore = request.TargetScore;
        s.Player1Id = request.Player1Id;
        s.Player1Name = player1Name!;
        s.Player1DisplayName = request.Player1DisplayName;
        s.OpponentType = request.OpponentType;
        s.IsCorrespondence = true;
        s.TimePerMoveDays = request.TimePerMoveDays;
        s.TimeControl = new TimeControlConfig { Type = TimeControlType.None };
        s.IsRated = request.IsRated;
        s.CreatedAt = DateTime.UtcNow;
        s.LastUpdatedAt = DateTime.UtcNow;

        if (request.OpponentType == "Friend")
        {
            var player2 = await _userRepository.GetByUserIdAsync(request.Player2Id!);
            s.Player2Id = request.Player2Id!;
            s.Player2Name = player2?.DisplayName ?? "Unknown";
            s.Status = MatchStatus.InProgress;
            s.IsOpenLobby = false;
            s.CurrentTurnPlayerId = request.Player1Id;
            s.TurnDeadline = DateTime.UtcNow.AddDays(request.TimePerMoveDays);
        }
        else
        {
            s.Status = MatchStatus.WaitingForPlayers;
            s.IsOpenLobby = true;
        }

        var gameId = Guid.NewGuid().ToString();
        var game = BuildFirstGame(gameId, s, isCrawford: false);
        game.IsRated = request.IsRated;
        s.CurrentGameId = gameId;
        s.GamesSummary.Add(new MatchGameSummary { GameId = gameId });

        await _gameRepository.SaveGameAsync(game);

        var match = BuildMatchFromState();
        await _matchRepository.SaveMatchAsync(match);
        await _matchRepository.AddGameToMatchAsync(matchId, gameId);
        await _state.WriteStateAsync();

        _logger.LogInformation(
            "Created correspondence match {MatchId} (type: {OpponentType}), time per move: {TimePerMove} days",
            matchId,
            request.OpponentType,
            request.TimePerMoveDays);

        return new MatchCreationResult
        {
            MatchId = matchId,
            FirstGameId = gameId,
            Player1Id = s.Player1Id,
            Player2Id = string.IsNullOrEmpty(s.Player2Id) ? null : s.Player2Id,
            Player1Name = s.Player1Name,
            Player2Name = string.IsNullOrEmpty(s.Player2Name) ? null : s.Player2Name,
            TargetScore = s.TargetScore,
            OpponentType = s.OpponentType,
            IsRated = s.IsRated,
            IsCorrespondence = true,
            TimePerMoveDays = s.TimePerMoveDays,
            TurnDeadline = s.TurnDeadline,
        };
    }

    /// <inheritdoc/>
    public async Task<MatchJoinResult> JoinAsync(string player2Id, string? player2DisplayName)
    {
        var matchId = this.GetPrimaryKeyString();
        var s = _state.State;

        if (!s.IsInitialized)
        {
            throw new InvalidOperationException($"Match {matchId} not found");
        }

        if (s.OpponentType != "OpenLobby" && s.OpponentType != "Friend")
        {
            throw new InvalidOperationException(
                $"Match {matchId} does not allow joining (OpponentType: {s.OpponentType})");
        }

        if (!string.IsNullOrEmpty(s.Player2Id))
        {
            throw new InvalidOperationException($"Match {matchId} already has a second player");
        }

        if (s.Status != MatchStatus.WaitingForPlayers)
        {
            throw new InvalidOperationException(
                $"Match {matchId} is not accepting players (Status: {s.Status})");
        }

        var player2 = await _userRepository.GetByUserIdAsync(player2Id);
        s.Player2Id = player2Id;
        s.Player2Name = player2?.DisplayName ?? "Unknown";
        s.Player2DisplayName = player2DisplayName;
        s.Status = MatchStatus.InProgress;
        s.LastUpdatedAt = DateTime.UtcNow;

        // Player 2's player-match index item is created on join (wasn't created on save).
        await _matchRepository.CreatePlayerMatchIndexAsync(
            player2Id,
            matchId,
            s.Player1Id,
            "InProgress",
            s.CreatedAt);

        // Correspondence: open the turn-tracking window for the opening roll phase.
        if (s.IsCorrespondence)
        {
            s.TurnDeadline = DateTime.UtcNow.AddDays(s.TimePerMoveDays);
            _logger.LogInformation(
                "Initialized correspondence turn tracking for match {MatchId} on join, Deadline={Deadline}",
                matchId,
                s.TurnDeadline);
        }

        var match = BuildMatchFromState();
        await _matchRepository.UpdateMatchAsync(match);
        await _state.WriteStateAsync();

        _logger.LogInformation(
            "Player {Player2Id} joined match {MatchId}, Status: WaitingForPlayers -> InProgress",
            player2Id,
            matchId);

        return new MatchJoinResult
        {
            MatchId = matchId,
            CurrentGameId = s.CurrentGameId,
            TargetScore = s.TargetScore,
            OpponentType = s.OpponentType,
            Player1Id = s.Player1Id,
            Player2Id = s.Player2Id,
            Player1Name = s.Player1Name,
            Player2Name = s.Player2Name,
            IsCorrespondence = s.IsCorrespondence,
        };
    }

    /// <inheritdoc/>
    public async Task<EnsureNextGameResult> EnsureNextGameAsync(string playerId)
    {
        var matchId = this.GetPrimaryKeyString();
        var s = _state.State;

        if (!s.IsInitialized)
        {
            return new EnsureNextGameResult { Error = "Match not found" };
        }

        if (playerId != s.Player1Id && playerId != s.Player2Id)
        {
            return new EnsureNextGameResult { Error = "You are not a player in this match" };
        }

        // If the current game is still playable, hand it back so the caller rejoins it.
        if (!string.IsNullOrEmpty(s.CurrentGameId))
        {
            var existingGrain = GrainFactory.GetGrain<IGameGrain>(s.CurrentGameId);
            var status = await existingGrain.GetStatusAsync();
            if (status == SessionStatus.InProgress || status == SessionStatus.WaitingForOpponent)
            {
                _logger.LogInformation(
                    "EnsureNextGame: returning existing game {GameId} (status={Status}) for match {MatchId}",
                    s.CurrentGameId,
                    status,
                    matchId);
                return new EnsureNextGameResult { GameId = s.CurrentGameId };
            }
        }

        if (s.Status != MatchStatus.InProgress)
        {
            return new EnsureNextGameResult { Error = "Cannot continue to next game" };
        }

        // Create the next game.
        var gameId = Guid.NewGuid().ToString();
        var game = new ServerGame
        {
            Id = gameId,
            GameId = gameId,
            WhitePlayerId = s.Player1Id,
            RedPlayerId = s.Player2Id,
            WhitePlayerName = s.Player1Name,
            RedPlayerName = s.Player2Name,
            Status = "InProgress",
            MatchId = matchId,
            IsCrawfordGame = s.IsCrawfordGame,
        };

        await _gameRepository.SaveGameAsync(game);
        await _matchRepository.AddGameToMatchAsync(matchId, gameId);

        s.CurrentGameId = gameId;
        s.GamesSummary.Add(new MatchGameSummary { GameId = gameId });
        s.LastUpdatedAt = DateTime.UtcNow;

        await _state.WriteStateAsync();

        _logger.LogInformation(
            "EnsureNextGame: created next game {GameId} for match {MatchId}",
            gameId,
            matchId);

        return new EnsureNextGameResult { GameId = gameId };
    }

    /// <inheritdoc/>
    public async Task<MatchCompletionResult> CompleteGameAsync(string gameId, GameCompletionInfo result)
    {
        var matchId = this.GetPrimaryKeyString();
        var s = _state.State;

        if (!s.IsInitialized)
        {
            throw new InvalidOperationException($"Match {matchId} not found");
        }

        _logger.LogInformation(
            "CompleteGameAsync: match={MatchId}, game={GameId}, winner={WinnerId}, points={Points}",
            matchId,
            gameId,
            result.WinnerId,
            result.PointsWon);

        // Update the embedded game summary.
        var gameSummary = s.GamesSummary.FirstOrDefault(g => g.GameId == gameId);
        if (gameSummary == null)
        {
            _logger.LogWarning(
                "Game {GameId} not found in GamesSummary for match {MatchId}, adding it now",
                gameId,
                matchId);
            gameSummary = new MatchGameSummary { GameId = gameId };
            s.GamesSummary.Add(gameSummary);
        }

        gameSummary.Winner = result.WinnerColor?.ToString();
        gameSummary.Stakes = result.PointsWon;
        gameSummary.WinType = result.WinType.ToString();
        gameSummary.IsCrawford = s.IsCrawfordGame;
        gameSummary.CompletedAt = DateTime.UtcNow;

        // Apply scoring + Crawford via Core.Match logic (operating on state mirror).
        var coreMatch = BuildCoreMatch();
        var coreGame = new Core.Game(gameId)
        {
            Winner = result.WinnerColor,
            Stakes = result.PointsWon,
            WinType = result.WinType,
            MatchId = matchId,
            IsCrawfordGame = s.IsCrawfordGame,
            Status = result.IsAbandoned ? Core.GameStatus.Abandoned
                : result.IsForfeit ? Core.GameStatus.Forfeit
                : Core.GameStatus.Completed,
        };

        coreMatch.AddGame(coreGame);

        var wasCrawford = s.IsCrawfordGame;
        if (!result.IsAbandoned)
        {
            coreMatch.UpdateScores(result.WinnerId, result.PointsWon);
        }

        // Mirror updates back to state.
        s.Player1Score = coreMatch.Player1Score;
        s.Player2Score = coreMatch.Player2Score;
        s.IsCrawfordGame = coreMatch.IsCrawfordGame;
        s.HasCrawfordGameBeenPlayed = coreMatch.HasCrawfordGameBeenPlayed;
        s.Status = coreMatch.Status;
        s.LastUpdatedAt = DateTime.UtcNow;

        if (!wasCrawford && s.IsCrawfordGame)
        {
            _logger.LogInformation("Crawford rule activated for match {MatchId}", matchId);
        }
        else if (wasCrawford && !s.IsCrawfordGame)
        {
            _logger.LogInformation("Crawford game completed for match {MatchId}", matchId);
        }

        var isComplete = coreMatch.IsMatchComplete();
        if (isComplete)
        {
            s.WinnerId = coreMatch.GetWinnerId();
            s.DurationSeconds = (int)(DateTime.UtcNow - s.CreatedAt).TotalSeconds;
            s.Status = MatchStatus.Completed;
            s.CompletedAt = DateTime.UtcNow;

            _aiPlayerManager.RemoveMatch(matchId);
            await GrainFactory.GetGrain<IMatchChatGrain>(matchId).ClearAsync();

            _logger.LogInformation(
                "Match {MatchId} completed. Winner: {WinnerId}, Score: {P1}-{P2}",
                matchId,
                s.WinnerId,
                s.Player1Score,
                s.Player2Score);
        }

        var match = BuildMatchFromState();
        await _matchRepository.UpdateMatchAsync(match);
        await _state.WriteStateAsync();

        // Update the game record with the win type metadata.
        var game = await _gameRepository.GetGameByGameIdAsync(gameId);
        if (game != null)
        {
            game.WinType = result.WinType.ToString();
            await _gameRepository.SaveGameAsync(game);
        }

        return new MatchCompletionResult
        {
            Player1Score = s.Player1Score,
            Player2Score = s.Player2Score,
            IsCrawfordGame = s.IsCrawfordGame,
            IsMatchComplete = isComplete,
            WinnerId = s.WinnerId,
        };
    }

    /// <inheritdoc/>
    public async Task AbandonAsync(string abandoningPlayerId)
    {
        var matchId = this.GetPrimaryKeyString();
        var s = _state.State;

        if (!s.IsInitialized)
        {
            return;
        }

        if (s.Status != MatchStatus.InProgress && s.Status != MatchStatus.WaitingForPlayers)
        {
            return;
        }

        s.Status = MatchStatus.Abandoned;
        s.CompletedAt = DateTime.UtcNow;
        s.DurationSeconds = (int)(s.CompletedAt.Value - s.CreatedAt).TotalSeconds;

        if (string.IsNullOrEmpty(s.Player2Id))
        {
            s.WinnerId = null;
        }
        else
        {
            s.WinnerId = s.Player1Id == abandoningPlayerId ? s.Player2Id : s.Player1Id;
        }

        var match = BuildMatchFromState();
        await _matchRepository.UpdateMatchAsync(match);
        await _state.WriteStateAsync();

        await GrainFactory.GetGrain<IMatchChatGrain>(matchId).ClearAsync();

        _logger.LogInformation("Match {MatchId} abandoned by {PlayerId}", matchId, abandoningPlayerId);
    }

    // ==================== Correspondence ====================

    /// <inheritdoc/>
    public async Task HandleTurnCompletedAsync(string nextPlayerId)
    {
        var matchId = this.GetPrimaryKeyString();
        var s = _state.State;

        if (!s.IsInitialized || !s.IsCorrespondence)
        {
            _logger.LogWarning(
                "Cannot handle turn completion: match {MatchId} not found or not correspondence",
                matchId);
            return;
        }

        var newDeadline = DateTime.UtcNow.AddDays(s.TimePerMoveDays);
        s.CurrentTurnPlayerId = nextPlayerId;
        s.TurnDeadline = newDeadline;
        s.LastUpdatedAt = DateTime.UtcNow;

        await _matchRepository.UpdateCorrespondenceTurnAsync(matchId, nextPlayerId, newDeadline);
        await _state.WriteStateAsync();

        _logger.LogInformation(
            "Turn completed in correspondence match {MatchId}. Next player: {PlayerId}, Deadline: {Deadline}",
            matchId,
            nextPlayerId,
            newDeadline);
    }

    /// <inheritdoc/>
    public async Task HandleTimeoutAsync()
    {
        var matchId = this.GetPrimaryKeyString();
        var s = _state.State;

        if (!s.IsInitialized || !s.IsCorrespondence)
        {
            _logger.LogWarning(
                "Cannot handle timeout: match {MatchId} not found or not correspondence",
                matchId);
            return;
        }

        if (s.TurnDeadline == null || DateTime.UtcNow < s.TurnDeadline)
        {
            _logger.LogWarning("Timeout check for match {MatchId}: deadline not expired", matchId);
            return;
        }

        var timedOutPlayerId = s.CurrentTurnPlayerId;
        if (timedOutPlayerId == null)
        {
            _logger.LogWarning(
                "Cannot handle timeout for match {MatchId}: CurrentTurnPlayerId is null",
                matchId);
            return;
        }

        var winnerId = timedOutPlayerId == s.Player1Id ? s.Player2Id : s.Player1Id;

        s.Status = MatchStatus.Completed;
        s.WinnerId = winnerId;
        s.CompletedAt = DateTime.UtcNow;
        s.DurationSeconds = (int)(DateTime.UtcNow - s.CreatedAt).TotalSeconds;
        s.CurrentTurnPlayerId = null;
        s.TurnDeadline = null;

        var match = BuildMatchFromState();
        await _matchRepository.UpdateMatchAsync(match);
        await _state.WriteStateAsync();

        // Update stats / ELO for rated games.
        if (!string.IsNullOrEmpty(s.CurrentGameId))
        {
            var currentGame = await _gameRepository.GetGameByGameIdAsync(s.CurrentGameId);
            if (currentGame != null && currentGame.IsRated)
            {
                currentGame.Status = "Completed";
                await _gameRepository.SaveGameAsync(currentGame);

                await _playerStatsService.UpdateStatsAfterGameCompletionAsync(currentGame);

                _logger.LogInformation(
                    "Updated stats and ELO ratings for timeout in match {MatchId}",
                    matchId);
            }
        }

        _logger.LogInformation(
            "Correspondence match {MatchId} ended by timeout. Winner: {WinnerId}, Timed out: {TimedOutId}",
            matchId,
            winnerId,
            timedOutPlayerId);
    }

    /// <inheritdoc/>
    public Task<MatchCorrespondenceInfo> GetCorrespondenceInfoAsync()
    {
        var s = _state.State;
        return Task.FromResult(new MatchCorrespondenceInfo
        {
            IsCorrespondence = s.IsCorrespondence,
            TimePerMoveDays = s.TimePerMoveDays,
            TurnDeadline = s.TurnDeadline,
            TargetScore = s.TargetScore,
            Player1Score = s.Player1Score,
            Player2Score = s.Player2Score,
        });
    }

    // ==================== Connection tracking (ephemeral) ====================

    /// <inheritdoc/>
    public Task<string?> GetCurrentGameIdAsync() => Task.FromResult(_state.State.CurrentGameId);

    /// <inheritdoc/>
    public async Task SetCurrentGameIdAsync(string gameId)
    {
        _state.State.CurrentGameId = gameId;
        _state.State.LastUpdatedAt = DateTime.UtcNow;
        await _state.WriteStateAsync();
    }

    /// <inheritdoc/>
    public Task<List<string>> GetPlayerConnectionsAsync(string playerId)
    {
        if (_playerConnections.TryGetValue(playerId, out var connections))
        {
            return Task.FromResult(connections.ToList());
        }

        return Task.FromResult(new List<string>());
    }

    /// <inheritdoc/>
    public Task TrackPlayerConnectionAsync(string playerId, string connectionId)
    {
        if (!_playerConnections.TryGetValue(playerId, out var connections))
        {
            connections = new HashSet<string>();
            _playerConnections[playerId] = connections;
        }

        connections.Add(connectionId);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task RemovePlayerConnectionAsync(string playerId, string connectionId)
    {
        if (_playerConnections.TryGetValue(playerId, out var connections))
        {
            connections.Remove(connectionId);
        }

        return Task.CompletedTask;
    }

    // ==================== Helpers ====================

    private static void ValidateMatchCreationCommon(string player1Id, int targetScore)
    {
        if (string.IsNullOrWhiteSpace(player1Id))
        {
            throw new ArgumentException("Player IDs cannot be null or empty");
        }

        if (targetScore <= 0 || targetScore > 25)
        {
            throw new ArgumentException("Target score must be between 1 and 25");
        }
    }

    private static ServerGame BuildFirstGame(string gameId, MatchGrainState s, bool isCrawford)
    {
        return new ServerGame
        {
            GameId = gameId,
            WhitePlayerId = s.Player1Id,
            RedPlayerId = s.Player2Id, // Empty string for OpenLobby matches until joined
            WhitePlayerName = s.Player1Name,
            RedPlayerName = s.Player2Name, // Empty string for OpenLobby matches until joined
            Status = "InProgress",
            MatchId = s.MatchId,
            IsCrawfordGame = isCrawford,
        };
    }

    /// <summary>
    /// Build a Core.Match mirror of the current persisted state so we can reuse Core
    /// scoring/Crawford/completion logic without duplicating it.
    /// </summary>
    private Core.Match BuildCoreMatch()
    {
        var s = _state.State;
        return new Core.Match
        {
            MatchId = s.MatchId,
            TargetScore = s.TargetScore,
            Player1Id = s.Player1Id,
            Player2Id = s.Player2Id,
            Player1Score = s.Player1Score,
            Player2Score = s.Player2Score,
            IsCrawfordGame = s.IsCrawfordGame,
            HasCrawfordGameBeenPlayed = s.HasCrawfordGameBeenPlayed,
            Status = s.Status,
            CreatedAt = s.CreatedAt,
            CompletedAt = s.CompletedAt,
            TimeControl = s.TimeControl,
        };
    }

    /// <summary>
    /// Build the server Match model from current persisted state. This is the shape
    /// dual-written to <see cref="IMatchRepository"/> so HTTP/query endpoints stay live.
    /// </summary>
    private Match BuildMatchFromState()
    {
        var s = _state.State;
        var match = new Match
        {
            CoreMatch = new Core.Match
            {
                MatchId = s.MatchId,
                TargetScore = s.TargetScore,
                Player1Id = s.Player1Id,
                Player2Id = s.Player2Id,
                Player1Score = s.Player1Score,
                Player2Score = s.Player2Score,
                IsCrawfordGame = s.IsCrawfordGame,
                HasCrawfordGameBeenPlayed = s.HasCrawfordGameBeenPlayed,
                Status = s.Status,
                CreatedAt = s.CreatedAt,
                CompletedAt = s.CompletedAt,
                TimeControl = s.TimeControl,
            },
            Player1Name = s.Player1Name,
            Player2Name = s.Player2Name,
            Player1DisplayName = s.Player1DisplayName,
            Player2DisplayName = s.Player2DisplayName,
            OpponentType = s.OpponentType,
            IsOpenLobby = s.IsOpenLobby,
            IsRated = s.IsRated,
            IsCorrespondence = s.IsCorrespondence,
            TimePerMoveDays = s.TimePerMoveDays,
            TurnDeadline = s.TurnDeadline,
            CurrentTurnPlayerId = s.CurrentTurnPlayerId,
            CurrentGameId = s.CurrentGameId,
            LastUpdatedAt = s.LastUpdatedAt,
            WinnerId = s.WinnerId,
            DurationSeconds = s.DurationSeconds,
            GamesSummary = s.GamesSummary.Select(g => new MatchGameSummary
            {
                GameId = g.GameId,
                Winner = g.Winner,
                Stakes = g.Stakes,
                WinType = g.WinType,
                IsCrawford = g.IsCrawford,
                CompletedAt = g.CompletedAt,
            }).ToList(),
        };
        return match;
    }

    /// <summary>
    /// Capture a Match (loaded from the repository fallback path) into Orleans state.
    /// Used during activation to migrate matches created before Phase 4b.
    /// </summary>
    private void CaptureMatchToState(Match match)
    {
        var s = _state.State;
        s.MatchId = match.MatchId;
        s.TargetScore = match.TargetScore;
        s.Player1Id = match.Player1Id;
        s.Player2Id = match.Player2Id;
        s.Player1Name = match.Player1Name ?? string.Empty;
        s.Player2Name = match.Player2Name ?? string.Empty;
        s.Player1DisplayName = match.Player1DisplayName;
        s.Player2DisplayName = match.Player2DisplayName;
        s.OpponentType = match.OpponentType ?? "Friend";
        s.IsOpenLobby = match.IsOpenLobby;
        s.IsRated = match.IsRated;
        s.Player1Score = match.Player1Score;
        s.Player2Score = match.Player2Score;
        s.IsCrawfordGame = match.IsCrawfordGame;
        s.HasCrawfordGameBeenPlayed = match.HasCrawfordGameBeenPlayed;
        s.TimeControl = match.TimeControl ?? new TimeControlConfig();
        s.Status = match.CoreMatch.Status;
        s.CreatedAt = match.CreatedAt;
        s.CompletedAt = match.CompletedAt;
        s.LastUpdatedAt = match.LastUpdatedAt;
        s.WinnerId = match.WinnerId;
        s.DurationSeconds = match.DurationSeconds;
        s.CurrentGameId = match.CurrentGameId;
        s.GamesSummary = match.GamesSummary?.Select(g => new MatchGameSummary
        {
            GameId = g.GameId,
            Winner = g.Winner,
            Stakes = g.Stakes,
            WinType = g.WinType,
            IsCrawford = g.IsCrawford,
            CompletedAt = g.CompletedAt,
        }).ToList() ?? new List<MatchGameSummary>();
        s.IsCorrespondence = match.IsCorrespondence;
        s.TimePerMoveDays = match.TimePerMoveDays;
        s.TurnDeadline = match.TurnDeadline;
        s.CurrentTurnPlayerId = match.CurrentTurnPlayerId;
    }
}
