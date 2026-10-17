using Content.Shared.Containers.ItemSlots;

namespace Content.Server.SS220.DarkForces.Ratvar.Righteous.Gear;

[RegisterComponent]
public sealed partial class RatvarGearTargetComponent : Component
{
    [DataField]
    public string GearSlotId = "RatvarGear";

    [DataField]
    public ItemSlot GearSlot = new();
}
