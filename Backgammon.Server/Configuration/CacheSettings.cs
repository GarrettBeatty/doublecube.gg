namespace Backgammon.Server.Configuration;

/// <summary>
/// Configuration settings for HybridCache TTLs.
/// These values can be overridden in appsettings.json or environment variables.
/// </summary>
public class CacheSettings
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public const string SectionName = "CacheSettings";

    /// <summary>
    /// User profile cache settings
    /// </summary>
    public CacheDuration UserProfile { get; set; } = new()
    {
        Expiration = TimeSpan.FromMinutes(5),
        LocalCacheExpiration = TimeSpan.FromMinutes(1)
    };

    /// <summary>
    /// Player stats cache settings
    /// </summary>
    public CacheDuration PlayerStats { get; set; } = new()
    {
        Expiration = TimeSpan.FromMinutes(15),
        LocalCacheExpiration = TimeSpan.FromMinutes(3)
    };

    /// <summary>
    /// Player game history cache settings
    /// </summary>
    public CacheDuration PlayerGames { get; set; } = new()
    {
        Expiration = TimeSpan.FromMinutes(10),
        LocalCacheExpiration = TimeSpan.FromMinutes(2)
    };

    /// <summary>
    /// Friends list cache settings
    /// </summary>
    public CacheDuration Friends { get; set; } = new()
    {
        Expiration = TimeSpan.FromMinutes(5),
        LocalCacheExpiration = TimeSpan.FromMinutes(1)
    };
}

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
