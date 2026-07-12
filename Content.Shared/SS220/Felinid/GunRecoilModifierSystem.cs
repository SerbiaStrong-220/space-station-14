using Content.Shared.SS220.Felinid.Components;
using Content.Shared.Tag;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Felinid;

public sealed partial class GunRecoilModifierSystem : EntitySystem
{
    private static readonly ProtoId<TagPrototype> Caliber45AcpTag = "CartridgeAmmoAcp";
    private static readonly ProtoId<TagPrototype> Caliber45MagnumTag = "CartridgeMagnum";
    private static readonly ProtoId<TagPrototype> Caliber20RifleTag = "CartridgeRifle";
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
        float ImpulseMultiplier,
        float KnockdownChance,
        TimeSpan KnockdownTime);

    private static readonly RecoilProfile Caliber45Profile = new(
        0.25f,
        0.03f,
        TimeSpan.FromSeconds(0.7));

    private static readonly RecoilProfile Caliber20RifleProfile = new(
        0.45f,
        0.06f,
        TimeSpan.FromSeconds(0.9));

    private static readonly RecoilProfile Caliber50Profile = new(
        0.8f,
        0.10f,
        TimeSpan.FromSeconds(1.15));

    private static readonly RecoilProfile Caliber60Profile = new(
        1.5f,
        0.20f,
        TimeSpan.FromSeconds(1.75));

    [Dependency] private TagSystem _tags = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GunRecoilModifierComponent, ShooterGunShotEvent>(OnShooterGunShot);
    }

    private void OnShooterGunShot(Entity<GunRecoilModifierComponent> ent, ref ShooterGunShotEvent args)
    {
        if (GetRecoilProfile(args.Ammo) is not { } profile)
            return;

        args.ImpulseMultiplier = Math.Max(args.ImpulseMultiplier, profile.ImpulseMultiplier);

        var recoilStarted = new GunRecoilStartedEvent(profile);
        RaiseLocalEvent(ent.Owner, ref recoilStarted);
    }

    public RecoilProfile? GetRecoilProfile(List<(EntityUid? Uid, IShootable Shootable)> ammo)
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

        if (Caliber50Projectiles.Contains(projectileId))
            return Caliber50Profile;

        if (Caliber60Projectiles.Contains(projectileId))
            return Caliber60Profile;

        return null;
    }
}

[ByRefEvent]
public readonly record struct GunRecoilStartedEvent(GunRecoilModifierSystem.RecoilProfile Profile);
