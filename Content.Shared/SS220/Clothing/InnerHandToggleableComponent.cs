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
    public Dictionary<string, ContainerSlot> HandsContainers;
}

[DataDefinition]
public sealed partial class Hand
{
    /// <summary>
    ///     Action used to toggle the clothing on or off.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId Action = "ActionToggleSuitPiece";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    /// <summary>
    ///     The inventory slot that the clothing is equipped to.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField, AutoNetworkedField]
    public string Slot = "head";

    /// <summary>
    ///     The container that the clothing is stored in when not equipped.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string ContainerId = DefaultClothingContainerId;

    [ViewVariables]
    public ContainerSlot? Container;

    /// <summary>
    ///     The Id of the piece of clothing that belongs to this component. Required for map-saving if the clothing is
    ///     currently not inside of the container.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? ClothingUid;
}