// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.FixedPoint;
using Content.Shared.Whitelist;

namespace Content.Shared.SS220.Teleport.Triggers;

/// <summary>
///     Requests teleportation when the user activates an alternative verb while holding the teleporter in their active hand.
/// </summary>
[RegisterComponent]
public sealed partial class AltVerbTeleportTriggerComponent : Component
{
    /// <summary>
    ///     Text displayed for the alternative-use verb.
    /// </summary>
    [DataField]
    public LocId VerbText = "teleport-enter-verb";

    /// <summary>
    ///     Entities allowed to use the teleporter.
    /// </summary>
    [DataField]
    public EntityWhitelist? UserWhitelist;

    /// <summary>
    ///     Entities prevented from using the teleporter.
    /// </summary>
    [DataField]
    public EntityWhitelist? UserBlacklist;

    /// <summary>
    ///     Message shown when the user does not pass the whitelist.
    /// </summary>
    [DataField]
    public LocId? WhitelistRejectedLoc;

    /// <summary>
    ///     Time required to enter the teleporter.
    ///     Null when teleportation should be immediate.
    /// </summary>
    [DataField]
    public TimeSpan? TeleportDoAfterTime;

    /// <summary>
    ///     Damage required to interrupt the teleport DoAfter.
    /// </summary>
    [DataField]
    public FixedPoint2? DamageThreshold;
}
