// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Damage;
using Content.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.LimitationRevive;

/// <summary>
/// This is used for limiting the number of defibrillator resurrections
/// </summary>
[RegisterComponent]
public sealed partial class LimitationReviveComponent : Component
{
    [DataField]
    public int MaxRevive = 2;

    [ViewVariables]
    public int CounterOfDead = 0;

    public bool IsAlreadyDead = false;

    public bool IsDamageTaken = false;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public DamageSpecifier TypeDamageOnDead;

    [DataField]
    public TimeSpan DelayBeforeDamage = TimeSpan.FromSeconds(60);

    public TimeSpan TimeToDamage = TimeSpan.Zero;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<WeightedRandomPrototype> WeightListProto = "TraitAfterDeathList";

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ChanceToAddTrait  = 0.6f;
}
