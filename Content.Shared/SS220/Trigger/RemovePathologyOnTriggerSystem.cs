// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Pathology;
using Content.Shared.Trigger;

namespace Content.Shared.SS220.Trigger;

public sealed class RemovePathologyOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly SharedPathologySystem _pathology = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RemovePathologyOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<RemovePathologyOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var nullableTarget = ent.Comp.TargetUser ? args.User : ent.Owner;

        if (nullableTarget is not { } target)
            return;

        foreach (var selector in ent.Comp.CurePathologyStacksSelectors)
        {
            foreach (var (id, deltaStack) in selector)
            {
                if (_pathology.TryChangePathologyStack(target, id, deltaStack))
                    break;
            }
        }
    }
}
