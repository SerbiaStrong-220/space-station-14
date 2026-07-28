// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Weapons.Ranged.Events;

/// <summary>
/// Raised on the client to request it change of aiming status.
/// </summary>
[Serializable, NetSerializable]
public sealed class AimStatusChangeAttemptEvent : EntityEventArgs
{
    public NetEntity Gun;

    public NetEntity User;

    public bool Aim;
}
