// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Hallucination;
using Content.Shared.Random;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared.Inventory.Events;
using Content.Shared.Mind.Components;
using System.Linq;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Inventory;

namespace Content.Server.SS220.Hallucination;
/// <summary> System which make it easier to work with Hallucinations </summary>
public sealed class HallucinationSystem : EntitySystem
{
    [Dependency] IGameTiming _gameTiming = default!;
    [Dependency] EntityLookupSystem _entityLookup = default!;
    [Dependency] InventorySystem _inventory = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EyeProtectionComponent, GotEquippedEvent>(OnEyeProtectionEquip);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var sourceQuery = EntityQueryEnumerator<HallucinationSourceComponent>();
        while (sourceQuery.MoveNext(out var sourceUid, out var hallucinationSource))
        {
            if (_gameTiming.CurTime < hallucinationSource.NextUpdateTime)
                continue;
            if (!HasComp<TransformComponent>(sourceUid))
                continue;
            // Still have problems with things like RDs portal and endless hallucinations,
            // but for this case... well... goodluck)
            var hallucination = _entityLookup.GetEntitiesInRange<MindContainerComponent>(Transform(sourceUid).Coordinates, hallucinationSource.RangeOfHallucinations);
            var endHallucination = _entityLookup.GetEntitiesInRange<MindContainerComponent>(Transform(sourceUid).Coordinates, hallucinationSource.RangeOfEndHallucinations);

            foreach (var entity in hallucination)
            {
                if (HasComp<HallucinationImmuneComponent>(entity.Owner))
                    continue;
                if (hallucinationSource.EyeProtectionDependent
                        && _inventory.TryGetSlotEntity(entity.Owner, "eyes", out var eyesSlotEntity)
                        && HasComp<EyeProtectionComponent>(eyesSlotEntity))
                    continue;
                if (TryFindKey(entity.Owner, sourceUid.Id))
                    continue;
                Add(entity.Owner, sourceUid.Id, hallucinationSource.RandomEntitiesProto,
                    hallucinationSource.GetTimeParams(), hallucinationSource.EyeProtectionDependent);
            }
            foreach (var entity in endHallucination.Except(hallucination))
            {
                if (TryFindKey(entity.Owner, sourceUid.Id))
                    Remove(entity.Owner, sourceUid.Id);
            }
        }
        // Logic for deleting hallucinations which wasnt handled correctly
        var hallucinationQuery = EntityQueryEnumerator<HallucinationComponent>();
        while (hallucinationQuery.MoveNext(out var entityUid, out var hallucination))
        {
            foreach (var (key, timer) in hallucination.TotalDurationTimeSpans)
                if (_gameTiming.CurTime > timer)
                    Remove(entityUid, key);
        }

    }
    /// <summary> for Key use the id of author/performing entity </summary>
    public void Add(EntityUid target, int key, ProtoId<WeightedRandomEntityPrototype> randomEntities,
                                    (float BetweenHallucinations, float HallucinationMinTime,
                                    float HallucinationMaxTime, float TotalDuration)? timeParams = null,
                                    bool eyeProtectionDependent = false)
    {
        var hallucinationComponent = EnsureComp<HallucinationComponent>(target);
        hallucinationComponent.AddToRandomEntities(key, randomEntities, timeParams, eyeProtectionDependent);
        if (timeParams != null && timeParams.Value.TotalDuration != float.NaN)
            hallucinationComponent.TotalDurationTimeSpans[key] = _gameTiming.CurTime + TimeSpan.FromSeconds(timeParams.Value.TotalDuration);
        Dirty(target, hallucinationComponent);
    }
    public void Remove(EntityUid target, int key) => TryRemove(target, key);

    /// <returns> False if target dont have HallucinationComponent.
    /// If key doesnt exist in component throw exception </returns>
    public bool TryRemove(EntityUid target, int key)
    {
        if (!TryComp<HallucinationComponent>(target, out var hallucinationComponent))
            return false;
        hallucinationComponent.RemoveFromRandomEntities(key);
        Dirty(target, hallucinationComponent);
        return true;
    }
    /// <returns> False if target dont have HallucinationComponent or if key wasnt found </returns>
    public bool TryFindKey(EntityUid target, int key)
    {
        if (!TryComp<HallucinationComponent>(target, out var hallucinationComponent))
            return false;
        return hallucinationComponent.TryFindKey(key);
    }

    private void OnEyeProtectionEquip(Entity<EyeProtectionComponent> entity, ref GotEquippedEvent args)
    {
        if (args.Slot != "eyes")
            return;

        if (TryComp<HallucinationComponent>(args.Equipee, out var hallucinationComponent))
            foreach (var (key, depends) in hallucinationComponent.EyeProtectionDependent)
                if (depends)
                    Remove(args.Equipee, key);

    }
}
