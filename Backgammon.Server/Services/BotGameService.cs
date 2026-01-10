using Backgammon.Core;
using Backgammon.Server.Configuration;
using Backgammon.Server.Hubs;
using Backgammon.Server.Hubs.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Backgammon.Server.Services;

/// <summary>
/// Background service that manages always-running bot vs bot games.
/// Maintains 1-2 bot games at all times, cycling through different AI matchups.
/// </summary>
public class BotGameService : BackgroundService
{
    private readonly IGameSessionManager _sessionManager;
    private readonly IAiMoveService _aiMoveService;
    private readonly IHubContext<GameHub, IGameHubClient> _hubContext;
    private readonly IOptions<FeatureFlags> _features;
    private readonly ILogger<BotGameService> _logger;

    // Matchup definitions (White vs Red)
    private readonly (string White, string Red)[] _matchups = new[]
    {
        ("greedy", "greedy"),  // Greedy vs Greedy
        ("greedy", "random"),  // Greedy vs Random
        ("random", "random") // Random vs Random
    };

    // Track active bot game IDs
    private readonly List<string> _activeBotGameIds = new();
    private int _currentMatchupIndex = 0;

    public BotGameService(
        IGameSessionManager sessionManager,
        IAiMoveService aiMoveService,
        IHubContext<GameHub, IGameHubClient> hubContext,
        IOptions<FeatureFlags> features,
        ILogger<BotGameService> logger)
    {
        _sessionManager = sessionManager;
        _aiMoveService = aiMoveService;
        _hubContext = hubContext;
        _features = features;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Check if bot games are enabled
        if (!_features.Value.BotGamesEnabled)
        {
            _logger.LogInformation("Bot games disabled via feature flag (BotGamesEnabled=false)");
            return;
        }

        _logger.LogInformation(
            "Bot game service starting. MaxBotGames={MaxGames}, RestartDelay={Delay}s",
            _features.Value.MaxBotGames,
            _features.Value.BotGameRestartDelaySeconds);

        // Start initial bot games
        for (int i = 0; i < _features.Value.MaxBotGames; i++)
        {
            await StartBotGameAsync(stoppingToken);
            // Small delay between starting games to avoid simultaneous initialization
            await Task.Delay(500, stoppingToken);
        }

        // Keep service running (games manage themselves via autonomous loops)
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            _logger.LogDebug("Bot game service heartbeat. Active games: {Count}", _activeBotGameIds.Count);
        }

        _logger.LogInformation("Bot game service stopping");
    }

    private async Task StartBotGameAsync(CancellationToken ct)
    {
        try
        {
            // Select next matchup
            var (white, red) = _matchups[_currentMatchupIndex];
            var whiteAi = white;
            var redAi = red;
            _currentMatchupIndex = (_currentMatchupIndex + 1) % _matchups.Length;

            // Create game session
            var session = _sessionManager.CreateGame();
            session.IsBotGame = true;

            // Add AI players
            var whitePlayerId = _aiMoveService.GenerateAiPlayerId(whiteAi);
            var redPlayerId = _aiMoveService.GenerateAiPlayerId(redAi);

            session.AddPlayer(whitePlayerId, string.Empty);  // Empty connection ID for bots
            session.AddPlayer(redPlayerId, string.Empty);

            // Set friendly names
            session.SetPlayerName(whitePlayerId, GetBotName(whiteAi, "White"));
            session.SetPlayerName(redPlayerId, GetBotName(redAi, "Red"));

            _activeBotGameIds.Add(session.Id);

            _logger.LogInformation(
                "Started bot game {GameId}: {White} vs {Red}",
                session.Id,
                session.WhitePlayerName,
                session.RedPlayerName);

            // Start autonomous game loop (non-blocking)
            _ = Task.Run(() => ExecuteBotGameLoopAsync(session, ct), ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start bot game");
        }
    }

    private async Task ExecuteBotGameLoopAsync(GameSession session, CancellationToken ct)
    {
        try
        {
            var engine = session.Engine;

            _logger.LogInformation("Bot game loop started for {GameId}", session.Id);

            // Game loop - alternate turns between AI players
            while (!engine.GameOver && !ct.IsCancellationRequested)
            {
                var currentPlayerId = engine.CurrentPlayer.Color == CheckerColor.White
                    ? session.WhitePlayerId
                    : session.RedPlayerId;

                if (currentPlayerId == null)
                {
                    _logger.LogError("Current player ID is null in bot game {GameId}", session.Id);
                    break;
                }

                // Execute AI turn with spectator broadcasting
                await _aiMoveService.ExecuteAiTurnAsync(session, currentPlayerId, async () =>
                {
                    // Broadcast to all spectators
                    var state = session.GetState(null);  // null = spectator view
                    foreach (var spectatorId in session.SpectatorConnections)
                    {
                        await _hubContext.Clients.Client(spectatorId).GameUpdate(state);
                    }
                });
            }

            // Game over
            var winner = engine.Winner;
            if (winner != null)
            {
                var winnerName = winner.Color == CheckerColor.White ? session.WhitePlayerName : session.RedPlayerName;
                var stakes = engine.GetGameResult();

                _logger.LogInformation(
                    "Bot game {GameId} completed. Winner: {Winner} (Stakes: {Stakes})",
                    session.Id,
                    winnerName,
                    stakes);

                // Broadcast final state to spectators
                var finalState = session.GetState(null);
                foreach (var spectatorId in session.SpectatorConnections)
                {
                    await _hubContext.Clients.Client(spectatorId).GameOver(finalState);
                }

                // Wait before restarting (allow spectators to see results)
                var delayMs = _features.Value.BotGameRestartDelaySeconds * 1000;
                _logger.LogInformation("Waiting {Delay}ms before restarting bot game {GameId}", delayMs, session.Id);
                await Task.Delay(delayMs, ct);
            }
            else
            {
                _logger.LogWarning("Bot game {GameId} ended without winner (cancelled?)", session.Id);
            }

            // Cleanup
            _activeBotGameIds.Remove(session.Id);
            _sessionManager.RemoveGame(session.Id);
            _logger.LogInformation("Bot game {GameId} cleaned up", session.Id);

            // Start new bot game (if not cancelled)
            if (!ct.IsCancellationRequested)
            {
                await StartBotGameAsync(ct);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Bot game loop cancelled for {GameId}", session.Id);
            // Cleanup on cancellation
            _activeBotGameIds.Remove(session.Id);
            _sessionManager.RemoveGame(session.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bot game loop for {GameId}", session.Id);
            // Cleanup on error
            _activeBotGameIds.Remove(session.Id);
            _sessionManager.RemoveGame(session.Id);

            // Attempt to restart if not cancelled
            if (!ct.IsCancellationRequested)
            {
                _logger.LogInformation("Attempting to restart bot game after error");
                await Task.Delay(5000, ct);  // Wait 5 seconds before retry
                await StartBotGameAsync(ct);
            }
        }
    }

    private string GetBotName(string aiType, string color)
    {
        var botType = aiType == "greedy" ? "GreedyBot" : "RandomBot";
        return $"{botType} ({color})";
    }
}
