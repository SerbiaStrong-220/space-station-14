using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Inventory;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Clothing.Components;

namespace Content.Shared.SS220.Clothing;

[RegisterComponent, NetworkedComponent/*, AutoGenerateComponentState*/]
public sealed partial class InnerToggleableComponent : Component
{
    [ViewVariables]
    public Dictionary<string, ContainerSlot> HandsContainers;
}
