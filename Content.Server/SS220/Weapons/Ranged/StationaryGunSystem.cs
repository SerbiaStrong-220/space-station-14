// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.DeviceLinking.Components;
using Content.Server.Power.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Events;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.FixedPoint;
using Content.Shared.Power;
using Content.Shared.PowerCell.Components;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Weapons.Ranged;

public sealed partial class StationaryGunSystem : EntitySystem
{
    [Dependency] private readonly SharedGunSystem _gun = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationaryGunComponent, PowerChangedEvent>(OnSignalReceived);
        SubscribeLocalEvent<StationaryGunComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);
    }

    private void OnSignalReceived(Entity<StationaryGunComponent> ent, ref PowerChangedEvent args)
    {
        if (args.Powered)
            return;

        if (!ent.Comp.RequiredPower)
            return;

        if (!TryComp<AutoShootGunComponent>(ent, out var autoShoot))
            return;

        _gun.SetEnabled(ent, autoShoot, false);
    }

    private void OnAnchorStateChanged(Entity<AutoShootGunComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (args.Transform.Anchored)
            return;

        _gun.SetEnabled(ent, ent.Comp, false);
    }
}
