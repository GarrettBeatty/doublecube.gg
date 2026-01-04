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
            // Export SGF and encode as base64 for clean URLs
            var sgf = SgfSerializer.ExportPosition(session.Engine);
            var bytes = System.Text.Encoding.UTF8.GetBytes(sgf);
            return Convert.ToBase64String(bytes);
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

    public async Task ImportPositionAsync(string connectionId, string positionData)
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
            var trimmed = positionData.Trim();
            string sgf;

            // Auto-detect format: raw SGF starts with "(;", otherwise assume base64
            if (trimmed.StartsWith("(;"))
            {
                // Raw SGF format
                _logger.LogInformation("Importing raw SGF position for game {GameId}", session.Id);
                sgf = trimmed;
            }
            else
            {
                // Assume base64-encoded SGF
                _logger.LogInformation("Importing base64-encoded SGF position for game {GameId}", session.Id);
                var bytes = Convert.FromBase64String(trimmed);
                sgf = System.Text.Encoding.UTF8.GetString(bytes);
            }

            // Import the SGF
            SgfSerializer.ImportPosition(session.Engine, sgf);

            // Send updated game state to all player connections (supports multi-tab)
            foreach (var whiteConnectionId in session.WhiteConnections)
            {
                var whiteState = session.GetState(whiteConnectionId);
                await _hubContext.Clients.Client(whiteConnectionId).SendAsync("GameUpdate", whiteState);
            }

            foreach (var redConnectionId in session.RedConnections)
            {
                var redState = session.GetState(redConnectionId);
                await _hubContext.Clients.Client(redConnectionId).SendAsync("GameUpdate", redState);
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
