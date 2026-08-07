// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;
using Content.Shared.Damage.Systems;

namespace Content.Shared.SS220.Virology.Behaviors;

public sealed class VirusDamageModifierSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VirusDamageModifierComponent, DamageModifyEvent>(OnDamageModify);
    }

    private void OnDamageModify(Entity<VirusDamageModifierComponent> ent, ref DamageModifyEvent args)
    {
        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, ent.Comp.Modifier);
    }
}
