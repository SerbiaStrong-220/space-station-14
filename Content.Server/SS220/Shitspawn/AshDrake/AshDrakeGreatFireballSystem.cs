// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Actions;
using Content.Shared.SS220.Shitspawn.AshDrake;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using System.Numerics;

namespace Content.Server.SS220.Shitspawn.AshDrake;

public sealed class AshDrakeGreatFireballSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;

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

        var dirNorm = Vector2.Normalize(dir);
        var spawnPos = new MapCoordinates(drakePos + dirNorm * 1.5f, drakeXform.MapID);
        var fireball = Spawn(comp.FireballProto, spawnPos);

        _gun.ShootProjectile(fireball, dirNorm, Vector2.Zero, uid, uid, comp.FireballSpeed);
    }
}
