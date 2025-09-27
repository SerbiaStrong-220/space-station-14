// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Experience.SkillChecks;

/// <summary>
/// Called on component when component effect being overridden by other
/// </summary>
[ByRefEvent]
public record struct SkillCheckEvent(ProtoId<SkillTreePrototype> TreeProto, ProtoId<SkillPrototype> SkillProto, bool HasSkill = false);

