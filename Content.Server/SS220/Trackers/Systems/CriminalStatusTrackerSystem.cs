// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Access.Systems;
using Content.Server.Mind;
using Content.Server.SS220.CriminalRecords;
using Content.Server.SS220.Trackers.Components;
using Content.Shared.Mind.Components;


public sealed class CriminalStatusTrackerSystem : EntitySystem
{
    [Dependency] private readonly IdCardSystem _idCard = default!;
    [Dependency] private readonly MindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CriminalStatusTrackerComponent, CriminalStatusEvent>(OnStatusChanged);
    }

    private void OnStatusChanged(Entity<CriminalStatusTrackerComponent> entity, ref CriminalStatusEvent args)
    {
        var (_, comp) = entity;

        // we check if sender is able to move the progress
        if (comp.NeedToCheckMind() && TryComp<MindContainerComponent>(args.Sender, out var mindContainer)
            && mindContainer.HasMind && comp.CanBeChangedByMind(mindContainer.Mind.Value))
            return;

        if (args.CurrentCriminalRecord.RecordType == null)
            return;

        comp.TryMove(args.CurrentCriminalRecord.RecordType.Value);
    }
}

