namespace Content.Shared.Climbing.Events;

// SS220-felinid-climb
[ByRefEvent]
public record struct ModifyClimbDelayEvent(EntityUid User, EntityUid EntityToMove, EntityUid Climbable, float Delay)
{
    public void ModifyDelay(float multiplier)
    {
        Delay *= multiplier;
    }
}
