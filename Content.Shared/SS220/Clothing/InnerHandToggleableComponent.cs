using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Inventory;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Clothing.Components;

namespace Content.Shared.SS220.Clothing;

[RegisterComponent, NetworkedComponent/*, AutoGenerateComponentState*/]
public sealed partial class InnerHandToggleableComponent : Component
{
    [ViewVariables]
    public Dictionary<string, ToggleableHandInfoPrototype> HandsContainers = [];
}


[Prototype, AutoGenerateComponentState]
public sealed partial class ToggleableHandInfoPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     Action used to toggle the clothing on or off.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId Action = "ActionToggleSuitPiece";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;


    /// <summary>
    ///     The container that the clothing is stored in when not equipped.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public string ContainerId = "toggleable-clothing";

    [ViewVariables]
    public ContainerSlot? Container;

    /// <summary>
    ///     The Id of the piece of clothing that belongs to this component. Required for map-saving if the clothing is
    ///     currently not inside of the container.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? InnerItemUid;
}
