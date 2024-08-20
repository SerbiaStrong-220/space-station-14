// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Actions;
using Content.Shared.Bed.Sleep;
using Content.Shared.Buckle.Components;

namespace Content.Server.SS220.AddSleepAction;

public sealed class AddSleepActionSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SleepingSystem _sleepingSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AddSleepActionComponent, StrappedEvent>(OnBuckled);
        SubscribeLocalEvent<AddSleepActionComponent, UnstrappedEvent>(OnUnbuckled);
    }

    private void OnBuckled(EntityUid uid, AddSleepActionComponent component, StrappedEvent args)
    {
        _actionsSystem.AddAction(args.Buckle, ref component.SleepAction, SleepingSystem.SleepActionId, uid);
    }

    private void OnUnbuckled(EntityUid uid, AddSleepActionComponent component, UnstrappedEvent args)
    {
        _actionsSystem.RemoveAction(args.Buckle, component.SleepAction);
        if (TryComp<SleepingComponent>(args.Buckle, out var sleepingComponent))
            _sleepingSystem.TryWaking((args.Buckle.Owner, sleepingComponent));
    }
}
