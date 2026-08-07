// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Virology.Effects;

public sealed partial class VirusToxicConversionEffect : IVirusEffect
{
    /// <summary>Damage types converted.</summary>
    [DataField]
    public List<ProtoId<DamageTypePrototype>> HealTypes = new() { "Blunt", "Slash", "Piercing", "Heat" };

    /// <summary>Damage convertedt to this type.</summary>
    [DataField]
    public ProtoId<DamageTypePrototype> PoisonType = "Poison";

    /// <summary>Total damage converted to poison per second.</summary>
    [DataField]
    public FixedPoint2 Rate = FixedPoint2.New(0.3f);

    public void ApplyEffect(in VirusProgressArgs args)
    {
        var entMan = args.EntityManager;
        if (!entMan.TryGetComponent<DamageableComponent>(args.Carrier, out var damageable))
            return;

        var damageSystem = entMan.System<DamageableSystem>();
        var positive = damageSystem.GetPositiveDamage((args.Carrier, damageable));

        var budget = Rate;
        var change = new DamageSpecifier();
        var drained = FixedPoint2.Zero;

        foreach (var typeId in HealTypes)
        {
            if (budget <= FixedPoint2.Zero)
                break;

            if (!positive.DamageDict.TryGetValue(typeId, out var current) || current <= FixedPoint2.Zero)
                continue;

            var amount = current < budget ? current : budget;
            change.DamageDict[typeId] = -amount;
            budget -= amount;
            drained += amount;
        }

        if (drained <= FixedPoint2.Zero)
            return;

        change.DamageDict[PoisonType] = drained;
        damageSystem.TryChangeDamage((args.Carrier, damageable), change, ignoreResistances: true, interruptsDoAfters: false);
    }
}
