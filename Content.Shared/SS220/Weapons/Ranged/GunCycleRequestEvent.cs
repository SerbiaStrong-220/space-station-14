// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Timing;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Weapons.Ranged;

/// <summary>
///     Raised when attemting to cycle the entity in your hands.
/// </summary>
[NetSerializable, Serializable]
public sealed class GunCycleRequestEvent : EntityEventArgs
{
    /// <summary>
    ///     The gun
    /// </summary>
    public NetEntity Gun;

    /// <summary>
    ///     Entity holding the gun in their hand.
    /// </summary>
    public NetEntity User;

    public bool Handled = false;

    public GunCycleRequestEvent(NetEntity user, NetEntity gun)
    {
        User = user;
        Gun = gun;
    }
}
