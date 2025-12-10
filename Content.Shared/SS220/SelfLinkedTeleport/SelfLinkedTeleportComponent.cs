// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.SelfLinkedTeleport;

/// <summary>
/// This component allows you to create a teleport side that will look for the second side when the component is initialized.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SelfLinkedTeleportComponent : Component
{
    /// <summary>
    ///     The entity to which or from which the teleport will be performed
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? LinkedEntity;

    /// <summary>
    ///     Which entities can it linked to
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? WhitelistLinked;
}

[Serializable, NetSerializable]
public enum SelfLinkedVisuals : byte
{
    State
}
