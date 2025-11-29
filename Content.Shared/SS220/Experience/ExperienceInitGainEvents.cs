// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Experience;

/// <summary>
/// This is bit mask of what type of experience we've already was processed by Initializer
/// Each one that is higher will reinit others
/// </summary>
[Serializable, NetSerializable]
[Flags]
public enum InitGainedExperienceType : byte
{
    NotInitialized = 1 << 0,
    MapInit = 1 << 1,
    JobInit = 1 << 2
}

/// <summary>
/// Raised directed on an entity when all spawning and post spawn procedures are done
/// </summary>
[ByRefEvent]
public record struct AfterExperienceInitComponentGained()
{
    public InitGainedExperienceType Type { init; get; } = InitGainedExperienceType.JobInit;
}
