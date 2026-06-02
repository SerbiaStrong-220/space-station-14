// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.SS220.Pathology;
using Content.Shared.Armor;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Projectiles;
using Content.Shared.SS220.Pathology;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.SS220.PathologyProvider;

public sealed partial class PathologyProviderSystem : EntitySystem
{
    [Dependency] private PathologySystem _pathology = default!;
    [Dependency] private IPrototypeManager _prototype = default!;

    private readonly HashSet<ProtoId<DamageTypePrototype>> _damageTypesToIgnore = new() { "Structural" };
    // having any armor is better than nothing
    private const float AnyArmorModifierBonus = 0.85f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PathologyOnProjectileHitComponent, ProjectileHitEvent>(OnProjectileHit);
    }

    private void OnProjectileHit(Entity<PathologyOnProjectileHitComponent> entity, ref ProjectileHitEvent args)
    {
        var validDamages = args.Damage.DamageDict.Where(x => !_damageTypesToIgnore.Contains(x.Key));
        if (!validDamages.Any())
            return;

        var (key, _) = validDamages.MaxBy(x => x.Value);

        if (!_prototype.Resolve(entity.Comp.WeightedRandomPathology, out var weightedRandomPrototype))
            return;

        var (armorAffectedWeights, chanceMultiplier) = GetAffectedByArmoredChance(weightedRandomPrototype.Weights, args.Target, key);
        _pathology.TryMakeEntityContext(entity.Owner, entity.Comp.WeightedRandomProviderEntity, out var context);

        _pathology.TryAddRandom(args.Target, armorAffectedWeights, entity.Comp.ChanceToApply * chanceMultiplier, context);
    }

    private (Dictionary<string, float>, float) GetAffectedByArmoredChance(Dictionary<string, float> baseValues, EntityUid target, ProtoId<DamageTypePrototype> maxDamageTypeId)
    {
        var chanceMultiplier = 1f;
        Dictionary<string, float> result = [];
        foreach (var (key, value) in baseValues)
        {
            if (!_prototype.Resolve<PathologyPrototype>(key, out var pathologyPrototype))
                continue;

            if (pathologyPrototype.ArmorSlotFlags is null)
                continue;

            var armorEv = new CoefficientQueryEvent(pathologyPrototype.ArmorSlotFlags.Value);
            RaiseLocalEvent(target, armorEv);

            var newValue = armorEv.DamageModifiers.Coefficients.TryGetValue(maxDamageTypeId, out var armorCoefficient) ? armorCoefficient * AnyArmorModifierBonus * value : value;

            result.Add(key, newValue);
        }

        var baseSum = baseValues.Values.Sum();
        var newSum = result.Values.Sum();
        chanceMultiplier = newSum / baseSum;

        return (result, chanceMultiplier);
    }


}
