// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.FixedPoint;
using Content.Shared.Whitelist;

namespace Content.Shared.SS220.Teleport.Triggers;

/// <summary>
///     Requests teleportation through a verb or drag-and-drop interaction.
/// </summary>
[RegisterComponent]
public sealed partial class InteractionTeleportTriggerComponent : Component
{
    /// <summary>
    ///     Entities allowed to be teleported.
    /// </summary>
    [DataField]
    public EntityWhitelist? TargetWhitelist;

    /// <summary>
    ///     Entities prevented from being teleported.
    /// </summary>
    [DataField]
    public EntityWhitelist? TargetBlacklist;

    /// <summary>
    ///     Message shown when the target does not pass the whitelist.
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
