using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.SS220.LiquidSplatters;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.SS220.LiquidSplatters;

public sealed class LiquidSplattersSystem : EntitySystem
{
    [Dependency] private InventorySystem _inventorySystem = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    private static readonly ProtoId<LiquidSplattersSettingsPrototype> DefaultSettings = "liquidSplattersSettingsDefault";

    private static readonly string[] PhysicalDamageTypes = { "Blunt", "Slash", "Piercing" };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MeleeWeaponComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<LiquidSplattersComponent, SolutionContainerChangedEvent>(OnSolutionChanged);
    }

    private void OnMeleeHit(Entity<MeleeWeaponComponent> ent, ref MeleeHitEvent args)
    {
        if (args.Handled || args.HitEntities.Count == 0)
            return;

        if (!IsPhysicalDamage(args.BaseDamage))
            return;

        var settings = _proto.Index(DefaultSettings);
        var attackingWeapon = GetAttackingWeapon(args.User, args.Weapon);

        foreach (var hit in args.HitEntities)
        {
            if (!TryComp<BloodstreamComponent>(hit, out var bloodstreamComp))
                continue;

            if (GetBloodSample((hit, bloodstreamComp)) is not { } sample)
                continue;

            AddSplatter(attackingWeapon, ScaledCopy(sample, settings.WeaponContainerIncrement));

            foreach (var (slot, chance) in settings.clothingSlotChances)
            {
                if (!_inventorySystem.TryGetSlotEntity(args.User, slot, out var item))
                    continue;

                if (item.Value == attackingWeapon)
                    continue;

                if (!_random.Prob(chance))
                    continue;

                AddSplatter(item.Value, ScaledCopy(sample, settings.ClothingContainerIncrement));
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

        _solution.AddSolution(sol.Value, sample);
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

    private static bool IsPhysicalDamage(DamageSpecifier damage)
    {
        foreach (var damageType in PhysicalDamageTypes)
        {
            if (damage.DamageDict.TryGetValue(damageType, out var amount) && amount > 0)
                return true;
        }

        return false;
    }

    private EntityUid GetAttackingWeapon(EntityUid user, EntityUid weapon)
    {
        if (weapon != user)
            return weapon;

        if (_inventorySystem.TryGetSlotEntity(user, "gloves", out var gloves))
            return gloves.Value;

        return user;
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
}
