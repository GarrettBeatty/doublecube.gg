namespace Backgammon.Plugins.Registration;

/// <summary>
/// Metadata about a registered evaluator
/// </summary>
public record EvaluatorMetadata(
    string EvaluatorId,
    string DisplayName,
    bool RequiresExternalResources,
    Type ImplementationType);
