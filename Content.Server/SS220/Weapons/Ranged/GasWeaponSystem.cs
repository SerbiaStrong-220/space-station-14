using Content.Server.Atmos.EntitySystems;
using Content.Server.Storage.EntitySystems;
using Content.Server.Stunnable;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;
using Content.Shared.PneumaticCannon;
using Content.Shared.SS220.Weapons.Components;
using Content.Shared.SS220.Weapons.Ranged.Events;
using Content.Shared.SS220.Weapons.Ranged.Systems;
using Content.Shared.Tools.Systems;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Containers;

namespace Content.Server.SS220.Weapons.Ranged.Systems;

public sealed class GasWeaponSystem : SharedGasWeaponSystem
{
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly GasTankSystem _gasTank = default!;
    [Dependency] private readonly GunSystem _gun = default!;
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly ItemSlotsSystem _slots = default!;
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasWeaponComponent, ShotAttemptedEvent>(OnShootAttempt);
    }

    private void OnShootAttempt(Entity<GasWeaponComponent> ent, ref ShotAttemptedEvent args)
    {
        // require a gas tank if it uses gas
        var gas = GetGas(ent);

        if (gas == null || gas.Value.Comp.Air.TotalMoles < ent.Comp.GasUsage)
        {
            args.Cancel();
            return;
        }


        foreach (Gas type in Enum.GetValues(typeof(Gas)))
        {
            float amountOfWrongGasMoles = 0f;

            if (type != ent.Comp.GasType && gas.Value.Comp.Air.GetMoles(type) > 0f)
                amountOfWrongGasMoles += gas.Value.Comp.Air.GetMoles(type);

            if (amountOfWrongGasMoles > 0f)
            {
                args.Cancel();

                var ev = new GasWeaponWrongGasEvent(ent, amountOfWrongGasMoles);
                RaiseLocalEvent(ent.Owner, ref ev);

                return;
            }
        }

        _gasTank.RemoveAir(gas.Value, ent.Comp.GasUsage);
    }

    private Entity<GasTankComponent>? GetGas(EntityUid uid)
    {
        if (!_container.TryGetContainer(uid, GasWeaponComponent.TankSlotId, out var container) ||
            container is not ContainerSlot slot || slot.ContainedEntity is not { } contained)
            return null;

        return TryComp<GasTankComponent>(contained, out var gasTank) ? (contained, gasTank) : null;
    }


}
