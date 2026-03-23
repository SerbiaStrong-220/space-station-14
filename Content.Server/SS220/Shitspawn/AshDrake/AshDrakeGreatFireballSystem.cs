// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Actions;
using Content.Shared.SS220.Shitspawn.AshDrake;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using System.Numerics;

namespace Content.Server.SS220.Shitspawn.AshDrake;

public sealed class AshDrakeGreatFireballSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AshDrakeGreatFireballComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AshDrakeGreatFireballComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<AshDrakeGreatFireballComponent, AshDrakeGreatFireballActionEvent>(OnFireballAction);
    }

    private void OnMapInit(EntityUid uid, AshDrakeGreatFireballComponent comp, MapInitEvent args)
        => _actions.AddAction(uid, ref comp.ActionEntity, comp.ActionId);

    private void OnShutdown(EntityUid uid, AshDrakeGreatFireballComponent comp, ComponentShutdown args)
        => _actions.RemoveAction(uid, comp.ActionEntity);

    private void OnFireballAction(EntityUid uid, AshDrakeGreatFireballComponent comp, AshDrakeGreatFireballActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        _audio.PlayPvs(comp.Sound, uid);

        var drakeXform = Transform(uid);
        var drakePos = _transform.GetWorldPosition(drakeXform);
        var targetCoords = _transform.ToMapCoordinates(args.Target);
        var targetPos = new Vector2(targetCoords.X, targetCoords.Y);

        var dir = targetPos - drakePos;
        if (dir.LengthSquared() < 0.01f)
            return;

        var velocity = Vector2.Normalize(dir) * comp.FireballSpeed;
        var spawnPos = new MapCoordinates(drakePos + Vector2.Normalize(dir) * 1.5f, drakeXform.MapID);
        var fireball = Spawn(comp.FireballProto, spawnPos);

        _transform.SetWorldRotation(fireball, new Angle(velocity));

        var body = EnsureComp<PhysicsComponent>(fireball);
        _physics.SetBodyStatus(fireball, body, BodyStatus.InAir);
        _physics.SetLinearVelocity(fireball, velocity, body: body);
    }
}
