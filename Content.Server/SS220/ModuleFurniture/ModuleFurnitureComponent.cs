// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.ModuleFurniture.Components;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class ModuleFurnitureComponent : SharedModuleFurnitureComponent
{
    /// <summary>
    /// Contains things of which furniture consists.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public Container DrawerContainer;

    /// <summary>
    /// Starting pixel offset for placing drawers sprite
    /// </summary>
    [DataField(required: true)]
    [ViewVariables(VVAccess.ReadOnly)]
    public Vector2i StartingDrawerPixelOffset;

    [DataField(required: true)]
    public Vector2i DrawerPixelInterval;
}
