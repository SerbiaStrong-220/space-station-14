using Content.Server.DeviceLinking.Components;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Server.Power.Components;

namespace Content.Server.DeviceLinking.Systems;

public sealed partial class GunSignalControlSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _signalSystem = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GunSignalControlComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<GunSignalControlComponent, SignalReceivedEvent>(OnSignalReceived);
    }

    private void OnInit(Entity<GunSignalControlComponent> gunControl, ref MapInitEvent args)
    {
        _signalSystem.EnsureSinkPorts(gunControl, gunControl.Comp.TriggerPort, gunControl.Comp.TogglePort, gunControl.Comp.OnPort, gunControl.Comp.OffPort);
    }

    private void OnSignalReceived(Entity<GunSignalControlComponent> gunControl, ref SignalReceivedEvent args)
    {
        if (!TryComp<GunComponent>(gunControl, out var gun))
            return;

        //SS220 ShuttleGuns_fix start (#3180)
        if (!TryComp<AutoShootGunComponent>(gunControl, out var autoShootGun))
            return;

        if (EntityManager.TryGetComponent(gunControl, out TransformComponent? transform) && !transform.Anchored && !autoShootGun.CanShootUnanchored)
            return;

        if (TryComp<ApcPowerReceiverComponent>(gunControl, out var apc) && !apc.Powered && autoShootGun.RequiredPower)
            return;
        //SS220 ShuttleGuns_fix end (#3180)

        if (args.Port == gunControl.Comp.TriggerPort)
            _gun.AttemptShoot(gunControl, gun);

        if (!TryComp<AutoShootGunComponent>(gunControl, out var autoShoot))
            return;

        if (args.Port == gunControl.Comp.TogglePort)
           _gun.SetEnabled(gunControl, autoShoot, !autoShoot.Enabled);

        if (args.Port == gunControl.Comp.OnPort)
            _gun.SetEnabled(gunControl, autoShoot, true);

        if (args.Port == gunControl.Comp.OffPort)
            _gun.SetEnabled(gunControl, autoShoot, false);
    }
}
