using System.Linq;
using Content.Server.SS220.DarkForces.Ratvar.Righteous.Structures.Beacon;
using Content.Shared.Objectives.Components;
using Robust.Shared.Random;

namespace Content.Server.SS220.DarkForces.Ratvar.Righteous.Progress.Objectives.Beacon;

public sealed class RatvarBeaconObjectiveSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RatvarBeaconObjectiveComponent, GroupObjectiveAssignedEvent>(OnAssigned);
        SubscribeLocalEvent<RatvarBeaconObjectiveComponent, GroupObjectiveAfterAssignEvent>(OnAfterAssigned);
        SubscribeLocalEvent<RatvarBeaconObjectiveComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(EntityUid uid, RatvarBeaconObjectiveComponent component, ref ObjectiveGetProgressEvent args)
    {
        var query = EntityQuery<RatvarBeaconComponent>();
        var progress = query.Count() / component.RequiredCount;
        if (progress >= 1f)
        {
            progress = 1f;
        }

        args.Progress = progress;
    }

    private void OnAfterAssigned(EntityUid uid, RatvarBeaconObjectiveComponent component,
        ref GroupObjectiveAfterAssignEvent args)
    {
        _metaData.SetEntityName(uid,
            $"Необходимо накопить мощь, чтобы выпустить Ратвара из тюрьмы. Постройте {component.RequiredCount} маяков");
    }

    private void OnAssigned(EntityUid uid, RatvarBeaconObjectiveComponent component, ref GroupObjectiveAssignedEvent args)
    {
        component.RequiredCount = _robustRandom.Next(4, 7);
    }
}
