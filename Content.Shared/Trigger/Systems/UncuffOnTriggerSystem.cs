using System.Linq;
using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;
using Content.Shared.Ensnaring;
using Content.Shared.Ensnaring.Components;
using Content.Shared.Trigger.Components.Effects;

namespace Content.Shared.Trigger.Systems;

public sealed class UncuffOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly SharedCuffableSystem _cuffable = default!;
    [Dependency] private readonly SharedEnsnareableSystem _ensnareable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UncuffOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<UncuffOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;

        if (target == null)
            return;

        if (!TryComp<CuffableComponent>(target.Value, out var cuffs) || cuffs.Container.ContainedEntities.Count < 1)
            return;

        //ss220 add freedom from bola start
        if (TryComp<EnsnareableComponent>(args.User, out var ensnareableComponent))
        {
            var list = ensnareableComponent.Container.ContainedEntities.ToList();

            foreach (var containedEntity in list)
            {
                if (!TryComp<EnsnaringComponent>(containedEntity, out var ensnaringComponent))
                    continue;

                _ensnareable.ForceFree(containedEntity, ensnaringComponent);
            }
        }
        //ss220 add freedom from bola end

        _cuffable.Uncuff(target.Value, args.User, cuffs.LastAddedCuffs);
        args.Handled = true;
    }
}
