using Content.Server.Warps;
using Content.Shared.Ninja.Components;
using Content.Shared.Objectives.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.SS220.DarkForces.Ratvar.Righteous.Progress.Objectives.Summon;

public sealed class RatvarSummonObjectiveSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RatvarSummonObjectiveComponent, GroupObjectiveAssignedEvent>(OnAssigned);
        SubscribeLocalEvent<RatvarSummonObjectiveComponent, GroupObjectiveAfterAssignEvent>(OnAfterAssigned);
        SubscribeLocalEvent<RatvarSummonObjectiveComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<RatvarSummonObjectiveComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out var component, out var meta))
        {
            if (component.Target == null)
                continue;

            if (component.UpdateCoordinatesTime > time)
                continue;

            var transform = Transform(component.Target.Value);
            var title = $"Призовите Ратвара по координатам X: {transform.MapPosition.X}; Y: {transform.MapPosition.Y}";

            if (TryComp<WarpPointComponent>(component.Target, out var warp) && warp.Location != null)
            {
                title += $"\nТочка также может быть известна, как - {warp.Location}";
            }

            _metaData.SetEntityName(uid, title, meta);
            component.UpdateCoordinatesTime = time + component.UpdateCoordinatesPeriod;
        }
    }

    private void OnGetProgress(EntityUid uid, RatvarSummonObjectiveComponent component,
        ref ObjectiveGetProgressEvent args)
    {
        args.Progress = component.IsCompleted ? 1f : 0f;
    }

    private void OnAfterAssigned(EntityUid uid, RatvarSummonObjectiveComponent component,
        ref GroupObjectiveAfterAssignEvent args)
    {
        if (component.Target == null)
            return;

        component.UpdateCoordinatesTime = _timing.CurTime;
    }

    private void OnAssigned(EntityUid uid, RatvarSummonObjectiveComponent component,
        ref GroupObjectiveAssignedEvent args)
    {
        var warps = new List<EntityUid>();
        var query = EntityQueryEnumerator<BombingTargetComponent, WarpPointComponent>();
        while (query.MoveNext(out var warpUid, out _, out _))
        {
            warps.Add(warpUid);
        }

        if (warps.Count > 0)
        {
            component.Target = _random.Pick(warps);
            return;
        }

        warps.Clear();

        var queryWarps = EntityQueryEnumerator<WarpPointComponent>();
        while (queryWarps.MoveNext(out var warpUid, out var warpPoint))
        {
            if (warpPoint.Location?.Equals("immovable rod") == true ||
                warpPoint.Location?.Equals("powersink") == true || warpPoint.Location?.Equals("nuke disk") == true ||
                warpPoint.Location?.Equals("singularity") == true)
                continue;

            warps.Add(warpUid);
        }

        if (warps.Count > 0)
        {
            component.Target = _random.Pick(warps);
            return;
        }
    }
}
