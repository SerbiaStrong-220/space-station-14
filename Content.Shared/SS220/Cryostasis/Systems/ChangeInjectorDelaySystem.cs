using Content.Shared.Bed.Components;
using Content.Shared.Buckle.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.SS220.Cryostasis.Components;
using Content.Shared.SS220.Cryostasis.Events;

namespace Content.Shared.SS220.Cryostasis.Systems;

public sealed class ChangeInjectorDelaySystem : EntitySystem
{
    [Dependency] private readonly SharedPowerReceiverSystem _powerReceiver = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CryostasisComponent, ChangeInjectorDelayEvent>(OnChangeDelay);
    }

    private void OnChangeDelay(Entity<CryostasisComponent> ent, ref ChangeInjectorDelayEvent ev)
    {
        if (TryComp<BuckleComponent>(ev.Target, out var buckle) &&
            HasComp<StasisBedComponent>(buckle.BuckledTo) && _powerReceiver.IsPowered(buckle.BuckledTo.Value))
        {
            ev.Delay /= ent.Comp.InjectionSpeedMultiply;
        }
    }
}
