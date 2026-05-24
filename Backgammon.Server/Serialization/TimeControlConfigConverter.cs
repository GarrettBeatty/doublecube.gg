using Backgammon.Core;
using Orleans;

namespace Backgammon.Server.Serialization;

/// <summary>
/// Orleans surrogate for TimeControlConfig, which lives in Backgammon.Core (no Orleans dependency).
/// </summary>
[GenerateSerializer]
public struct TimeControlConfigSurrogate
{
    [Id(0)]
    public TimeControlType Type;

    [Id(1)]
    public int DelaySeconds;
}

/// <summary>
/// Converts between TimeControlConfig and its Orleans surrogate for serialization.
/// </summary>
[RegisterConverter]
public sealed class TimeControlConfigConverter
    : IConverter<TimeControlConfig, TimeControlConfigSurrogate>,
      IPopulator<TimeControlConfig, TimeControlConfigSurrogate>
{
    public TimeControlConfig ConvertFromSurrogate(in TimeControlConfigSurrogate surrogate)
        => new() { Type = surrogate.Type, DelaySeconds = surrogate.DelaySeconds };

    public TimeControlConfigSurrogate ConvertToSurrogate(in TimeControlConfig value)
        => new() { Type = value.Type, DelaySeconds = value.DelaySeconds };

    public void Populate(in TimeControlConfigSurrogate surrogate, TimeControlConfig value)
    {
        value.Type = surrogate.Type;
        value.DelaySeconds = surrogate.DelaySeconds;
    }
}
