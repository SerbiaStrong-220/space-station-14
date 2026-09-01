// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Weapons.Ranged.Events;

/// <summary>
/// Raised on the client to request active blocking
/// </summary>
[Serializable, NetSerializable]
public sealed class BlockAttemptEvent : EntityEventArgs
{
    public NetEntity User;

    public bool Handled;
}
