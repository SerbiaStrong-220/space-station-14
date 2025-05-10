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
    /// <summary>
    /// Resurrection limit
    /// </summary>
    [DataField]
    public int ReviveLimit = 2;

    /// <summary>
    /// How many times has the creature already died
    /// </summary>
    [ViewVariables]
    public int DeathCounter = 0;

    /// <summary>
    /// Delay before target takes brain damage
    /// </summary>
    [DataField]
    public TimeSpan BeforeDamageDelay = TimeSpan.FromSeconds(60);

    /// <summary>
    /// The exact time when the target will take damage
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? DamageTime;

    /// <summary>
    /// How much and what type of damage will be dealt
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public DamageSpecifier Damage;

    /// <summary>
    /// Delay before target takes brain damage
    /// </summary>
    [DataField]
    public TimeSpan TimeBetweenIncidents = TimeSpan.FromSeconds(60);

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<WeightedRandomPrototype> WeightListProto = "TraitAfterDeathList";

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ChanceToAddTrait = 0.6f;
}
