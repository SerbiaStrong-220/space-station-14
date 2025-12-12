// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

// THE only reason if DoAfter living in on folder and namespace is it abstract nature to match DoAfterEvents and base functions
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Experience.DoAfterEffect;

/// <summary>
/// This component hold data for changing DoAfter events parameters started by entity with <see cref="ExperienceComponent"/>
/// </summary>
public abstract partial class BaseDoAfterSkillComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public ProtoId<SkillTreePrototype> SkillTreeGroup;

    [DataField]
    [AutoNetworkedField]
    public float DurationScale = 1f;

    [DataField]
    [AutoNetworkedField]
    public float FailureChance = 0f;

    [DataField]
    [AutoNetworkedField]
    public bool FullBlock = false;

    [DataField]
    public LocId? FailurePopup = null;

    [DataField]
    public LocId? FullBlockPopup = null;
}
