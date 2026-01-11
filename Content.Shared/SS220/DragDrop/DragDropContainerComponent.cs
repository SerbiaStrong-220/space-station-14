using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.DragDrop;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class DragDropContainerComponent : Component
{
    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;
}
