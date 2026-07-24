// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Movement.Systems;

namespace Content.Shared.SS220.Virology.Behaviors;

public sealed partial class VirusMovementModifierSystem : EntitySystem
{
    [Dependency] private MovementSpeedModifierSystem _movement = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VirusMovementModifierComponent, RefreshMovementSpeedModifiersEvent>(OnRefresh);
        SubscribeLocalEvent<VirusMovementModifierComponent, ComponentStartup>(OnChanged);
        SubscribeLocalEvent<VirusMovementModifierComponent, AfterAutoHandleStateEvent>(OnChanged);
        SubscribeLocalEvent<VirusMovementModifierComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnRefresh(Entity<VirusMovementModifierComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.Reverting)
            return;

        args.ModifySpeed(ent.Comp.Walk, ent.Comp.Sprint);
    }

    // recompute speed whenever the modifier appears or its networked values arrive on the client
    private void OnChanged<T>(Entity<VirusMovementModifierComponent> ent, ref T args)
    {
        _movement.RefreshMovementSpeedModifiers(ent);
    }

    private void OnShutdown(Entity<VirusMovementModifierComponent> ent, ref ComponentShutdown args)
    {
        ent.Comp.Reverting = true;
        _movement.RefreshMovementSpeedModifiers(ent);
    }
}
