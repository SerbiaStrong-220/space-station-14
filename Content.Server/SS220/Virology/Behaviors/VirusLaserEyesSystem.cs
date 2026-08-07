// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Numerics;
using Content.Shared.Actions;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Projectiles;
using Content.Shared.SS220.Virology.Behaviors;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;

namespace Content.Server.SS220.Virology.Behaviors;

public sealed partial class VirusLaserEyesSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private BlindableSystem _blindable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VirusLaserEyesComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<VirusLaserEyesComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<VirusLaserEyesComponent, VirusLaserEyesActionEvent>(OnLaser);
    }

    private void OnStartup(Entity<VirusLaserEyesComponent> ent, ref ComponentStartup args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.ActionEntity, ent.Comp.ActionId);
        _actions.SetUseDelay(ent.Comp.ActionEntity, ent.Comp.Cooldown);
    }

    private void OnShutdown(Entity<VirusLaserEyesComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
    }

    private void OnLaser(Entity<VirusLaserEyesComponent> ent, ref VirusLaserEyesActionEvent args)
    {
        if (args.Handled)
            return;

        var origin = _transform.GetWorldPosition(ent);
        var target = _transform.ToMapCoordinates(args.Target).Position;

        var dir = target - origin;
        if (dir.LengthSquared() < ent.Comp.AimDeadzoneSq)
            return;

        args.Handled = true;
        _audio.PlayPvs(ent.Comp.Sound, ent);

        var dirNorm = Vector2.Normalize(dir);
        var spawnPos = new MapCoordinates(origin + dirNorm * ent.Comp.SpawnOffset, Transform(ent).MapID);
        var bolt = Spawn(ent.Comp.LaserProto, spawnPos);

        if (TryComp<ProjectileComponent>(bolt, out var proj))
        {
            proj.Damage = ent.Comp.Damage;
            Dirty(bolt, proj);
        }

        _gun.ShootProjectile(bolt, dirNorm, Vector2.Zero, ent, ent, ent.Comp.LaserSpeed);

        BurnEyes(ent);
    }

    private void BurnEyes(Entity<VirusLaserEyesComponent> ent)
    {
        if (!TryComp<BlindableComponent>(ent, out var blindable))
            return;

        ent.Comp.ShotsFired++;
        var capped = Math.Min(ent.Comp.ShotsFired, ent.Comp.ShotsToMax);
        var target = (int)MathF.Ceiling((float)blindable.MaxDamage * capped / ent.Comp.ShotsToMax);

        var delta = target - ent.Comp.AppliedEyeDamage;
        if (delta <= 0)
            return;

        _blindable.AdjustEyeDamage((ent.Owner, blindable), delta);
        ent.Comp.AppliedEyeDamage = target;
    }
}
