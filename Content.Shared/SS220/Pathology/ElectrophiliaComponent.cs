// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Pathology;

/// <summary>
/// Electrophilia symptom: electric shock damage shrinks each stage and, at the final stage, is turned
/// into healing instead. Only the damage is touched — the shock's stun still lands at every stage.
/// </summary>
[RegisterComponent]
public sealed partial class ElectrophiliaComponent : Component
{
    /// <summary>Pathology this belongs to, so the system can read the current stage.</summary>
    [DataField]
    public ProtoId<PathologyPrototype> Pathology = "Electrophilia";

    /// <summary>Damage type treated as electric shock.</summary>
    [DataField]
    public ProtoId<DamageTypePrototype> ShockType = "Shock";

    /// <summary>Fraction of shock damage still taken, indexed by stage (clamped to the last entry).</summary>
    [DataField]
    public List<float> ShockCoefficientPerStage = new() { 0.5f, 0f, 0f };

    /// <summary>Fraction of the incoming shock turned into healing, indexed by stage.</summary>
    [DataField]
    public List<FixedPoint2> HealFractionPerStage = new() { 0, 0, 1 };

    /// <summary>Damage groups healed when shock converts to healing; each gets the converted amount.</summary>
    [DataField]
    public List<ProtoId<DamageGroupPrototype>> HealGroups = new() { "Brute", "Burn" };

    /// <summary>Stage (0-indexed) from which the electricity addiction kicks in: no shock → stamina loss.</summary>
    [DataField]
    public int WithdrawalFromStage = 2;

    /// <summary>Without a shock for this long while in the withdrawal stage, the host starts losing stamina.</summary>
    [DataField]
    public TimeSpan WithdrawalDelay = TimeSpan.FromMinutes(5);

    /// <summary>Stamina damage dealt per second while in withdrawal.</summary>
    [DataField]
    public float WithdrawalStaminaDamage = 5f;

    /// <summary>Game time of the last shock (and reset on each stage change); withdrawal is measured from here.</summary>
    [ViewVariables]
    public TimeSpan LastShock;
}
