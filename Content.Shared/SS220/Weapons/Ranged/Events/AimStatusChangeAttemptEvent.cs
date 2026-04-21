// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
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
