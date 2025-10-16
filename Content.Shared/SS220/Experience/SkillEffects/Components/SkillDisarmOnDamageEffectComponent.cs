// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Experience.SkillEffects.Components;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class SkillDisarmOnDamageEffectComponent : Component
{
    [DataField(required: true)]
    [AutoNetworkedField]
    public float DisarmChance;

    [DataField(required: true)]
    [AutoNetworkedField]
    public FixedPoint2 DamageThreshold;

    [DataField]
    public LocId OnDropPopup = "disarm-on-damage-popup";
}
