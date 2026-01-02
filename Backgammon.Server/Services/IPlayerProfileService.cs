using Backgammon.Server.Models;

namespace Backgammon.Server.Services;

/// <summary>
/// Service for retrieving player profile data
/// </summary>
public interface IPlayerProfileService
{
    /// <summary>
    /// Get player profile with privacy settings applied
    /// </summary>
    Task<(PlayerProfileDto? Profile, string? Error)> GetPlayerProfileAsync(string username, string? viewingUserId);
}
