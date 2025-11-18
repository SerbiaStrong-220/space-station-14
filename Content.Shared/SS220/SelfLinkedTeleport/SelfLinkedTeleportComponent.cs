// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.SelfLinkedTeleport;

/// <summary>
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SelfLinkedTeleportComponent : Component
{
    /// <summary>
    ///     Аnother part of the teleport
    /// </summary>
    [ViewVariables]
    public EntityUid? LinkedEntity;

    /// <summary>
    ///     Which entities can it linked to
    /// </summary>
    [DataField]
    public EntityWhitelist? WhitelistLinked;
}

