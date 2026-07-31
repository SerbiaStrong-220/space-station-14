// SS220 Changeling
using System.Numerics;
using Content.Shared.Changeling.Mutations;
using Content.Shared.Interaction;
using Content.Shared.Silicons.StationAi;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Changeling.Systems;

/// <summary>
/// Synchronizes digital-camouflage targets only to the player controlling a station AI.
/// </summary>
public sealed class ChangelingDigitalCamouflageSystem : EntitySystem
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMilliseconds(100);

    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly HashSet<NetEntity> _observableEntities = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingDigitalCamouflageComponent, ComponentInit>(OnCamouflageInit);
        SubscribeLocalEvent<ChangelingDigitalCamouflageComponent, ComponentShutdown>(OnCamouflageShutdown);
        SubscribeLocalEvent<StationAiOverlayComponent, ComponentInit>(OnAiOverlayInit);
        SubscribeLocalEvent<StationAiOverlayComponent, ComponentShutdown>(OnAiOverlayShutdown);
        SubscribeLocalEvent<StationAiOverlayComponent, PlayerAttachedEvent>(OnAiAttached);
        SubscribeLocalEvent<StationAiOverlayComponent, PlayerDetachedEvent>(OnAiDetached);
    }

    private void OnCamouflageInit(Entity<ChangelingDigitalCamouflageComponent> ent, ref ComponentInit args)
    {
        if (!TryGetNetEntity(ent.Owner, out var netEntity))
            return;

        var query = AllEntityQuery<StationAiOverlayComponent>();
        while (query.MoveNext(out var ai, out _))
        {
            var viewer = EnsureComp<StationAiDigitalCamouflageComponent>(ai);
            if (!IsObservableByAi(ai, ent.Owner) || !viewer.CamouflagedEntities.Add(netEntity.Value))
                continue;

            Dirty(ai, viewer);
        }
    }

    private void OnCamouflageShutdown(Entity<ChangelingDigitalCamouflageComponent> ent, ref ComponentShutdown args)
    {
        if (!TryGetNetEntity(ent.Owner, out var netEntity))
            return;

        var query = AllEntityQuery<StationAiDigitalCamouflageComponent>();
        while (query.MoveNext(out var ai, out var viewer))
        {
            if (!viewer.CamouflagedEntities.Remove(netEntity.Value))
                continue;

            Dirty(ai, viewer);
        }
    }

    private void OnAiOverlayInit(Entity<StationAiOverlayComponent> ent, ref ComponentInit args)
    {
        SynchronizeAi(ent.Owner, forceDirty: true);
    }

    private void OnAiOverlayShutdown(Entity<StationAiOverlayComponent> ent, ref ComponentShutdown args)
    {
        RemCompDeferred<StationAiDigitalCamouflageComponent>(ent.Owner);
    }

    private void OnAiAttached(Entity<StationAiOverlayComponent> ent, ref PlayerAttachedEvent args)
    {
        SynchronizeAi(ent.Owner, forceDirty: true);
    }

    private void OnAiDetached(Entity<StationAiOverlayComponent> ent, ref PlayerDetachedEvent args)
    {
        RemComp<StationAiDigitalCamouflageComponent>(ent.Owner);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<StationAiOverlayComponent, StationAiDigitalCamouflageComponent>();
        while (query.MoveNext(out var ai, out _, out var viewer))
        {
            if (now < viewer.NextRefresh)
                continue;

            ScheduleNextRefresh(viewer, now);
            SynchronizeAi((ai, viewer));
        }
    }

    private void SynchronizeAi(EntityUid ai, bool forceDirty = false)
    {
        var viewer = EnsureComp<StationAiDigitalCamouflageComponent>(ai);
        ScheduleNextRefresh(viewer, _timing.CurTime);
        SynchronizeAi((ai, viewer), forceDirty);
    }

    private void SynchronizeAi(Entity<StationAiDigitalCamouflageComponent> ai, bool forceDirty = false)
    {
        _observableEntities.Clear();
        var query = AllEntityQuery<ChangelingDigitalCamouflageComponent>();
        while (query.MoveNext(out var target, out _))
        {
            if (IsObservableByAi(ai.Owner, target) && TryGetNetEntity(target, out var netEntity))
                _observableEntities.Add(netEntity.Value);
        }

        if (!forceDirty && ai.Comp.CamouflagedEntities.SetEquals(_observableEntities))
            return;

        ai.Comp.CamouflagedEntities.Clear();
        ai.Comp.CamouflagedEntities.UnionWith(_observableEntities);
        Dirty(ai);
    }

    private bool IsObservableByAi(EntityUid ai, EntityUid target)
    {
        if (TerminatingOrDeleted(ai) ||
            TerminatingOrDeleted(target) ||
            !TryComp<ActorComponent>(ai, out var actor) ||
            !TryComp<EyeComponent>(ai, out var aiEye) ||
            aiEye.Target is not { } eyeTarget ||
            TerminatingOrDeleted(eyeTarget) ||
            !actor.PlayerSession.ViewSubscriptions.Contains(eyeTarget))
        {
            return false;
        }

        var targetXform = Transform(target);
        if (_containers.IsEntityOrParentInContainer(target, xform: targetXform))
            return false;

        var eyeXform = Transform(eyeTarget);
        if (targetXform.MapID != eyeXform.MapID ||
            targetXform.GridUid is not { } grid ||
            eyeXform.GridUid != grid)
        {
            return false;
        }

        var visibilityMask = EyeComponent.DefaultVisibilityMask | aiEye.VisibilityMask;
        foreach (var subscription in actor.PlayerSession.ViewSubscriptions)
        {
            if (TryComp<EyeComponent>(subscription, out var subscriptionEye))
                visibilityMask |= subscriptionEye.VisibilityMask;
        }

        var targetVisibilityMask = MetaData(target).VisibilityMask;
        if ((visibilityMask & targetVisibilityMask) != targetVisibilityMask)
            return false;

        // NetMaxUpdateRange is the side length of the PVS square. Ignoring the engine's chunk-edge margin here is
        // deliberate: this remains a conservative subset of PVS and cannot expose an out-of-PVS UID.
        var pvsScale = 1f;
        var eyeOffset = Vector2.Zero;
        if (TryComp<EyeComponent>(eyeTarget, out var remoteEye))
        {
            pvsScale = MathF.Max(remoteEye.PvsScale, 0.1f);
            eyeOffset = remoteEye.Offset;
        }

        var halfPvsRange = _configuration.GetCVar(CVars.NetMaxUpdateRange) * pvsScale / 2f;
        var targetPosition = _transform.GetRelativePosition(targetXform, grid);
        var eyeWorldPosition = _transform.GetWorldPosition(eyeXform) + eyeOffset;
        var eyePosition = Vector2.Transform(eyeWorldPosition, _transform.GetInvWorldMatrix(grid));
        var offset = targetPosition - eyePosition;
        if (MathF.Abs(offset.X) > halfPvsRange || MathF.Abs(offset.Y) > halfPvsRange)
            return false;

        // Station AI overrides this interaction check with powered camera coverage and camera line-of-sight.
        return _interaction.InRangeUnobstructed(ai, target);
    }

    private static void ScheduleNextRefresh(StationAiDigitalCamouflageComponent viewer, TimeSpan now)
    {
        if (viewer.NextRefresh > now)
            return;

        var overdue = now - viewer.NextRefresh;
        var intervals = overdue.Ticks / RefreshInterval.Ticks + 1;
        viewer.NextRefresh += TimeSpan.FromTicks(intervals * RefreshInterval.Ticks);
    }
}
