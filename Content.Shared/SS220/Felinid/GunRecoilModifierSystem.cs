using System.Numerics;
using Content.Shared.ActionBlocker;
using Content.Shared.Movement.Events;
using Content.Shared.SS220.Felinid.Components;
using Content.Shared.Tag;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.Felinid;

public sealed partial class GunRecoilModifierSystem : EntitySystem
{
    private static readonly ProtoId<TagPrototype> Caliber45AcpTag = "CartridgeAmmoAcp";
    private static readonly ProtoId<TagPrototype> Caliber45MagnumTag = "CartridgeMagnum";
    private static readonly ProtoId<TagPrototype> Caliber20RifleTag = "CartridgeRifle";
    private static readonly ProtoId<TagPrototype> Caliber30RifleTag = "CartridgeLightRifle";
    private static readonly ProtoId<TagPrototype> Caliber10RifleTag = "CartridgeHeavyRifle";
    private static readonly ProtoId<TagPrototype> Caliber50ShotgunTag = "ShellShotgun";
    private static readonly ProtoId<TagPrototype> Caliber50ShotgunToyTag = "ShellShotgunToy";
    private static readonly ProtoId<TagPrototype> Caliber60AntiMaterielTag = "CartridgeAntiMateriel";
    private static readonly ProtoId<TagPrototype> Caliber60AntiMaterielToyTag = "CartridgeAntiMaterielToy";

    private static readonly HashSet<EntProtoId> Caliber45Projectiles =
    [
        "BulletAmmoAcp",
        "BulletMagnum",
        "BulletMagnumPractice",
        "BulletMagnumIncendiary",
        "BulletMagnumAP",
        "BulletMagnumUranium",
    ];

    private static readonly HashSet<EntProtoId> Caliber20RifleProjectiles =
    [
        "BulletRifle",
        "BulletRiflePractice",
        "BulletRifleIncendiary",
        "BulletRifleUranium",
    ];

    private static readonly HashSet<EntProtoId> Caliber30RifleProjectiles =
    [
        "BulletLightRifle",
        "BulletLightRiflePractice",
        "BulletLightRifleIncendiary",
        "BulletLightRifleUranium",
    ];

    private static readonly HashSet<EntProtoId> Caliber10RifleProjectiles =
    [
        "BulletHeavyRifle",
        "BulletMinigun",
    ];

    private static readonly HashSet<EntProtoId> Caliber50Projectiles =
    [
        "PelletShotgunSlug",
        "PelletShotgunBeanbag",
        "PelletShotgun",
        "PelletShotgunSpread",
        "PelletShotgunIncendiary",
        "PelletShotgunIncendiarySpread",
        "PelletShotgunPractice",
        "PelletShotgunPracticeSpread",
        "PelletShotgunImprovised",
        "PelletShotgunImprovisedSpread",
        "PelletShotgunTranquilizer",
        "PelletShotgunFlare",
        "PelletShotgunUranium",
        "PelletShotgunUraniumSpread",
        "PelletGrapeshot",
        "PelletGrapeshotSpread",
        "PelletGlass",
        "PelletGlassSpread",
        "PelletShotgunCaps",
        "PelletShotgunSpreadCaps",
        "PelletShotgunSpreadFoam",
        "BulletUnitarySlug",
    ];

    private static readonly HashSet<EntProtoId> Caliber60Projectiles =
    [
        "BulletAntiMateriel",
        "BulletAntiMaterielToyStun",
        "BulletAntiMaterielToyMeme",
        "BulletAntiMaterielToyHappiness",
    ];

    public readonly record struct RecoilProfile(
        float SlideDistance,
        TimeSpan SlideDuration,
        float KnockdownChance,
        TimeSpan KnockdownTime);

    private static readonly RecoilProfile Caliber45Profile = new(
        0.45f,
        TimeSpan.FromSeconds(0.24),
        0.03f,
        TimeSpan.FromSeconds(0.7));

    private static readonly RecoilProfile Caliber20RifleProfile = new(
        0.7f,
        TimeSpan.FromSeconds(0.30),
        0.06f,
        TimeSpan.FromSeconds(0.9));

