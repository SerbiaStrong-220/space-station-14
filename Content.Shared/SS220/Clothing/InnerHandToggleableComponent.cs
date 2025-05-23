// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Clothing;

[RegisterComponent, NetworkedComponent]
public sealed partial class InnerHandToggleableComponent : Component
{
    [ViewVariables]
    public Dictionary<string, ToggleableHandInfo> HandsContainers = [];
}

public sealed partial class ToggleableHandInfo
{
    /// <summary>
    ///     Action used to toggle the clothing on or off.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId Action = "ActionToggleInnerHand";// based on ActionToggleSuitPiece

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    /// <summary>
    ///     The container that the clothing is stored in when not equipped.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public string ContainerId = "inner-toggleable";

    [ViewVariables]
    public ContainerSlot? Container;

    [DataField, AutoNetworkedField]
    public EntityUid? InnerItemUid;
}


[Prototype]
public sealed partial class ToggleableInnerHandPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     Action used to toggle the clothing on or off.
    /// </summary
    [DataField(required: true)]
    public string Action = "ActionToggleSuitPiece";
}
