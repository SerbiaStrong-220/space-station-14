// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.InnerHandToggleable;

/// <summary>
///     Allows user to hide whitelisted items inside specific inner hand slot.
///     Actions appear depending on whether the item is equipped in a slot inside the hand or if the item is suitable for being placed in the hand.
/// </summary>

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class InnerHandToggleableComponent : Component
{
    /// <summary>
    ///     Dictionary of hands and cointainers for better accesability
    /// </summary>
    [ViewVariables]
    public Dictionary<string, InnerContainerInfo> HandsContainers = [];

    /// <summary>
    ///     Action used to toggle the item in or out.
    /// </summary>
    [ViewVariables, DataField(required: true)]
    public EntProtoId Action = "ActionToggleHand";

    public EntityUid? ActionEntity;

    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;
}

public sealed partial class InnerContainerInfo
{
    /// <summary>
    ///     The container that the clothing is stored in when not equipped.
    /// </summary>
    [ViewVariables]
    public string ContainerId = "inner-toggleable";

    [ViewVariables]
    public ContainerSlot? Container;

    [ViewVariables]
    public EntityUid? InnerItemUid;
}

public sealed partial class ToggleInnerHandEvent : InstantActionEvent
{
}
