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
    public TimeSpan BeforeDamageDelay = TimeSpan.FromSeconds(180);

    /// <summary>
    /// The exact time when the target will take damage
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? DamageTime;

    /// <summary>
    /// How much and what type of damage will be dealt
    /// </summary>
    [DataField]
    public DamageSpecifier Damage = new() //I hardcoded the base value because it can't be null
    {
        DamageDict = new()
        {
            { "Сerebral", 400 }
        }
    };

    [DataField]
    public ProtoId<WeightedRandomPrototype> WeightListProto = "TraitAfterDeathList";

    [ViewVariables]
    public List<string> RecievedDebuffs = [];

    /// <summary>
    /// The probability from 0 to 1 that a negative feature will be added in case of unsuccessful use of the defibrillator.
    /// </summary>
    [DataField]
    public float ChanceToAddTrait = 0.6f;

    /// <summary>
    /// How much does the metabolic modifier affect the damage timer
    /// </summary>
    [DataField]
    public float MetabolismModifierAffect = 0.2f; // original bed vaule is 10f (StasisBedComponent), not ok and i dont want to change official code
}
