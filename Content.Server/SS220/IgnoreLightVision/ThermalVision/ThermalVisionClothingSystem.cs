// EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Inventory.Events;
using Content.Shared.SS220.IgnoreLightVision;


namespace Content.Server.SS220.Thermals;

/// <summary>
/// Handles enabling of thermal vision when clothing is equipped and disabling when unequipped.
/// </summary>
public sealed class ThermalVisionClothingSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThermalVisionClothingComponent, GotEquippedEvent>(OnCompEquip);
        SubscribeLocalEvent<ThermalVisionClothingComponent, GotUnequippedEvent>(OnCompUnequip);
    }
    private void OnCompEquip(Entity<ThermalVisionClothingComponent> ent, ref GotEquippedEvent args)
    {
        if (args.Slot != "eyes")
            return;

        if (!TryComp<ThermalVisionComponent>(args.Equipee, out var thermalVisionComp))
        {
            AddComp(args.Equipee, new ThermalVisionComponent(ent.Comp.VisionRadius, ent.Comp.CloseVisionRadius) { State = IgnoreLightVisionOverlayState.Half } );
            return;
        }
    }

    private void OnCompUnequip(Entity<ThermalVisionClothingComponent> ent, ref GotUnequippedEvent args)
    {
        if (HasComp<ThermalVisionComponent>(args.Equipee))
            RemComp<ThermalVisionComponent>(args.Equipee);
    }

}
