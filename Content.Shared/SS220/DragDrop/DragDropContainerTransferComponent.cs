using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.DragDrop;

/// <summary>
/// This component marks the item that can transfer items from its storage when to be drag n dropped
/// </summary>
[RegisterComponent]
[NetworkedComponent]
public sealed partial class DragDropContainerTransferComponent : Component
{
    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;
}
