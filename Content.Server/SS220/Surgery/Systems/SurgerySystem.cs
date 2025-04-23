// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Surgery.Components;
using Content.Shared.SS220.Surgery.Graph;
using Content.Shared.SS220.Surgery.Ui;

namespace Content.Server.SS220.Surgery.Systems;

public sealed partial class SurgerySystem : SharedSurgerySystem
{

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SurgeryStarterComponent, StartSurgeryMessage>(OnStartSurgeryMessage);
    }

    private void OnStartSurgeryMessage(Entity<SurgeryStarterComponent> entity, ref StartSurgeryMessage args)
    {
        if (args.User == null)
        {
            Log.Error($"Got {nameof(StartSurgeryMessage)} with user null field!");
            return;
        }

        var ev = new StartSurgeryEvent(args.SurgeryGraphId, args.Target, args.User.Value);
        RaiseLocalEvent(entity, ev);
    }

    protected override void ProceedToNextStep(Entity<OnSurgeryComponent> entity, EntityUid user, EntityUid? used, SurgeryGraphEdge chosenEdge)
    {
        foreach (var action in SurgeryGraph.GetActions(chosenEdge))
        {
            action.PerformAction(entity.Owner, user, used, EntityManager);
        }

        base.ProceedToNextStep(entity, user, used, chosenEdge);

        Dirty(entity);
    }
}
