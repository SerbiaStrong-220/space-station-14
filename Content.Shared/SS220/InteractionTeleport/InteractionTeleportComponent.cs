// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Whitelist;

namespace Content.Shared.SS220.InteractionTeleport;

/// <summary>
///     Used when you need to teleport not through contact, but through DragDrop or Verb
/// </summary>
[RegisterComponent]
public sealed partial class InteractionTeleportComponent : Component
{
    /// <summary>
    ///     Which entities can use teleportation
    /// </summary>
    [DataField]
    public EntityWhitelist? UserWhitelist;

    /// <summary>
    ///     Message when whitelisting is rejected
    /// </summary>
    [DataField]
    public LocId? WhitelistRejectedLoc;

    /// <summary>
    ///     How long we are entering teleport
    /// </summary>
    [DataField]
    public TimeSpan? TeleportDoAfterTime;
}

