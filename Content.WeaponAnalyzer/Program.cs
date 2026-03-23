using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using CommandLine;
using Content.IntegrationTests;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Content.Shared.Power.Components;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.UnitTesting.Pool;

namespace Content.WeaponAnalyzer;

// TODO:
// - BatteryWeaponFireModes

public static class Program
{
    private static IPrototypeManager _prototypeManager = default!;
    private static IComponentFactory _componentFactory = default!;
    private static Options _options = default!;

    public sealed class Options
    {
        [Option("keep-errors", HelpText = "Set this flag to keep weapons with analyze errors in resulting list.")]
        public bool KeepErrors { get; set; }

        [Option('o', "out", Default = "Content.WeaponAnalyzer/weapons.csv", HelpText = "Output .csv file path.")]
        public string? OutputPath { get; set; }

        [Option('t', "targets", Default = new string[] { "MobHuman" }, HelpText = "List of targets.")]
        public IEnumerable<string> TargetIds { get; set; } = [];
    }

    public static async Task Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var result = Parser.Default.ParseArguments<Options>(args);
        if (result.Tag == ParserResultType.NotParsed)
        {
            foreach (var error in result.Errors)
            {
                if (error.Tag is ErrorType.VersionRequestedError or ErrorType.HelpRequestedError or ErrorType.HelpVerbRequestedError)
                {
                    continue;
                }
                Console.WriteLine($"Error: {error.Tag}");
            }
            return;
        }

        _options = result.Value;

