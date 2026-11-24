// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.SS220.Pathology;
using Content.Shared.Armor;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Projectiles;
using Content.Shared.Random;
using Content.Shared.SS220.Pathology;
using Robust.Shared.Collections;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.SS220.PathologyProvider;

public sealed partial class PathologyProviderSystem : EntitySystem
{
    [Dependency] private PathologySystem _pathology = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IGameTiming _timing = default!;

    // Having any armor on a zone reduces trauma chance on top of damage absorption.
    private const float AnyArmorModifierBonus = 0.85f;

    private static readonly HashSet<ProtoId<DamageTypePrototype>> DamageTypesToIgnore = new() { "Structural" };
    private static readonly ProtoId<DamageTypePrototype> DamageBlunt = "Blunt";
    private static readonly ProtoId<DamageTypePrototype> DamageSlash = "Slash";
    private static readonly ProtoId<DamageTypePrototype> DamagePiercing = "Piercing";
    private static readonly ProtoId<DamageTypePrototype> DamageHeat = "Heat";
    private static readonly ProtoId<DamageTypePrototype> DamageCaustic = "Caustic";
    private static readonly ProtoId<DamageTypePrototype> DamageShock = "Shock";

    private static readonly ProtoId<WeightedRandomPrototype> DislocationTable = "DislocationPathology";
    private static readonly ProtoId<WeightedRandomPrototype> HairlineFractureTable = "HairlineFracturePathology";
    private static readonly ProtoId<WeightedRandomPrototype> CompoundFractureTable = "CompoundFracturePathology";
    private static readonly ProtoId<WeightedRandomPrototype> ArterialPunctureTable = "ArterialPuncturePathology";
    private static readonly ProtoId<WeightedRandomPrototype> BurnNecrosisTable = "BurnNecrosisPathology";

    private static readonly TraumaRoll CompoundFractureRoll = new(35f, 20f, 0.035f, 0.50f, 0.90f);
    private static readonly TraumaRoll HairlineFractureRoll = new(22f, 15f, 0.03f, 0.20f, 0.55f);
    private static readonly TraumaRoll DislocationRoll = new(12f, 10f, 0.025f, 0.05f, 0.35f);
    private static readonly TraumaRoll ArterialPunctureRoll = new(18f, 14f, 0.025f, 0.10f, 0.65f);
    private static readonly TraumaRoll BurnNecrosisRoll = new(20f, 15f, 0.025f, 0.12f, 0.65f);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PathologyOnProjectileHitComponent, ProjectileHitEvent>(OnProjectileHit);
        SubscribeLocalEvent<PathologyHolderComponent, DamageChangedEvent>(OnDamageChanged);

