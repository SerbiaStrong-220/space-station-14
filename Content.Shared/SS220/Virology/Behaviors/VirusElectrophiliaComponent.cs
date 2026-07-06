// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Virology.Behaviors;

[RegisterComponent]
public sealed partial class VirusElectrophiliaComponent : Component
{
    [DataField]
    public ProtoId<DamageTypePrototype> ShockType = "Shock";

    /// <summary>Fraction of shock damage still taken at this stage.</summary>
    [DataField]
    public float ShockCoefficient = 0.5f;

    /// <summary>Fraction of the incoming shock turned into healing at this stage.</summary>
    [DataField]
    public FixedPoint2 HealFraction = 0;

    /// <summary>Damage types healed when shock converts to healing.</summary>
    [DataField]
    public List<ProtoId<DamageTypePrototype>> HealTypes = new() { "Blunt", "Slash", "Piercing", "Heat" };

    /// <summary>If true, going without a shock drains stamina (final-stage addiction).</summary>
    [DataField]
    public bool Withdrawal;

    /// <summary>Without a shock for this long - host starts losing stamina.</summary>
    [DataField]
    public TimeSpan WithdrawalDelay = TimeSpan.FromMinutes(5);

    /// <summary>Stamina damage dealt per second while in withdrawal.</summary>
    [DataField]
    public float WithdrawalStaminaDamage = 5f;

    /// <summary>Game time of the last shock, timer starts from here.</summary>
    [ViewVariables]
    public TimeSpan LastShock;
}
