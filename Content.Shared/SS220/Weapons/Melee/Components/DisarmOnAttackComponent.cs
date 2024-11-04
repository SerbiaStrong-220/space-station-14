// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Weapons.Melee.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class DisarmOnAttackComponent : Component
{
    //Should be between 0 and 1
    [DataField(required: true)]
    public float Chance = 0;

    [DataField]
    public float HeavyAttackChance = 0;
}
