namespace Content.Shared.SS220.Flash;

/// <summary>
/// Called when a player is successfully flashed, once for each flashed player.
/// Raised on the target hit by the flash, the user of the flash and the flash used.
/// The Melee parameter is used to check for rev conversion.
/// </summary>
[ByRefEvent]
public record struct BeforeFlashedEvent(EntityUid Target, EntityUid? User, EntityUid? Used, bool Melee)
{
    public float FlashDurationMultiplier = 1f;
    public float StunDurationMultiplier = 1f;
}
