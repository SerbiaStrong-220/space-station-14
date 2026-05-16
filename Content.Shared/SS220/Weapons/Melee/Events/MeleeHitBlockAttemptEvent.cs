// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
namespace Content.Shared.SS220.Weapons.Melee.Events;


[ByRefEvent]
public record struct MeleeHitBlockAttemptEvent(EntityUid Attacker, bool CancelledHit = false)
{
    public EntityUid Blocker;

    public Color? HitMarkColor = Color.Red;
}
