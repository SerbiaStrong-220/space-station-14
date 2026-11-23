// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Whitelist;

namespace Content.Shared.SS220.Teleport.Triggers;

/// <summary>
///     Requests teleportation when a target collides with a teleporter fixture.
/// </summary>
[RegisterComponent]
public sealed partial class CollisionTeleportTriggerComponent : Component
{
    /// <summary>
    ///     Fixture that triggers teleportation. Any fixture can trigger it when not specified.
    /// </summary>
    [DataField]
    public string? TriggerFixtureId;

    /// <summary>
    ///     Entities matching this whitelist are deleted instead of teleported.
    /// </summary>
    [DataField]
    public EntityWhitelist? DeleteTargetWhitelist;
}
