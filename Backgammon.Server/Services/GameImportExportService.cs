using Backgammon.Core;
using Backgammon.Server.Extensions;
using Backgammon.Server.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Backgammon.Server.Services;

/// <summary>
/// Handles game position import/export using SGF format
/// </summary>
public class GameImportExportService : IGameImportExportService
{
    private readonly IGameSessionManager _sessionManager;
    private readonly IHubContext<GameHub> _hubContext;
    private readonly ILogger<GameImportExportService> _logger;

    public GameImportExportService(
        IGameSessionManager sessionManager,
        IHubContext<GameHub> hubContext,
        ILogger<GameImportExportService> logger)
    {
        _sessionManager = sessionManager;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task<string> ExportPositionAsync(string connectionId)
    {
        var session = _sessionManager.GetGameByPlayer(connectionId);

        if (session == null)
        {
            await _hubContext.Clients.Client(connectionId).SendAsync("Error", "You are not in a game");
            return string.Empty;
        }

        try
        {
            return SgfSerializer.ExportPosition(session.Engine);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting position");
            await _hubContext.Clients.Client(connectionId).SendAsync(
                "Error",
                $"Failed to export position: {ex.Message}");
            return string.Empty;
        }
    }

    public async Task ImportPositionAsync(string connectionId, string sgf)
    {
        var session = _sessionManager.GetGameByPlayer(connectionId);

        if (session == null)
        {
            await _hubContext.Clients.Client(connectionId).SendAsync("Error", "You are not in a game");
            return;
        }

        // Check if import is allowed in this game mode
        var features = session.GameMode.GetFeatures();
        if (!features.AllowImportExport)
        {
            await _hubContext.Clients.Client(connectionId).SendAsync("Error", "Cannot import positions in this game mode");
            return;
        }

        try
        {
            SgfSerializer.ImportPosition(session.Engine, sgf);

            // Send updated game state to all players
            if (!string.IsNullOrEmpty(session.WhiteConnectionId))
            {
                var whiteState = session.GetState(session.WhiteConnectionId);
                await _hubContext.Clients.Client(session.WhiteConnectionId).SendAsync("GameUpdate", whiteState);
            }

            if (!string.IsNullOrEmpty(session.RedConnectionId))
            {
                var redState = session.GetState(session.RedConnectionId);
                await _hubContext.Clients.Client(session.RedConnectionId).SendAsync("GameUpdate", redState);
            }

            _logger.LogInformation("Position imported successfully for game {GameId}", session.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing position");
            await _hubContext.Clients.Client(connectionId).SendAsync(
                "Error",
                $"Failed to import position: {ex.Message}");
        }
    }
}
