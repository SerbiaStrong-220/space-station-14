// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.SS220.CriminalRecords;
using Content.Server.SS220.Trackers.Components;
using Content.Shared.Mind.Components;


public sealed class CriminalStatusTrackerSystem : EntitySystem
{

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CriminalStatusTrackerComponent, CriminalStatusEvent>(OnStatusChanged);
    }

    private void OnStatusChanged(Entity<CriminalStatusTrackerComponent> entity, ref CriminalStatusEvent args)
    {
        var (_, comp) = entity;

        if (args.CurrentCriminalRecord.RecordType == null)
            return;

        EntityUid? mindUid = null;
        // we check if sender is able to move the progress
        if (TryComp<MindContainerComponent>(args.Sender, out var mindContainer))
            mindUid = mindContainer.Mind;

        comp.TryMove(args.CurrentCriminalRecord.RecordType.Value, mindUid);
    }
}

