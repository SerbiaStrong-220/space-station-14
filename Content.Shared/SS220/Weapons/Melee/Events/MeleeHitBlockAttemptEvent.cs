namespace Content.Shared.SS220.Weapons.Melee.Events;


[ByRefEvent]
public record struct MeleeHitBlockAttemptEvent()
{
    public bool CancelledHit = false;
    public NetEntity? blocker = null;
    public Color? hitMarkColor = Color.Red;
}
