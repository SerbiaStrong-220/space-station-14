// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
namespace Content.Shared.SS220.Weapons.Melee.Events;


[ByRefEvent]
public record struct MeleeHitBlockAttemptEvent(EntityUid Attacker)
{
    public bool CancelledHit = false;

    public EntityUid Blocker;

    public Color? HitMarkColor = Color.Red;
}
