using System.Linq;
using Content.Server.SS220.DarkForces.Narsi.Buildings.Altar.Rituals.Prototypes;
using Content.Server.SS220.DarkForces.Narsi.Progress.Components;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Shared.SS220.DarkForces.Narsi.Roles;
using Content.Shared.Whitelist;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.SS220.DarkForces.Narsi.Buildings.Altar.Rituals.Base;

[ImplicitDataDefinitionForInheritors]
public abstract partial class NarsiRitualEffect
{
    public bool IsRitualAvailable(
        Entity<NarsiAltarComponent> altar,
        NarsiRitualRequirements requirements,
        IEntityManager entityManager
    )
    {
        if (!entityManager.TryGetComponent<TransformComponent>(altar, out var altarTransform))
            return false;

        var entityLookupSystem = entityManager.System<EntityLookupSystem>();
        var cultistsNearAltar =
            entityLookupSystem.GetEntitiesInRange<NarsiCultistComponent>(altarTransform.Coordinates, 2f);

        if (cultistsNearAltar.Count < requirements.CultistsCount)
            return false;

        if (requirements.BuckledEntityWhitelist != null && altar.Comp.BuckledEntity == null)
            return false;

        var entitiesInRange = entityLookupSystem.GetEntitiesInRange(altar, requirements.EntitiesFoundingRange);

        if (!IsBloodPuddlesValid(entitiesInRange, requirements, entityManager))
            return false;

        if (requirements.EntitiesRequirements == null || requirements.EntitiesRequirements.Count == 0)
            return true;

        var whitelistSystem = entityManager.System<EntityWhitelistSystem>();
        foreach (var requirement in requirements.EntitiesRequirements)
        {
            var validEntities = entitiesInRange.Where(entity => whitelistSystem.IsValid(requirement.Whitelist, entity));
            if (validEntities.Count() < requirement.Count)
                return false;
        }

        return true;
    }

    private bool IsBloodPuddlesValid(
        HashSet<EntityUid> entitiesInRange,
        NarsiRitualRequirements requirements,
        IEntityManager entityManager)
    {
        var bloodRequirements = requirements.BloodPuddleRequirements;
        if (bloodRequirements == null)
            return true;

        if (entitiesInRange.Count < bloodRequirements.Count)
            return false;

        var validReagentsCount = 0;
        var solutionSystem = entityManager.System<SolutionContainerSystem>();
        foreach (var entity in entitiesInRange)
        {
            var hasTargetReagents = bloodRequirements
                .ReagentsWhitelist
                .Any(reagent => solutionSystem.GetTotalPrototypeQuantity(entity, reagent) > 0);

            if (hasTargetReagents)
            {
                validReagentsCount++;
            }
        }

        return validReagentsCount >= bloodRequirements.Count;
    }

    public abstract void MakeRitualEffect(EntityUid altar,
        EntityUid perfomer,
        NarsiAltarComponent component,
        IEntityManager entityManager);

    public virtual void OnStartRitual(Entity<NarsiAltarComponent> altar,
        NarsiRitualPrototype ritual,
        IEntityManager entityManager)
    {
        var audioSystem = entityManager.System<AudioSystem>();
        var filter = Filter.Pvs(altar, 0.2f, entityManager: entityManager);
        altar.Comp.ActiveSound = audioSystem.PlayEntity(
                ritual.Sound,
                filter,
                altar,
                true,
                ritual.SoundParams
            )
            ?.Entity;

        var timing = IoCManager.Resolve<IGameTiming>();
        altar.Comp.VisualsParams.VisualsTick = timing.CurTime;
    }

    public virtual void OnRitualCanceled(Entity<NarsiAltarComponent> altar,
        NarsiRitualPrototype ritual,
        IEntityManager entityManager)
    {
        StopSound(altar, entityManager);
    }

    public virtual void OnRitualFinished(Entity<NarsiAltarComponent> altar,
        NarsiRitualPrototype ritual,
        IEntityManager entityManager)
    {
        StopSound(altar, entityManager);
    }

    private void StopSound(Entity<NarsiAltarComponent> altar, IEntityManager entityManager)
    {
        var audioSystem = entityManager.System<AudioSystem>();
        audioSystem.Stop(altar.Comp.ActiveSound);
        altar.Comp.ActiveSound = null;
    }

    public virtual void OnUpdate(Entity<NarsiAltarComponent> altar,
        NarsiRitualPrototype ritual,
        IEntityManager entityManager)
    {
        var timing = IoCManager.Resolve<IGameTiming>();
        var visuals = altar.Comp.VisualsParams;

        if (visuals.VisualsTick > timing.CurTime)
            return;

        if (!entityManager.TryGetComponent<TransformComponent>(altar, out var altarTransform))
            return;

        var random = IoCManager.Resolve<IRobustRandom>();
        var transform = entityManager.System<TransformSystem>();
        if (ritual.Requirements.BuckledEntityWhitelist != null && altar.Comp.BuckledEntity == null)
        {
            var buckledEffectId = random.Pick(visuals.VisualsEntities);
            var buckledEffect = entityManager.SpawnEntity(buckledEffectId, altarTransform.Coordinates);

            transform.SetParent(buckledEffect, altar);
        }

        var lookupSystem = entityManager.System<EntityLookupSystem>();
        var cultistsNearAltar = lookupSystem.GetEntitiesInRange<NarsiCultistComponent>(altarTransform.Coordinates, 4f);

        var cultistsEffect = random.Pick(visuals.VisualsEntities);
        foreach (var cultist in cultistsNearAltar)
        {
            if (!entityManager.TryGetComponent<TransformComponent>(cultist, out var cultistTransform))
                continue;

            var effect = entityManager.SpawnEntity(cultistsEffect, cultistTransform.Coordinates);
            transform.SetParent(effect, cultist);
        }

        visuals.VisualsTick = timing.CurTime + visuals.VisualsThreshold;
    }

    protected Entity<NarsiCultOfferingTargetComponent>? GetOfferingTarget(IEntityManager entityManager)
    {
        var query = entityManager.EntityQueryEnumerator<NarsiCultOfferingTargetComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Objectives == null)
                continue;

            return (uid, comp);
        }

        return null;
    }
}
