// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Rejuvenate;
using Content.Shared.SS220.PathologyStatusEffects;
using Content.Shared.StatusEffectNew.Components;


namespace Content.Shared.SS220.Pathology;

public abstract partial class SharedPathologySystem : EntitySystem
{
    public void InitializeStatusEffectContainerEvents()
    {
        SubscribeLocalEvent<PathologyHolderComponent, RejuvenateEvent>(OnRejuvenate);

        SubscribeLocalEvent<StatusEffectContainerComponent, PathologyStackCountChanged>(OnStatusEffectPathologyStackChange);
        SubscribeLocalEvent<StatusEffectContainerComponent, PathologySeverityChanged>(OnStatusEffectPathologySeverityChange);
    }

    private void OnStatusEffectPathologyStackChange(Entity<StatusEffectContainerComponent> entity, ref PathologyStackCountChanged args)
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
            if (!_statusEffects.TryUpdateStatusEffectDuration(entity.Owner, effectId, out var effect, null, null))
                continue;

            if (!TryComp<PathologyStatusEffectStackableComponent>(effect, out var stackableComponent))
                continue;

            stackableComponent.StackCount = args.NewCount;
        }
    }

    private void OnStatusEffectPathologySeverityChange(Entity<StatusEffectContainerComponent> entity, ref PathologySeverityChanged args)
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
            if (!_statusEffects.TryUpdateStatusEffectDuration(entity, effectId, null, null))
                return;
        }
    }
}
