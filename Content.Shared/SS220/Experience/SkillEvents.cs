// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Experience;

/// <summary>
/// Called on component when component effect being overridden by other
/// </summary>
[ByRefEvent]
public record struct SkillComponentOverridden();

/// <summary>
/// Called on component init or when effect override deletes
/// </summary>
[ByRefEvent]
public record struct SkillComponentActive();
