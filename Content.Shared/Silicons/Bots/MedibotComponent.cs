using System.Linq; //ss220 fix medibot
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage; //ss220 fix medibot
using Content.Shared.EntityEffects.Effects; //ss220 fix medibot
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Silicons.Bots;

/// <summary>
/// Used by the server for NPC medibot injection.
/// Currently no clientside prediction done, only exists in shared for emag handling.
/// </summary>
[RegisterComponent]
[Access(typeof(MedibotSystem))]
public sealed partial class MedibotComponent : Component
{
    /// <summary>
    /// Treatments the bot will apply for each mob state.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<MobState, MedibotTreatment> Treatments = new();

    /// <summary>
    /// Sound played after injecting a patient.
    /// </summary>
    [DataField("injectSound")]
    public SoundSpecifier InjectSound = new SoundPathSpecifier("/Audio/Items/hypospray.ogg");
}

/// <summary>
/// An injection to treat the patient with.
/// </summary>
[DataDefinition]
public sealed partial class MedibotTreatment
{
    /// <summary>
    /// Reagent to inject into the patient.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<ReagentPrototype> Reagent = string.Empty;

    /// <summary>
    /// How much of the reagent to inject.
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 Quantity;

    /// <summary>
    /// Do nothing when the patient is at or below this total damage.
    /// When null this will inject meds into completely healthy patients.
    /// </summary>
    [DataField]
    public FixedPoint2? MinDamage;

    /// <summary>
    /// Do nothing when the patient is at or above this total damage.
    /// Useful for tricordrazine which does nothing above 50 damage.
    /// </summary>
    [DataField]
    public FixedPoint2? MaxDamage;

    //ss220 fix medibot start
    /// <summary>
    /// Returns whether the treatment will probably work for an amount of damage.
    /// Doesn't account for specific damage types only total amount.
    /// </summary>
    public bool IsValid(DamageSpecifier damage, bool isEmagged, IPrototypeManager proto)
    {
        if (isEmagged)
            return true;

        var reagent = proto.Index(Reagent);

        var heals = reagent.Metabolisms?
            .Values
            .SelectMany(m => m.Effects)
            .OfType<HealthChange>()
            .SelectMany(h => h.Damage.DamageDict.Keys)
            .ToHashSet();

        var canHeal = heals != null && heals.Any(type => damage.DamageDict.GetValueOrDefault(type) > 0);

        if (!canHeal)
            return false;

        var total = damage.GetTotal();

        return (MaxDamage == null || total < MaxDamage) &&
               (MinDamage == null || total > MinDamage);
    }
    //ss220 fix medibot end
}
