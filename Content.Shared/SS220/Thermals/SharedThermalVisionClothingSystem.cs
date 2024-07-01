// EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Inventory.Events;
using Robust.Shared.Timing;


namespace Content.Shared.SS220.Thermals;

/// <summary>
/// Handles enabling of thermal vision when clothing is equipped and disabling when unequipped.
/// </summary>
public sealed class SharedThermalVisionClothingSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThermalVisionClothingComponent, GotEquippedEvent>(OnCompEquip);
        SubscribeLocalEvent<ThermalVisionClothingComponent, GotUnequippedEvent>(OnCompUnequip);
    }
    private void OnCompEquip(Entity<ThermalVisionClothingComponent> ent, ref GotEquippedEvent args)
    {
        if (!HasComp<ThermalVisionComponent>(args.Equipee) && !_gameTiming.ApplyingState)
            EnsureComp<ThermalVisionComponent>(args.Equipee);
    }

    private void OnCompUnequip(Entity<ThermalVisionClothingComponent> ent, ref GotUnequippedEvent args)
    {
        if (HasComp<ThermalVisionComponent>(args.Equipee))
            RemComp<ThermalVisionComponent>(args.Equipee);
    }

}