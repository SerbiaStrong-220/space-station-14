// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chemistry.Events;
using Content.Shared.Implants;

namespace Content.Shared.SS220.Virology.Behaviors;

public sealed class VirusInjectionBlockSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VirusInjectionBlockComponent, TargetBeforeInjectEvent>(OnBeforeInject);
        SubscribeLocalEvent<VirusInjectionBlockComponent, AddImplantAttemptEvent>(OnAddImplantAttempt);
    }

    private void OnBeforeInject(Entity<VirusInjectionBlockComponent> ent, ref TargetBeforeInjectEvent args)
    {
        if (args.Cancelled)
            return;

        args.Cancel();
        args.OverrideMessage = Loc.GetString(ent.Comp.Message);
    }

    private void OnAddImplantAttempt(Entity<VirusInjectionBlockComponent> ent, ref AddImplantAttemptEvent args)
    {
        args.Cancel();
    }
}
