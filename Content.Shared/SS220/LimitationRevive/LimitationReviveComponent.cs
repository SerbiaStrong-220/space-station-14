// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;
using Content.Shared.Random;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.LimitationRevive;

/// <summary>
/// This is used for limiting the number of defibrillator resurrections
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class LimitationReviveComponent : Component
{
    /// <summary>
    /// Resurrection limit
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public int ReviveLimit = 2;

    /// <summary>
    /// How many times has the creature already died
    /// </summary>
    [ViewVariables]
    [AutoNetworkedField]
    public int DeathCounter = 0;

    /// <summary>
    /// How much and what type of damage will be dealt
    /// </summary>
    [DataField]
    public DamageSpecifier Damage = new() //I hardcoded the base value because it can't be null
    {
        DamageDict = new()
        {
            { "Cerebral", 20 }
        },
    };

    [DataField]
    public ProtoId<WeightedRandomPrototype> WeightListProto = "PathologyAfterDeathList";

    [ViewVariables]
    [AutoNetworkedField]
    public List<string> RecievedDebuffs = [];

    /// <summary>
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public float ChanceToAddPathology = 0.6f;

    /// <summary>
    /// Multiplier applied to <see cref="UpdateInterval"/> for adjusting based on metabolic rate multiplier.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public float UpdateIntervalMultiplier = 1f;

    /// <summary>
    /// Delay before target takes brain damage
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public TimeSpan BeforeDamageDelay = TimeSpan.FromSeconds(180);

    /// <summary>
    /// The exact time when the target will take damage
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public TimeSpan? DamageCountingTime;
}
