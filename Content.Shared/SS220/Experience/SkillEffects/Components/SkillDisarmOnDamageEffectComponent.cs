// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.FixedPoint;

namespace Content.Shared.SS220.Experience.SkillEffects.Components;

[RegisterComponent]
public sealed partial class SkillDisarmOnDamageEffectComponent : Component
{
    [DataField(required: true)]
    public float DisarmChance;

    [DataField(required: true)]
    public FixedPoint2 DamageThreshold;
}
