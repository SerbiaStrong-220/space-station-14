// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Actions;
using Content.Shared.SS220.Shitspawn.AshDrake;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Numerics;

namespace Content.Server.SS220.Shitspawn.AshDrake;

public sealed class AshDrakeMeteorSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AshDrakeMeteorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AshDrakeMeteorComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<AshDrakeMeteorComponent, AshDrakeMeteorActionEvent>(OnMeteorAction);
    }

    private void OnMapInit(EntityUid uid, AshDrakeMeteorComponent comp, MapInitEvent args)
        => _actions.AddAction(uid, ref comp.ActionEntity, comp.ActionId);

    private void OnShutdown(EntityUid uid, AshDrakeMeteorComponent comp, ComponentShutdown args)
        => _actions.RemoveAction(uid, comp.ActionEntity);

    private void OnMeteorAction(EntityUid uid, AshDrakeMeteorComponent comp, AshDrakeMeteorActionEvent args)
    {
        if (args.Handled) return;
        args.Handled = true;

        _audio.PlayPvs(comp.Sound, uid);

        var mapCoords = _transform.GetMapCoordinates(uid);

        for (var i = 0; i < comp.Count; i++)
        {
            var pos = new MapCoordinates(
                mapCoords.X + _random.NextFloat(-comp.Radius, comp.Radius),
                mapCoords.Y + _random.NextFloat(-comp.Radius, comp.Radius),
                mapCoords.MapId);

            var proto = comp.ProjectileProto;
            var height = comp.SpawnHeight;
            var speed = comp.Speed;
            Timer.Spawn(TimeSpan.FromSeconds(i * comp.SpawnInterval), () => SpawnMeteor(pos, proto, height, speed));
        }
    }

    private void SpawnMeteor(MapCoordinates target, EntProtoId proto, float spawnHeight, float speed)
    {
        var startPos = new MapCoordinates(target.X, target.Y + spawnHeight, target.MapId);
        var visual = Spawn(proto, startPos);

        var falling = AddComp<AshDrakeMeteorFallingComponent>(visual);
        falling.Target = new Vector2(target.X, target.Y);
        falling.Speed = speed;
        Dirty(visual, falling);

        Spawn("AshDrakeFireMeteorMarker", target);
    }
}
