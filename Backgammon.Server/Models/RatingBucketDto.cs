using Tapper;

namespace Backgammon.Server.Models;

/// <summary>
/// A single bucket in the rating distribution histogram.
/// </summary>
[TranspilationSource]
public class RatingBucketDto
{
    /// <summary>
    /// Lower bound of the rating range (inclusive).
    /// </summary>
    public int MinRating { get; set; }

    /// <summary>
    /// Upper bound of the rating range (exclusive).
    /// </summary>
    public int MaxRating { get; set; }

    /// <summary>
    /// Label for this bucket (e.g., "1400-1500").
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Number of players in this rating range.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Percentage of total players in this bucket.
    /// </summary>
    public double Percentage { get; set; }

    /// <summary>
    /// Whether the current user falls in this bucket.
    /// </summary>
    public bool IsUserBucket { get; set; }
}
