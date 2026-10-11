// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.AutoLinkingTeleport;

/// <summary>
///     Automatically links this teleporter with an available matching teleporter.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AutoLinkingTeleportComponent : Component
{
    /// <summary>
    ///     The linked destination teleporter.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? LinkedTeleporter;

    /// <summary>
    ///     Filter used when searching for an unlinked teleport candidate.
    ///     Only the searching teleporter's whitelist is checked.
    /// </summary>
    [DataField]
    public EntityWhitelist? LinkWhitelist;

    /// <summary>
    ///     Whether this teleporter can link to a teleporter located on another map.
    /// </summary>
    [DataField]
    public bool CanLinkToOtherMaps = true;
}

[Serializable, NetSerializable]
public enum AutoLinkingTeleportVisuals : byte
{
    Linked
}
