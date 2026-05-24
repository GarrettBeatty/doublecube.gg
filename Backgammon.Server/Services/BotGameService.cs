using Backgammon.Server.Configuration;
using Backgammon.Server.Grains.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;

namespace Backgammon.Server.Services;

/// <summary>
/// Background service that manages always-running bot vs bot games.
/// Maintains 1-2 bot games at all times, cycling through different AI matchups.
/// Game state is managed by Orleans grains — this service only creates and monitors games.
/// </summary>
public class BotGameService : BackgroundService
{
    private readonly IGrainFactory _grainFactory;
    private readonly IAiMoveService _aiMoveService;
    private readonly IOptions<FeatureFlags> _features;
    private readonly ILogger<BotGameService> _logger;

    // Matchup definitions (White vs Red)
    private readonly (string White, string Red)[] _matchups = new[]
    {
        ("greedy", "greedy"),  // Greedy vs Greedy
        ("greedy", "random"),  // Greedy vs Random
        ("random", "random")   // Random vs Random
    };

    private int _currentMatchupIndex = 0;

    public BotGameService(
        IGrainFactory grainFactory,
        IAiMoveService aiMoveService,
        IOptions<FeatureFlags> features,
        ILogger<BotGameService> logger)
    {
        _grainFactory = grainFactory;
        _aiMoveService = aiMoveService;
        _features = features;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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
            await Task.Delay(500, stoppingToken);
        }

        // Keep service running (games manage themselves via grain timers)
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }

        _logger.LogInformation("Bot game service stopping");
    }

    private async Task StartBotGameAsync(CancellationToken ct)
    {
        try
        {
            var (white, red) = _matchups[_currentMatchupIndex];
            _currentMatchupIndex = (_currentMatchupIndex + 1) % _matchups.Length;

            var gameId = Guid.NewGuid().ToString();
            var grain = _grainFactory.GetGrain<IGameGrain>(gameId);

            var whitePlayerId = _aiMoveService.GenerateAiPlayerId(white);
            var redPlayerId = _aiMoveService.GenerateAiPlayerId(red);
            var whiteName = GetBotName(white, "White");
            var redName = GetBotName(red, "Red");

            // Configure the game as a bot game (unrated, no match context)
            await grain.SetAiGameAsync(isBotGame: true);
            await grain.SetWhitePlayerAsync(whitePlayerId, whiteName);
            await grain.SetRedPlayerAsync(redPlayerId, redName);

            // Join both AI players with empty connection IDs — grain handles AI execution internally
            await grain.JoinAsync(whitePlayerId, string.Empty, whiteName);
            await grain.JoinAsync(redPlayerId, string.Empty, redName);

            _logger.LogInformation(
                "Started bot game {GameId}: {White} vs {Red}",
                gameId,
                whiteName,
                redName);

            // Monitor game completion in background and restart when done
            _ = Task.Run(() => MonitorBotGameAsync(gameId, ct), ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start bot game");
        }
    }

    private async Task MonitorBotGameAsync(string gameId, CancellationToken ct)
    {
        try
        {
            var grain = _grainFactory.GetGrain<IGameGrain>(gameId);

            // Poll until game is no longer in progress
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
                var status = await grain.GetStatusAsync();
                if (status == Models.SessionStatus.Completed)
                {
                    _logger.LogInformation("Bot game {GameId} completed", gameId);
                    break;
                }
            }

            // Wait before restarting to allow spectators to see results
            var delayMs = _features.Value.BotGameRestartDelaySeconds * 1000;
            await Task.Delay(delayMs, ct);

            if (!ct.IsCancellationRequested)
            {
                await StartBotGameAsync(ct);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Bot game monitor cancelled for {GameId}", gameId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error monitoring bot game {GameId}", gameId);

            if (!ct.IsCancellationRequested)
            {
                await Task.Delay(5000, ct);
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
