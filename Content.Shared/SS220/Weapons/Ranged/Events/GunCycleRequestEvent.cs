// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Timing;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Weapons.Ranged.Events;

/// <summary>
///     Raised when attemting to cycle the entity in your hands.
/// </summary>
[Serializable, NetSerializable]
public sealed class GunCycleRequestEvent : HandledEntityEventArgs
{
    /// <summary>
    ///     The gun
    /// </summary>
    public NetEntity Gun;

    /// <summary>
    ///     Entity holding the gun in their hand.
    /// </summary>
    public NetEntity User;

    /// <summary>
    ///     Whether or not to apply a UseDelay when used.
    ///     Mostly used by the <see cref="ClothingSystem"/> quick-equip to not apply the delay to entities that have the <see cref="UseDelayComponent"/>.
    /// </summary>
    public bool ApplyDelay = true;

    public GunCycleRequestEvent(NetEntity user, NetEntity gun)
    {
        User = user;
        Gun = gun;
    }
}
