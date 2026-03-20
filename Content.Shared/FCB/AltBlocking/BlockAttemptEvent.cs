// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Robust.Shared.Serialization;

namespace Content.Shared.FCB.Weapons.Ranged.Events;

/// <summary>
/// Raised on the client to request active blocking
/// </summary>
[Serializable, NetSerializable]
public sealed class BlockAttemptEvent : EntityEventArgs
{
    public NetEntity User;

    public bool Handled;
}
