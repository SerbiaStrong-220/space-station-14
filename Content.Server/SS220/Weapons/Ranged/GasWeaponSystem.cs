// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.SS220.Weapons.Components;
using Content.Shared.SS220.Weapons.Ranged.Events;
using Content.Shared.SS220.Weapons.Ranged.Systems;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Containers;

namespace Content.Server.SS220.Weapons.Ranged.Systems;

public sealed class GasWeaponSystem : SharedGasWeaponSystem
{
    [Dependency] private readonly GasTankSystem _gasTank = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GasWeaponComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<GasWeaponComponent, EntInsertedIntoContainerMessage>(OnItemInserted);
        SubscribeLocalEvent<GasWeaponComponent, EntRemovedFromContainerMessage>(OnItemRemoved);
    }

    protected override void OnShootAttempt(Entity<GasWeaponComponent> ent, ref ShotAttemptedEvent args)
    {
        // require a gas tank if it uses gas
        var gas = GetGas(ent);

        if (gas == null || gas.Value.Comp.Air.TotalMoles < ent.Comp.GasUsage)
        {
            args.Cancel();
            return;
        }

        float amountOfWrongGasMoles = 0f;

        foreach (Gas type in Enum.GetValues(typeof(Gas)))
        {
            if (type != ent.Comp.GasType && gas.Value.Comp.Air.GetMoles(type) > 0f)
                amountOfWrongGasMoles += gas.Value.Comp.Air.GetMoles(type);
        }

        if (amountOfWrongGasMoles > 0f)
        {
            args.Cancel();

            var ev = new GasWeaponWrongGasEvent(ent, amountOfWrongGasMoles);
            RaiseLocalEvent(ent.Owner, ref ev);

            ent.Comp.CanShoot = false;
            Dirty(ent);
        }

        _gasTank.RemoveAir(gas.Value, ent.Comp.GasUsage);

        if (gas.Value.Comp.Air.TotalMoles < ent.Comp.GasUsage) //I didn't call CheckAbilityToShoot because i don't want to check the amount of whong gases twice
        {
            ent.Comp.CanShoot = false;
            Dirty(ent);
        }
    }

    private void OnComponentStartup(Entity<GasWeaponComponent> ent, ref ComponentStartup args)
    {
        bool canShoot = CheckAbilityToShoot(ent);

        if (canShoot != ent.Comp.CanShoot)
        {
            ent.Comp.CanShoot = canShoot;
            Dirty(ent);
        }
    }

    private bool CheckAbilityToShoot(Entity<GasWeaponComponent> ent)
    {
        var gas = GetGas(ent);

        if (gas == null || gas.Value.Comp.Air.TotalMoles < ent.Comp.GasUsage)
            return false;


        foreach (Gas type in Enum.GetValues(typeof(Gas)))
        {
            float amountOfWrongGasMoles = 0f;

            if (type != ent.Comp.GasType && gas.Value.Comp.Air.GetMoles(type) > 0f)
                amountOfWrongGasMoles += gas.Value.Comp.Air.GetMoles(type);

            if (amountOfWrongGasMoles > 0f)
                return false;
        }

        return true;
    }

    private void OnItemInserted(Entity<GasWeaponComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        bool canShoot = CheckAbilityToShoot(ent);

        if (canShoot != ent.Comp.CanShoot)
        {
            ent.Comp.CanShoot = canShoot;
            Dirty(ent);
        }
    }

    private void OnItemRemoved(Entity<GasWeaponComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        bool canShoot = CheckAbilityToShoot(ent);

        if (canShoot != ent.Comp.CanShoot)
        {
            ent.Comp.CanShoot = canShoot;
            Dirty(ent);
        }
    }

    private Entity<GasTankComponent>? GetGas(Entity<GasWeaponComponent> ent)
    {
        if (!_container.TryGetContainer(ent.Owner, ent.Comp.TankSlotId, out var container) ||
            container is not ContainerSlot slot || slot.ContainedEntity is not { } contained)
            return null;

        return TryComp<GasTankComponent>(contained, out var gasTank) ? (contained, gasTank) : null;
    }


}
