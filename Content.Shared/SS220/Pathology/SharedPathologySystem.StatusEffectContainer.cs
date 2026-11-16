// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.PathologyStatusEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Pathology;

public abstract partial class SharedPathologySystem : EntitySystem
{
    public void InitializeStatusEffectContainerEvents()
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

        HandleTraumaSeverityChanged(entity, ref args);
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

            if (otherProto.Definition[level].StatusEffects.Contains(effectId))
                totalStacks += otherData.StackCount;
        }

        if (totalStacks <= 0)
            return;

        if (!_statusEffects.TryUpdateStatusEffectDuration(entity.Owner, effectId, out var effect, null, null))
            return;

        if (!TryComp<PathologyStatusEffectStackableComponent>(effect, out var stackableComponent))
            return;

        if (stackableComponent.StackCount == totalStacks)
            return;

        stackableComponent.StackCount = totalStacks;
        Dirty(effect.Value, stackableComponent);
    }
}
