using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Backgammon.Server.Services;

/// <summary>
/// Background service that periodically checks for expired correspondence games
/// and handles timeouts (auto-forfeit).
/// </summary>
public class CorrespondenceTimeoutService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CorrespondenceTimeoutService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

    public CorrespondenceTimeoutService(
        IServiceProvider serviceProvider,
        ILogger<CorrespondenceTimeoutService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Correspondence timeout service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckForTimeoutsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for correspondence timeouts");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Correspondence timeout service stopped");
    }

    private async Task CheckForTimeoutsAsync(CancellationToken stoppingToken)
    {
        _logger.LogDebug("Checking for expired correspondence matches...");

        // Create a scope to resolve scoped services
        using var scope = _serviceProvider.CreateScope();
        var matchRepository = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var correspondenceService = scope.ServiceProvider.GetRequiredService<ICorrespondenceGameService>();

        var expiredMatches = await matchRepository.GetExpiredCorrespondenceMatchesAsync();

        if (expiredMatches.Count == 0)
        {
            _logger.LogDebug("No expired correspondence matches found");
            return;
        }

        _logger.LogInformation("Found {Count} expired correspondence matches to process", expiredMatches.Count);

        foreach (var match in expiredMatches)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await correspondenceService.HandleTimeoutAsync(match.MatchId);
                _logger.LogInformation(
                    "Processed timeout for match {MatchId}. Timed out player: {PlayerId}",
                    match.MatchId,
                    match.CurrentTurnPlayerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process timeout for match {MatchId}", match.MatchId);
            }
        }
    }
}
