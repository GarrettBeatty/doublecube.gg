namespace Backgammon.Plugins.Registration;

/// <summary>
/// Registration record for bots (used during DI setup)
/// </summary>
public record BotRegistration(
    string BotId,
    string DisplayName,
    string Description,
    int EstimatedElo,
    bool RequiresExternalResources,
    Type ImplementationType);
