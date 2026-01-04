namespace Backgammon.Server.Services;

/// <summary>
/// Handles game position import/export using SGF format
/// </summary>
public interface IGameImportExportService
{
    /// <summary>
    /// Export the current position (base64-encoded SGF)
    /// </summary>
    Task<string> ExportPositionAsync(string connectionId);

    /// <summary>
    /// Import a position (auto-detects raw SGF or base64-encoded SGF)
    /// </summary>
    Task ImportPositionAsync(string connectionId, string positionData);
}
