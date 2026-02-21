// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Experience.Skill.Components;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WeightlessChangingReadySkillComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public HashSet<ProtoId<TagPrototype>> HardsuitTags = new() { "Hardsuit" };

    [ViewVariables(VVAccess.ReadOnly)]
    public bool MagbootsActive;

    [DataField]
    [AutoNetworkedField]
    public float VomitChance = 0.4f;

    [DataField]
    [AutoNetworkedField]
    public float HardsuitFallChance = 0.1f;

    [DataField]
    [AutoNetworkedField]
    public float WithoutHardsuitFallChance = 0.4f;

    [DataField]
    [AutoNetworkedField]
    public TimeSpan KnockdownDuration = TimeSpan.FromSeconds(2f);

    [DataField, AutoNetworkedField]
    public float WeightlessAcceleration = 1f;

    [DataField, AutoNetworkedField]
    public float WeightlessFriction = 1f;

    [DataField, AutoNetworkedField]
    public float WeightlessModifier = 1f;
}
