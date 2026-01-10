// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
namespace Content.Shared.SS220.Weapons.Melee.Events;


[ByRefEvent]
public record struct MeleeHitBlockAttemptEvent()
{
    public bool CancelledHit = false;
    public NetEntity? blocker = null;
    public Color? hitMarkColor = Color.Red;
}
