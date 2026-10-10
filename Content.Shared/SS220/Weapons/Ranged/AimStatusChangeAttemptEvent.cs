// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.

namespace Content.Shared.SS220.Weapons.Ranged.Events;

[ByRefEvent]
public record struct AimStatusChangeAttemptEvent
{
    public EntityUid User;

    public bool Aim;
}
