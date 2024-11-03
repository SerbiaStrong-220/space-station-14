// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Weapons.Melee.Components;

[RegisterComponent]

public sealed partial class DisarmOnAttackComponent : Component
{
    [DataField]
    public bool DisarmOnHeavyAtack = true;

    [DataField]
    public bool DisarmOnLightAtack = true;

    [DataField("chance", required: true)]
    public float Chance;

    [DataField]
    public float? HeavyAttackChance;
}
