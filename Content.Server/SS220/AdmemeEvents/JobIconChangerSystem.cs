// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Interaction;
using Content.Shared.SS220.AdmemeEvents;
using Robust.Server.GameObjects;

namespace Content.Server.SS220.AdmemeEvents;

public sealed class JobIconChangerSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<JobIconChangerComponent, JobIconChangerChangedMessage>(OnJobChanged);
        SubscribeLocalEvent<JobIconChangerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<JobIconChangerComponent, BoundUIOpenedEvent>(OnBoundUIOpened);
    }

    private void OnBoundUIOpened(Entity<JobIconChangerComponent> entity, ref BoundUIOpenedEvent args)
    {
        if (!_ui.HasUi(entity, args.UiKey))
            return;

        _ui.SetUiState(entity.Owner, JobIconChangerKey.Key, new JobIconChangerBoundUserInterfaceState(entity.Comp.IconFilterGroup));
    }

    private void OnJobChanged(Entity<JobIconChangerComponent> entity, ref JobIconChangerChangedMessage args)
    {
        if (string.IsNullOrWhiteSpace(args.JobIcon))
            return;

        entity.Comp.JobIcon = args.JobIcon;
    }

    private void OnAfterInteract(Entity<JobIconChangerComponent> entity, ref AfterInteractEvent args)
    {
        if (entity.Comp.CheckReach && !args.CanReach)
            return;

        if (args.Handled || args.Target is not { } target)
            return;

        if (entity.Comp.JobIcon == null)
            return;

        var eventRoleComponent = EnsureComp<EventRoleComponent>(target);
        eventRoleComponent.StatusIcon = entity.Comp.JobIcon.Value;
        args.Handled = true;

        Dirty(target, eventRoleComponent);
    }
}
