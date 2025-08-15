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
            { "Bloodloss", 5 }
        }
    };
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool StopBlooding = true;
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float BloodlossModifier = -12; // wanna 12.5, but float can't. like 5% at 1 sec
    public Dictionary<EntityUid, TimeSpan> Healed = [];
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int Cooldown = 1;
    public List<EntityUid> Collided = [];

}
