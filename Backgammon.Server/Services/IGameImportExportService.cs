namespace Backgammon.Server.Services;

/// <summary>
/// Handles game position import/export using SGF format
/// </summary>
public interface IGameImportExportService
{
    /// <summary>
    /// Export the current position to SGF format
    /// </summary>
    Task<string> ExportPositionAsync(string connectionId);

    /// <summary>
    /// Import a position from SGF format
    /// </summary>
    Task ImportPositionAsync(string connectionId, string sgf);
}
