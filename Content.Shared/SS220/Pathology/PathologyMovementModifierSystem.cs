// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Movement.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Pathology;

/// <summary>
/// Applies a symptom's <see cref="PathologyMovementModifierComponent"/> to the host's movement speed.
/// One reusable system for every movement-altering symptom.
/// </summary>
public sealed partial class PathologyMovementModifierSystem : EntitySystem
{
    [Dependency] private MovementSpeedModifierSystem _movement = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PathologyMovementModifierComponent, RefreshMovementSpeedModifiersEvent>(OnRefresh);
        SubscribeLocalEvent<PathologyMovementModifierComponent, ComponentStartup>(OnChanged);
        SubscribeLocalEvent<PathologyMovementModifierComponent, AfterAutoHandleStateEvent>(OnChanged);
        SubscribeLocalEvent<PathologyMovementModifierComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnRefresh(Entity<PathologyMovementModifierComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.Reverting)
            return;

        args.ModifySpeed(ent.Comp.Walk, ent.Comp.Sprint);
    }

    // recompute speed whenever the modifier appears or its networked values arrive on the client
    private void OnChanged<T>(Entity<PathologyMovementModifierComponent> ent, ref T args)
    {
        _movement.RefreshMovementSpeedModifiers(ent);
    }

    private void OnShutdown(Entity<PathologyMovementModifierComponent> ent, ref ComponentShutdown args)
    {
        ent.Comp.Reverting = true;
        _movement.RefreshMovementSpeedModifiers(ent);
    }
}
