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

}
