// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.SelfLinkedTeleport;

/// <summary>
/// If you want to create a kind of linked tunnel that will find its own exit when initialized or the linked exit is lost
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
