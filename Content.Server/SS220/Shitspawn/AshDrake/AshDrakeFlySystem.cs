// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Actions;
using Content.Shared.Mobs;
using Content.Shared.SS220.Shitspawn.AshDrake;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using System.Numerics;

namespace Content.Server.SS220.Shitspawn.AshDrake;

public sealed partial class AshDrakeFlySystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private MapSystem _map = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IRobustRandom _random = default!;

    private readonly Dictionary<EntityUid, (Vector2 Target, MapId MapId)> _flying = new();
    private readonly List<EntityUid> _toRemove = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AshDrakeFlyComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AshDrakeFlyComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<AshDrakeFlyComponent, AshDrakeFlyActionEvent>(OnFlyAction);
        SubscribeLocalEvent<AshDrakeFlyComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_flying.Count == 0)
            return;

        foreach (var (uid, data) in _flying)
        {
            if (!TryComp<AshDrakeFlyComponent>(uid, out var comp))
            {
                _toRemove.Add(uid);
                continue;
            }

            if (!comp.IsFlying)
                continue;

            if (!TryComp(uid, out TransformComponent? xform))
            {
                _toRemove.Add(uid);
                continue;
            }

            if (xform.MapID != data.MapId)
            {
                SetFlying(uid, comp, false);
                _toRemove.Add(uid);
                continue;
            }

            var cur = _transform.GetWorldPosition(xform);
            var dir = data.Target - cur;
            var step = comp.FlySpeed * frameTime;

            if (dir.Length() <= step)
            {
                _transform.SetWorldPosition((uid, xform), data.Target);
                _toRemove.Add(uid);
                Land(uid, comp, xform);
            }
            else
            {
                _transform.SetWorldPosition((uid, xform), cur + Vector2.Normalize(dir) * step);
            }
        }

        foreach (var uid in _toRemove)
            _flying.Remove(uid);
        _toRemove.Clear();
    }

    private void OnMapInit(EntityUid uid, AshDrakeFlyComponent comp, MapInitEvent args)
    {
        _actions.AddAction(uid, ref comp.ActionEntity, comp.ActionId);
    }

    private void OnShutdown(EntityUid uid, AshDrakeFlyComponent comp, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, comp.ActionEntity);
        _flying.Remove(uid);
    }

    private void OnFlyAction(EntityUid uid, AshDrakeFlyComponent comp, AshDrakeFlyActionEvent args)
    {
        if (args.Handled || comp.IsFlying)
            return;

        args.Handled = true;

        var targetMap = _transform.ToMapCoordinates(args.Target);
        _flying[uid] = (new Vector2(targetMap.X, targetMap.Y), targetMap.MapId);

        if (TryComp(uid, out PhysicsComponent? body))
            _physics.SetLinearVelocity(uid, Vector2.Zero, body: body);

        _audio.PlayPvs(comp.FlySound, uid);
        SetFlying(uid, comp, true);
    }

    private void OnMobStateChanged(EntityUid uid, AshDrakeFlyComponent comp, MobStateChangedEvent args)
    {
        if (args.NewMobState is MobState.Critical or MobState.Dead && comp.IsFlying)
        {
            SetFlying(uid, comp, false);
            _flying.Remove(uid);
        }
    }

    private void Land(EntityUid uid, AshDrakeFlyComponent comp, TransformComponent xform)
    {
        SetFlying(uid, comp, false);
        SpawnLavaAround(uid, comp, xform);
    }

    private void SetFlying(EntityUid uid, AshDrakeFlyComponent comp, bool flying)
    {
        comp.IsFlying = flying;
        Dirty(uid, comp);
    }

    private void SpawnLavaAround(EntityUid uid, AshDrakeFlyComponent comp, TransformComponent xform)
    {
        if (xform.GridUid is not { Valid: true } gridUid || !TryComp<MapGridComponent>(xform.GridUid, out var mapGridComp))
            return;

        var tilePos = _map.TileIndicesFor((gridUid, mapGridComp), _transform.GetMapCoordinates(uid));

        for (var x = -comp.LavaRadius; x <= comp.LavaRadius; x++)
        {
            for (var y = -comp.LavaRadius; y <= comp.LavaRadius; y++)
            {
                if (x * x + y * y > comp.LavaRadius * comp.LavaRadius)
                    continue;

                if (!_random.Prob(comp.LavaChance))
                    continue;

                var coordinates = _map.GridTileToLocal(gridUid, mapGridComp, new Vector2i(tilePos.X + x, tilePos.Y + y));
                Spawn(comp.LavaProto, coordinates);
            }
        }
    }
}
