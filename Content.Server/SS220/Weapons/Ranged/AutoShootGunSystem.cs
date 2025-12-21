// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Power.Components;
using Content.Shared.Power;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Server.SS220.Weapons.Ranged;

public sealed partial class AutoShootGunSystem : EntitySystem
{
    [Dependency] private readonly SharedGunSystem _gun = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutoShootGunComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<AutoShootGunComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);
    }

    private void OnPowerChanged(Entity<AutoShootGunComponent> ent, ref PowerChangedEvent args)
    {
        if (!ent.Comp.RequiredPower)
            return;

        if (!TryComp<ApcPowerReceiverComponent>(ent, out var apc))
            return;

        if (apc.Powered)
            return;

        _gun.SetEnabled(ent, ent.Comp, false);
    }

    private void OnAnchorStateChanged(Entity<AutoShootGunComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (ent.Comp.CanShootUnanchored)
            return;

        if (args.Transform.Anchored)
            return;

        _gun.SetEnabled(ent, ent.Comp, false);
    }
}
