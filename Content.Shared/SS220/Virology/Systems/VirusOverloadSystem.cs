// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Movement.Systems;

namespace Content.Shared.SS220.Virology;

public sealed partial class VirusOverloadSystem : EntitySystem
{
    [Dependency] private MovementSpeedModifierSystem _movement = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VirusOverloadComponent, RefreshMovementSpeedModifiersEvent>(OnRefresh);
        SubscribeLocalEvent<VirusOverloadComponent, ComponentStartup>(OnChanged);
        SubscribeLocalEvent<VirusOverloadComponent, AfterAutoHandleStateEvent>(OnChanged);
        SubscribeLocalEvent<VirusOverloadComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnRefresh(Entity<VirusOverloadComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.Reverting)
            return;

        args.ModifySpeed(ent.Comp.Walk, ent.Comp.Sprint);
    }

    private void OnChanged<T>(Entity<VirusOverloadComponent> ent, ref T args)
    {
        _movement.RefreshMovementSpeedModifiers(ent);
    }

    private void OnShutdown(Entity<VirusOverloadComponent> ent, ref ComponentShutdown args)
    {
        ent.Comp.Reverting = true;
        _movement.RefreshMovementSpeedModifiers(ent);
    }
}
