namespace Backgammon.Server.Services;

/// <summary>
/// Configuration options for correspondence timeout checking
/// </summary>
public class CorrespondenceTimeoutOptions
{
    /// <summary>
    /// How often to check for expired correspondence games (in hours)
    /// </summary>
    public int CheckIntervalHours { get; set; } = 1;
}
