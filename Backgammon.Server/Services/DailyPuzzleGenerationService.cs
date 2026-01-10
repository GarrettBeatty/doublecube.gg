using Backgammon.Server.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Backgammon.Server.Services;

/// <summary>
/// Background service that generates daily puzzles at the configured time (default: midnight UTC).
/// Also ensures today's puzzle exists on startup.
/// </summary>
public class DailyPuzzleGenerationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly PuzzleSettings _settings;
    private readonly ILogger<DailyPuzzleGenerationService> _logger;

    public DailyPuzzleGenerationService(
        IServiceProvider serviceProvider,
        IOptions<PuzzleSettings> settings,
        ILogger<DailyPuzzleGenerationService> logger)
    {
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.EnableAutomaticGeneration)
        {
            _logger.LogInformation("Automatic puzzle generation is disabled");
            return;
        }

        _logger.LogInformation(
            "Daily puzzle generation service started. Generation time: {Time} UTC",
            _settings.GenerationTimeUtc);

        // Ensure today's puzzle exists on startup
        await EnsureTodaysPuzzleExistsAsync(stoppingToken);

        // Main loop - generate puzzles at the scheduled time
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var delay = CalculateDelayUntilNextGeneration();
                _logger.LogDebug("Next puzzle generation in {Delay}", delay);

                await Task.Delay(delay, stoppingToken);

                if (!stoppingToken.IsCancellationRequested)
                {
                    await GenerateDailyPuzzleAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in puzzle generation loop. Retrying in 1 hour.");
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        _logger.LogInformation("Daily puzzle generation service stopped");
    }

    private async Task EnsureTodaysPuzzleExistsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var puzzleService = scope.ServiceProvider.GetRequiredService<IDailyPuzzleService>();

            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

            if (await puzzleService.PuzzleExistsAsync(today))
            {
                _logger.LogInformation("Today's puzzle ({Date}) already exists", today);
                return;
            }

            _logger.LogInformation("Generating today's puzzle for {Date}", today);
            await puzzleService.GeneratePuzzleForDateAsync(today);
            _logger.LogInformation("Successfully generated puzzle for {Date}", today);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure today's puzzle exists");
            // Don't throw - we'll try again at the scheduled time
        }
    }

    private async Task GenerateDailyPuzzleAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var puzzleService = scope.ServiceProvider.GetRequiredService<IDailyPuzzleService>();

            // Generate for today (which is now after midnight)
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

            if (await puzzleService.PuzzleExistsAsync(today))
            {
                _logger.LogInformation("Puzzle for {Date} already exists, skipping generation", today);
                return;
            }

            _logger.LogInformation("Generating puzzle for {Date}", today);
            await puzzleService.GeneratePuzzleForDateAsync(today);
            _logger.LogInformation("Successfully generated puzzle for {Date}", today);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate daily puzzle");
            throw;
        }
    }

    private TimeSpan CalculateDelayUntilNextGeneration()
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var generationTime = today.Add(_settings.GenerationTimeUtc);

        // If we've already passed today's generation time, schedule for tomorrow
        if (now >= generationTime)
        {
            generationTime = generationTime.AddDays(1);
        }

        var delay = generationTime - now;

        // Ensure minimum delay of 1 second to avoid tight loops
        if (delay < TimeSpan.FromSeconds(1))
        {
            delay = TimeSpan.FromSeconds(1);
        }

        return delay;
    }
}
