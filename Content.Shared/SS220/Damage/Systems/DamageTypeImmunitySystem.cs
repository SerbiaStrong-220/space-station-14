// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage.Systems;
using Content.Shared.SS220.Damage.Components;

namespace Content.Shared.SS220.Damage.Systems;

public sealed class DamageTypeImmunitySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageTypeImmunityComponent, BeforeDamageChangedEvent>(OnBeforeDamageChanged);
    }

    private void OnBeforeDamage(Entity<DamageTypeImmunityComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (args.Cancelled)
            return;

        foreach (var type in ent.Comp.ImmuneTypes)
        {
            args.Damage.DamageDict.Remove(type);
        }
    }
}

