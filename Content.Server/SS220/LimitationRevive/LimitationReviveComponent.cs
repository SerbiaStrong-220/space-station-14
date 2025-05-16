// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Damage;
using Content.Shared.Random;
using Content.Shared.SS220.LimitationRevive;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.LimitationRevive;

/// <summary>
/// This is used for limiting the number of defibrillator resurrections
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class LimitationReviveComponent : SharedLimitationReviveComponent
{
    [DataField]
    public int MaxRevive = 2;

    [ViewVariables]
    public int CounterOfDead = 0;

    public bool IsAlreadyDead = false;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public DamageSpecifier TypeDamageOnDead;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<WeightedRandomPrototype> WeightListProto = "TraitAfterDeathList";

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ChanceToAddTrait = 0.6f;
}
