// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chemistry.Events;
using Content.Shared.Implants;

namespace Content.Shared.SS220.Pathology;

public sealed class PathologyInjectionBlockSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PathologyInjectionBlockComponent, TargetBeforeInjectEvent>(OnBeforeInject);
        SubscribeLocalEvent<PathologyInjectionBlockComponent, AddImplantAttemptEvent>(OnAddImplantAttempt);
    }

    private void OnBeforeInject(Entity<PathologyInjectionBlockComponent> ent, ref TargetBeforeInjectEvent args)
    {
        if (args.Cancelled)
            return;

        args.Cancel();
        args.OverrideMessage = Loc.GetString(ent.Comp.Message);
    }

    private void OnAddImplantAttempt(Entity<PathologyInjectionBlockComponent> ent, ref AddImplantAttemptEvent args)
    {
        args.Cancel();
    }
}
