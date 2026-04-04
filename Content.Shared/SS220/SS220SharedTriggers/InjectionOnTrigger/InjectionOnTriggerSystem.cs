// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Trigger;

namespace Content.Shared.SS220.SS220SharedTriggers.InjectionOnTrigger;

/// <summary>
/// This is used for injects reagents when a trigger is activated
/// </summary>
public sealed class InjectionOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainers = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<InjectionOnTriggerComponent, TriggerEvent>(OnTriggered);
    }

    private void OnTriggered(Entity<InjectionOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (!ent.Comp.Reagent.HasValue || !args.User.HasValue)
            return;

        if (!_solutionContainers.TryGetInjectableSolution(args.User.Value, out var injectable, out _))
            return;

        _solutionContainers.TryAddReagent(injectable.Value, ent.Comp.Reagent, ent.Comp.Quantity, out _);
    }
}
