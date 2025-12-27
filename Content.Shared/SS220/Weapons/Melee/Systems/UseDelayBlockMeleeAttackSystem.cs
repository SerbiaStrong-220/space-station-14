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

        if (ent.Comp.Delays.Any(delay => _useDelay.IsDelayed((ent, useDelay), delay)))
            args.Cancelled = true;
    }
}
