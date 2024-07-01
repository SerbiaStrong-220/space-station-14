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

        SubscribeLocalEvent<AddSleepActionComponent, BuckleChangeEvent>(OnBuckleChanged);
    }

    private void OnBuckleChanged(EntityUid uid, AddSleepActionComponent component, BuckleChangeEvent args)
    {
        // Partially yoinked from BedSystem

        if (args.Buckling)
        {
            _actionsSystem.AddAction(args.BuckledEntity, ref component.SleepAction, SleepingSystem.SleepActionId, uid);
            return;
        }

        _actionsSystem.RemoveAction(args.BuckledEntity, component.SleepAction);
        _sleepingSystem.TryWaking(args.BuckledEntity);
    }
}
