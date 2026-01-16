namespace Backgammon.Plugins.Abstractions;

/// <summary>
/// Interface for evaluators that support configurable ply depth.
/// Allows bots to set a plies override for different difficulty levels.
/// </summary>
public interface IPliesConfigurable
{
    /// <summary>
    /// Optional override for evaluation plies. When set, takes precedence over default settings.
    /// </summary>
    int? PliesOverride { get; set; }
}
