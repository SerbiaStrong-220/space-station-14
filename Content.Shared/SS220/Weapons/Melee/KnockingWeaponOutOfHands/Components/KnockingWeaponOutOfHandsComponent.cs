// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
namespace Content.Shared.SS220.Weapons.Melee.KnockingWeaponOutOfHands.Components;

[RegisterComponent]

public sealed partial class KnockingWeaponOutOfHandsComponent : Component
{
    [DataField("chance", required: true)]
    public float Chance;
}
