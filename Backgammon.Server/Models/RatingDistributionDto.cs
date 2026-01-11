using Tapper;

namespace Backgammon.Server.Models;

/// <summary>
/// Rating distribution data for displaying where a player stands compared to others.
/// </summary>
[TranspilationSource]
public class RatingDistributionDto
{
    /// <summary>
    /// Distribution buckets for the histogram.
    /// </summary>
    public List<RatingBucketDto> Buckets { get; set; } = new();

    /// <summary>
    /// Current user's rating (null if not authenticated).
    /// </summary>
    public int? UserRating { get; set; }

    /// <summary>
    /// Current user's percentile (e.g., 75 means better than 75% of players).
    /// </summary>
    public double? UserPercentile { get; set; }

    /// <summary>
    /// Total number of rated players.
    /// </summary>
    public int TotalPlayers { get; set; }

    /// <summary>
    /// Average rating across all players.
    /// </summary>
    public double AverageRating { get; set; }

    /// <summary>
    /// Median rating.
    /// </summary>
    public int MedianRating { get; set; }
}
