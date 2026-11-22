// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.SS220.PathologyStatusEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Pathology;

public abstract partial class SharedPathologySystem : EntitySystem
{
    [Dependency] private MovementSpeedModifierSystem _movementSpeedModifier = default!;

    private void InitializeStatusEffectContainerEvents()
    {
        SubscribeLocalEvent<PathologyHolderComponent, PathologyStackCountChanged>(OnStatusEffectPathologyStackChange);
        SubscribeLocalEvent<PathologyHolderComponent, PathologySeverityChanged>(OnStatusEffectPathologySeverityChange);
    }

    private void OnStatusEffectPathologyStackChange(Entity<PathologyHolderComponent> entity, ref PathologyStackCountChanged args)
    {
        if (!_prototype.Resolve(args.PathologyId, out var pathologyPrototype))
            return;

        if (pathologyPrototype.Definition.Length <= args.Severity)
        {
            Log.Error($"Got severity level more than maximum index of pathology definition array! Error entity {entity}, pathology is {args.PathologyId}");
            return;
        }

        var definition = pathologyPrototype.Definition[args.Severity];
        foreach (var effectId in definition.StatusEffects)
        {
            UpdateStackableStatusEffect(entity, effectId);
        }
    }

    private void OnStatusEffectPathologySeverityChange(Entity<PathologyHolderComponent> entity, ref PathologySeverityChanged args)
    {
        if (!_prototype.Resolve(args.PathologyId, out var pathologyPrototype))
            return;

        if (pathologyPrototype.Definition.Length <= args.CurrentSeverity)
        {
            Log.Error($"Got severity level more than maximum index of pathology definition array! Error entity {entity}, pathology is {args.PathologyId}");
            return;
        }

        var definition = pathologyPrototype.Definition[args.CurrentSeverity];
        foreach (var effectId in definition.StatusEffects)
        {
            UpdateStackableStatusEffect(entity, effectId);
        }

        RefreshOverriddenStatusEffects(entity, pathologyPrototype);
        HandleTraumaSeverityChanged(entity, ref args);
    }

    /// <summary>
    /// Takes off or puts back the status effects of the pathologies this one overrides, after it was added or removed.
    /// </summary>
    private void RefreshOverriddenStatusEffects(Entity<PathologyHolderComponent> entity, PathologyPrototype pathologyPrototype)
    {
        foreach (var overriddenId in pathologyPrototype.OverridesStatusEffects)
        {
            if (!entity.Comp.ActivePathologies.TryGetValue(overriddenId, out var data) || !_prototype.Resolve(overriddenId, out var overridden))
                continue;

            var maxLevel = Math.Min(data.Level, overridden.Definition.Length - 1);
            for (var i = 0; i <= maxLevel; i++)
            {
                foreach (var effectId in overridden.Definition[i].StatusEffects)
                {
                    UpdateStackableStatusEffect(entity, effectId);
                }
            }
        }
    }

    private bool IsStatusEffectOverridden(PathologyHolderComponent holder, ProtoId<PathologyPrototype> pathologyId)
    {
        foreach (var otherId in holder.ActivePathologies.Keys)
        {
            if (_prototype.Resolve(otherId, out var otherProto) && otherProto.OverridesStatusEffects.Contains(pathologyId))
                return true;
        }

        return false;
    }

    public void UpdateStackableStatusEffect(Entity<PathologyHolderComponent> entity, EntProtoId effectId)
    {
        var totalStacks = 0;
        foreach (var (otherId, otherData) in entity.Comp.ActivePathologies)
        {
            if (!_prototype.Resolve(otherId, out var otherProto))
                continue;

            var level = Math.Min(otherData.Level, otherProto.Definition.Length - 1);
            if (level < 0)
                continue;

            if (otherProto.Definition[level].StatusEffects.Contains(effectId) && !IsStatusEffectOverridden(entity.Comp, otherId))
                totalStacks += otherData.StackCount;
        }

        if (totalStacks <= 0)
        {
            // every pathology still using the effect is overridden
            _statusEffects.TryRemoveStatusEffect(entity.Owner, effectId);
            return;
        }

        var alreadyApplied = _statusEffects.HasStatusEffect(entity.Owner, effectId);
        if (!_statusEffects.TryUpdateStatusEffectDuration(entity.Owner, effectId, out var effect, null, null))
            return;

        // a movement status effect applied outside MovementModStatusSystem doesn't refresh speed by itself
        if (!alreadyApplied && HasComp<MovementModStatusEffectComponent>(effect))
            _movementSpeedModifier.RefreshMovementSpeedModifiers(entity.Owner);

        if (!TryComp<PathologyStatusEffectStackableComponent>(effect, out var stackableComponent))
            return;

        if (stackableComponent.StackCount == totalStacks)
            return;

        stackableComponent.StackCount = totalStacks;
        Dirty(effect.Value, stackableComponent);
    }
}
