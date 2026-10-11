using Content.Shared.Inventory;

namespace Content.Shared.SS220.Felinid;

/// <summary>
/// Raised on a shooter thrown by ground recoil, and relayed to their clothing, to scale the knockdown chance.
/// </summary>
[ByRefEvent]
public record struct GetGroundRecoilKnockdownModifierEvent() : IInventoryRelayEvent
{
    public float Multiplier = 1f;

    SlotFlags IInventoryRelayEvent.TargetSlots => ~SlotFlags.POCKET;
}