        await Analyze();
    }

    private static async Task Analyze()
    {
        PoolManager.Startup();
        var testContext = new ExternalTestContext("Content.WeaponAnalyzer", Console.Out);
        await using var pair = await PoolManager.GetServerClient(testContext: testContext);
        _prototypeManager = pair.Server.ResolveDependency<IPrototypeManager>();
        _componentFactory = pair.Server.ResolveDependency<IComponentFactory>();
        var weapons = new List<WeaponInfo>();
        var targets = new List<TargetInfo>();

        await pair.Server.WaitPost(() =>
        {
            foreach (var proto in _prototypeManager.EnumeratePrototypes<EntityPrototype>())
            {
                if (ExtractWeaponInfo(proto) is { } weapon)
                    weapons.Add(weapon);
            }

            foreach (var targetId in _options.TargetIds)
            {
                if (!_prototypeManager.TryIndex<EntityPrototype>(targetId, out var proto))
                {
                    Console.WriteLine($"[ERROR]: Target prototype {targetId} not found");
                    continue;
                }
                if (ExtractTargetInfo(proto) is { } target)
                    targets.Add(target);
            }
        });

        foreach (var target in targets)
        {
            var outputPath = GenerateOutputPath(_options.OutputPath ?? "weapons.csv", target.Id);
            using var outputFile = File.Open(outputPath, FileMode.Create);
            using var writer = new StreamWriter(outputFile);

            Write(writer, "\"PrototypeId\"");
            Write(writer, "\"FireRate\"");
            Write(writer, "\"GunMagazine\"");
            Write(writer, "\"Capacity\"");
            Write(writer, "\"GunChamber\"");
            Write(writer, "\"CartridgeAmmo\"");
            Write(writer, "\"IgnoreResistances\"");
            Write(writer, "\"ActualDamage\"");
            Write(writer, "\"TDPS\"");
            Write(writer, "\"SummaryDamage\"");
            Write(writer, "\"StaminaDamage\"");
            Write(writer, "\"StaminaDPS\"");
            Write(writer, "\"SummaryStaminaDamage\"");
            Write(writer, "\"Health\"");
            Write(writer, "\"TTK\"");
            Write(writer, "\"BTK\"");
            writer.Write("\n");

            foreach (var weapon in weapons)
            {
                if (weapon.Error != WeaponAnalyzeError.None)
                {
                    if (!_options.KeepErrors)
                        continue;

                    Console.WriteLine($"Got error for weapon {weapon.Id} error is {weapon.Error}!");
                }

                var hitDamage = weapon.HitDamage;
                var hitStaminaDamage = weapon.HitStaminaDamage;
                hitDamage = target.ModifierSet != null && !weapon.IgnoreResistances
                    ? DamageSpecifier.ApplyModifierSet(hitDamage, target.ModifierSet)
                    : hitDamage;
                var originalDamage = hitDamage;
                hitDamage = new(hitDamage);
                foreach (var damageType in originalDamage.DamageDict.Keys)
                {
                    if (target.SupportedDamageTypes != null && !target.SupportedDamageTypes.Contains(damageType))
                    {
                        hitDamage.DamageDict.Remove(damageType);
                    }
                }

                await pair.Server.WaitPost(() =>
                {
                    var entityManager = pair.Server.ResolveDependency<IEntityManager>();
                    if (!weapon.IgnoreResistances)
                    {
                        var spawnedTarget = entityManager.Spawn(target.Id);

                        var damageModifyEvent = new DamageModifyEvent(hitDamage, null);
                        entityManager.EventBus.RaiseLocalEvent(spawnedTarget, damageModifyEvent);
                        hitDamage = damageModifyEvent.Damage;

                        var beforeStaminaDamageEvent = new BeforeStaminaDamageEvent(hitStaminaDamage);
                        entityManager.EventBus.RaiseLocalEvent(spawnedTarget, ref beforeStaminaDamageEvent);
                        hitStaminaDamage = beforeStaminaDamageEvent.Cancelled ? 0f : beforeStaminaDamageEvent.Value;

                        entityManager.DeleteEntity(spawnedTarget);
                    }
                    hitDamage = entityManager.System<DamageableSystem>().ApplyUniversalAllModifiers(hitDamage);
                });

                var testResults = new WeaponTestInfo()
                {
                    Weapon = weapon,
                    HitDamage = hitDamage,
                    HitStaminaDamage = hitStaminaDamage,
                    TimeToKillSeconds = target.DamageToKill.Float() / (hitDamage.GetTotal() * weapon.FireRate).Float(),
                    ShotsToKill = (int)Math.Ceiling(target.DamageToKill.Float() / hitDamage.GetTotal().Float()),
                };

                Write(writer, weapon.Id);
                Write(writer, weapon.FireRate);
                Write(writer, weapon.MagazineId ?? "-");
                Write(writer, weapon.Capacity);
                Write(writer, weapon.CartridgeId ?? "-");
                Write(writer, weapon.AmmoId ?? "-");
                Write(writer, weapon.IgnoreResistances);
                Write(writer, DamageToString(testResults.HitDamage));
                Write(writer, DamageToString(testResults.Dps));
                Write(writer, DamageToString(testResults.MagazineDamage));
                Write(writer, testResults.HitStaminaDamage);
                Write(writer, testResults.StaminaDps);
                Write(writer, testResults.MagazineStaminaDamage);
                Write(writer, target.DamageToKill);
                Write(writer, testResults.TimeToKillSeconds.ToString("F1"));
                Write(writer, testResults.ShotsToKill);
                writer.Write("\n");
            }

            Console.WriteLine($"Writen to: {Path.GetFullPath(outputPath)}");
        }

        void Write(TextWriter writer, object value)
        {
            writer.Write(value);
            writer.Write(",");
        }

        static string DamageToString(DamageSpecifier damage)
        {
            return $"{{{damage.ToString()["DamageSpecifier(".Length..^1]}}}";
        }

        static string GenerateOutputPath(string path, string targetName)
        {
            var directory = Path.GetDirectoryName(path);
            var fileName = Path.GetFileNameWithoutExtension(path);
            var extension = Path.GetExtension(path);
            var newName = $"{fileName}_{targetName}{extension}";
            return string.IsNullOrEmpty(directory)
                ? newName
                : Path.Combine(directory, newName);
        }
    }

    private static WeaponInfo? ExtractWeaponInfo(EntityPrototype proto)
    {
        if (proto.Abstract)
        {
            return null;
        }
        if (!proto.TryGetComponent<GunComponent>(out var gun, _componentFactory))
        {
            return null;
        }
        var info = new WeaponInfo()
        {
            Id = proto.ID,
            FireRate = gun.FireRate,
        };
        if (!TryGetAmmo(proto, info, out var ammoId))
        {
            info.Error = WeaponAnalyzeError.NoAmmoFound;
            return info;
        }
        _prototypeManager.TryIndex<EntityPrototype>(ammoId, out var ammoProto);
        if (ammoProto?.TryGetComponent<CartridgeAmmoComponent>(out var cartridgeAmmo, _componentFactory) ?? false)
        {
            ammoId = cartridgeAmmo.Prototype;
            info.CartridgeId = ammoId;
            _prototypeManager.TryIndex(ammoId, out ammoProto);
        }
        if (ammoProto?.TryGetComponent<ProjectileSpreadComponent>(out var projectileSpread, _componentFactory) ?? false)
        {
            ammoId = projectileSpread.Proto;
            info.SpreadCount = projectileSpread.Count;
            _prototypeManager.TryIndex(ammoId, out ammoProto);
        }
        if (ammoProto?.TryGetComponent<ProjectileComponent>(out var projectile, _componentFactory) ?? false)
        {
            info.AmmoId = ammoId;
            info.HitDamage = projectile.Damage;
            info.IgnoreResistances = projectile.IgnoreResistances;
            if (ammoProto.TryGetComponent<StaminaDamageOnCollideComponent>(out var staminaDamageOnCollide, _componentFactory))
                info.HitStaminaDamage = staminaDamageOnCollide.Damage;
            return info;
        }
        if (_prototypeManager.TryIndex(ammoId, out HitscanPrototype? hitscan))
        {
            info.AmmoId = ammoId;
            info.HitDamage = hitscan.Damage ?? info.HitDamage;
            info.HitStaminaDamage = hitscan.StaminaDamage;
            info.SpreadCount = hitscan.HitscanSpread?.Count ?? 1;
            return info;
        }

        info.Error = WeaponAnalyzeError.InvalidAmmo;
        return info;
    }

    private static bool TryGetAmmo(EntityPrototype proto, WeaponInfo info, [NotNullWhen(true)] out string? ammoId)
    {
        EntityPrototype? powerCellProto = null;

        Console.WriteLine($"Trying to get ammo of {proto}");
        if (proto.TryGetComponent<ItemSlotsComponent>(out var itemSlots, _componentFactory))
        {
#pragma warning disable RA0002 // Invalid access
            if (itemSlots.Slots.TryGetValue("gun_magazine", out var magazine) &&
                magazine.StartingItem != null)
            {
                if (_prototypeManager.Resolve(magazine.StartingItem, out var resolvedProto))
                    proto = resolvedProto;
                info.MagazineId = magazine.StartingItem;
            }
            else if (itemSlots.Slots.TryGetValue("cell_slot", out var cell) &&
                cell.StartingItem != null)
            {
                if (_prototypeManager.Resolve(cell.StartingItem, out var resolvedProto))
                    powerCellProto = resolvedProto;
                info.MagazineId = cell.StartingItem;
            }
#pragma warning restore RA0002 // Invalid access
        }
        if (proto.TryGetComponent<BallisticAmmoProviderComponent>(out var ballisticProvider, _componentFactory))
        {
            ammoId = ballisticProvider.Proto;
            info.Capacity = ballisticProvider.Capacity;
            return ammoId != null;
        }
        if (proto.TryGetComponent<ProjectileBatteryAmmoProviderComponent>(out var projectileBatteryProvider, _componentFactory))
        {
            ammoId = projectileBatteryProvider.Prototype;
            if (proto.TryGetComponent<BatteryComponent>(out var battery, _componentFactory)
                || (powerCellProto is not null && powerCellProto.TryGetComponent<BatteryComponent>(out battery, _componentFactory)))
                info.Capacity = (int)(battery.MaxCharge / projectileBatteryProvider.FireCost);
            else
                info.Error = WeaponAnalyzeError.BatteryNotFound;
            return ammoId != null;
        }
        if (proto.TryGetComponent<HitscanBatteryAmmoProviderComponent>(out var hitscanBatteryProvider, _componentFactory))
        {
            ammoId = hitscanBatteryProvider.Prototype;
            if (proto.TryGetComponent<BatteryComponent>(out var battery, _componentFactory))
                info.Capacity = (int)(battery.MaxCharge / hitscanBatteryProvider.FireCost);
            else
                info.Error = WeaponAnalyzeError.BatteryNotFound;
            return ammoId != null;
        }

        ammoId = null;
        return false;
    }

    private static TargetInfo? ExtractTargetInfo(EntityPrototype proto)
    {
        if (proto.Abstract)
        {
            return null;
        }
        if (!proto.TryGetComponent<MobThresholdsComponent>(out var thresholds, _componentFactory))
        {
            return null;
        }
        if (!proto.TryGetComponent<DamageableComponent>(out var damagable, _componentFactory))
        {
            return null;
        }
        var damageContainer = _prototypeManager.Index(damagable.DamageContainerID);
        HashSet<ProtoId<DamageTypePrototype>>? damageTypes = null;
        if (damageContainer is { })
        {
            foreach (var type in damageContainer.SupportedTypes)
            {
                damageTypes ??= [];
                damageTypes.Add(type);
            }

            foreach (var groupId in damageContainer.SupportedGroups)
            {
                var group = _prototypeManager.Index(groupId);
                foreach (var type in group.DamageTypes)
                {
                    damageTypes ??= [];
                    damageTypes.Add(type);
                }
            }
        }
        return new()
        {
            Id = proto.ID,
            DamageToKill = thresholds.BaseThresholds.FirstOrDefault(x => x.Value == Shared.Mobs.MobState.Critical).Key,
            SupportedDamageTypes = damageTypes,
            ModifierSet = _prototypeManager.Index(damagable.DamageModifierSetId),
        };
    }
}

