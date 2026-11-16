using Content.Shared.Inventory;

namespace Content.Shared.SS220.StaminaConvertArmor;

/// <summary>
/// Relay-safe version of a status effect addition attempt.
/// Can be intercepted and canceled by components through inventory relay.
/// </summary>
/// <param name="Key">The unique key of the status effect being applied.</param>
/// <param name="Cancelled">Whether this status effect attempt has been blocked.</param>
[ByRefEvent]
public record struct BeforeStatusEffectAddedRelayEvent(string Key, bool Cancelled = false) : IInventoryRelayEvent
{
    SlotFlags IInventoryRelayEvent.TargetSlots =>  ~SlotFlags.POCKET;
}
