using Backgammon.Server.Models;

namespace Backgammon.Server.Services;

/// <summary>
/// Handles queries for valid moves and destinations
/// </summary>
public interface IMoveQueryService
{
    /// <summary>
    /// Get list of points that have checkers that can be moved
    /// </summary>
    List<int> GetValidSources(string connectionId);

    /// <summary>
    /// Get list of valid destinations from a specific source point
    /// </summary>
    List<MoveDto> GetValidDestinations(string connectionId, int fromPoint);
}
