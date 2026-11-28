// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Movement.Systems;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.PathologyStatusEffects;

/// <summary>
/// Scales the slowdown of <see cref="PathologyMovementModStatusEffectComponent"/> by the stacks of the status effect.
/// Speed is refreshed by the pathology system when the stacks change.
/// </summary>
public sealed partial class PathologyMovementModStatusEffectSystem : EntitySystem
{
    [Dependency] private MovementSpeedModifierSystem _movementSpeedModifier = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PathologyMovementModStatusEffectComponent, StatusEffectRelayedEvent<RefreshMovementSpeedModifiersEvent>>(OnRefreshMovementSpeed);
        SubscribeLocalEvent<PathologyMovementModStatusEffectComponent, StatusEffectRemovedEvent>(OnRemoved);
    }

    private void OnRefreshMovementSpeed(Entity<PathologyMovementModStatusEffectComponent> ent, ref StatusEffectRelayedEvent<RefreshMovementSpeedModifiersEvent> args)
    {
        DebugTools.Assert(ent.Comp.MaxStacks > 0, $"{ToPrettyString(ent)} has no max stacks for its slowdown");

        if (!TryComp<PathologyStatusEffectStackableComponent>(ent, out var stackable))
            return;

        var strength = Math.Clamp((float) stackable.StackCount / ent.Comp.MaxStacks, 0f, 1f);
        args.Args.ModifySpeed(1f, 1f - (1f - ent.Comp.SprintSpeedModifier) * strength);
    }

    private void OnRemoved(Entity<PathologyMovementModStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        // the effect has already left the container, so it no longer gets the refresh
        _movementSpeedModifier.RefreshMovementSpeedModifiers(args.Target);
    }
}
