namespace Backgammon.Server.Configuration;

/// <summary>
/// Cache duration configuration for a specific cache type
/// </summary>
public class CacheDuration
{
    /// <summary>
    /// Distributed cache expiration time (how long before cache entry is removed)
    /// </summary>
    public TimeSpan Expiration { get; set; }

    /// <summary>
    /// Local in-memory cache expiration time (should be shorter than Expiration)
    /// </summary>
    public TimeSpan LocalCacheExpiration { get; set; }
}