    private static readonly RecoilProfile Caliber30RifleProfile = new(
        0.9f,
        TimeSpan.FromSeconds(0.34),
        0.08f,
        TimeSpan.FromSeconds(1.0));

    private static readonly RecoilProfile Caliber10RifleProfile = new(
        1.8f,
        TimeSpan.FromSeconds(0.46),
        0.15f,
        TimeSpan.FromSeconds(1.5));

    private static readonly RecoilProfile Caliber50Profile = new(
        1.25f,
        TimeSpan.FromSeconds(0.36),
        0.10f,
        TimeSpan.FromSeconds(1.15));

    private static readonly RecoilProfile Caliber60Profile = new(
        2.5f,
        TimeSpan.FromSeconds(0.5),
        0.20f,
        TimeSpan.FromSeconds(1.75));

    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private TagSystem _tags = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GunComponent, GunShotEvent>(OnGunShot);
        SubscribeLocalEvent<GunRecoilModifierComponent, UpdateCanMoveEvent>(OnRecoilCanMove);
        SubscribeLocalEvent<GunRecoilModifierComponent, AfterAutoHandleStateEvent>(OnRecoilHandleState);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<GunRecoilModifierComponent>();
        while (query.MoveNext(out var uid, out var recoil))
        {
            if (!recoil.RecoilActive)
                continue;

            if (recoil.RecoilMapUid == null || Transform(uid).MapUid != recoil.RecoilMapUid)
            {
                StopRecoil((uid, recoil));
                continue;
            }

            if (!TryComp<PhysicsComponent>(uid, out var physics))
            {
                StopRecoil((uid, recoil));
                continue;
            }

            var duration = recoil.RecoilEndsAt - recoil.RecoilStartedAt;
            var elapsed = _timing.CurTime - recoil.RecoilStartedAt;
            var progress = duration <= TimeSpan.Zero
                ? 1f
                : Math.Clamp((float) (elapsed.TotalSeconds / duration.TotalSeconds), 0f, 1f);
            if (progress >= 1f)
            {
                StopRecoil((uid, recoil), physics);
                continue;
            }

            SetRecoilVelocity((uid, recoil), physics, progress);
        }
    }

    private void OnGunShot(Entity<GunComponent> gun, ref GunShotEvent args)
    {
        if (!TryComp<GunRecoilModifierComponent>(args.User, out var recoil) ||
            GetRecoilProfile(args.Ammo) is not { } profile)
        {
            return;
        }

        var direction = _transform.GetWorldRotation(gun).ToWorldVec();
        if (gun.Comp.ShootCoordinates is { } targetCoordinates)
        {
            var origin = _transform.GetMapCoordinates(args.User);
            var target = _transform.ToMapCoordinates(targetCoordinates);
            if (origin.MapId != target.MapId)
                return;

            direction = target.Position - origin.Position;
        }

        if (direction == Vector2.Zero)
            return;

        StartRecoil((args.User, recoil), gun, profile, Vector2.Normalize(direction));
    }

    private void StartRecoil(
        Entity<GunRecoilModifierComponent> ent,
        Entity<GunComponent> gun,
        RecoilProfile profile,
        Vector2 shotDirection)
    {
        var mapUid = Transform(ent.Owner).MapUid;
        if (mapUid == null)
            return;

        var weaponRecoil = MathF.Max(0f, gun.Comp.CameraRecoilScalarModified);
        var distance = profile.SlideDistance * weaponRecoil * MathF.Max(0f, ent.Comp.DistanceModifier);
        var duration = TimeSpan.FromTicks((long) (profile.SlideDuration.Ticks * MathF.Max(0f, ent.Comp.DurationModifier)));

        ent.Comp.RecoilActive = duration > TimeSpan.Zero && distance > 0f;
        ent.Comp.RecoilDirection = -shotDirection;
        ent.Comp.RecoilDistance = distance;
        ent.Comp.RecoilStartedAt = _timing.CurTime;
        ent.Comp.RecoilEndsAt = _timing.CurTime + duration;
        ent.Comp.RecoilMapUid = mapUid;
        Dirty(ent);
        _blocker.UpdateCanMove(ent.Owner);

        if (ent.Comp.RecoilActive && TryComp<PhysicsComponent>(ent.Owner, out var physics))
            SetRecoilVelocity(ent, physics, 0f);

        var recoilStarted = new GunRecoilStartedEvent(profile);
        RaiseLocalEvent(ent.Owner, ref recoilStarted);
    }

    private void SetRecoilVelocity(Entity<GunRecoilModifierComponent> ent, PhysicsComponent physics, float progress)
    {
        var duration = (float) (ent.Comp.RecoilEndsAt - ent.Comp.RecoilStartedAt).TotalSeconds;
        if (duration <= 0f)
            return;

        var initial = Math.Clamp(ent.Comp.InitialVelocityFraction, 0.01f, 1f);
        var curve = (1f - progress) * (initial + (1f - initial) * MathF.Sin(MathF.PI * progress));
        var integral = initial / 2f + (1f - initial) / MathF.PI;
        var speed = ent.Comp.RecoilDistance / duration * curve / integral;
        _physics.SetLinearVelocity(ent.Owner, ent.Comp.RecoilDirection * speed, body: physics);
    }

    private void StopRecoil(Entity<GunRecoilModifierComponent> ent, PhysicsComponent? physics = null)
    {
        ent.Comp.RecoilActive = false;
        if (Resolve(ent.Owner, ref physics, false))
            _physics.SetLinearVelocity(ent.Owner, Vector2.Zero, body: physics);

        Dirty(ent);
        _blocker.UpdateCanMove(ent.Owner);
    }

    private void OnRecoilCanMove(Entity<GunRecoilModifierComponent> ent, ref UpdateCanMoveEvent args)
    {
        if (ent.Comp.RecoilActive)
            args.Cancel();
    }

    private void OnRecoilHandleState(Entity<GunRecoilModifierComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        _blocker.UpdateCanMove(ent.Owner);
    }

    private RecoilProfile? GetRecoilProfile(List<(EntityUid? Uid, IShootable Shootable)> ammo)
    {
        foreach (var (uid, shootable) in ammo)
        {
            if (shootable is not CartridgeAmmoComponent cartridge)
                continue;

            if (uid is { } ammoUid)
            {
                if (_tags.HasTag(ammoUid, Caliber60AntiMaterielTag) ||
                    _tags.HasTag(ammoUid, Caliber60AntiMaterielToyTag))
                {
                    return Caliber60Profile;
                }

                if (_tags.HasTag(ammoUid, Caliber50ShotgunTag) ||
                    _tags.HasTag(ammoUid, Caliber50ShotgunToyTag))
                {
                    return Caliber50Profile;
                }

                if (_tags.HasTag(ammoUid, Caliber45MagnumTag) ||
                    _tags.HasTag(ammoUid, Caliber45AcpTag))
                {
                    return Caliber45Profile;
                }

                if (_tags.HasTag(ammoUid, Caliber20RifleTag))
                    return Caliber20RifleProfile;

                if (_tags.HasTag(ammoUid, Caliber30RifleTag))
                    return Caliber30RifleProfile;

                if (_tags.HasTag(ammoUid, Caliber10RifleTag))
                    return Caliber10RifleProfile;
            }

            if (GetProjectileRecoilProfile(cartridge.Prototype) is { } profile)
                return profile;
        }

        return null;
    }

    private static RecoilProfile? GetProjectileRecoilProfile(EntProtoId projectileId)
    {
        if (Caliber45Projectiles.Contains(projectileId))
            return Caliber45Profile;

        if (Caliber20RifleProjectiles.Contains(projectileId))
            return Caliber20RifleProfile;

        if (Caliber30RifleProjectiles.Contains(projectileId))
            return Caliber30RifleProfile;

        if (Caliber10RifleProjectiles.Contains(projectileId))
            return Caliber10RifleProfile;

        if (Caliber50Projectiles.Contains(projectileId))
            return Caliber50Profile;

        if (Caliber60Projectiles.Contains(projectileId))
            return Caliber60Profile;

        return null;
    }
}

[ByRefEvent]
public readonly record struct GunRecoilStartedEvent(GunRecoilModifierSystem.RecoilProfile Profile);
