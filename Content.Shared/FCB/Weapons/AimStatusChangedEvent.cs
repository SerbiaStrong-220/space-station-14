using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Ranged.Events;

/// <summary>
/// Raised on the client to request it change of aiming status.
/// </summary>
[Serializable, NetSerializable]
public sealed class AimStatusChangedEvent : EntityEventArgs
{
    public NetEntity Gun;

    public NetEntity User;

    public bool Aim;
}
