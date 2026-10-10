using Content.Shared.Body.Components;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.SS220.LiquidSplatters;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.SS220.LiquidSplatters;

public sealed class LiquidSplattersSystem : SharedLiquidSplattersSystem
{
    [Dependency] private InventorySystem _inventorySystem = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;

    private static readonly ProtoId<LiquidSplattersSettingsPrototype> DefaultSettings = "liquidSplattersSettingsDefault";

    private static readonly string[] PhysicalDamageTypes = { "Blunt", "Slash", "Piercing" };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MeleeWeaponComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<LiquidSplattersComponent, SolutionContainerChangedEvent>(OnSolutionChanged);
        SubscribeLocalEvent<LiquidSplattersComponent, ReactionEntityEvent>(OnReaction);
        SubscribeLocalEvent<InventoryComponent, ReactionEntityEvent>(OnWearerReaction);
    }

    private void OnReaction(Entity<LiquidSplattersComponent> ent, ref ReactionEntityEvent args)
    {
        if (!TryGetCleaningAmount(ref args, out var amount))
            return;

        CleanSplatters(ent.Owner, amount);
    }

    private void OnWearerReaction(Entity<InventoryComponent> ent, ref ReactionEntityEvent args)
    {
        if (!TryGetCleaningAmount(ref args, out var amount))
            return;

        var enumerator = _inventorySystem.GetSlotEnumerator((ent.Owner, ent.Comp));
        while (enumerator.NextItem(out var item))
        {
            CleanSplatters(item, amount);
        }
    }

    private void OnMeleeHit(Entity<MeleeWeaponComponent> ent, ref MeleeHitEvent args)
    {
        if (args.Handled || args.HitEntities.Count == 0)
            return;

        var settings = _proto.Index(DefaultSettings);
        var meleeDamage = GetMeleeDamage(args.BaseDamage);
        if (meleeDamage < settings.MinSplatterDamage)
            return;

        var attackingWeapon = args.Weapon;

        var solutionAmountOnWeapon = meleeDamage * settings.DamageSolutionScale;
        var solutionAmountOnClothing = meleeDamage * settings.ClothingSolutionScale;

        foreach (var hit in args.HitEntities)
        {
            if (!TryComp<BloodstreamComponent>(hit, out var bloodstreamComp))
                continue;

            if (GetBloodSample((hit, bloodstreamComp)) is not { } sample)
                continue;

            if (!(args.User == attackingWeapon))
            {
                Log.Debug("Adding splatter to {0}, amount: {1}", attackingWeapon, solutionAmountOnWeapon);
                AddSplatter(attackingWeapon, ScaledCopy(sample, solutionAmountOnWeapon));
            }

            foreach (var (slot, chance) in settings.ClothingSlotChances)
            {
                if (!_inventorySystem.TryGetSlotEntity(args.User, slot, out var item))
                    continue;

                if (item.Value == attackingWeapon)
                    continue;

                if (!_random.Prob(chance))
                    continue;

                Log.Debug("Adding splatter to {0} on {1}, amount: {2}", item.Value, slot, solutionAmountOnClothing);
                AddSplatter(item.Value, ScaledCopy(sample, solutionAmountOnClothing));
            }
        }
    }

    private void OnSolutionChanged(Entity<LiquidSplattersComponent> ent, ref SolutionContainerChangedEvent args)
    {
        if (args.SolutionId != ent.Comp.ContainerName)
            return;

        UpdateVisuals(ent, args.Solution);
    }

    public void AddSplatter(EntityUid target, Solution sample)
    {
        var comp = EnsureComp<LiquidSplattersComponent>(target);

        if (!_solution.EnsureSolution(target, comp.ContainerName, out _, comp.MaxVolume))
            return;

        Entity<SolutionComponent>? sol = null;
        if (!_solution.ResolveSolution(target, comp.ContainerName, ref sol))
            return;

        Log.Debug("Addding {0} units to solution with volume {1}, new volume {2}",
            sample.Volume, sol.Value.Comp.Solution.Volume, sol.Value.Comp.Solution.Volume + sample.Volume);
        _solution.AddSolution(sol.Value, sample);
    }

    private void UpdateVisuals(Entity<LiquidSplattersComponent> ent, Solution solution)
    {
        var fill = ent.Comp.MaxVolume > 0
            ? Math.Clamp(solution.Volume.Float() / ent.Comp.MaxVolume, 0f, 1f)
            : 0f;

        ent.Comp.Enabled = fill > 0;
        ent.Comp.Intensity = fill;

        if (fill > 0)
            ent.Comp.Color = solution.GetColor(_proto);

        Dirty(ent);
    }

    private Solution? GetBloodSample(Entity<BloodstreamComponent> victim)
    {
        if (!_solution.ResolveSolution(victim.Owner, victim.Comp.BloodSolutionName,
                ref victim.Comp.BloodSolution, out var blood) || blood.Volume <= 0)
            return null;

        var sample = new Solution();
        foreach (var (reagent, quantity) in blood.Contents)
        {
            if (victim.Comp.BloodReferenceSolution.ContainsPrototype(reagent.Prototype))
                sample.AddReagent(reagent, quantity);
        }

        if (sample.Volume <= 0)
            return null;

        sample.ScaleSolution(1f / sample.Volume.Float());
        return sample;
    }

    private static Solution ScaledCopy(Solution sample, FixedPoint2 amount)
    {
        var copy = sample.Clone();
        copy.ScaleSolution(amount);
        return copy;
    }

    private bool CanClean(ref ReactionEntityEvent reaction)
    {
        if (reaction.Method != ReactionMethod.Touch)
            return false;

        var settings = _proto.Index(DefaultSettings);
        return settings.CleaningReagentsEffectiveness.ContainsKey(reaction.Reagent);
    }

    private bool TryGetCleaningAmount(ref ReactionEntityEvent args, out FixedPoint2 amount)
    {
        amount = FixedPoint2.Zero;

        if (!CanClean(ref args))
            return false;

        var settings = _proto.Index(DefaultSettings);
        if (!settings.CleaningReagentsEffectiveness.TryGetValue(args.Reagent.ID, out var effectiveness))
            return false;

        amount = args.ReagentQuantity.Quantity * effectiveness;
        return amount > 0;
    }

    public void ClearSplatters(EntityUid target)
    {
        if (!TryComp<LiquidSplattersComponent>(target, out var comp))
            return;

        Entity<SolutionComponent>? sol = null;
        if (!_solution.ResolveSolution(target, comp.ContainerName, ref sol))
            return;

        _solution.RemoveAllSolution(sol.Value);
    }

    public FixedPoint2 CleanSplatters(EntityUid target, FixedPoint2 amount)
    {
        if (!TryComp<LiquidSplattersComponent>(target, out var comp))
            return FixedPoint2.Zero;

        Entity<SolutionComponent>? sol = null;
        if (!_solution.ResolveSolution(target, comp.ContainerName, ref sol))
            return FixedPoint2.Zero;

        var oldVolume = sol.Value.Comp.Solution.Volume;
        _solution.SplitSolution(sol.Value, amount);
        var actuallyRemoved = oldVolume - sol.Value.Comp.Solution.Volume;
        return actuallyRemoved;
    }

    public FixedPoint2 CleanSplattersOnTile(TileRef tile, ReagentPrototype reagent, FixedPoint2 reactVolume)
    {
        var settings = _proto.Index(DefaultSettings);
        if (!settings.CleaningReagentsEffectiveness.TryGetValue(reagent.ID, out var effectiveness) || effectiveness <= 0)
            return FixedPoint2.Zero;

        var budget = reactVolume * effectiveness;
        var removed = FixedPoint2.Zero;

        foreach (var uid in _lookup.GetLocalEntitiesIntersecting(tile))
        {
            if (removed >= budget)
                break;

            removed += CleanSplatters(uid, budget - removed);
        }

        return removed / effectiveness;
    }

    private static FixedPoint2 GetMeleeDamage(DamageSpecifier damage)
    {
        var total = FixedPoint2.Zero;
        foreach (var damageType in PhysicalDamageTypes)
        {
            if (damage.DamageDict.TryGetValue(damageType, out var amount))
                total += amount;
        }

        return total;
    }
}
