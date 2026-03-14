// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
namespace Content.Shared.FCB.Weapons.Melee.Events;


[ByRefEvent]
public record struct MeleeHitBlockAttemptEvent()
{
    public bool CancelledHit = false;
    public NetEntity? blocker = null;
    public Color? hitMarkColor = Color.Red;
}
