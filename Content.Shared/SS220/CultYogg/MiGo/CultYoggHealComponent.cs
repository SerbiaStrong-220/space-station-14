// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.CultYogg.MiGo;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CultYoggHealComponent : Component
{
    /// <summary>
    /// Time restriction of the healing component
    /// Null if it should be removed by another event
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan? HealingEffectTime;

    /// <summary>
    /// Damage that heals in a single incident
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public DamageSpecifier Heal = new();

    [DataField, AutoNetworkedField]
    public float BloodlossModifier;

    /// <summary>
    /// Restore missing blood.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ModifyBloodLevel;

    [DataField, AutoNetworkedField]
    public float ModifyStamina;

    /// <summary>
    /// Time between each healing incident
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan TimeBetweenHealingTicks = TimeSpan.FromSeconds(2.5); // most balanced value

    public TimeSpan? NextHealingTickTime;

    [DataField, AutoNetworkedField]
    public SpriteSpecifier.Rsi Sprite = new(new("SS220/Effects/CultYogg/healing.rsi"), "healingEffect");

    /// <summary>
    /// At what damage will the heal be cancelled?
    /// It should be more damage from decompression
    /// </summary>
    [ViewVariables]
    public FixedPoint2 CancelDamageTreshhold = 3;

    [ViewVariables]
    public bool ShouldStopOnDamage = true;
}
