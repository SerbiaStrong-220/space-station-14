// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Timing;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Weapons.Ranged;

/// <summary>
///     Raised when attemting to cycle the entity in your hands.
/// </summary>
[ByRefEvent]
public record struct GunCycleRequestEvent
{
    /// <summary>
    ///     The gun
    /// </summary>
    public EntityUid Gun;

    /// <summary>
    ///     Entity holding the gun in their hand.
    /// </summary>
    public EntityUid User;

    public bool Handled = false;

    public GunCycleRequestEvent(EntityUid user, EntityUid gun)
    {
        User = user;
        Gun = gun;
    }
}
