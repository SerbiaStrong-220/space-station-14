// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Armor;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Projectiles;
using Content.Shared.SS220.Pathology;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Shared.SS220.PathologyProvider;

public sealed class PathologyProviderSystem : EntitySystem
{
    [Dependency] private readonly PathologySystem _pathology = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly HashSet<ProtoId<DamageTypePrototype>> _damageTypesToIgnore = new() { "Structural" };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PathologyOnProjectileHitComponent, ProjectileHitEvent>(OnProjectileHit);
    }

    private void OnProjectileHit(Entity<PathologyOnProjectileHitComponent> entity, ref ProjectileHitEvent args)
    {
        var (key, _) = args.Damage.DamageDict.Where(x => !_damageTypesToIgnore.Contains(x.Key)).MaxBy(x => x.Value);

        if (key is null)
            return;

        if (!_prototype.HasIndex<DamageTypePrototype>(key))
            return;

        if (!_prototype.Resolve(entity.Comp.WeightedRandom, out var weightedRandomPrototype))
            return;

        var armorAffectedWeights = GetAffectedByArmoredChance(weightedRandomPrototype.Weights, key);

        _pathology.TryAddRandom(args.Target, armorAffectedWeights, entity.Comp.ChanceToApply);
    }


    private Dictionary<string, float> GetAffectedByArmoredChance(Dictionary<string, float> baseValues, ProtoId<DamageTypePrototype> maxDamageTypeId)
    {
        Dictionary<string, float> result = new();
        foreach (var (key, value) in baseValues)
        {
            if (!_prototype.Resolve<PathologyPrototype>(key, out var pathologyPrototype))
                continue;

            if (pathologyPrototype.ArmorSlotFlags is null)
                continue;

            var armorEv = new CoefficientQueryEvent(pathologyPrototype.ArmorSlotFlags.Value);

            var newValue = armorEv.DamageModifiers.Coefficients.TryGetValue(maxDamageTypeId, out var armorCoefficient) ? armorCoefficient * value : value;

            result.Add(key, newValue);
        }

        return result;
    }

}
