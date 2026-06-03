namespace Content.Shared.SS220.Flash;

/// <summary>
/// Called before a player is flashed, once for each flashed player.
/// Raised only on the target hit by the flash.
/// The Melee parameter is used to check for rev conversion.
/// </summary>
[ByRefEvent]
public record struct BeforeFlashedEvent(EntityUid Target, EntityUid? User, EntityUid? Used, bool Melee)
{
    public float FlashDurationMultiplier = 1f;
    public float StunDurationMultiplier = 1f;
}
