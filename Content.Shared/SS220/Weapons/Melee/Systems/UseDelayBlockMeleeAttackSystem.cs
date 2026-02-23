using Content.Shared.Timing;
using Content.Shared.Weapons.Melee.Events;
using System.Linq;

namespace Content.Shared.SS220.Weapons.Melee.UseDelayBlockAtack;

public sealed class UseDelayBlockMeleeAttackSystem : EntitySystem
{
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<UseDelayBlockMeleeAttackComponent, AttemptMeleeEvent>(OnMeleeAttempt);
    }

    private void OnMeleeAttempt(Entity<UseDelayBlockMeleeAttackComponent> ent, ref AttemptMeleeEvent args)
    {
        if (!TryComp(ent, out UseDelayComponent? useDelay))
            return;

        foreach (var delay in ent.Comp.Delays)
        {
            if (_useDelay.IsDelayed((ent, useDelay), delay))
            {
                args.Cancelled = true;
                break;
            }
        }
    }
}
