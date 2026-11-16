using System.Linq;
using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;
using Content.Shared.Ensnaring;
using Content.Shared.Ensnaring.Components;
using Content.Shared.Trigger.Components.Effects;

namespace Content.Shared.Trigger.Systems;

public sealed class UncuffOnTriggerSystem : XOnTriggerSystem<UncuffOnTriggerComponent>
{
    [Dependency] private readonly SharedCuffableSystem _cuffable = default!;
    [Dependency] private readonly SharedEnsnareableSystem _ensnareable = default!; // SS220-make-freedom-remove-bola

    protected override void OnTrigger(Entity<UncuffOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        if (TryComp<CuffableComponent>(target, out var cuffs) && _cuffable.TryGetLastCuff(target, out var cuff)) // SS220-make-freedom-remove-bola
        {
            _cuffable.Uncuff(target, args.User, cuff.Value);
            args.Handled = true;
        }

        // SS220-make-freedom-remove-bola-begin
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
        }
        // SS220-make-freedom-remove-bola-end
    }
}
