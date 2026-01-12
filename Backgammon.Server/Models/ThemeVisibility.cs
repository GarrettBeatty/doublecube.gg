namespace Backgammon.Server.Models;

/// <summary>
/// Theme visibility options.
/// </summary>
public enum ThemeVisibility
{
    /// <summary>
    /// Theme is visible to everyone.
    /// </summary>
    Public,

    /// <summary>
    /// Theme is only visible to the author.
    /// </summary>
    Private,

    /// <summary>
    /// Theme is accessible via link but not listed publicly.
    /// </summary>
    Unlisted
}
