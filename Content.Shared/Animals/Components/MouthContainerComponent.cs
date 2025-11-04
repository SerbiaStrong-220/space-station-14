using Content.Shared.Whitelist;
using Robust.Shared.Containers;

namespace Content.Shared.Animals.Components;

[RegisterComponent]
public sealed partial class MouthContainerComponent : Component
{
    [ViewVariables]
    public ContainerSlot MouthSlot = default!;

    [ViewVariables]
    public readonly string MouthSlotId = "mouth-slot";

    [DataField]
    public EntityWhitelist? EquipmentWhitelist;

    [DataField]
    public LocId InsertVerb = "insert-to-mouth";
    [DataField]
    public LocId EjectVerb = "eject-from-mouth";
    [DataField]
    public LocId InsertMessage = "insert-to-mouth-success";
    [DataField]
    public LocId EjectMessage = "eject-from-mouth-success";

}
