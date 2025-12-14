// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Experience;

/// <summary>
/// This is used to mark skill entity
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AffectedByMentorComponent : Component
{
    [DataField(required: true)]
    [AutoNetworkedField]
    public Dictionary<ProtoId<SkillTreePrototype>, MentorEffectData> TeachInfo;
}

[DataDefinition]
[Serializable, NetSerializable]
public partial struct MentorEffectData : IComparable<MentorEffectData>
{
    [DataField(required: true)]
    public FixedPoint4 Multiplier;

    [DataField(required: true)]
    public FixedPoint4 Flat;

    [DataField]
    public int? MaxBuffSkillLevel;

    public readonly int CompareTo(MentorEffectData other)
    {
        var valueCompareResult = 0;
        if (MaxBuffSkillLevel.HasValue)
        {
            if (other.MaxBuffSkillLevel.HasValue)
                valueCompareResult = MaxBuffSkillLevel.Value.CompareTo(other.MaxBuffSkillLevel.Value);
            else
                return -1;
        }

        if (other.MaxBuffSkillLevel.HasValue)
            return 1;

        if (valueCompareResult != 0)
            return valueCompareResult;

        return Multiplier.CompareTo(other.Multiplier);
    }

    public static bool operator >(MentorEffectData left, MentorEffectData right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator <(MentorEffectData left, MentorEffectData right)
    {
        return left.CompareTo(right) < 0;
    }
}
