// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Shared.Popups;
using Content.Shared.SS220.Weapons.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Containers;

namespace Content.Shared.SS220.Weapons.Ranged.Systems;

public abstract class SharedGasWeaponSystem : EntitySystem
{

    [Dependency] protected readonly SharedContainerSystem _container = default!;
    [Dependency] protected readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasWeaponComponent, ShotAttemptedEvent>(OnShootAttempt);
    }

    protected virtual void OnShootAttempt(Entity<GasWeaponComponent>ent, ref ShotAttemptedEvent args)
    {
        if (!ent.Comp.CanShoot)
        {
            args.Cancel();
            return;
        }

        if (ent.Comp.GasUsage == 0f)
            return;

        if (!_container.TryGetContainer(ent.Owner, ent.Comp.TankSlotId, out var container) ||
            container is not ContainerSlot slot || slot.ContainedEntity is null)
            args.Cancel();
    }

}
