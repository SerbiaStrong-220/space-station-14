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

        // ss220 fix freedom from bola start
        if (TryComp<CuffableComponent>(target.Value, out var cuffs) && cuffs.Container.ContainedEntities.Count >= 1)
        {
            _cuffable.Uncuff(target.Value, args.User, cuffs.LastAddedCuffs);
            args.Handled = true;
        }
        // ss220 fix freedom from bola end

        //ss220 add freedom from bola start
        if (TryComp<EnsnareableComponent>(target, out var ensnareableComponent))
        {
            var list = ensnareableComponent.Container.ContainedEntities.ToList();

            foreach (var containedEntity in list)
            {
                if (!TryComp<EnsnaringComponent>(containedEntity, out var ensnaringComponent))
                    continue;

                _ensnareable.ForceFree(containedEntity, ensnaringComponent);
                args.Handled = true;
            }

            Dirty(target.Value, ensnareableComponent);
        }
        //ss220 add freedom from bola end
    }
}