        InitializeOrganSources();
    }

    private void OnProjectileHit(Entity<PathologyOnProjectileHitComponent> entity, ref ProjectileHitEvent args)
    {
        if (HasComp<GodmodeComponent>(args.Target) || !TryComp<PathologyHolderComponent>(args.Target, out var holder))
            return;

        ProtoId<DamageTypePrototype>? maxDamageType = null;
        var maxDamage = FixedPoint2.Zero;
        foreach (var (type, amount) in args.Damage.DamageDict)
        {
            if (DamageTypesToIgnore.Contains(type) || amount <= maxDamage)
                continue;

            maxDamageType = type;
            maxDamage = amount;
        }

        if (maxDamageType is null)
            return;

        _pathology.TryMakeEntityContext(entity.Owner, entity.Comp.WeightedRandomProviderEntity, out var context);
        AddArmoredRandom(args.Target, entity.Comp.WeightedRandomPathology, maxDamageType.Value, entity.Comp.ChanceToApply, context);

        // fragments are rolled here, the damage of this projectile comes right after this event and rolls only the arterial puncture
        holder.ProjectileTraumaTick = _timing.CurTick;
    }

    private void OnDamageChanged(Entity<PathologyHolderComponent> entity, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.DamageDelta is not { } delta)
            return;

        if (HasComp<GodmodeComponent>(entity.Owner))
            return;

        if (entity.Comp.ProjectileTraumaTick == _timing.CurTick)
        {
            entity.Comp.ProjectileTraumaTick = GameTick.Zero;

            RollArterialPuncture(entity, delta);
            return;
        }

        var blunt = GetDamageAmount(delta, DamageBlunt);
        if (CompoundFractureRoll.TryGetChance(blunt.Float(), out var compoundChance))
            AddArmoredRandom(entity.Owner, CompoundFractureTable, DamageBlunt, compoundChance);
        else if (HairlineFractureRoll.TryGetChance(blunt.Float(), out var hairlineChance))
            AddArmoredRandom(entity.Owner, HairlineFractureTable, DamageBlunt, hairlineChance);
        else if (DislocationRoll.TryGetChance(blunt.Float(), out var dislocationChance))
            AddArmoredRandom(entity.Owner, DislocationTable, DamageBlunt, dislocationChance);

        RollArterialPuncture(entity, delta);

        var heat = GetDamageAmount(delta, DamageHeat);
        var caustic = GetDamageAmount(delta, DamageCaustic);
        var shock = GetDamageAmount(delta, DamageShock);
        if (BurnNecrosisRoll.TryGetChance((heat + caustic + shock).Float(), out var burnChance))
        {
            var burnType = GetMaxDamageType(heat, DamageHeat, caustic, DamageCaustic, shock, DamageShock);
            AddArmoredRandom(entity.Owner, BurnNecrosisTable, burnType, burnChance);
        }
    }

    private void RollArterialPuncture(Entity<PathologyHolderComponent> entity, DamageSpecifier delta)
    {
        var slash = GetDamageAmount(delta, DamageSlash);
        var piercing = GetDamageAmount(delta, DamagePiercing);
        if (!ArterialPunctureRoll.TryGetChance((slash + piercing).Float(), out var arterialChance))
            return;

        var sharpType = piercing >= slash ? DamagePiercing : DamageSlash;
        AddArmoredRandom(entity.Owner, ArterialPunctureTable, sharpType, arterialChance);
    }

    private void AddArmoredRandom(
        EntityUid target,
        ProtoId<WeightedRandomPrototype> tableId,
        ProtoId<DamageTypePrototype> damageType,
        float baseChance,
        IPathologyContext? context = null)
    {
        if (!_prototype.Resolve(tableId, out var weightedRandomPrototype))
            return;

        var (armorAffectedWeights, chanceMultiplier) = GetAffectedByArmoredChance(weightedRandomPrototype.Weights, target, damageType);
        if (armorAffectedWeights.Count == 0 || chanceMultiplier <= 0f)
            return;

        _pathology.TryAddRandom(target, armorAffectedWeights, baseChance * chanceMultiplier, context);
    }

    private (Dictionary<string, float> Weights, float ChanceMultiplier) GetAffectedByArmoredChance(
        Dictionary<string, float> baseValues,
        EntityUid target,
        ProtoId<DamageTypePrototype> maxDamageTypeId)
    {
        Dictionary<string, float> result = new(baseValues.Count);
        var cachedSlotMultipliers = new ValueList<(SlotFlags Slot, float Multiplier)>();
        var baseSum = 0f;
        var newSum = 0f;

        foreach (var (key, value) in baseValues)
        {
            if (!_prototype.Resolve<PathologyPrototype>(key, out var pathologyPrototype))
                continue;

            baseSum += value;

            var newValue = value;
            if (pathologyPrototype.ArmorSlotFlags is { } slotFlags)
                newValue *= GetArmorSlotMultiplier(target, slotFlags, maxDamageTypeId, ref cachedSlotMultipliers);

            result.Add(key, newValue);
            newSum += newValue;
        }

        if (baseSum <= 0f)
            return (result, 0f);

        return (result, newSum / baseSum);
    }

    private float GetArmorSlotMultiplier(
        EntityUid target,
        SlotFlags slotFlags,
        ProtoId<DamageTypePrototype> damageTypeId,
        ref ValueList<(SlotFlags Slot, float Multiplier)> cache)
    {
        foreach (var (cachedSlot, cachedMultiplier) in cache)
        {
            if (cachedSlot == slotFlags)
                return cachedMultiplier;
        }

        var armorEv = new CoefficientQueryEvent(slotFlags);
        RaiseLocalEvent(target, armorEv);

        var multiplier = armorEv.DamageModifiers.Coefficients.TryGetValue(damageTypeId, out var armorCoefficient)
            ? Math.Clamp(armorCoefficient * AnyArmorModifierBonus, 0f, 1f)
            : 1f;

        cache.Add((slotFlags, multiplier));
        return multiplier;
    }

    private static FixedPoint2 GetDamageAmount(DamageSpecifier delta, ProtoId<DamageTypePrototype> type)
    {
        return delta.DamageDict.TryGetValue(type, out var amount) && amount > FixedPoint2.Zero
            ? amount
            : FixedPoint2.Zero;
    }

    private static ProtoId<DamageTypePrototype> GetMaxDamageType(
        FixedPoint2 a,
        ProtoId<DamageTypePrototype> typeA,
        FixedPoint2 b,
        ProtoId<DamageTypePrototype> typeB,
        FixedPoint2 c,
        ProtoId<DamageTypePrototype> typeC)
    {
        if (a >= b && a >= c)
            return typeA;

        return b >= c ? typeB : typeC;
    }
}
