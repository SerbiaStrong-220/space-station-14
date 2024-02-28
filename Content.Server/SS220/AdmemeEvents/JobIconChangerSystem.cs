// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Administration.Logs;
using Content.Shared.Interaction;
using Content.Shared.SS220.AdmemeEvents;

namespace Content.Server.SS220.AdmemeEvents;

public sealed class JobIconChangerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<JobIconChangerComponent, JobIconChangerChangedMessage>(OnJobChanged);
        SubscribeLocalEvent<JobIconChangerComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnJobChanged(EntityUid uid, JobIconChangerComponent component, JobIconChangerChangedMessage args)
    {
        if (string.IsNullOrWhiteSpace(args.JobIcon))
            return;

        component.JobIcon = args.JobIcon;
    }

    private void OnAfterInteract(EntityUid uid, JobIconChangerComponent component, AfterInteractEvent args)
    {
        //if (component.JobIcon == null || !args.CanReach)
        //    return;

        if (args.Handled || args.Target is not { } target)
            return;

        if (!TryComp(target, out EventRoleComponent? eventRoleComponent))
            return;

        eventRoleComponent.StatusIcon = component.JobIcon;

        args.Handled = true;

        Dirty(target, eventRoleComponent);
    }
}
