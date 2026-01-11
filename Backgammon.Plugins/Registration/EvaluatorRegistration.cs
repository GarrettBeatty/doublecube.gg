namespace Backgammon.Plugins.Registration;

/// <summary>
/// Registration record for evaluators (used during DI setup)
/// </summary>
public record EvaluatorRegistration(
    string EvaluatorId,
    string DisplayName,
    bool RequiresExternalResources,
    Type ImplementationType);
