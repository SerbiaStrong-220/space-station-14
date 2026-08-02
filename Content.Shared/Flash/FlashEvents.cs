using Content.Shared.Inventory;
using Content.Shared.SS220.SiliconComponents;

namespace Content.Shared.Flash;

/// <summary>
/// Called before a flash is used to check if the attempt is cancelled by blindness, items or FlashImmunityComponent.
/// Raised on the target hit by the flash and their inventory items.
/// </summary>
[ByRefEvent]
public record struct FlashAttemptEvent(EntityUid Target, EntityUid? User, EntityUid? Used, bool Cancelled = false) : IInventoryRelayEvent, ISiliconPartRelayEvent //SS220 add synthetic
{
    SlotFlags IInventoryRelayEvent.TargetSlots => SlotFlags.HEAD | SlotFlags.EYES | SlotFlags.MASK;

    PartType ISiliconPartRelayEvent.Parts => PartType.ALL; //SS220 add synthetic
}

/// <summary>
/// Called when a player is successfully flashed, once for each flashed player.
/// Raised on the target hit by the flash, the user of the flash and the flash used.
/// The Melee parameter is used to check for rev conversion.
/// </summary>
[ByRefEvent]
public record struct AfterFlashedEvent(EntityUid Target, EntityUid? User, EntityUid? Used, bool Melee);

/// <summary>
/// Raised once on the flash entity when it was used, regardless of the flashed status being applied or not.
/// </summary>
[ByRefEvent]
public record struct AfterFlashActivatedEvent(EntityUid? Target, EntityUid? User);
