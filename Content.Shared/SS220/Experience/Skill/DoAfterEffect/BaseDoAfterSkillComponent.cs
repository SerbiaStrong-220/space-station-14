// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

// THE only reason if DoAfter living in on folder and namespace is it abstract nature to match DoAfterEvents and base functions
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Experience.DoAfterEffect;

/// <summary>
/// This component hold data for changing DoAfter events parameters started by entity with <see cref="ExperienceComponent"/>
/// </summary>
public abstract partial class BaseDoAfterSkillComponent : Component
{
    /// <summary>
    /// This skillTree id will be used to find in used entity if do after with it should progress skill tree
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public virtual ProtoId<SkillTreePrototype> SkillTreeGroup { get; set; }

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
    [AutoNetworkedField]
    public LocId? FailurePopup = null;

    [DataField]
    [AutoNetworkedField]
    public LocId? FullBlockPopup = null;
}
