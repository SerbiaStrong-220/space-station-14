using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.DarkForces.Ratvar.Righteous.Roles;

[RegisterComponent, NetworkedComponent]
public sealed partial class RatvarMarauderShellComponent : Component
{
    [DataField("soulVesselSlot", required: true)]
    public ItemSlot SoulVesselSlot = new();

    public readonly string SoulVesselSlotId = "SoulVessel";
}
