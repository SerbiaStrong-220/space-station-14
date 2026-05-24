// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Content.Shared.Damage.Systems;
using Content.Shared.SS220.AltArmor;

namespace Content.Shared.SS220.ArmorBlock;

public sealed class ArmorBlockSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly AltArmorSystem _altArmor = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ArmorBlockComponent, DamageModifyEvent>(OnDamageChange);
    }

    public void OnDamageChange(Entity<ArmorBlockComponent> ent, ref DamageModifyEvent args)
    {
        _altArmor.ModifyDamage(ent.Owner, args.OriginalDamage, out var resultDamage, out var resultArmorDamage);

        args.Damage = resultArmorDamage;

        if (ent.Comp.User == null)
            return;

        _damageable.TryChangeDamage((EntityUid)ent.Comp.User, resultDamage);
    }
}
