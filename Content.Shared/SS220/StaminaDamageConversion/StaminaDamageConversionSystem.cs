// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;

namespace Content.Shared.SS220.StaminaDamageConversion;

public sealed partial class StaminaDamageConversionSystem : EntitySystem
{
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StaminaDamageConversionComponent, DamageChangedEvent>(OnDamageDealt);
    }

    private void OnDamageDealt(Entity<StaminaDamageConversionComponent> ent, ref DamageChangedEvent args)
    {
        if (args.DamageDelta == null || !args.DamageDelta.AnyPositive())
            return;

        foreach (var (key, value) in args.DamageDelta.DamageDict)
            if (ent.Comp.ConversionDict.ContainsKey(key))
                _stamina.TakeStaminaDamage(ent.Owner, (args.DamageDelta.DamageDict[key] * ent.Comp.ConversionDict[key]).Float());
    }
}