public sealed class WeaponInfo
{
    public EntProtoId Id;
    public DamageSpecifier HitDamage = new();
    public float HitStaminaDamage;
    public int Capacity = 1;
    public float FireRate;
    public int SpreadCount = 1;
    public bool IgnoreResistances;
    public string? MagazineId;
    public string? CartridgeId;
    public string? AmmoId;
    public WeaponAnalyzeError Error;

    public DamageSpecifier ShotDamage => HitDamage * SpreadCount;
    public DamageSpecifier MagazineDamage => ShotDamage * Capacity;
    public DamageSpecifier Dps => ShotDamage * FireRate;
    public float ShotStaminaDamage => HitStaminaDamage * SpreadCount;
    public float MagazineStaminaDamage => HitStaminaDamage * Capacity;
    public float StaminaDps => HitStaminaDamage * FireRate;
}

public sealed class TargetInfo
{
    public required EntProtoId Id;
    public FixedPoint2 DamageToKill;
    public HashSet<ProtoId<DamageTypePrototype>>? SupportedDamageTypes;
    public DamageModifierSet? ModifierSet;
}

public sealed class WeaponTestInfo
{
    public required WeaponInfo Weapon;
    public DamageSpecifier HitDamage = new();
    public DamageSpecifier ShotDamage => HitDamage * Weapon.SpreadCount;
    public DamageSpecifier MagazineDamage => ShotDamage * Weapon.Capacity;
    public DamageSpecifier Dps => ShotDamage * Weapon.FireRate;
    public float HitStaminaDamage;
    public float ShotStaminaDamage => HitStaminaDamage * Weapon.SpreadCount;
    public float MagazineStaminaDamage => HitStaminaDamage * Weapon.Capacity;
    public float StaminaDps => HitStaminaDamage * Weapon.FireRate;
    public float TimeToKillSeconds;
    public int ShotsToKill;
}

public enum WeaponAnalyzeError
{
    None,
    NoAmmoFound,
    InvalidAmmo,
    BatteryNotFound,
}
