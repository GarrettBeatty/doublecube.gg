using Tapper;

namespace Backgammon.Server.Models;

/// <summary>
/// Privacy levels for profile visibility
/// </summary>
[TranspilationSource]
public enum ProfilePrivacyLevel
{
    /// <summary>
    /// Visible to everyone
    /// </summary>
    Public = 0,

    /// <summary>
    /// Visible only to friends
    /// </summary>
    FriendsOnly = 1,

    /// <summary>
    /// Hidden from everyone
    /// </summary>
    Private = 2
}
