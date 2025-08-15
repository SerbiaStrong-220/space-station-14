// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;

namespace Content.Shared.SS220.HealOnCollide.Components;

[RegisterComponent]
public sealed partial class HealOnCollideComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier Heal = new()
    {
        DamageDict = new()
        {
            { "Blunt", 0 }
        }
    };
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool StopBlooding = true;
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int BloodlossModifier = -10;
    public Dictionary<EntityUid, TimeSpan> Healed = [];
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int Cooldown = 15;

}
