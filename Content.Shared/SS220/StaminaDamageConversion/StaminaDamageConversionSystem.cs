// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Components;

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

        if (TryComp<MobThresholdsComponent>(ent, out var thresholdsComp) && thresholdsComp.CurrentThresholdState == Mobs.MobState.Dead)
            return;

        foreach (var (key, value) in args.DamageDelta.DamageDict)
            if (ent.Comp.ConversionDict.ContainsKey(key))
                _stamina.TakeStaminaDamage(ent.Owner, (args.DamageDelta.DamageDict[key] * ent.Comp.ConversionDict[key]).Float());
    }
}
