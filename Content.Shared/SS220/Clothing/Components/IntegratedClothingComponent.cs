// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Content.Shared.SS220.Clothing.Systems;
using Content.Shared.Inventory;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Clothing.Components;

[Access(typeof(IntegratedClothingSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class IntegratedClothingComponent : Component
{
    public const string DefaultClothingContainerId = "integrated-clothing";

    [DataField(required: true), AutoNetworkedField]
    public EntProtoId ClothingPrototype = default!;

    [DataField, AutoNetworkedField]
    public string Slot = "head";

    [DataField("requiredSlot"), AutoNetworkedField]
    public SlotFlags RequiredFlags = SlotFlags.OUTERCLOTHING;

    [DataField, AutoNetworkedField]
    public string ContainerId = DefaultClothingContainerId;

    [ViewVariables]
    public ContainerSlot? Container;

    [AutoNetworkedField]
    public EntityUid? ClothingUid;
}
