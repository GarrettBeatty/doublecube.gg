namespace Backgammon.Plugins.Registration;

/// <summary>
/// Metadata about a registered bot
/// </summary>
public record BotMetadata(
    string BotId,
    string DisplayName,
    string Description,
    int EstimatedElo,
    bool RequiresExternalResources,
    Type ImplementationType);
